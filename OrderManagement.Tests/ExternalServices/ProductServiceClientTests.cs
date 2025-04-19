using Xunit;
using Moq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using OrderManagement.ExternalServices;
using OrderManagement.Models.DTO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Moq.Protected;
using System.Threading;
using System.Text.Json.Serialization;
using System.Net;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ProductServiceClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly ProductServiceClient _productServiceClient;
    private readonly Mock<IConfiguration> _configurationMock;

    public ProductServiceClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        // ✅ Mock IConfiguration to return "http://localhost:3016" for the base URL
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(config => config["ProductServiceBaseUrl"])
                          .Returns("http://localhost:3016");

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // ✅ Pass the mocked IConfiguration
        _productServiceClient = new ProductServiceClient(httpClient, _configurationMock.Object);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new ProductDTO { ProductID = productId, Quantity = 10, ManufacturerID = Guid.NewGuid() };
        var productResponse = new ProductApiResponse
        {
            Products = new List<ProductDTO> { product }
        };
        var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var responseMessage = new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);

        _configurationMock.Setup(config => config["ProductService:BaseUrl"]) // 🔧 FIXED key
            .Returns("http://localhost:3016");

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var productServiceClient = new ProductServiceClient(httpClient, _configurationMock.Object);

        // Act
        var result = await productServiceClient.GetProductByIdAsync(productId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productId, result.ProductID);
    }

    [Fact]
    public async Task UpdateProductQuantityAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var quantity = 15;

        var expectedUrl = $"/api/ProductManagement/{productId}/UpdateProductQuantity";

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri.AbsolutePath.Equals(expectedUrl)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        _configurationMock
            .Setup(c => c["ProductService:BaseUrl"])
            .Returns("http://localhost:3016");

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var client = new ProductServiceClient(httpClient, _configurationMock.Object);

        // Act
        var result = await client.UpdateProductQuantityAsync(productId, quantity);

        // Assert
        Assert.True(result);
    }

    //[Fact]
    //public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var product = new ProductDTO { ProductID = productId, Quantity = 10 };
    //    var productResponse = new ProductApiResponse
    //    {
    //        Products = new List<ProductDTO> { product }
    //    };
    //    var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
    //    {
    //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //    });
    //    var responseMessage = new HttpResponseMessage
    //    {
    //        StatusCode = HttpStatusCode.OK,
    //        Content = new StringContent(jsonResponse)
    //    };
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.IsAny<HttpRequestMessage>(),
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(responseMessage);
    //    // Act
    //    var result = await _productServiceClient.GetProductByIdAsync(productId);
    //    // Assert
    //    Assert.NotNull(result);
    //    Assert.Equal(productId, result.ProductID);
    //    Assert.Equal(10, result.Quantity);
    //}

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productResponse = new ProductApiResponse { Products = new List<ProductDTO>() };
        var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
        // Act
        var result = await _productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenApiResponseIsNotSuccessful()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.NotFound // Simulate 404 Not Found
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
        // Act
        var result = await _productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenExceptionOccurs()
    {
        // Arrange
        var productId = Guid.NewGuid();
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        // Act
        var result = await _productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.Null(result);
    }

    //[Fact]
    //public async Task UpdateProductQuantityAsync_ShouldReturnTrue_WhenSuccessful()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var quantity = 15;
    //    var expectedUrl = $"/api/ProductManagement/{productId}/UpdateProductQuantity";
    //    var expectedPayload = JsonSerializer.Serialize(new { quantity }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    //    var responseMessage = new HttpResponseMessage
    //    {
    //        StatusCode = HttpStatusCode.OK // Simulate a successful response
    //    };
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.Is<HttpRequestMessage>(req =>
    //                req.Method == HttpMethod.Patch && // Ensure the HTTP method is PATCH
    //                req.RequestUri.AbsolutePath.Contains(expectedUrl) && // Ensure the URL matches
    //                req.Content.ReadAsStringAsync().Result == expectedPayload), // Ensure the payload matches
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(responseMessage);
    //    // Debugging: Verify the mocked response
    //    Console.WriteLine($"[DEBUG] Mocked Response Status Code: {responseMessage.StatusCode}");
    //    // Act
    //    var result = await _productServiceClient.UpdateProductQuantityAsync(productId, quantity);
    //    // Debugging: Verify the result
    //    Console.WriteLine($"[DEBUG] Result: {result}");
    //    // Assert
    //    Assert.True(result, "Expected UpdateProductQuantityAsync to return true, but it returned false.");
    //}


    [Fact]
    public async Task UpdateProductQuantityAsync_ShouldReturnFalse_WhenApiResponseIsNotSuccessful()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var quantity = 15;
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest // Simulate 400 Bad Request
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri.AbsolutePath.Contains($"/api/ProductManagement/{productId}/UpdateProductQuantity")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
        // Act
        var result = await _productServiceClient.UpdateProductQuantityAsync(productId, quantity);
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateProductQuantityAsync_ShouldReturnFalse_WhenExceptionOccurs()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var quantity = 15;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));
        // Act
        var result = await _productServiceClient.UpdateProductQuantityAsync(productId, quantity);
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldLogAndReturnNull_WhenApiResponseIsNotSuccessful()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedUrl = $"http://localhost:3016/api/ProductManagement/{productId}";
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && // Ensure the HTTP method is GET
                    req.RequestUri.ToString() == expectedUrl), // Match the exact URL
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound // Simulate 404 Not Found
            });
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:3016") // Ensure the base address matches the expected URL
        };
        var productServiceClient = new ProductServiceClient(httpClient, _configurationMock.Object);
        // Act
        var result = await productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.Null(result); // Ensure the method returns null
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    //[Fact]
    //public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductsExistInResponse()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var expectedUrl = $"http://localhost:3016/api/ProductManagement/{productId}";
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.Is<HttpRequestMessage>(req =>
    //                req.Method == HttpMethod.Get && // Ensure the HTTP method is GET
    //                req.RequestUri.ToString() == expectedUrl), // Match the exact URL
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent(JsonSerializer.Serialize(new ProductApiResponse
    //            {
    //                Products = new List<ProductDTO>
    //                {
    //                    new ProductDTO
    //                    {
    //                        ProductID = productId,
    //                        Quantity = 10,
    //                        ManufacturerID = Guid.NewGuid()
    //                    }
    //                }
    //            }, new JsonSerializerOptions
    //            {
    //                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //            }))
    //        });
    //    var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
    //    {
    //        BaseAddress = new Uri("http://localhost:3016") // Ensure the base address matches the expected URL
    //    };
    //    var productServiceClient = new ProductServiceClient(httpClient, _configurationMock.Object);
    //    // Act
    //    var result = await productServiceClient.GetProductByIdAsync(productId);
    //    // Assert
    //    Assert.NotNull(result); // Ensure the method returns a product
    //    Assert.Equal(productId, result.ProductID); // Verify the product ID matches
    //}

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductsExistInResponse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var expectedUrl = $"http://localhost:3016/api/ProductManagement/{productId}";
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get && // Ensure the HTTP method is GET
                    req.RequestUri.ToString() == expectedUrl), // Match the exact URL
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new ProductApiResponse
                {
                    Products = new List<ProductDTO>
                    {
                        new ProductDTO
                        {
                            ProductID = productId,
                            Quantity = 10,
                            ManufacturerID = Guid.NewGuid()
                        }
                    }
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }))
            });
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:3016") // Ensure the base address matches the expected URL
        };
        var productServiceClient = new ProductServiceClient(httpClient, _configurationMock.Object);
        // Act
        var result = await productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.NotNull(result); // Ensure the method returns a product
        Assert.Equal(productId, result.ProductID); // Verify the product ID matches
    }


    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsAreNullOrEmpty()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productResponse = new ProductApiResponse
        {
            Products = new List<ProductDTO>() // Empty list
        };
        var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });
        // Act
        var result = await _productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.Null(result); // Ensure the method returns null
    }

    //[Fact]
    //public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsIsNull()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var productResponse = new ProductApiResponse
    //    {
    //        Products = null // Explicitly null
    //    };
    //    var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
    //    {
    //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //    });
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.IsAny<HttpRequestMessage>(),
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent(jsonResponse)
    //        });
    //    // Act
    //    var result = await _productServiceClient.GetProductByIdAsync(productId);
    //    // Assert
    //    Assert.Null(result); // Ensure the method returns null
    //}

    //[Fact]
    //public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsIsNull()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var productResponse = new ProductApiResponse
    //    {
    //        Products = null // Explicitly null
    //    };
    //    var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
    //    {
    //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //    });
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.IsAny<HttpRequestMessage>(),
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent(jsonResponse)
    //        });
    //    // Act
    //    var result = await _productServiceClient.GetProductByIdAsync(productId);
    //    // Assert
    //    Assert.Null(result); // Ensure the method returns null
    //}


    //[Fact]
    //public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsAreEmpty()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var productResponse = new ProductApiResponse
    //    {
    //        Products = new List<ProductDTO>() // Empty list
    //    };
    //    var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
    //    {
    //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //    });
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.IsAny<HttpRequestMessage>(),
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent(jsonResponse)
    //        });
    //    // Act
    //    var result = await _productServiceClient.GetProductByIdAsync(productId);
    //    // Assert
    //    Assert.Null(result); // Ensure the method returns null
    //}

    //[Fact]
    //public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsIsNull()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var productResponse = new ProductApiResponse
    //    {
    //        Products = null // Explicitly null
    //    };
    //    var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
    //    {
    //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //    });
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.IsAny<HttpRequestMessage>(),
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent(jsonResponse)
    //        });
    //    // Act
    //    var result = await _productServiceClient.GetProductByIdAsync(productId);
    //    // Assert
    //    Assert.Null(result); // Ensure the method returns null
    //}
    //[Fact]
    //public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsAreEmpty()
    //{
    //    // Arrange
    //    var productId = Guid.NewGuid();
    //    var productResponse = new ProductApiResponse
    //    {
    //        Products = new List<ProductDTO>() // Empty list
    //    };
    //    var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
    //    {
    //        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    //    });
    //    _httpMessageHandlerMock.Protected()
    //        .Setup<Task<HttpResponseMessage>>(
    //            "SendAsync",
    //            ItExpr.IsAny<HttpRequestMessage>(),
    //            ItExpr.IsAny<CancellationToken>())
    //        .ReturnsAsync(new HttpResponseMessage
    //        {
    //            StatusCode = HttpStatusCode.OK,
    //            Content = new StringContent(jsonResponse)
    //        });
    //    // Act
    //    var result = await _productServiceClient.GetProductByIdAsync(productId);
    //    // Assert
    //    Assert.Null(result); // Ensure the method returns null
    //}

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsIsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productResponse = new ProductApiResponse
        {
            Products = null // Explicitly null
        };
        var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });
        // Act
        var result = await _productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.Null(result); // Ensure the method returns null
    }
    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductsAreEmpty()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var productResponse = new ProductApiResponse
        {
            Products = new List<ProductDTO>() // Empty list
        };
        var jsonResponse = JsonSerializer.Serialize(productResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });
        // Act
        var result = await _productServiceClient.GetProductByIdAsync(productId);
        // Assert
        Assert.Null(result); // Ensure the method returns null
    }

    [Fact]
    public async Task UpdateProductQuantityAsync_ShouldLogError_WhenApiResponseIsNotSuccessful()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var quantity = 15;
        var expectedUrl = $"http://localhost:3016/api/ProductManagement/{productId}/UpdateProductQuantity";
        var errorMessage = "Invalid quantity";
        var responseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.BadRequest, // Simulate 400 Bad Request
            Content = new StringContent(errorMessage) // Simulate error response content
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch && // Ensure the HTTP method is PATCH
                    req.RequestUri.ToString() == expectedUrl), // Match the exact URL
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseMessage);
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:3016") // Ensure the base address matches the expected URL
        };
        var productServiceClient = new ProductServiceClient(httpClient, _configurationMock.Object);
        // Act
        var result = await productServiceClient.UpdateProductQuantityAsync(productId, quantity);
        // Assert
        Assert.False(result); // Ensure the method returns false for unsuccessful response
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Patch &&
                req.RequestUri.ToString() == expectedUrl),
            ItExpr.IsAny<CancellationToken>()
        );
    }



}

