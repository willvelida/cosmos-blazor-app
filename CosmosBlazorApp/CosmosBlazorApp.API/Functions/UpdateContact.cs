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
    public class UpdateContact
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly CosmosClient _cosmosClient;

        private Container _contactContainer;

        public UpdateContact(
            ILogger<CreateContact> logger,
            IConfiguration config,
            CosmosClient cosmosClient)
        {
            _logger = logger;
            _config = config;
            _cosmosClient = cosmosClient;

            _contactContainer = _cosmosClient.GetContainer(_config[Settings.COSMOS_DB_DATABASE_NAME], config[Settings.COSMOS_DB_COLLECTION_NAME]);
        }

        [FunctionName(nameof(UpdateContact))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "Contact/{id}")] HttpRequest req,
            string id)
        {
            IActionResult result = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var updatedContact = JsonConvert.DeserializeObject<Contact>(requestBody);

                updatedContact.ContactId = id;

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
                        _logger.LogWarning($"Couldn't find contact with {id}");
                        result = new StatusCodeResult(StatusCodes.Status404NotFound);
                    }

                    var oldContact = await _contactContainer.ReadItemAsync<Contact>(id, new PartitionKey(contact.ContactType));
                    var newContact = await _contactContainer.ReplaceItemAsync(updatedContact, oldContact.Resource.ContactId, new PartitionKey(oldContact.Resource.ContactType));

                    result = new OkObjectResult(newContact.Resource);
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
