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
    public class CreateContact
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly CosmosClient _cosmosClient;

        private Container _contactContainer;

        public CreateContact(
            ILogger<CreateContact> logger,
            IConfiguration config,
            CosmosClient cosmosClient)
        {
            _logger = logger;
            _config = config;
            _cosmosClient = cosmosClient;

            _contactContainer = _cosmosClient.GetContainer(_config[Settings.COSMOS_DB_DATABASE_NAME], config[Settings.COSMOS_DB_COLLECTION_NAME]);
        }

        [FunctionName(nameof(CreateContact))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "Contact")] HttpRequest req)
        {
            IActionResult result = null;

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                var input = JsonConvert.DeserializeObject<Contact>(requestBody);

                var contact = new Contact
                {
                    ContactId = Guid.NewGuid().ToString(),
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    BirthDate = input.BirthDate,
                    Email = input.Email,
                    PhoneNumber = input.PhoneNumber,
                    Gender = input.Gender,
                    ContactType = input.ContactType
                };

                ItemResponse<Contact> response = await _contactContainer.CreateItemAsync(
                    contact,
                    new PartitionKey(contact.ContactType));

                result = new StatusCodeResult(StatusCodes.Status201Created);
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
