namespace RemcoEissing.Examples.ComputeSchedulingApi;


/// <summary>
/// Represents a response containing compute utilization information.
/// </summary>
public class ComputeUtilizationResponse
{
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the location.
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// Gets or sets the subscription ID.
    /// </summary>
    public Guid SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets the utilization.
    /// </summary>
    public Utilization Utilization { get; set; }
}
