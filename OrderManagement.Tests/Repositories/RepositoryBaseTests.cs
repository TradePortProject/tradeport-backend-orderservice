using Xunit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using OrderManagement.Data;
using OrderManagement.Tests.TestEntities;
using OrderManagement.Tests.TestRepositories;
using OrderManagement.Tests.TestUtilities;
using System.Diagnostics.CodeAnalysis;
namespace OrderManagement.Tests.Repositories
{
    [ExcludeFromCodeCoverage]
    public class RepositoryBaseTests
    {
        private readonly DbContextOptions<AppDbContext> _options;
        public RepositoryBaseTests()
        {
            // Configure the in-memory database for AppDbContext
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }
        [Fact]
        public void Create_ShouldAddEntityToDatabase()
        {
            // Arrange
            using var context = new TestDbContext(_options);
            var repository = new TestRepository(context);
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test Entity" };
            // Act
            repository.Create(entity);
            context.SaveChanges();
            // Assert
            Assert.Single(context.TestEntities);
            Assert.Equal("Test Entity", context.TestEntities.First().Name);
        }
        [Fact]
        public void Update_ShouldUpdateEntityInDatabase()
        {
            // Arrange
            using var context = new TestDbContext(_options);
            var repository = new TestRepository(context);
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Old Name" };
            context.TestEntities.Add(entity);
            context.SaveChanges();
            // Act
            entity.Name = "Updated Name";
            repository.Update(entity);
            context.SaveChanges();
            // Assert
            Assert.Single(context.TestEntities);
            Assert.Equal("Updated Name", context.TestEntities.First().Name);
        }
        [Fact]
        public void Delete_ShouldRemoveEntityFromDatabase()
        {
            // Arrange
            using var context = new TestDbContext(_options);
            var repository = new TestRepository(context);
            var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test Entity" };
            context.TestEntities.Add(entity);
            context.SaveChanges();
            // Act
            repository.Delete(entity);
            context.SaveChanges();
            // Assert
            Assert.Empty(context.TestEntities);
        }
        [Fact]
        public void FindAll_ShouldReturnAllEntities()
        {
            // Arrange
            using var context = new TestDbContext(_options);
            var repository = new TestRepository(context);
            var entities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity 1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity 2" }
            };
            context.TestEntities.AddRange(entities);
            context.SaveChanges();
            // Act
            var result = repository.FindAll().ToList();
            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, e => e.Name == "Entity 1");
            Assert.Contains(result, e => e.Name == "Entity 2");
        }
        [Fact]
        public void FindByCondition_ShouldReturnMatchingEntities()
        {
            // Arrange
            using var context = new TestDbContext(_options);
            var repository = new TestRepository(context);
            var entities = new List<TestEntity>
            {
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity 1" },
                new TestEntity { Id = Guid.NewGuid(), Name = "Entity 2" }
            };
            context.TestEntities.AddRange(entities);
            context.SaveChanges();
            // Act
            var result = repository.FindByCondition(e => e.Name == "Entity 1").ToList();
            // Assert
            Assert.Single(result);
            Assert.Equal("Entity 1", result.First().Name);
        }
    }
}
