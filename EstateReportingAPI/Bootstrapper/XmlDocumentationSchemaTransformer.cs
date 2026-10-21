using System.Collections.Concurrent;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EstateReportingAPI.Bootstrapper;

/// <summary>
/// Applies XML summaries to OpenAPI schemas because the API uses the versioned
/// AddOpenApi registration, which does not run the ASP.NET Core XML-doc source generator.
/// </summary>
internal static class XmlDocumentationSchemaTransformer
{
    private static readonly ConcurrentDictionary<Assembly, IReadOnlyDictionary<string, string>> DocumentationByAssembly = new();

    public static void Apply(OpenApiSchema schema, OpenApiSchemaTransformerContext context)
    {
        Type? type = context.JsonTypeInfo?.Type;
        if (type is null)
        {
            return;
        }

        MemberInfo? member = context.JsonPropertyInfo?.AttributeProvider as MemberInfo;
        Assembly documentationAssembly = GetDocumentationAssembly(member, type);
        IReadOnlyDictionary<string, string> documentation = DocumentationByAssembly.GetOrAdd(documentationAssembly, LoadDocumentation);
        string? memberId = GetDocumentationMemberId(member, type);

        if (memberId is not null && documentation.TryGetValue(memberId, out string? summary))
        {
            schema.Description = summary;
        }
    }

    private static Assembly GetDocumentationAssembly(MemberInfo? member, Type type)
    {
        return member?.DeclaringType?.Assembly ?? type.Assembly;
    }

    private static string? GetDocumentationMemberId(MemberInfo? member, Type type)
    {
        return member is not null ? GetMemberId(member) : $"T:{GetTypeName(type)}";
    }

    private static IReadOnlyDictionary<string, string> LoadDocumentation(Assembly assembly)
    {
        string? assemblyPath = assembly.Location;
        if (string.IsNullOrEmpty(assemblyPath))
        {
            return new Dictionary<string, string>();
        }

        string documentationPath = Path.ChangeExtension(assemblyPath, ".xml");
        if (!File.Exists(documentationPath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return XDocument.Load(documentationPath)
                .Descendants("member")
                .Select(element => new
                {
                    Id = (string?)element.Attribute("name"),
                    Summary = NormalizeSummary(element.Element("summary"))
                })
                .Where(member => !string.IsNullOrWhiteSpace(member.Id) && !string.IsNullOrWhiteSpace(member.Summary))
                .ToDictionary(member => member.Id!, member => member.Summary!, StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Xml.XmlException or ArgumentException)
        {
            return new Dictionary<string, string>();
        }
    }

    private static string? GetMemberId(MemberInfo member)
    {
        return member switch
        {
            PropertyInfo property => $"P:{GetTypeName(property.DeclaringType!)}.{property.Name}",
            FieldInfo field => $"F:{GetTypeName(field.DeclaringType!)}.{field.Name}",
            _ => null
        };
    }

    private static string GetTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            string genericName = type.GetGenericTypeDefinition().FullName!;
            int aritySeparator = genericName.IndexOf('`');
            string baseName = aritySeparator < 0 ? genericName : genericName[..aritySeparator];
            return $"{baseName}{{{string.Join(",", type.GetGenericArguments().Select(GetTypeName))}}}";
        }

        return type.FullName!.Replace('+', '.');
    }

    private static string? NormalizeSummary(XElement? summary)
    {
        string text = string.Join(" ", summary?.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? []);
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

