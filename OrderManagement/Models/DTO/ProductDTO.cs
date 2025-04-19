using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OrderManagement.Models.DTO
{
    public class ProductDTO
    {
        [JsonPropertyName("productID")]
        public Guid ProductID { get; set; }

        [JsonPropertyName("quantity")]
        public int? Quantity { get; set; }

        [JsonPropertyName("productName")]
        public string? ProductName { get; set; }

        [JsonPropertyName("manufacturerID")]
        public Guid ManufacturerID { get; set; }
        
    }

    public class ProductApiResponse
    {
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("product")]
        public List<ProductDTO> Products { get; set; } // ✅ API returns a list

        [JsonPropertyName("errorMessage")]
        public string ErrorMessage { get; set; }
    }
}
