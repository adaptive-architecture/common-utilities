using System.Text.Json;
using AdaptArch.Common.Utilities.Configuration.Contracts;

namespace AdaptArch.Common.Utilities.Configuration.Implementation;

/// <summary>
/// An <see cref="IConfigurationParser"/> implementation that handles JSON data.
/// Based on https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration.Json/src/JsonConfigurationFileParser.cs .
/// </summary>
public class JsonConfigurationParser: IConfigurationParser
{
    private readonly Dictionary<string, string?> _data = new(StringComparer.OrdinalIgnoreCase);
    private readonly Stack<string> _paths = new();
    private readonly string _keyDelimiter;
    private readonly object _parseLock = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="keyDelimiter">The key delimiter.</param>
    public JsonConfigurationParser(string keyDelimiter)
    {
        _keyDelimiter = keyDelimiter ?? throw new ArgumentNullException(nameof(keyDelimiter));
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> Parse(string input)
    {
        lock (_parseLock)
        {
            try
            {
                var jsonDocumentOptions = new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                using (var doc = JsonDocument.Parse(input, jsonDocumentOptions))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new FormatException($"Top-level JSON element must be an object. Instead, '{doc.RootElement.ValueKind}' was found.");
                    }
                    VisitObjectElement(doc.RootElement);
                }

                return _data.ToDictionary(k => k.Key, v => v.Value);
            }
            finally
            {
                _data.Clear();
                _paths.Clear();
            }
        }
    }

    private void VisitObjectElement(JsonElement element)
    {
        var isEmpty = true;

        foreach (var property in element.EnumerateObject())
        {
            isEmpty = false;
            EnterContext(property.Name);
            VisitValue(property.Value);
            ExitContext();
        }

        SetNullIfElementIsEmpty(isEmpty);
    }

    private void VisitArrayElement(JsonElement element)
    {
        var index = 0;

        foreach (var arrayElement in element.EnumerateArray())
        {
            EnterContext(index.ToString());
            VisitValue(arrayElement);
            ExitContext();
            index++;
        }

        SetNullIfElementIsEmpty(isEmpty: index == 0);
    }

    private void SetNullIfElementIsEmpty(bool isEmpty)
    {
        if (isEmpty && _paths.Count > 0)
        {
            _data[_paths.Peek()] = null;
        }
    }

    private void VisitValue(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                VisitObjectElement(value);
                break;

            case JsonValueKind.Array:
                VisitArrayElement(value);
                break;

            case JsonValueKind.Number:
            case JsonValueKind.String:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                var key = _paths.Peek();
                if (_data.ContainsKey(key))
                {
                    throw new FormatException($"A duplicate key '{key}' was found.");
                }
                _data[key] = value.ToString();
                break;

            default:
                throw new FormatException($"Unsupported JSON token '{value.ValueKind}' was found.");
        }
    }

    private void EnterContext(string context) =>
        _paths.Push(_paths.Count > 0
            ? _paths.Peek() + _keyDelimiter + context
            : context);

    private void ExitContext() => _paths.Pop();
}
