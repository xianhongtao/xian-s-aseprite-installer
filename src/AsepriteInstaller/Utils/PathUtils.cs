namespace AsepriteInstaller.Utils;

/// <summary>Filesystem path utilities.</summary>
public static class PathUtils
{
    /// <summary>
    /// Recursively delete a directory if it exists. Tolerates locked files
    /// by retrying a few times.
    /// </summary>
    public static void DeleteDirectorySafe(string path, int retries = 3)
    {
        if (!Directory.Exists(path)) return;

        for (int i = 0; i < retries; i++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException)
            {
                Thread.Sleep(500);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(500);
            }
        }

        // Last attempt — let it throw.
        Directory.Delete(path, recursive: true);
    }

    /// <summary>Move a directory atomically (rename). Falls back to copy+delete.</summary>
    public static void MoveDirectory(string source, string dest)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException($"Source not found: {source}");

        // Try direct rename first (atomic on same volume).
        try
        {
            if (Directory.Exists(dest))
                DeleteDirectorySafe(dest);
            Directory.Move(source, dest);
            return;
        }
        catch (Exception)
        {
            // Fall through to copy+delete.
        }

        // Fallback: copy then delete.
        CopyDirectory(source, dest);
        DeleteDirectorySafe(source);
    }

    /// <summary>Recursively copy a directory.</summary>
    public static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(dest, rel);
            var dir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Copy(file, destFile, overwrite: true);
        }
    }

    /// <summary>Check available disk space in bytes for a given path.</summary>
    public static long GetAvailableDiskSpace(string path)
    {
        var root = Path.GetPathRoot(path) ?? path;
        var drive = new DriveInfo(root);
        return drive.AvailableFreeSpace;
    }

    /// <summary>Get the directory size in bytes.</summary>
    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        return new DirectoryInfo(path)
            .EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(f => f.Length);
    }
}
