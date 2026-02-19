using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using App.ServiceInvoker.Attributes;
using App.ServiceInvoker.Auth;
using App.ServiceInvoker.Interfaces;
using Contracts.Dto;
using Microsoft.AspNetCore.Http;

namespace App.ServiceInvoker.Reflection
{
    public class ServiceMethodInvoker
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRequestAuthenticator _requestAuthenticator;
        private readonly IMethodAuthorizer _methodAuthorizer;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Lazy<MethodMetadata>>> _serviceMethodCache =
            new();

        private readonly JsonSerializerOptions _jsonOptions;

        public ServiceMethodInvoker(
            IServiceProvider serviceProvider,
            IRequestAuthenticator requestAuthenticator,
            IMethodAuthorizer methodAuthorizer,
            JsonSerializerOptions? jsonOptions = null)
        {
            _serviceProvider = serviceProvider;
            _requestAuthenticator = requestAuthenticator;
            _methodAuthorizer = methodAuthorizer;
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                MaxDepth = 256
            };
        }

        public async Task<object?> InvokeAsync(HttpRequest request)
        {
            var requestDto = await request.ReadFromJsonAsync<ServiceInvocationRequestDto>(_jsonOptions);
            if (requestDto == null)
            {
                return Results.BadRequest("Invalid JSON body.");
            }

            // 1) Resolve the service
            var serviceInstance = GetManagerServiceByName(requestDto.ManagerName);
            if (serviceInstance == null)
            {
                throw new InvalidOperationException($"Service '{requestDto.ManagerName}' not found.");
            }

            // 2) Retrieve or cache method metadata
            MethodMetadata methodMetadata = _serviceMethodCache
                .GetOrAdd(requestDto.ManagerName, _ => new ConcurrentDictionary<string, Lazy<MethodMetadata>>())
                .GetOrAdd(requestDto.MethodName, new Lazy<MethodMetadata>(() => LoadMethodMetadata(serviceInstance, requestDto.MethodName)))
                .Value;

            if (methodMetadata.MethodInfo == null)
            {
                throw new MissingMethodException($"Method '{requestDto.MethodName}' not found in service '{requestDto.ManagerName}'.");
            }

            // 3) Authenticate request (establish HttpContext.User)
            await _requestAuthenticator.AuthenticateAsync(request);

            // 4) Authorize method call via metadata (RequireAuthenticated / RequireRole)
            _methodAuthorizer.Authorize(methodMetadata);

            // 5) Prepare parameters
            var typedParameters = PrepareParameters(requestDto.Parameters, methodMetadata.MethodInfo);

            // 6) Invoke the method
            object? result = methodMetadata.MethodInfo.Invoke(serviceInstance, typedParameters);

            // 7) If the method returns a Task, await it
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                var taskType = task.GetType();
                if (taskType.IsGenericType)
                {
                    PropertyInfo? resultProp = taskType.GetProperty("Result");
                    return resultProp?.GetValue(task);
                }
                return null;
            }

            return result;
        }

        private object?[]? PrepareParameters(object[]? rawParams, MethodInfo methodInfo)
        {
            var paramInfos = methodInfo.GetParameters();
            if (paramInfos.Length == 0)
                return null;

            if (rawParams == null || rawParams.Length != paramInfos.Length)
            {
                throw new ArgumentException($"Parameter count mismatch for method '{methodInfo.Name}'.");
            }

            var finalArgs = new object?[paramInfos.Length];
            for (int i = 0; i < paramInfos.Length; i++)
            {
                var paramType = paramInfos[i].ParameterType;
                var paramValue = rawParams[i];
                // Use a JSON round-trip conversion for robust conversion
                var json = JsonSerializer.Serialize(paramValue, _jsonOptions);
                finalArgs[i] = JsonSerializer.Deserialize(json, paramType, _jsonOptions);
            }
            return finalArgs;
        }

        private IManagerService? GetManagerServiceByName(string serviceName)
        {
            var serviceType = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName.StartsWith("App") || a.FullName.StartsWith("Contracts"))
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.Name.Equals(serviceName, StringComparison.OrdinalIgnoreCase)
                    && typeof(IManagerService).IsAssignableFrom(t));

            if (serviceType == null)
                return null;

            return _serviceProvider.GetService(serviceType) as IManagerService;
        }

        private MethodMetadata LoadMethodMetadata(IManagerService serviceInstance, string methodName)
        {
            var methodInfo = serviceInstance.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (methodInfo == null)
                return new MethodMetadata();

            return new MethodMetadata
            {
                MethodInfo = methodInfo,
                RequireAuthenticated = methodInfo.GetCustomAttribute<RequireAuthenticatedAttribute>(),
                RequireRole = methodInfo.GetCustomAttribute<RequireRoleAttribute>()
            };
        }
    }
}
