using System.Text.Json;
using System.Text.Json.Serialization;

namespace EFCore.DynamicQuery.Filtering;

/// <summary>
/// Deserializes a JSON array of filters by reading each element's "filter" (or "Filter") key
/// and resolving the concrete <see cref="IFilterItem"/> type from a <see cref="FilterTypeRegistry"/>
/// - framework-agnostic, so add an instance to whatever <see cref="JsonSerializerOptions"/> your
/// app already uses, e.g. <c>options.Converters.Add(new FilterJsonConverter(registry))</c>.
/// </summary>
public sealed class FilterJsonConverter(FilterTypeRegistry registry) : JsonConverter<IFilterItem[]>
{
    public override IFilterItem[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array)
            throw new JsonException("Expected an array of filters.");

        var filters = new List<IFilterItem>();

        foreach (var element in root.EnumerateArray())
        {
            if (!element.TryGetProperty("filter", out var keyElement) &&
                !element.TryGetProperty("Filter", out keyElement))
                throw new JsonException("Missing 'filter' property in filter.");

            var key = keyElement.GetString()
                ?? throw new JsonException("'filter' must be a string.");

            var filterType = registry.Resolve(key);

            var filter = (IFilterItem?)element.Deserialize(filterType, options)
                ?? throw new JsonException($"Failed to deserialize filter of type '{filterType.Name}'.");

            filters.Add(filter);
        }

        return [.. filters];
    }

    public override void Write(Utf8JsonWriter writer, IFilterItem[] value, JsonSerializerOptions options)
    {
        // FilterKey is a plain C# property name - it never serializes as "filter" on its own,
        // so the discriminator is written explicitly here rather than left to reflection-based
        // serialization to happen to produce the right JSON property name.
        var filterKeyPropertyName = options.PropertyNamingPolicy?.ConvertName(nameof(IFilterItem.FilterKey))
            ?? nameof(IFilterItem.FilterKey);

        writer.WriteStartArray();

        foreach (var filter in value)
        {
            using var document = JsonSerializer.SerializeToDocument(filter, filter.GetType(), options);

            writer.WriteStartObject();
            writer.WriteString("filter", filter.FilterKey);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (!property.NameEquals(filterKeyPropertyName))
                    property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
