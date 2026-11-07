using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLib.Common;

/// <summary>
/// Crash-safe file writes. The content goes to a temp file in the same folder and then replaces the
/// target in one step, so a power cut or crash can never leave a half-written settings/config file
/// (File.WriteAllText truncates first, which is how configs end up empty or corrupt).
/// </summary>
public static class AtomicFile
{
	private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

	public static void WriteAllText(string path, string content)
	{
		string full = Path.GetFullPath(path);
		string dir = Path.GetDirectoryName(full)!;
		Directory.CreateDirectory(dir);
		string tmp = Path.Combine(dir, Path.GetFileName(full) + "." + Guid.NewGuid().ToString("N") + ".tmp");
		try
		{
			using (FileStream fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
			{
				byte[] bytes = Utf8NoBom.GetBytes(content);
				fs.Write(bytes, 0, bytes.Length);
				fs.Flush(flushToDisk: true);
			}
			if (File.Exists(full))
			{
				File.Replace(tmp, full, null, ignoreMetadataErrors: true);
			}
			else
			{
				File.Move(tmp, full);
			}
		}
		catch
		{
			try
			{
				if (File.Exists(tmp))
				{
					File.Delete(tmp);
				}
			}
			catch
			{
			}
			throw;
		}
	}

	public static Task WriteAllTextAsync(string path, string content)
	{
		return Task.Run(() => WriteAllText(path, content));
	}
}
