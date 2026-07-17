using System.IO.Compression;
using Spectre.Console;

namespace AsepriteInstaller.Utils;

/// <summary>
/// ZIP archive extractor — uses System.IO.Compression which is AOT-compatible.
/// </summary>
public static class ArchiveExtractor
{
    /// <summary>Extract a ZIP file to a destination directory.</summary>
    public static void ExtractZip(string zipPath, string destDir, Logger log)
    {
        Directory.CreateDirectory(destDir);
        log.Info($"Extracting {Path.GetFileName(zipPath)} → {destDir}");

        ZipFile.ExtractToDirectory(zipPath, destDir, overwriteFiles: true);
        log.Info($"Extraction complete.");
    }

    /// <summary>
    /// Extract a ZIP file to a destination directory with a Spectre progress bar.
    /// </summary>
    public static void ExtractZipWithProgress(string zipPath, string destDir, Logger log)
    {
        Directory.CreateDirectory(destDir);
        log.Info($"Extracting {Path.GetFileName(zipPath)} → {destDir}");

        using var archive = ZipFile.OpenRead(zipPath);
        var totalEntries = archive.Entries.Count;
        var done = 0;

        AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
            })
            .Start(ctx =>
            {
                var task = ctx.AddTask("[cyan]Extracting[/]");
                task.MaxValue(totalEntries);

                foreach (var entry in archive.Entries)
                {
                    var destPath = Path.Combine(destDir, entry.FullName);
                    var dir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);

                    if (entry.Length > 0 || !entry.FullName.EndsWith('/'))
                    {
                        entry.ExtractToFile(destPath, overwrite: true);
                    }

                    done++;
                    task.Increment(1);
                }
            });

        log.Info($"Extraction complete ({totalEntries} entries).");
    }
}
