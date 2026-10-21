using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json;

namespace EstateReportingAPI.Bootstrapper;

/// <summary>
/// Adds representative request and response payloads to the generated OpenAPI document.
/// </summary>
internal static class OpenApiExamplesTransformer
{
    public static void Apply(OpenApiOperation operation, OpenApiOperationTransformerContext context)
    {
        AddParameterExamples(operation, context);
        AddRequestBodyExample(operation, context);
        AddSuccessResponseExample(operation, context);

        string path = context.Description.RelativePath?.TrimEnd('/') ?? string.Empty;
        if (path.EndsWith("/transactiondetailreport", StringComparison.OrdinalIgnoreCase))
        {
            SetRequestExample(operation, """
                {
                  "start_date": "2026-10-01",
                  "end_date": "2026-10-21",
                  "operators": [12],
                  "merchants": [1042],
                  "products": [7],
                  "page_number": 1,
                  "page_size": 50
                }
                """);
            SetResponseExample(operation, """
                {
                  "transactions": [
                    {
                      "id": "c6e8c2ba-bb6d-4fe5-aad9-62444fc13a70",
                      "date_time": "2026-10-21T10:32:15+01:00",
                      "merchant": "Central Market",
                      "merchant_id": "ae24b06c-ef12-4c12-a1f7-73112ba18a31",
                      "merchant_reporting_id": 1042,
                      "operator": "Example Operator",
                      "operator_id": "6942e4b8-e2ec-45f7-bd89-d9c75ac27c1f",
                      "operator_reporting_id": 12,
                      "product": "Lunch special",
                      "product_id": "e2b72da1-50dc-49b9-91ae-992a01b98164",
                      "product_reporting_id": 7,
                      "type": "Sale",
                      "status": "Authorised",
                      "value": 18.5,
                      "total_fees": 0.42,
                      "settlement_reference": "SET-20261021-0042",
                      "transaction_number": 42018
                    }
                  ],
                  "summary": {
                    "total_value": 18.5,
                    "total_fees": 0.42,
                    "transaction_count": 1
                  },
                  "pagination": {
                    "page_number": 1,
                    "page_size": 50,
                    "total_items": 1,
                    "total_pages": 1
                  }
                }
                """);
        }
        else if (path.EndsWith("/transactionmixsummary", StringComparison.OrdinalIgnoreCase))
        {
            SetRequestExample(operation, """
                {
                  "merchant_reporting_id": 1042,
                  "start_date": "2026-10-01",
                  "end_date": "2026-10-21",
                  "breakdown": "Product",
                  "measure": "Value",
                  "top_n": 5,
                  "page_number": 1,
                  "page_size": 50
                }
                """);
            SetResponseExample(operation, """
                {
                  "from_date": "2026-10-01",
                  "to_date": "2026-10-21",
                  "breakdown": "Product",
                  "measure": "Value",
                  "total_count": 83,
                  "total_value": 1274.5,
                  "groups": [
                    {
                      "group_key": "7",
                      "group_name": "Lunch special",
                      "transaction_count": 31,
                      "transaction_value": 542.5
                    }
                  ],
                  "transactions": [],
                  "pagination": {
                    "page_number": 1,
                    "page_size": 50,
                    "total_items": 1,
                    "total_pages": 1
                  }
                }
                """);
        }
    }

    private static void AddParameterExamples(OpenApiOperation operation, OpenApiOperationTransformerContext context)
    {
        if (operation.Parameters is not { } parameters)
        {
            return;
        }

        foreach (OpenApiParameter parameter in parameters)
        {
            if (parameter.Example is not null)
            {
                continue;
            }

            ApiParameterDescription? description = context.Description.ParameterDescriptions
                .FirstOrDefault(item => string.Equals(item.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            Type? parameterType = description?.ModelMetadata?.ModelType;
            if (parameterType is not null)
            {
                parameter.Example = CreateExample(parameterType, parameter.Name);
            }
        }
    }

    private static void AddRequestBodyExample(OpenApiOperation operation, OpenApiOperationTransformerContext context)
    {
        if (operation.RequestBody is null)
        {
            return;
        }

        Type? bodyType = context.Description.ParameterDescriptions
            .FirstOrDefault(parameter => parameter.Source?.Id == "Body")?
            .ModelMetadata?.ModelType;
        if (bodyType is null || !operation.RequestBody.Content.TryGetValue("application/json", out OpenApiMediaType? mediaType))
        {
            return;
        }

        mediaType.Example ??= CreateExample(bodyType, "request");
    }

    private static void AddSuccessResponseExample(OpenApiOperation operation, OpenApiOperationTransformerContext context)
    {
        ApiResponseType? successResponse = context.Description.SupportedResponseTypes
            .FirstOrDefault(response => response.StatusCode == StatusCodes.Status200OK && response.Type is not null);
        if (successResponse?.Type is null
            || !operation.Responses.TryGetValue(StatusCodes.Status200OK.ToString(), out IOpenApiResponse response))
        {
            return;
        }

        if (response.Content.TryGetValue("application/json", out OpenApiMediaType? mediaType))
        {
            mediaType.Example ??= CreateExample(successResponse.Type, "response");
        }
    }

    private static JsonNode? CreateExample(Type type, string propertyName, int depth = 0)
    {
        Type? nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType is not null)
        {
            type = nullableType;
        }

        if (type == typeof(string))
        {
            string name = propertyName.ToLowerInvariant();
            string value = name switch
            {
                var n when n.Contains("email") => "alex@example.com",
                var n when n.Contains("status") => "Authorised",
                var n when n.Contains("type") => "Sale",
                var n when n.Contains("reference") => "REF-20261021-0042",
                var n when n.Contains("description") => "Example description",
                var n when n.Contains("name") || n.Contains("product") || n.Contains("merchant") || n.Contains("operator") => "Example " + propertyName,
                var n when n.Contains("address") || n.Contains("town") || n.Contains("region") || n.Contains("country") || n.Contains("postcode") => "Example value",
                _ => "Example value"
            };
            return JsonValue.Create(value);
        }

        if (type == typeof(Guid))
        {
            return JsonValue.Create("c6e8c2ba-bb6d-4fe5-aad9-62444fc13a70");
        }

        if (type == typeof(DateOnly))
        {
            return JsonValue.Create("2026-10-21");
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return JsonValue.Create("2026-10-21T10:32:15+01:00");
        }

        if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
        {
            return JsonValue.Create("09:00:00");
        }

        if (type == typeof(bool))
        {
            return JsonValue.Create(true);
        }

        if (type.IsEnum)
        {
            string enumValue = propertyName.Contains("measure", StringComparison.OrdinalIgnoreCase)
                ? "Value"
                : Enum.GetNames(type).FirstOrDefault() ?? "Unknown";
            return JsonValue.Create(enumValue);
        }

        if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
        {
            return JsonValue.Create(18.50m);
        }

        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
            || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong))
        {
            string name = propertyName.ToLowerInvariant();
            long value = name.Contains("page") && name.Contains("size") ? 50
                : name.Contains("page") && name.Contains("number") ? 1
                : name.Contains("year") ? 2026
                : name.Contains("hour") ? 10
                : name.Contains("count") || name.Contains("total") ? 3
                : 1001;
            return JsonValue.Create(value);
        }

        if (type.IsArray)
        {
            return new JsonArray(CreateExample(type.GetElementType()!, propertyName, depth + 1));
        }

        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
        {
            return new JsonArray(CreateExample(type.GetGenericArguments()[0], propertyName, depth + 1));
        }

        if (depth >= 5 || type.IsAbstract || type.IsInterface)
        {
            return null;
        }

        JsonObject result = new();
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
        {
            string jsonName = JsonNamingPolicy.SnakeCaseLower.ConvertName(property.Name);
            result[jsonName] = CreateExample(property.PropertyType, property.Name, depth + 1);
        }

        return result;
    }

    private static void SetRequestExample(OpenApiOperation operation, string json)
    {
        if (operation.RequestBody?.Content.TryGetValue("application/json", out OpenApiMediaType? mediaType) == true)
        {
            mediaType.Example = JsonNode.Parse(json);
        }
    }

    private static void SetResponseExample(OpenApiOperation operation, string json)
    {
        if (operation.Responses.TryGetValue("200", out IOpenApiResponse response)
            && response.Content.TryGetValue("application/json", out OpenApiMediaType? mediaType))
        {
            mediaType.Example = JsonNode.Parse(json);
        }
    }
}

