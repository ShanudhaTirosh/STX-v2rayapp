using System;
using System.Linq;
using ServiceLib.Common;

namespace ServiceLib.Models.Dto;

public class SemanticVersion
{
	private readonly int major;

	private readonly int minor;

	private readonly int patch;

	private readonly string version;

	public SemanticVersion(int major, int minor, int patch)
	{
		this.major = major;
		this.minor = minor;
		this.patch = patch;
		version = $"{major}.{minor}.{patch}";
	}

	public SemanticVersion(string? version)
	{
		try
		{
			if (string.IsNullOrEmpty(version))
			{
				major = 0;
				minor = 0;
				patch = 0;
				return;
			}
			this.version = version.RemovePrefix('v');
			string[] array = this.version.Split('.');
			if (array.Length == 2)
			{
				major = int.Parse(array.First());
				minor = int.Parse(array.Last());
				patch = 0;
				return;
			}
			int num = array.Length;
			if ((uint)(num - 3) <= 1u)
			{
				major = int.Parse(array[0]);
				minor = int.Parse(array[1]);
				patch = int.Parse(array[2]);
				return;
			}
			throw new ArgumentException("Invalid version string");
		}
		catch
		{
			major = 0;
			minor = 0;
			patch = 0;
		}
	}

	public override bool Equals(object? obj)
	{
		if (obj is SemanticVersion semanticVersion)
		{
			if (major == semanticVersion.major && minor == semanticVersion.minor)
			{
				return patch == semanticVersion.patch;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return major.GetHashCode() ^ minor.GetHashCode() ^ patch.GetHashCode();
	}

	public override string ToString()
	{
		return version;
	}

	public string ToVersionString(string? prefix = null)
	{
		if (prefix == null)
		{
			return version;
		}
		return prefix + version;
	}

	public static bool operator ==(SemanticVersion v1, SemanticVersion v2)
	{
		return v1.Equals(v2);
	}

	public static bool operator !=(SemanticVersion v1, SemanticVersion v2)
	{
		return !v1.Equals(v2);
	}

	public static bool operator >=(SemanticVersion v1, SemanticVersion v2)
	{
		return v1.GreaterEquals(v2);
	}

	public static bool operator <=(SemanticVersion v1, SemanticVersion v2)
	{
		return v1.LessEquals(v2);
	}

	private bool GreaterEquals(SemanticVersion other)
	{
		if (major < other.major)
		{
			return false;
		}
		if (major > other.major)
		{
			return true;
		}
		if (minor < other.minor)
		{
			return false;
		}
		if (minor > other.minor)
		{
			return true;
		}
		if (patch < other.patch)
		{
			return false;
		}
		_ = patch;
		_ = other.patch;
		return true;
	}

	private bool LessEquals(SemanticVersion other)
	{
		if (major < other.major)
		{
			return true;
		}
		if (major > other.major)
		{
			return false;
		}
		if (minor < other.minor)
		{
			return true;
		}
		if (minor > other.minor)
		{
			return false;
		}
		if (patch < other.patch)
		{
			return true;
		}
		if (patch > other.patch)
		{
			return false;
		}
		return true;
	}
}
