using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OrderManagement.Models.DTO;

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
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProductDTO>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching product: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateProductAsync(Guid productId, ProductDTO product)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_productServiceBaseUrl}/api/ProductManagement/{productId}", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                return false;
            }
        }
    }
}
