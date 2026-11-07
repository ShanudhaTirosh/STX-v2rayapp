using System;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ServiceLib.Common;

public class YamlUtils
{
	private static readonly string _tag = "YamlUtils";

	public static T FromYaml<T>(string str)
	{
		IDeserializer deserializer = new DeserializerBuilder().WithNamingConvention(PascalCaseNamingConvention.Instance).Build();
		try
		{
			return deserializer.Deserialize<T>(str);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return deserializer.Deserialize<T>("");
		}
	}

	public static string ToYaml(object? obj)
	{
		string result = string.Empty;
		if (obj == null)
		{
			return result;
		}
		ISerializer serializer = new SerializerBuilder().WithNamingConvention(HyphenatedNamingConvention.Instance).Build();
		try
		{
			result = serializer.Serialize(obj);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return result;
	}

	public static string? PreprocessYaml(string str)
	{
		try
		{
			MergingParser parser = new MergingParser(new Parser(new StringReader(str)));
			return ToYaml(new DeserializerBuilder().Build().Deserialize(parser));
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return null;
		}
	}
}
