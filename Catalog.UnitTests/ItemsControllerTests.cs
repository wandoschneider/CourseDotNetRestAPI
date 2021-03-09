using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Catalog.Api.Controllers;
using Catalog.Api.Entities;
using Catalog.Api.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static Catalog.Api.Dtos;

namespace Catalog.UnitTests
{
    public class ItemsControllerTests
    {
        private readonly Mock<IItemsRepository> repositoryStub = new();
        private readonly Mock<ILogger<ItemsController>> loggerStub = new();
        private readonly Random rand = new();

        [Fact]
        public async Task GetItemAsync_WithUnexistingItem_ReturnsNotFound()
        {
            //Arrange
            repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Item)null);

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            //Act
            var result = await controller.GetItemAsync(Guid.NewGuid());

            //Assert
            result.Result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetItemAsync_WithExistingItem_ReturnsExpectedItem()
        {
            var expectedItem = CreateRandomItem();

            repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
                .ReturnsAsync(expectedItem);

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            var result = await controller.GetItemAsync(Guid.NewGuid());

            result.Value.Should().BeEquivalentTo(expectedItem);
            // options => options.ComparingByMembers<Item>()); 
            // Essa opção é necessária pois nossa classe item é um record
        }

        [Fact]
        public async Task GetItemsAsync_WithExistingItem_ReturnsAllItems()
        {
            var expectedItems = new[]{
                CreateRandomItem(),
                CreateRandomItem(),
                CreateRandomItem()
            };

            repositoryStub.Setup(repo => repo.GetItemsAsync()).ReturnsAsync(expectedItems);

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            var actualItems = await controller.GetItemsAsync();

            actualItems.Should().BeEquivalentTo(expectedItems);
        }

        [Fact]
        public async Task GetItemsAsync_WithMatchingItems_ReturnsMatchingItems()
        {
            var allItems = new[]{
                new Item(){Name = "Potion"},
                new Item(){Name = "Antidote"},
                new Item(){Name = "Hi-Potion"}
            };

            var nameToMatch = "Potion";

            repositoryStub.Setup(repo => repo.GetItemsAsync()).ReturnsAsync(allItems);

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            IEnumerable<ItemDto> foundItens = await controller.GetItemsAsync(nameToMatch);

            foundItens.Should().OnlyContain(
                item => item.Name == allItems[0].Name || item.Name == allItems[2].Name
            );
        }

        [Fact]
        public async Task CreateItemsAsync_WitItemToCreate_ReturnsCreatedItem()
        {
            var itemToCreate = new CreateItemDto(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                rand.Next(1000));

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            var result = await controller.CreateItemAsync(itemToCreate);

            var createdItem = (result.Result as CreatedAtActionResult).Value as ItemDto;
            itemToCreate.Should().BeEquivalentTo(
                createdItem,
                options => options.ComparingByMembers<ItemDto>().ExcludingMissingMembers()
            );
            createdItem.Id.Should().NotBeEmpty();
            createdItem.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, 1000);
        }

        [Fact]
        public async Task UpdateItemAsync_WithUnexistingItem_ReturnsNotFound()
        {
            //Arrange
            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            //Act
            var result = await controller.UpdateItemAsync(Guid.NewGuid(), (UpdateItemDto)null);

            //Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task UpdateItemAsync_WithExistingItem_ReturnsNoContent()
        {
            Item existingItem = CreateRandomItem();

            repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
                .ReturnsAsync(existingItem);

            var itemId = existingItem.Id;
            var itemToUpdate = new UpdateItemDto(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                existingItem.Price + 3);

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            var result = await controller.UpdateItemAsync(itemId, itemToUpdate);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteItemAsync_WithExistingItem_ReturnsNoContent()
        {
            Item existingItem = CreateRandomItem();

            repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
                .ReturnsAsync(existingItem);

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            var result = await controller.DeleteItemAsync(existingItem.Id);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteItemAsync_WithUnexistingItem_ReturnsNotFound()
        {
            Item existingItem = CreateRandomItem();

            repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Item)null);

            var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

            var result = await controller.DeleteItemAsync(existingItem.Id);

            result.Should().BeOfType<NotFoundResult>();
        }

        private Item CreateRandomItem()
        {
            return new()
            {
                Id = Guid.NewGuid(),
                Name = Guid.NewGuid().ToString(),
                Price = rand.Next(1000),
                CreatedDate = DateTimeOffset.UtcNow
            };
        }
    }
}
