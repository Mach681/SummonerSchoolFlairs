using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace LeagueFlairRedditUpdateService.Helpers
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
