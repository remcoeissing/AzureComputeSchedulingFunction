namespace RemcoEissing.Examples.ComputeSchedulingApi;


/// <summary>
/// Holds the result of VM and VMSS utilization that are retrieved by an Azure Resource Graph Query
/// </summary>
public class ResourceGraphUtilizationQueryResult
{
    /// <summary>
    /// Gets or sets the Sku size of the VM and / or VMSS.
    /// </summary>
    public required string Size { get; set; }

    /// <summary>
    /// Gets or sets the location of the VM and / or VMSS.
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// Gets or sets the number of VM / VMSS of this particular Size.
    /// </summary>
    public required int Capacity { get; set; }

    /// <summary>
    /// Gets or sets the number of cores per machine.
    /// </summary>
    public required int CoresPerMachine { get; set; }

    /// <summary>
    /// Gets or sets the family of the VM and / or VMSS.
    /// </summary>
    public required string Family { get; set; }

    /// <summary>
    /// Gets the total number of cores based on the capacity and cores per machine.
    /// </summary>
    public int TotalCores
    {
        get
        {
            return Capacity * CoresPerMachine;
        }
    }
}
