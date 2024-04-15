using Azure.Core;
using Azure.Identity;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace RemcoEissing.Examples.ComputeSchedulingApi;

/// <summary>
/// Repository for fetching VM / VMSS core usage from Azure. Including quota and reservation limits.
/// </summary>
public class AzureUsageRepository : IAzureUsageRepository
{
    private HttpClient httpClient;
    private JObject _cachedSkuInfo;

    /// <summary>
    /// Constructs an instance of the repository that fetches Azure Compute Usage from the different APIs.
    /// </summary>
    public AzureUsageRepository()
    {
        var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = "16b3c013-d300-468d-ac64-7eda0820b6d3" });

        httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://management.azure.com/subscriptions/")
        };

        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials.GetToken(new TokenRequestContext(new[] { "https://management.azure.com/.default" })).Token);
    }

    private JObject CachedSkuInfo { get; set; }

    /// <summary>
    /// Get the Azure VM core quota on the specified subscription and location for the specified VM Type.
    /// </summary>
    /// <param name="vmType">The type of the VM.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="location">The location of the VM.</param>
    /// <returns>The <see cref="UsageLimit"/> object representing the core quota.</returns>
    public async Task<UsageLimit> GetCoreQuota(string vmType, string subscriptionId, string location)
    {
        var responseMessage = await httpClient.GetAsync($"{subscriptionId}/providers/Microsoft.Capacity/resourceProviders/Microsoft.Compute/locations/{location}/serviceLimits/{vmType}?api-version=2020-10-25");
        var propertyObject = await GetObjectFromResponse(responseMessage, "properties");

        return propertyObject.ToObject<UsageLimit>();
    }

    /// <summary>
    /// Queries the current usage of a specific VM type in a given location and subscription.
    /// </summary>
    /// <param name="vmType">The type of the VM.</param>
    /// <param name="location">The location of the VM.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>The total number of cores used by the VMs of the specified type in the given location and subscription.</returns>
    public async Task<int> QueryCurrentUsage(string vmType, string location, string subscriptionId)
    {
        StringContent body = BuildResourceGraphQueryBody(vmType, location);
        var responseMessage = await httpClient.PostAsync("https://management.azure.com/providers/Microsoft.ResourceGraph/resources?api-version=2022-10-01", body);

        var dataObject = await GetObjectFromResponse(responseMessage, "data");
        var results = dataObject.ToObject<ResourceGraphUtilizationQueryResult[]>();

        if (results != null)
        {
            foreach (var result in results)
            {
                result.Family = await GetFamilyTypeBySku(result.Size, location, subscriptionId);
                result.CoresPerMachine = await GetCoresBySkuAndLocation(result.Size, location, subscriptionId);
            }
        }

        return results
            .Where(vm => vm.Family.Equals(vmType))
            .Sum(vm => vm.TotalCores);
    }

    /// <summary>
    /// Fetches the reservations and calculates the core for the VM family type.
    /// </summary>
    /// <param name="vmType">The type of the VM.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="location">The location of the VM.</param>
    /// <returns>The total number of reserved cores for the specified VM family type.</returns>
    public async Task<int> GetReservedCores(string vmType, string subscriptionId, string location)
    {
        string reserverationUri = $"https://management.azure.com/providers/Microsoft.Capacity/reservations?api-version=2022-11-01";
        var responseMessage = await httpClient.GetAsync(reserverationUri);
        if (!responseMessage.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get reservations. Status code: {responseMessage.StatusCode}");
        }

        var jsonReservationResponse = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
        var reservations = jsonReservationResponse["value"].ToObject<ReservationApiDetails[]>();

        var reservationsForLocation = reservations.Where(r => r.Location.Equals(location, StringComparison.InvariantCultureIgnoreCase)).ToList();

        foreach (var reservation in reservationsForLocation)
        {
            reservation.Sku.Family = await GetFamilyTypeBySku(reservation.Sku.Name, location, subscriptionId);
            reservation.CoresPerMachine = await GetCoresBySkuAndLocation(reservation.Sku.Name, location, subscriptionId);
        }

        var applicableReservations = reservationsForLocation.Where(r => r.Sku.Family.Equals(vmType, StringComparison.InvariantCultureIgnoreCase)).ToList();
        int totalReservedCores = applicableReservations.Sum(r => r.Quantity * r.CoresPerMachine);

        return totalReservedCores;
    }

    /// <summary>
    /// Gets the number of cores for a specific VM SKU in the given location and subscription.
    /// </summary>
    /// <param name="sku">The SKU name.</param>
    /// <param name="location">The location of the VM.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>The number of cores for the specified VM SKU.</returns>
    public async Task<int> GetCoresBySkuAndLocation(string sku, string location, string subscriptionId)
    {
        if (CachedSkuInfo == null)
        {
            await InitializeSkuInfoCache(location, subscriptionId);
        }

        var skuObject = GetSkuObjectFromCache(sku);
        var capabilityObject = skuObject.SelectToken("..capabilities[?(@.name == 'vCPUs')]['value']");
        int cores = 0;
        if (capabilityObject != null)
        {
            cores = capabilityObject.Value<int>();
        }

        return cores;
    }

    /// <summary>
    /// Gets the VM family type for a specific SKU.
    /// </summary>
    /// <param name="sku">The SKU name.</param>
    /// <param name="location">The location of the SKU.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <returns>The VM family type.</returns>
    public async Task<string> GetFamilyTypeBySku(string sku, string location, string subscriptionId)
    {
        if (CachedSkuInfo == null)
        {
            var responseMessage = await httpClient.GetAsync($"{subscriptionId}/providers/Microsoft.Compute/skus?api-version=2021-07-01&$filter=location eq '{location}'");
            var jsonResponse = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
            CachedSkuInfo = jsonResponse;
        }

        var skuObject = GetSkuObjectFromCache(sku);
        return skuObject["family"].Value<string>();
    }


    /// <summary>
    /// Retrieves the VM core reservation and current usage.
    /// </summary>
    /// <param name="vmType">The type of the VM.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="location">The location of the VM.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation that returns the <see cref="UsageLimit"/> object containing the reservation limit and current usage.</returns>
    public async Task<UsageLimit> GetReservationUsage(string vmType, string subscriptionId, string location)
    {
        Task<int> limitTask = GetReservedCores(vmType, subscriptionId, location);
        Task<int> usageTask = QueryCurrentUsage(vmType, location, subscriptionId);

        await Task.WhenAll(limitTask, usageTask);

        return new UsageLimit
        {
            Limit = limitTask.Result,
            Usage = usageTask.Result
        };
    }

    /// <summary>
    /// Builds the resource graph query body for querying the current usage of a specific VM type in a given location.
    /// </summary>
    /// <param name="vmType">The type of the VM.</param>
    /// <param name="location">The location of the VM.</param>
    /// <returns>The <see cref="StringContent"/> object representing the resource graph query body.</returns>
    private static StringContent BuildResourceGraphQueryBody(string vmType, string location)
    {
        var resourceGraphQuery = @$"( resources | where type =~ 'microsoft.compute/virtualMachineScalesets' 
          | where properties.virtualMachineProfile.priority != 'Spot' 
          | project Size = tostring(sku.name), Capacity = toint(sku.capacity), Location = location
          | union 
          resources
          | where type =~ 'microsoft.compute/virtualmachines'
            | where properties.extended.instanceView.powerState.code != 'PowerState/deallocated'
            | where properties.priority != 'Spot'
          | project Size = tostring(properties.hardwareProfile.vmSize), Capacity = 1, Location = location
        )
        | where Location =~ '{location}'// and Size == '{vmType}'
        | summarize Capacity = sum(Capacity) by Size, Location
        | order by Capacity desc";

        var jsonBody = "{\"query\":\"" + resourceGraphQuery + "\"}";

        return new StringContent(jsonBody, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Retrieves the specified property from the JSON response.
    /// </summary>
    /// <param name="responseMessage">The JSON response.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The value of the specified property.</returns>
    private async Task<JToken> GetObjectFromResponse(HttpResponseMessage responseMessage, string propertyName)
    {
        JObject jsonResponse = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());

        if (jsonResponse == null || !jsonResponse.ContainsKey(propertyName) || jsonResponse[propertyName] == null)
        {
            throw new KeyNotFoundException($"Property {propertyName} not found in response");
        }

        return jsonResponse[propertyName];
    }

    /// <summary>
    /// Retrieves the specified property from the JSON response.
    /// </summary>
    /// <param name="response">The JSON response.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The value of the specified property.</returns>
    private JToken GetObjectFromResponse(string response, string propertyName)
    {
        JObject jsonResponse = JObject.Parse(response);

        if (jsonResponse == null || !jsonResponse.ContainsKey(propertyName) || jsonResponse[propertyName] == null)
        {
            throw new KeyNotFoundException($"Property {propertyName} not found in response");
        }

        return jsonResponse[propertyName];
    }

    /// <summary>
    /// Initializes the SKU info cache by making a request to retrieve the SKU information for the specified location and subscription.
    /// </summary>
    /// <param name="location">The location of the SKU.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    private async Task InitializeSkuInfoCache(string location, string subscriptionId)
    {
        var responseMessage = await httpClient.GetAsync($"{subscriptionId}/providers/Microsoft.Compute/skus?api-version=2021-07-01&$filter=location eq '{location}'");
        var jsonResponse = JObject.Parse(await responseMessage.Content.ReadAsStringAsync());
        CachedSkuInfo = jsonResponse;
    }

    /// <summary>
    /// Retrieves the SKU object from the cache based on the specified SKU name.
    /// </summary>
    /// <param name="sku">The name of the SKU.</param>
    /// <returns>The SKU object from the cache.</returns>
    private JToken GetSkuObjectFromCache(string sku)
    {
        return CachedSkuInfo.SelectToken($"..value[?(@.name == '{sku}')]");
    }

}
