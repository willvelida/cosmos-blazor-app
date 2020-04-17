using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using CosmosBlazorApp.API.Helpers;
using CosmosBlazorApp.Common.Models;

namespace CosmosBlazorApp.API.Functions
{
    public class GetAllContacts
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly CosmosClient _cosmosClient;

        private Container _contactContainer;

        public GetAllContacts(
            ILogger<CreateContact> logger,
            IConfiguration config,
            CosmosClient cosmosClient)
        {
            _logger = logger;
            _config = config;
            _cosmosClient = cosmosClient;

            _contactContainer = _cosmosClient.GetContainer(_config[Settings.COSMOS_DB_DATABASE_NAME], config[Settings.COSMOS_DB_COLLECTION_NAME]);
        }

        [FunctionName(nameof(GetAllContacts))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Contacts")] HttpRequest req)
        {
            IActionResult result = null;
            string continuationToken = null;
            
            try
            {
                QueryDefinition query = new QueryDefinition($"SELECT * FROM {_contactContainer.Id} c");

                do
                {
                    FeedIterator<Contact> feed = _contactContainer.GetItemQueryIterator<Contact>(
                    query,
                    continuationToken,
                    new QueryRequestOptions()
                    {
                        MaxItemCount = 50
                    });

                    while (feed.HasMoreResults)
                    {
                        FeedResponse<Contact> response = await feed.ReadNextAsync();
                        continuationToken = response.ContinuationToken;
                        result = new OkObjectResult(response);
                    }                   
                } while (continuationToken != null);              
            }
            catch (CosmosException cex)
            {
                var statusCode = cex.StatusCode;
                _logger.LogError($"Cosmos DB Exception. Status code {statusCode}. Error thrown: {cex.Message}");
                result = CosmosExceptionHandler.HandleCosmosException(statusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Internal Server Error. Exception thrown: {ex.Message}");
                result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return result;
        }
    }
}
