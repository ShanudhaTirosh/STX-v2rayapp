using System;
using System.Runtime.InteropServices;

namespace ServiceLib.Events;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Unit : IEquatable<Unit>
{
	public static Unit Default => default;

	public bool Equals(Unit other)
	{
		return true;
	}

	public override bool Equals(object? obj)
	{
		return obj is Unit;
	}

	public override int GetHashCode()
	{
		return 0;
	}

	public override string ToString()
	{
		return "()";
	}

	public static bool operator ==(Unit left, Unit right)
	{
		return true;
	}

	public static bool operator !=(Unit left, Unit right)
	{
		return false;
	}
}
