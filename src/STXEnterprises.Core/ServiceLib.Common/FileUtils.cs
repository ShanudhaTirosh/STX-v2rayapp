using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLib.Common;

public static class FileUtils
{
	private static readonly string _tag = "FileManager";

	public static bool ByteArrayToFile(string fileName, byte[] content)
	{
		try
		{
			File.WriteAllBytes(fileName, content);
			return true;
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
		return false;
	}

	public static void DecompressFile(string fileName, byte[] content)
	{
		try
		{
			using FileStream destination = File.Create(fileName);
			using GZipStream gZipStream = new GZipStream(new MemoryStream(content), CompressionMode.Decompress, leaveOpen: false);
			gZipStream.CopyTo(destination);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	public static void DecompressFile(string fileName, string toPath, string? toName)
	{
		try
		{
			using FileStream stream = new FileInfo(fileName).OpenRead();
			using FileStream destination = File.Create((toName != null) ? Path.Combine(toPath, toName) : toPath);
			using GZipStream gZipStream = new GZipStream(stream, CompressionMode.Decompress);
			gZipStream.CopyTo(destination);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	public static void DecompressTarFile(string fileName, string toPath)
	{
		try
		{
			using FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			using GZipStream source = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
			TarFile.ExtractToDirectory(source, toPath, overwriteFiles: true);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
		}
	}

	public static string NonExclusiveReadAllText(string path)
	{
		return NonExclusiveReadAllText(path, Encoding.Default);
	}

	private static string NonExclusiveReadAllText(string path, Encoding encoding)
	{
		try
		{
			using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using StreamReader streamReader = new StreamReader(stream, encoding);
			return streamReader.ReadToEnd();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			throw;
		}
	}

	public static bool ZipExtractToFile(string fileName, string toPath, string ignoredName)
	{
		try
		{
			using ZipArchive zipArchive = ZipFile.OpenRead(fileName);
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				if (entry.Length == 0L)
				{
					continue;
				}
				try
				{
					if (!ignoredName.IsNotEmpty() || !entry.Name.Contains(ignoredName))
					{
						entry.ExtractToFile(Path.Combine(toPath, entry.Name), overwrite: true);
					}
				}
				catch (IOException ex)
				{
					Logging.SaveLog(_tag, ex);
				}
			}
		}
		catch (Exception ex2)
		{
			Logging.SaveLog(_tag, ex2);
			return false;
		}
		return true;
	}

	public static List<string>? GetFilesFromZip(string fileName)
	{
		if (!File.Exists(fileName))
		{
			return null;
		}
		try
		{
			using ZipArchive zipArchive = ZipFile.OpenRead(fileName);
			return zipArchive.Entries.Select((ZipArchiveEntry entry) => entry.FullName).ToList();
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return null;
		}
	}

	public static bool CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
	{
		try
		{
			if (File.Exists(destinationArchiveFileName))
			{
				File.Delete(destinationArchiveFileName);
			}
			ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, CompressionLevel.SmallestSize, includeBaseDirectory: true);
		}
		catch (Exception ex)
		{
			Logging.SaveLog(_tag, ex);
			return false;
		}
		return true;
	}

	public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive, bool overwrite, string? ignoredName = null)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDir);
		if (!directoryInfo.Exists)
		{
			throw new DirectoryNotFoundException("Source directory not found: " + directoryInfo.FullName);
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories();
		Directory.CreateDirectory(destinationDir);
		FileInfo[] files = directoryInfo.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			if ((!ignoredName.IsNotEmpty() || !fileInfo.Name.Contains(ignoredName)) && !(fileInfo.Extension == fileInfo.Name))
			{
				string text = Path.Combine(destinationDir, fileInfo.Name);
				if (overwrite || !File.Exists(text))
				{
					fileInfo.CopyTo(text, overwrite);
				}
			}
		}
		if (recursive)
		{
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo directoryInfo2 in array)
			{
				string destinationDir2 = Path.Combine(destinationDir, directoryInfo2.Name);
				CopyDirectory(directoryInfo2.FullName, destinationDir2, recursive: true, overwrite, ignoredName);
			}
		}
	}

	public static void DeleteExpiredFiles(string sourceDir, DateTime dtLine)
	{
		try
		{
			string[] files = Directory.GetFiles(sourceDir, "*.*");
			for (int i = 0; i < files.Length; i++)
			{
				FileInfo fileInfo = new FileInfo(files[i]);
				if (!(fileInfo.CreationTime >= dtLine))
				{
					fileInfo.Delete();
				}
			}
		}
		catch
		{
		}
	}

	public static async Task<string> CreateLinuxShellFile(string fileName, string contents, bool overwrite)
	{
		string shFilePath = Utils.GetBinConfigPath(fileName);
		if (!overwrite && File.Exists(shFilePath))
		{
			return shFilePath;
		}
		File.Delete(shFilePath);
		await File.WriteAllTextAsync(shFilePath, contents);
		await Utils.SetLinuxChmod(shFilePath);
		return shFilePath;
	}
}
