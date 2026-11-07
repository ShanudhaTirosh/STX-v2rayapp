using ServiceLib.Enums;

namespace ServiceLib.Models.Dto;

public class CheckUpdateModel
{
	public bool? IsSelected { get; set; }

	public ECoreType? CoreType { get; set; }

	public string? Remarks { get; set; }

	public string? FileName { get; set; }

	public bool? IsFinished { get; set; }

	public bool IsGeoFile { get; set; }

	public string CoreTypeForStorage
	{
		get
		{
			string text;
			if (!IsGeoFile)
			{
				text = CoreType?.ToString();
				if (text == null)
				{
					return "";
				}
			}
			else
			{
				text = "GeoFiles";
			}
			return text;
		}
	}
}
