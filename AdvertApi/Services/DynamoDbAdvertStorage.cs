﻿using AdvertApi.Models;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using AutoMapper;

namespace AdvertApi.Services;

public class DynamoDbAdvertStorage : IAdvertStorageService
{
    private readonly IMapper _mapper;

    public DynamoDbAdvertStorage(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task<string> AddAsync(CreateAdvertRequest model)
    {
        var dbModel = _mapper.Map<AdvertDbModel>(model);

        dbModel.Id = Guid.NewGuid().ToString();
        dbModel.CreationDateTime = DateTime.UtcNow;
        dbModel.Status = AdvertStatus.Pending;

        using var client = new AmazonDynamoDBClient();
        using var context = new DynamoDBContext(client);

        await context.SaveAsync(dbModel);

        return dbModel.Id;
    }

    public async Task ConfirmAsync(ConfirmAdvertRequest model)
    {
        using var client = new AmazonDynamoDBClient();
        using var context = new DynamoDBContext(client);

        var record = await context.LoadAsync<AdvertDbModel>(model.Id);
        if (record is null)
        {
            throw new KeyNotFoundException($"A record with ID={model.Id} was not found.");
        }

        if (model.Status == AdvertStatus.Active)
        {
            record.Status = AdvertStatus.Active;
            await context.SaveAsync(record);
        }
        else
        {
            await context.DeleteAsync(record);
        }
    }

    public async Task<bool> CheckHealthAsync()
    {
        using var client = new AmazonDynamoDBClient();

        var tableData = await client.DescribeTableAsync("Adverts");

        return tableData.Table.TableStatus == TableStatus.ACTIVE;
    }

    public async Task<AdvertDbModel> GetByIdAsync(string id)
    {
        using var client = new AmazonDynamoDBClient();
        using var context = new DynamoDBContext(client);

        var dbItem = await context.LoadAsync<AdvertDbModel>(id);

        return dbItem ?? throw new KeyNotFoundException();
    }
}
