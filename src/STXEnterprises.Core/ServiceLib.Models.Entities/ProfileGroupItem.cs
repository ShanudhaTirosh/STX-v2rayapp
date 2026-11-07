using System;
using ServiceLib.Enums;
using SQLite;

namespace ServiceLib.Models.Entities;

[Serializable]
[Obsolete("Use ProtocolExtraItem instead.")]
public class ProfileGroupItem
{
	[PrimaryKey]
	public string IndexId { get; set; }

	public string ChildItems { get; set; }

	public string? SubChildItems { get; set; }

	public string? Filter { get; set; }

	public EMultipleLoad MultipleLoad { get; set; }

	public bool NotHasChild()
	{
		if (string.IsNullOrWhiteSpace(ChildItems))
		{
			return string.IsNullOrWhiteSpace(SubChildItems);
		}
		return false;
	}
}
