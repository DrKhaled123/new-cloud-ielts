using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using App.ServiceInvoker.Auth;
using App.ServiceInvoker.Reflection;

namespace App.ServiceInvoker.Endpoints
{
    /// <summary>
    /// A static class with an extension method to map
    /// a single Minimal API endpoint for reflection-based service invocation.
    /// </summary>
    public static class ServiceInvokerEndpoints
    {
        /// <summary>
        /// Maps a POST endpoint that calls ServiceMethodInvoker via JSON requests.
        /// </summary>
        /// <param name="routes">IEndpointRouteBuilder (e.g. your WebApplication)</param>
        /// <param name="route">The path, e.g. "/invoke".</param>
        /// <returns></returns>
        public static IEndpointRouteBuilder MapServiceInvokerEndpoint(
            this IEndpointRouteBuilder routes,
            string route = "/invoke")
        {
            routes.MapPost(route, async (
                HttpContext context,
                IServiceProvider serviceProvider,
                IRequestAuthenticator requestAuthenticator,
                IMethodAuthorizer methodAuthorizer,
                JsonSerializerOptions jsonOptions     // Also from DI
            ) =>
            {
                // 1) Read the request body into a ServiceInvocationRequestDto


                try
                {
                    // 2) Create the shared reflection invoker
                    var invoker = new ServiceMethodInvoker(serviceProvider, requestAuthenticator, methodAuthorizer, jsonOptions);

                    // 3) Invoke the requested method
                    var result = await invoker.InvokeAsync(context.Request);

                    // 4) Return the result as JSON
                    return Results.Json(result, jsonOptions);
                }
                catch (UnauthorizedAccessException)
                {
                    // If CloudAuthHandler or something throws unauthorized
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    // Return a 500 with the exception details
                    return Results.Problem(ex.ToString());
                }
            });

            return routes;
        }
    }
}
