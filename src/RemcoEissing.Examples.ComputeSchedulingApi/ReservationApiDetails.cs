using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace RemcoEissing.Examples.ComputeSchedulingApi
{
    /// <summary>
    /// Represents the details of a reservation in Azure for Virtual Machine compute.
    /// </summary>
    public class ReservationApiDetails
    {
        /// <summary>
        /// Gets or sets the ID of the reservation.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the location of the reservation.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the quantity of the reservation.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the number of cores per machine in the reservation.
        /// </summary>
        public int CoresPerMachine { get; set; }

        /// <summary>
        /// Gets or sets the SKU of the virtual machine in the reservation.
        /// </summary>
        public VmSku Sku { get; set; }

        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        /// <summary>
        /// Callback method called after deserialization.
        /// </summary>
        /// <param name="context">The streaming context.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            JToken properties = _additionalData["properties"];

            if (properties != null)
            {
                if (properties["quantity"] != null)
                {
                    int quantity = properties["quantity"].ToObject<int>();
                    Quantity = quantity;
                }
            }
        }
    }
}
