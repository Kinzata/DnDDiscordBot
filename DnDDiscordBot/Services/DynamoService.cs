﻿using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using DnDDiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DnDDiscordBot.Services
{
    public class DynamoService
    {
        private string _characterDataTablename = "";

        private AmazonDynamoDBClient _dbClient;

        public DynamoService(AmazonDynamoDBClient dbClient)
        {
            _dbClient = dbClient;
            _characterDataTablename = Environment.GetEnvironmentVariable("DND_CHARACTER_TABLENAME");
        }

        public async Task<TableDescription> GetTableDescription(string tableName)
        {
            TableDescription result = null;

            // If the table exists, get its description.
            try
            {
                var response = await _dbClient.DescribeTableAsync(_characterDataTablename);
                result = response.Table;
            }
            catch (Exception)
            { }

            return result;
        }

        public async Task<string[]> GetAllCharacterNames()
        {
            string[] result = null;

            try
            {
                //var response = await _dbClient.DescribeTableAsync(CHARACTER_DATA_TABLENAME);
                var response = await _dbClient.ScanAsync(_characterDataTablename, new List<string> { "CharacterName" });

                result = response.Items.Select(item => item).Select(attributes => attributes["CharacterName"].S).ToArray();
            }
            catch (Exception)
            { }

            return result;
        }

        public async Task<List<LevelLog>> GetAllCharacterData()
        {
            List<LevelLog> result = null;

            try
            {
                //var response = await _dbClient.DescribeTableAsync(CHARACTER_DATA_TABLENAME);
                var response = await _dbClient.ScanAsync(_characterDataTablename, new List<string> { "CharacterName", "Level", "UserId" });

                result = response.Items.Select(item => item).Select(attributes => {
                    return new LevelLog
                    {
                        CharacterName = attributes["CharacterName"].S,
                        Level = int.Parse(attributes["Level"].S),
                        UserId = ulong.Parse(attributes["UserId"].S)
                    };
                }).ToList();
            }
            catch (Exception)
            { }

            return result;
        }

        public async Task<LevelLog> GetCharacterData(string name)
        {
            LevelLog log = null;

            name = name.ToLower();

            try
            {

                var query = new ScanRequest(_characterDataTablename);
                query.FilterExpression = $"SearchFieldCharacterName = :key";
                query.ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":key", new AttributeValue { S = name }}};

                var response = await _dbClient.ScanAsync(query);

                var result = response.Items.First();

                if(result != null)
                {
                    log = new LevelLog
                    {
                        CharacterName = result["CharacterName"].S,
                        Level = int.Parse(result["Level"].S),
                        UserId = ulong.Parse(result["UserId"].S)
                    };
                } 
            }
            catch (Exception ex)
            { }

            return log;
        }

        public async Task<LevelLog[]> GetCharacterData(ulong userId)
        {
            var logs = new List<LevelLog>();

            try
            {

                var query = new ScanRequest(_characterDataTablename);
                query.FilterExpression = $"UserId = :key";
                query.ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":key", new AttributeValue { S = userId.ToString() }}};

                var response = await _dbClient.ScanAsync(query);

                var items = response.Items;

                if (items != null)
                {
                    foreach(var item in items)
                    {
                        logs.Add(new LevelLog
                        {
                            CharacterName = item["CharacterName"].S,
                            Level = int.Parse(item["Level"].S),
                            UserId = ulong.Parse(item["UserId"].S)
                        });
                    }
                    
                }
            }
            catch (Exception ex)
            { }

            return logs.ToArray();
        }

        public async void InsertCharacterList(List<LevelLog> logs)
        {
            try
            {
                var request = new TransactWriteItemsRequest();

                var transactItems = new List<TransactWriteItem>();
                TransactWriteItemsResponse response;

                foreach( var log in logs)
                {
                    transactItems.Add(
                        new TransactWriteItem
                        {
                            Put = new Put { TableName = _characterDataTablename, Item = log.ToTransactItemDictionary() }
                        }
                    );

                    // Max page size
                    if (transactItems.Count == 25)
                    {
                        request.TransactItems = transactItems;
                        response = await _dbClient.TransactWriteItemsAsync(request);

                        transactItems.Clear();
                    }
                }

                // Then do one final request, as long as there is an item, since the last batch won't run unless exactly equal to 25
                // Probably a better way to do this, but this works.
                request.TransactItems = transactItems;
                response = await _dbClient.TransactWriteItemsAsync(request);
            }
            catch (Exception)
            { }
        }

        public async Task DeleteCharacterRecord(string name)
        {

            try
            {
                var deleteQuery = new DeleteItemRequest(_characterDataTablename, new Dictionary<string, AttributeValue> {
                    {"CharacterName", new AttributeValue { S = name }}});

                await _dbClient.DeleteItemAsync(deleteQuery);
            }
            catch (Exception ex)
            { }
        }
    }
}
