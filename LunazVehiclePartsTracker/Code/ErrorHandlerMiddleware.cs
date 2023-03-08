using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SAPB1Commons.ServiceLayer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace LunazVehiclePartsTracker.Code
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                //if the intent was not to provide a machine-readable response, just throw,
                //then either the default handler or the development handler will take over
                if (context.Request.Headers["Accept"].ToString()?.Contains("text/html") ?? false)
                {
                    //To Do: There is potential to add our own output here
                    throw;
                }

                var response = context.Response;
                response.ContentType = "application/json";

                switch (error)
                {
                    case ServiceLayerSecurityException e:
                        // custom application error
                        response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        break;
                    case KeyNotFoundException e:
                        // not found error
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                    default:
                        // unhandled error
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }

                _logger.LogError(error, "Unhandled Exception Occurred");
                var result = JsonSerializer.Serialize(new { message = error?.Message });
                await response.WriteAsync(result);
            }
        }
    }
}
