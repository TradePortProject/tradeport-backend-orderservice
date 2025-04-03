using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq.Protected;
using Moq;
using OrderManagement.Models.DTO;
using Xunit;

namespace OrderManagement.ExternalServices
{
    public class ProductServiceClient : IProductServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _productServiceBaseUrl;

        public ProductServiceClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _productServiceBaseUrl = configuration["ProductService:BaseUrl"];
        }

        public async Task<ProductDTO> GetProductByIdAsync(Guid productId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_productServiceBaseUrl}/api/ProductManagement/{productId}");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch product. Status Code: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Raw JSON Response from Product API: {content}"); // ✅ Debug JSON response

                var productResponse = JsonSerializer.Deserialize<ProductApiResponse>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (productResponse?.Products != null && productResponse.Products.Count > 0)
                {
                    var product = productResponse.Products.First(); // ✅ Extract first product from list
                    Console.WriteLine("Product Fetched Successfully:");
                    Console.WriteLine(JsonSerializer.Serialize(product, new JsonSerializerOptions { WriteIndented = true }));
                    return product;
                }
                else
                {
                    Console.WriteLine("No product found in response.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching product: {ex.Message}");
                return null;
            }
        }
        public async Task<bool> UpdateProductQuantityAsync(Guid productId, int quantity)
        {
            try
            {
                var updatePayload = new
                {
                    quantity = quantity
                };

                var jsonContent = JsonSerializer.Serialize(updatePayload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                Console.WriteLine($"Sending JSON to Product Service: {jsonContent}"); // ✅ Debugging

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{_productServiceBaseUrl}/api/ProductManagement/{productId}/UpdateProductQuantity", content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to update product quantity. Status Code: {response.StatusCode}");
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error Response: {errorResponse}");
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product quantity: {ex.Message}");
                return false;
            }
        }


    }
}
