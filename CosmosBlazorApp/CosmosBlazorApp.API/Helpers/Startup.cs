using CosmosBlazorApp.API.Helpers;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: FunctionsStartup(typeof(Startup))]
namespace CosmosBlazorApp.API.Helpers
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter(level => true);
            });

            var config = (IConfiguration)builder.Services.First(s => s.ServiceType == typeof(IConfiguration)).ImplementationInstance;

            builder.Services.AddSingleton((s) =>
            {
                CosmosClientBuilder cosmosClientBuilder = new CosmosClientBuilder(config[Settings.COSMOS_DB_CONNECTION_STRING]);

                return cosmosClientBuilder.WithConnectionModeDirect()
                .Build();
            });
        }
    }
}
