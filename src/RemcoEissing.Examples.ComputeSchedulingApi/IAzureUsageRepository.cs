namespace RemcoEissing.Examples.ComputeSchedulingApi;

public interface IAzureUsageRepository
{
    /// <summary>
    /// Retrieves the core quota for a specific virtual machine type, subscription, and location.
    /// </summary>
    /// <param name="vmType">The virtual machine type.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="location">The location.</param>
    /// <returns>The core quota for the specified virtual machine type, subscription, and location.</returns>
    Task<UsageLimit> GetCoreQuota(string vmType, string subscriptionId, string location);

    /// <summary>
    /// Retrieves the reservation usage for a specific virtual machine type, subscription, and location.
    /// </summary>
    /// <param name="vmType">The virtual machine type.</param>
    /// <param name="subscriptionId">The subscription ID.</param>
    /// <param name="location">The location.</param>
    /// <returns>The reservation usage for the specified virtual machine type, subscription, and location.</returns>
    Task<UsageLimit> GetReservationUsage(string vmType, string subscriptionId, string location);
}
