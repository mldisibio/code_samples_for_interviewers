using Newtonsoft.Json;

namespace contoso.utility.nullsafedynamic;

/// <summary>Converts an <see cref="NullSafeDynamic" /> to and from JSON.</summary>
/// <remarks>Copied from internals of <see cref="T:Newtonsoft.Json.Converters.ExpandoObjectConverter"/></remarks>
public class NullSafeDynamicConverter : JsonConverter
{
    /// <summary>Gets a value indicating whether this <see cref="T:Newtonsoft.Json.JsonConverter"/> can write JSON.</summary>
    public override bool CanWrite { get { return false; } }

    /// <summary>Determines whether this instance can convert the specified object type.</summary>
    public override bool CanConvert(Type objectType)
    {
        return objectType.Equals(typeof(NullSafeDynamic));
    }

    /// <summary>Reads the JSON representation of the object.</summary>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        return this.ReadValue(reader);
    }

    object ReadList(JsonReader reader)
    {
        IList<object?> objectList = new List<object?>();
        while (reader.Read())
        {
            JsonToken tokenType = reader.TokenType;
            if (tokenType == JsonToken.Comment)
                continue;
            if (tokenType == JsonToken.EndArray)
                return objectList;
            objectList.Add(this.ReadValue(reader));
        }
        throw GetSerializationException("Unexpected end when reading ExpandoObject.", reader);
    }

    object? ReadObject(JsonReader reader)
    {
        IDictionary<string, object?> expandoMembers = new NullSafeDynamic();
        while (reader.Read())
        {
            JsonToken tokenType = reader.TokenType;
            if (tokenType == JsonToken.PropertyName)
            {
                string? memberKey = reader.Value?.ToString();
                if (memberKey == null || !reader.Read())
                {
                    throw GetSerializationException("Unexpected end when reading ExpandoObject.", reader);
                }
                expandoMembers[memberKey] = this.ReadValue(reader);
            }
            else
            {
                if (tokenType == JsonToken.Comment)
                    continue;
                if (tokenType == JsonToken.EndObject)
                    return expandoMembers;
            }
        }
        throw GetSerializationException("Unexpected end when reading ExpandoObject.", reader);
    }

    object? ReadValue(JsonReader reader)
    {
        if (!MoveReaderToContent(reader))
        {
            throw GetSerializationException("Unexpected end when reading ExpandoObject.", reader);
        }
        JsonToken tokenType = reader.TokenType;
        if (tokenType == JsonToken.StartObject)
            return this.ReadObject(reader);
        if (tokenType == JsonToken.StartArray)
            return this.ReadList(reader);
        //if (!JsonTokenUtils.IsPrimitiveToken(reader.TokenType))
        //{
        //    throw GetSerializationException(String.Format("Unexpected token when converting ExpandoObject: {0}", reader.TokenType.ToString()), reader);
        //}
        return reader.Value;
    }


    bool MoveReaderToContent(JsonReader reader)
    {
        for (JsonToken i = reader.TokenType; i == JsonToken.None || i == JsonToken.Comment; i = reader.TokenType)
        {
            if (!reader.Read())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Writes the JSON representation of the object.</summary>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    JsonSerializationException GetSerializationException(string message, JsonReader reader)
    {
        int lineNumber = 0;
        int linePosition = 0;
        if(reader is IJsonLineInfo lineInfo && lineInfo.HasLineInfo())
        {
            lineNumber = lineInfo.LineNumber;
            linePosition = lineInfo.LinePosition;
        }
        return new JsonSerializationException(message, reader.Path, lineNumber, linePosition, null);

    }
}
