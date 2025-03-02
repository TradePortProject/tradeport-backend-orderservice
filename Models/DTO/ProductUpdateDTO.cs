using System;
using System.Text.Json.Serialization;

namespace OrderManagement.Models.DTO
{
    public class ProductUpdateDTO
    {
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("updatedOn")]
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }
}

