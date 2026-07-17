using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Services;

namespace MiniCPQ.Tests;

public sealed class MaterialAndServerRequirementTests
{
    [Fact]
    public async Task MaterialService_AllowsMotherboardAndNetworkCard()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new MaterialService(database.Context);

        var motherboard = await service.CreateAsync(
            new CreateMaterialRequest("主板-A", "motherboard", 1500m),
            default);
        var networkCard = await service.CreateAsync(
            new CreateMaterialRequest("网卡-A", "nic", 600m),
            default);

        Assert.Equal(MaterialTypes.Motherboard, motherboard.Type);
        Assert.Equal(MaterialTypes.NetworkCard, networkCard.Type);
    }

    [Theory]
    [InlineData(MaterialTypes.Cpu, 2, 1)]
    [InlineData(MaterialTypes.Memory, 5, 4)]
    [InlineData(MaterialTypes.Storage, 4, 3)]
    [InlineData(MaterialTypes.Motherboard, 2, 1)]
    public async Task ServerConfiguration_RejectsQuantityOverTypeLimit(
        string materialType,
        int requestedQuantity,
        int expectedLimit)
    {
        await using var database = await TestDatabase.CreateAsync();
        var material = new Material
        {
            Name = $"{materialType}-Limit-Test",
            Type = materialType,
            UnitPrice = 100m
        };
        database.Context.Materials.Add(material);
        await database.Context.SaveChangesAsync();

        var service = new ServerService(database.Context);
        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateServerRequest(
                $"Server-{materialType}",
                [new ServerMaterialInput(material.Id, requestedQuantity)]),
            default));

        Assert.Contains($"最多为 {expectedLimit}", exception.Message);
    }

    [Fact]
    public async Task ServerConfiguration_AllowsMaximumComponentsAndOptionalNetworkCard()
    {
        await using var database = await TestDatabase.CreateAsync();
        var materials = new[]
        {
            new Material { Name = "CPU-Max", Type = MaterialTypes.Cpu, UnitPrice = 2000m },
            new Material { Name = "RAM-Max", Type = MaterialTypes.Memory, UnitPrice = 800m },
            new Material { Name = "ROM-Max", Type = MaterialTypes.Storage, UnitPrice = 1000m },
            new Material { Name = "Board-Max", Type = MaterialTypes.Motherboard, UnitPrice = 1500m },
            new Material { Name = "NIC-Optional", Type = MaterialTypes.NetworkCard, UnitPrice = 600m }
        };
        database.Context.Materials.AddRange(materials);
        await database.Context.SaveChangesAsync();

        var service = new ServerService(database.Context);
        var result = await service.CreateAsync(
            new CreateServerRequest(
                "Server-Max",
                [
                    new ServerMaterialInput(materials[0].Id, 1),
                    new ServerMaterialInput(materials[1].Id, 4),
                    new ServerMaterialInput(materials[2].Id, 3),
                    new ServerMaterialInput(materials[3].Id, 1),
                    new ServerMaterialInput(materials[4].Id, 2)
                ]),
            default);

        Assert.Equal(5, result.Materials.Count);
        Assert.Equal(2, result.Materials.Single(x => x.Type == MaterialTypes.NetworkCard).Quantity);
    }

    [Theory]
    [InlineData(MaterialTypes.Cpu, 1)]
    [InlineData(MaterialTypes.Memory, 4)]
    [InlineData(MaterialTypes.Storage, 3)]
    [InlineData(MaterialTypes.Motherboard, 1)]
    public async Task ServerConfiguration_AggregatesDifferentModelsOfSameType(string materialType, int limit)
    {
        await using var database = await TestDatabase.CreateAsync();
        var modelA = new Material { Name = $"{materialType}-Aggregate-A", Type = materialType, UnitPrice = 1000m };
        var modelB = new Material { Name = $"{materialType}-Aggregate-B", Type = materialType, UnitPrice = 1500m };
        database.Context.Materials.AddRange(modelA, modelB);
        await database.Context.SaveChangesAsync();

        var service = new ServerService(database.Context);
        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateServerRequest(
                $"Server-Aggregate-{materialType}",
                [new ServerMaterialInput(modelA.Id, limit), new ServerMaterialInput(modelB.Id, 1)]),
            default));
    }

    [Fact]
    public async Task StorageModels_AcceptsCombinedThree_AndRejectsCombinedFour()
    {
        await using var database = await TestDatabase.CreateAsync();
        var oneTb = new Material { Name = "1TB-Storage-Test", Type = MaterialTypes.Storage, UnitPrice = 600m };
        var twoTb = new Material { Name = "2TB-Storage-Test", Type = "SSD", UnitPrice = 1000m };
        database.Context.Materials.AddRange(oneTb, twoTb);
        await database.Context.SaveChangesAsync();

        var service = new ServerService(database.Context);
        var valid = await service.CreateAsync(
            new CreateServerRequest(
                "Storage-Total-Three",
                [new ServerMaterialInput(oneTb.Id, 1), new ServerMaterialInput(twoTb.Id, 2)]),
            default);
        Assert.Equal(3, valid.Materials.Sum(x => x.Quantity));

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateServerRequest(
                "Storage-Total-Four",
                [new ServerMaterialInput(oneTb.Id, 2), new ServerMaterialInput(twoTb.Id, 2)]),
            default));
    }
}
