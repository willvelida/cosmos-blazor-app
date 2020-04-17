using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace CosmosBlazorApp.API.Helpers
{
    public static class CosmosExceptionHandler
    {
        public static IActionResult HandleCosmosException(HttpStatusCode statusCode)
        {
            IActionResult result = null;
            switch (statusCode)
            {
                case HttpStatusCode.BadRequest:
                    result = new StatusCodeResult(StatusCodes.Status400BadRequest);
                    break;
                case HttpStatusCode.Unauthorized:
                    result = new StatusCodeResult(StatusCodes.Status401Unauthorized);
                    break;
                case HttpStatusCode.Forbidden:
                    result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                    break;
                case HttpStatusCode.NotFound:
                    result = new StatusCodeResult(StatusCodes.Status404NotFound);
                    break;
                case HttpStatusCode.RequestTimeout:
                    result = new StatusCodeResult(StatusCodes.Status408RequestTimeout);
                    break;
                case HttpStatusCode.Conflict:
                    result = new StatusCodeResult(StatusCodes.Status409Conflict);
                    break;
                case HttpStatusCode.RequestEntityTooLarge:
                    result = new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
                    break;
                case HttpStatusCode.TooManyRequests:
                    result = new StatusCodeResult(StatusCodes.Status429TooManyRequests);
                    break;
                default:
                    break;
            }

            return result;
        }
    }
}
