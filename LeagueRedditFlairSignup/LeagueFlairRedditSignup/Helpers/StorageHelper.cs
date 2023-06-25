using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;

namespace LeagueFlairRedditSignup.Helpers
{
    internal class StorageHelper
    {
        private readonly IConfiguration _config;

        public StorageHelper(IConfiguration config)
        {
            _config = config;
        }

        // Get an Azure Storage TableClient to interact with Azure Table Storage.
        // Note that this is private.  It's really the Upsert piece below that matters.
        private async Task<TableClient> GetTableClientAsync(string tableName)
        {
            TableClient tableClient = new(_config.GetValue<string>("AZURE_STORAGE_ACCOUNT"), tableName);
            await tableClient.CreateIfNotExistsAsync();

            return tableClient;
        }

        // Get a table client and upsert the data to the table.
        public async Task<Response> UpsertCloudTableAsync<T>(string tableName, T tableEntity, bool replace = false) where T : class, ITableEntity
        {
            TableClient table = await GetTableClientAsync(tableName);

            if (replace)
            {
                return await table.UpsertEntityAsync(tableEntity, TableUpdateMode.Replace);
            }

            // No replace
            return await table.UpsertEntityAsync(tableEntity, TableUpdateMode.Merge);
        }
    }
}
