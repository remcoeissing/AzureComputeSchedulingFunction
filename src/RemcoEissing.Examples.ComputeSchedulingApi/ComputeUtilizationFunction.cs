using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace RemcoEissing.Examples.ComputeSchedulingApi;


/// <summary>
/// Represents a function for computing utilization of VMs and VMSSes in Azure.
/// </summary>
public class ComputeUtilizationFunction
{
    private readonly ILogger<ComputeUtilizationFunction> _logger;
    private readonly IAzureUsageRepository _azureUsageRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeUtilizationFunction"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="azureUsageRepository">The Azure usage repository.</param>
    public ComputeUtilizationFunction(ILogger<ComputeUtilizationFunction> logger, IAzureUsageRepository azureUsageRepository)
    {
        _logger = logger;
        _azureUsageRepository = azureUsageRepository;
    }

    /// <summary>
    /// Computes the utilization for the given subscription, location, and VM type.
    /// </summary>
    /// <param name="req">The HTTP request.</param>
    /// <param name="subscriptionId">The subscription ID to get the utilization for.</param>
    /// <param name="location">The location to get utilization for.</param>
    /// <param name="vmType">The VM type to get the utilization for.</param>
    /// <returns>The compute utilization response.</returns>
    [Function("ComputeUtilization")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "{subscriptionId}/ComputeUtilization/{location}/{vmType}")] HttpRequest req, [FromRoute] Guid subscriptionId, [FromRoute] string location, [FromRoute] string vmType)
    {
        _logger.LogInformation("ComputeUtilization trigger function processing a request.");

        var responseObject = new ComputeUtilizationResponse
        {
            Name = vmType,
            SubscriptionId = subscriptionId,
            Location = location,
            Utilization = new Utilization
            {
                Quota = _azureUsageRepository.GetCoreQuota(vmType, subscriptionId.ToString(), location).Result,
                Reservation = _azureUsageRepository.GetReservationUsage(vmType, subscriptionId.ToString(), location).Result
            }
        };

        _logger.LogInformation("ComputeUtilization trigger function processed a request.");

        return new OkObjectResult(responseObject);
    }
}
