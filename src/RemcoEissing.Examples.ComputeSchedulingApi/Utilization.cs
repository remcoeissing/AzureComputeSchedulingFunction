namespace RemcoEissing.Examples.ComputeSchedulingApi;

/// <summary>
/// Represents the utilization of Azure VM cores.
/// </summary>
public class Utilization
{
    /// <summary>
    /// Gets or sets the quota for VM core usage.
    /// </summary>
    public UsageLimit Quota { get; set; }

    /// <summary>
    /// Gets or sets the reservation for VM core usage.
    /// </summary>
    public UsageLimit Reservation { get; set; }
}
