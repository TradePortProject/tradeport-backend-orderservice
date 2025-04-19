using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;
using OrderManagement.Repositories;
using Xunit;

namespace OrderManagement.Tests.Repositories
{
    [ExcludeFromCodeCoverage]
    public class UserRepositoryTests
    {
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;
        public UserRepositoryTests()
        {
            // Configure the in-memory database
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique database for each test
                .Options;
        }
        [Fact]
        public async Task GetUserInfoByRetailerIdAsync_ShouldReturnMatchingUsers()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new UserRepository(context);
            var retailerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var users = new List<User>
        {
            new User { UserID = retailerIds[0], LoginID = "retailer@gmail.com", UserName = "Retailer 1",Address = "123 Main St",PhoneNo = "1234567890", Role = 1 },
            new User { UserID = retailerIds[1], LoginID = "retailer@gmail.com", UserName = "Retailer 2",Address = "123 Main St",PhoneNo = "1234567890", Role = 1 },
            new User { UserID = Guid.NewGuid(), LoginID = "retailer@gmail.com", UserName = "Non-Retailer",Address = "123 Main St",PhoneNo = "1234567890", Role = 1 } // Not in retailerIds
        };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            // Act
            var result = await repository.GetUserInfoByRetailerIdAsync(retailerIds);
            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(retailerIds[0], result.Keys);
            Assert.Contains(retailerIds[1], result.Keys);
        }
        [Fact]
        public async Task GetUserInfoByRetailerIdAsync_ShouldReturnEmptyDictionary_WhenNoMatchingUsers()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new UserRepository(context);
            var retailerIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var users = new List<User>
            {
                new User
                {
                    UserID = Guid.NewGuid(),
                    LoginID = "retailer@gmail.com",
                    UserName = "User 1",
                    Address = "123 Main St", // Required property
                    PhoneNo = "1234567890",  // Required property
                    Role = 1
                },
                new User
                {
                    UserID = Guid.NewGuid(),
                    LoginID = "retailer@gmail.com",
                    UserName = "User 2",
                    Address = "456 Elm St", // Required property
                    PhoneNo = "0987654321",  // Required property
                    Role = 1
                }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            // Act
            var result = await repository.GetUserInfoByRetailerIdAsync(retailerIds);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public async Task GetUserInfoByRetailerIdAsync_ShouldHandleEmptyRetailerIdList()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var repository = new UserRepository(context);
            var retailerIds = new List<Guid>();
            var users = new List<User>
            {
                new User
                {
                    UserID = Guid.NewGuid(),
                    LoginID = "retailer@gmail.com",
                    UserName = "User 1",
                    Address = "123 Main St", // Required property
                    PhoneNo = "1234567890",  // Required property
                    Role = 1
                },
                new User
                {
                    UserID = Guid.NewGuid(),
                    LoginID = "retailer@gmail.com",
                    UserName = "User 2",
                    Address = "456 Elm St", // Required property
                    PhoneNo = "0987654321",  // Required property
                    Role = 1
                }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();
            // Act
            var result = await repository.GetUserInfoByRetailerIdAsync(retailerIds);
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
