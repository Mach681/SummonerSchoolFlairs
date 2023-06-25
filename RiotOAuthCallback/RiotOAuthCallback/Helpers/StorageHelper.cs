using Azure;
using Azure.Data.Tables;
using Azure.Messaging.ServiceBus;
using System.Linq.Expressions;

namespace RiotOAuthCallback.Helpers
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

        private async Task<TableClient> GetTableClientAsync(string tableName)
        {
            TableClient tableClient = new(_config.GetValue<string>("AZURE_STORAGE_ACCOUNT"), tableName);
            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }

        private ServiceBusSender GetServiceBusSender(string topic = "")
        {
            if (string.IsNullOrEmpty(topic))
            {
                topic = _config.GetValue<string>("ServiceBusTopicName");
            }
            return _sbclient.CreateSender(topic);
        }

        public async Task SendServiceBusMessageAsync(ServiceBusMessage message, string topic = "")
        {
            ServiceBusSender sender = GetServiceBusSender(topic);
            await sender.SendMessageAsync(message);
        }

        public async Task<Response> UpsertCloudTableAsync<T>(string tableName, T tableEntity, bool replace = false) where T : class, ITableEntity
        {
            TableClient table = await GetTableClientAsync(tableName);

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

        public async Task<List<T>> QueryCloudTableAsync<T>(string tableName, Expression<Func<T, bool>> filter, int? maxMessages = null) where T : class, ITableEntity
        {
            TableClient table = await GetTableClientAsync(tableName);

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

        public async Task<bool> DeleteCloudTableAsync<T>(string tableName, T tableEntity) where T : ITableEntity
        {
            TableClient table = await GetTableClientAsync(tableName);
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
