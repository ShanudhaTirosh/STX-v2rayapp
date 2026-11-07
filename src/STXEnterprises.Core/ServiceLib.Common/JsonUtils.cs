using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ServiceLib.Common;

public class JsonUtils
{
	private static readonly string _tag = "JsonUtils";

	private static readonly JsonSerializerOptions _defaultDeserializeOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip
	};

	private static readonly JsonSerializerOptions _defaultSerializeOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private static readonly JsonSerializerOptions _defaultSerializeNoIndentedOptions = new JsonSerializerOptions
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private static readonly JsonSerializerOptions _nullValueSerializeOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private static readonly JsonSerializerOptions _nullValueSerializeNoIndentedOptions = new JsonSerializerOptions
	{
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
	};

	private static readonly JsonDocumentOptions _defaultDocumentOptions = new JsonDocumentOptions
	{
		CommentHandling = JsonCommentHandling.Skip
	};

	public static T? DeepCopy<T>(T? obj)
	{
		if (obj == null)
		{
			return default;
		}
		return Deserialize<T>(Serialize(obj, indented: false));
	}

	public static T? Deserialize<T>(string? strJson)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(strJson))
			{
				return default;
			}
			return JsonSerializer.Deserialize<T>(strJson, _defaultDeserializeOptions);
		}
		catch
		{
			return default;
		}
	}

	public static JsonNode? ParseJson(string? strJson)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(strJson))
			{
				return null;
			}
			return JsonNode.Parse(strJson, null, _defaultDocumentOptions);
		}
		catch
		{
			return null;
		}
	}

	public static string Serialize(object? obj, bool indented = true, bool nullValue = false)
	{
		string result = string.Empty;
		try
		{
			if (obj == null)
			{
				return result;
			}
			JsonSerializerOptions jsonSerializerOptions;
			if (nullValue)
			{
				jsonSerializerOptions = ((!indented) ? _nullValueSerializeNoIndentedOptions : _nullValueSerializeOptions);
			}
			else
			{
				jsonSerializerOptions = ((!indented) ? _defaultSerializeNoIndentedOptions : _defaultSerializeOptions);
			}
			JsonSerializerOptions options = jsonSerializerOptions;
			result = JsonSerializer.Serialize(obj, options);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return result;
	}

	public static string Serialize(object? obj, JsonSerializerOptions? options)
	{
		string result = string.Empty;
		try
		{
			if (obj == null)
			{
				return result;
			}
			result = JsonSerializer.Serialize(obj, options ?? _defaultSerializeOptions);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return result;
	}

	public static JsonNode? SerializeToNode(object? obj, JsonSerializerOptions? options = null)
	{
		return JsonSerializer.SerializeToNode(obj, options);
	}
}
