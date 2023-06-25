using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace LeagueFlairRiotUpdateService.Helpers
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

        public ServiceBusProcessor GetServiceBusProcessor()
        {
            return _sbclient.CreateProcessor(_config.GetValue<string>("ServiceBusListenTopicName"), _config.GetValue<string>("ServiceBusSubscriptionName"));
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

        public async Task<T?> RetrieveCloudTable<T>(string tableName, string partitionKey, string rowKey, List<string>? selectColumns = null) where T : class, ITableEntity
        {
            TableClient table = await GetTableClient(tableName);
            try
            {
                T? result = await table.GetEntityAsync<T>(partitionKey, rowKey, selectColumns);
                return result;
            }
            catch (RequestFailedException ex)
            {
                if (string.Equals(ex.ErrorCode, "ResourceNotFound"))
                {
                    return null;
                }

                throw;
            }
        }

        public async Task<Response> UpsertCloudTable<T>(string tableName, T tableEntity, bool replace = false) where T : class, ITableEntity
        {
            TableClient table = await GetTableClient(tableName);

            if (replace)
            {
                return await table.UpsertEntityAsync(tableEntity, TableUpdateMode.Replace);
            }
            else
            {
                return await table.UpsertEntityAsync(tableEntity, TableUpdateMode.Merge);
            }

            // No return here
        }
    }
}
