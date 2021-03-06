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
using System.Linq;

namespace CosmosBlazorApp.API.Functions
{
    public class DeleteContact
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly CosmosClient _cosmosClient;

        private Container _contactContainer;

        public DeleteContact(
            ILogger<CreateContact> logger,
            IConfiguration config,
            CosmosClient cosmosClient)
        {
            _logger = logger;
            _config = config;
            _cosmosClient = cosmosClient;

            _contactContainer = _cosmosClient.GetContainer(_config[Settings.COSMOS_DB_DATABASE_NAME], config[Settings.COSMOS_DB_COLLECTION_NAME]);
        }

        [FunctionName(nameof(DeleteContact))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "Contact/{id}")] HttpRequest req,
            string id)
        {
            IActionResult result = null;

            try
            {
                QueryDefinition queryDefinition = new QueryDefinition(
                    $"SELECT * FROM {_contactContainer.Id} c WHERE c.id = @id")
                    .WithParameter("@id", id);

                FeedIterator<Contact> iterator = _contactContainer.GetItemQueryIterator<Contact>
                    (
                        queryDefinition,
                        requestOptions: new QueryRequestOptions()
                        {
                            MaxItemCount = 1
                        }
                    );

                while (iterator.HasMoreResults)
                {
                    FeedResponse<Contact> response = await iterator.ReadNextAsync();
                    Contact contact = response.First();

                    if (contact == null)
                    {
                        _logger.LogWarning($"Contact with id: {id} doesn't exist");
                        result = new StatusCodeResult(StatusCodes.Status404NotFound);
                    }

                    ItemResponse<Contact> itemResponse = await _contactContainer.DeleteItemAsync<Contact>
                        (
                            id,
                            new PartitionKey(contact.ContactType)
                        );

                    result = new StatusCodeResult(StatusCodes.Status204NoContent);
                }
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
