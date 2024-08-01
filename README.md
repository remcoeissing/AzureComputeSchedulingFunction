# Azure Compute Scheduling Function

Azure offers different capabilities for optimizing the costs of running VMs and VMSSes. One of these capabilities is the ability to reserve instances for a specific family and location. This allows for a discount on the cost of running these instances. Another one is to utilize Spot / Low Priority instances, which are instances that are available at a lower cost but can be evicted at any time. As a workload you want to keep this in mind when you're running your workloads, and you want to first utilize reserved instances before utilizing Spot / Low Priority instances.

## Infrastructure

The solution is meant to run as an Azure Function App. This Function App will provide a single API that's reachable on `{hostname}/api/{subscription-id}/ComputeUtilization/{location}/{family}`

This Function App will be configured with a System Assigned Managed Identity, this identity should be provided access to the Azure subscriptions for which it should determine the VMs and VMSSes that are running. It should as well have access to the Reserved Instances so that it can collect information from this.

## API Endpoints

The Function App will provide a single API endpoint that will return the VMs and VMSSes that are running in a specific location and family. The API will be reachable on the following URL:

`{hostname}/api/{subscription-id}/ComputeUtilization/{location}/{family}`

In this request, the following parameters are expected:

- `subscription-id`: The subscription ID for which the information should be retrieved.
- `location`: The Azure region in which the VMs and VMSSes should be retrieved, in the form of westeurope or westus. The locations can be retrieved using Azure CLI `az account list-locations --query "[].name"` or PowerShell `Get-AzLocation | Select-Object -ExpandProperty Location`.
- `family`: The family of the VMs and VMSSes that should be retrieved, in the form of standardBSFamily or standardDSFamily. The families can be retrieved using Azure CLI `az vm list-skus --location westeurope --query "[].family"` or PowerShell `Get-AzComputeResourceSku -Location westeurope | Select-Object -ExpandProperty Family -Unique`.

The response will be something similar to the following:

```json
{
    "name": "standardBSFamily",
    "location": "westeurope",
    "subscriptionId": "{subscription-id}",
    "utilization": {
        "quota": {
            "limit": 100,
            "usage": 0
        },
        "reservation": {
            "limit": 6,
            "usage": 4
        }
    }
}
```

In this response, the following information is provided:

- `quota`: The core quota of the subscription for the specified location and family.
  - `limit`: The maximum number of cores that can be running in the specified location and family.
  - `usage`: The current number of cores that are currently running in the specified location and family.
- `reservation`: The number of cores of reserved instances that are currently available in the specified location and family.
  - `limit`: The maximum number of cores that are covered by reserved instances that can be used in the specified location and family.
  - `usage`: The current number of cores that are that are currently running in the specified location and family and can be covered using reserved instances.

## Known issues

- Reserved Instances have an option to be flexible or not. This solution does not take this into account and assumes that all reserved instances are flexible. This could be solved by making an additonal query to `https://learn.microsoft.com/en-us/rest/api/reserved-vm-instances/reservation/get?view=rest-reserved-vm-instances-2022-11-01&tabs=HTTP`.
