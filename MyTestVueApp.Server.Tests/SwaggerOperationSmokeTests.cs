using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MyTestVueApp.Server.Tests
{
    public class SwaggerOperationSmokeTests : IClassFixture<PixelPainterApiFactory>
    {
        private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "delete", "get", "head", "options", "patch", "post", "put", "trace"
        };

        private readonly PixelPainterApiFactory _factory;

        public SwaggerOperationSmokeTests(PixelPainterApiFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task EverySwaggerOperation_IsReachable_WithoutServerErrors()
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using var swaggerResponse = await client.GetAsync("/swagger/v1/swagger.json");
            swaggerResponse.EnsureSuccessStatusCode();
            using var swagger = JsonDocument.Parse(await swaggerResponse.Content.ReadAsStreamAsync());

            var failures = new List<string>();
            var operationCount = 0;

            foreach (var path in swagger.RootElement.GetProperty("paths").EnumerateObject())
            {
                foreach (var operation in path.Value.EnumerateObject())
                {
                    if (!HttpMethods.Contains(operation.Name))
                    {
                        continue;
                    }

                    operationCount++;
                    var request = BuildRequest(operation.Name, path.Name, operation.Value);
                    using var response = await client.SendAsync(request);

                    if ((int)response.StatusCode >= 500)
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        failures.Add($"{operation.Name.ToUpperInvariant()} {request.RequestUri}: {(int)response.StatusCode} {body}");
                    }
                }
            }

            Assert.True(operationCount >= 76, $"Expected at least 76 Swagger operations, but found {operationCount}.");
            Assert.True(failures.Count == 0, "Swagger operations returned server errors:\n" + string.Join("\n", failures));
        }

        private static HttpRequestMessage BuildRequest(string method, string path, JsonElement operation)
        {
            var query = new List<string>();

            if (operation.TryGetProperty("parameters", out var parameters))
            {
                foreach (var parameter in parameters.EnumerateArray())
                {
                    var name = parameter.GetProperty("name").GetString()!;
                    var location = parameter.GetProperty("in").GetString();
                    var value = GetExampleValue(parameter.GetProperty("schema"));

                    if (location == "path")
                    {
                        path = path.Replace($"{{{name}}}", Uri.EscapeDataString(value), StringComparison.Ordinal);
                    }
                    else if (location == "query")
                    {
                        query.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
                    }
                }
            }

            path = Regex.Replace(path, "\\{[^}]+\\}", "1");
            if (query.Count > 0)
            {
                path += "?" + string.Join("&", query);
            }

            var request = new HttpRequestMessage(new HttpMethod(method), path);
            if (operation.TryGetProperty("requestBody", out var requestBody))
            {
                request.Content = BuildBody(requestBody);
            }

            return request;
        }

        private static HttpContent BuildBody(JsonElement requestBody)
        {
            if (!requestBody.TryGetProperty("content", out var content))
            {
                return JsonContent.Create(new { });
            }

            if (content.TryGetProperty("application/json", out _))
            {
                return new StringContent("{}", Encoding.UTF8, "application/json");
            }

            if (content.TryGetProperty("multipart/form-data", out _))
            {
                return new MultipartFormDataContent();
            }

            return JsonContent.Create(new { });
        }

        private static string GetExampleValue(JsonElement schema)
        {
            if (schema.TryGetProperty("type", out var type))
            {
                return type.GetString() switch
                {
                    "boolean" => "false",
                    "integer" or "number" => "1",
                    _ => "swagger-smoke"
                };
            }

            return "1";
        }
    }
}
