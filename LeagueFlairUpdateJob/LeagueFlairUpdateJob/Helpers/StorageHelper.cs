using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace LeagueFlairUpdateJob.Helpers
{
    public class StorageHelper
    {
        private readonly IConfiguration _config;
        private readonly ServiceBusClient _sbclient;

        public StorageHelper(IConfiguration config, ServiceBusClient sbclient)
        {
            _config = config;
            _sbclient = sbclient;
        }

        private async Task<TableClient> GetTableClient(string tableName)
        {
            TableClient tableClient = new(_config.GetValue<string>("AZURE_STORAGE_ACCOUNT"), tableName);
            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }

        private ServiceBusSender GetServiceBusSender()
        {
            return _sbclient.CreateSender(_config.GetValue<string>("ServiceBusTopicName"));
        }

        public async Task SendServiceBusMessage(ServiceBusMessage message)
        {
            ServiceBusSender sender = GetServiceBusSender();
            await sender.SendMessageAsync(message);
        }

        public async Task<List<T>> QueryCloudTable<T>(string tableName, Expression<Func<T, bool>> filter, int? maxMessages = null) where T : class, ITableEntity
        {
            TableClient table = await GetTableClient(tableName);

            try
            {
                if (maxMessages.HasValue)
                {
                    return await table.QueryAsync(filter, maxMessages.Value).ToListAsync();
                }
                else
                {
                    return await table.QueryAsync(filter).ToListAsync();
                }
            }
            catch (RequestFailedException ex)
            {
                if (string.Equals(ex.ErrorCode, "ResourceNotFound"))
                {
                    return new List<T>();
                }

                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                throw;
            }
        }

        public async Task<bool> DeleteCloudTable<T>(string tableName, T tableEntity) where T : ITableEntity
        {
            TableClient table = await GetTableClient(tableName);
            try
            {
                await table.DeleteEntityAsync(tableEntity.PartitionKey, tableEntity.RowKey);

                return true;
            }
            catch (RequestFailedException ex)
            {
                if (string.Equals(ex.ErrorCode, "ResourceNotFound"))
                {
                    return true;
                }

                throw;
            }
        }
    }
}
