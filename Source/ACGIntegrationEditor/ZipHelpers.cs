using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ACGIntegrationEditor;

public static class ZipHelpers
{
        public static async Task ExtractToDirectoryAsync(
        string pathZip,
        string pathDestination,
        IProgress<float> progress,
        CancellationToken cancellationToken = default)
    {
        using var archive = ZipFile.OpenRead(pathZip);
        var totalLength = archive.Entries.Sum(entry => entry.Length);
        long currentProgression = 0;
        foreach (var entry in archive.Entries)
        {
            // Check if entry is a folder
            string filePath = Path.Combine(pathDestination, entry.FullName);
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(filePath);
                continue;
            }

            // Create folder anyway since a folder may not have an entry
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await using var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (var entryStream = entry.Open())
            {
                if (progress is null)
                {
                    await entryStream.CopyToAsync(file, cancellationToken);
                }
                else
                {
                    var progression = currentProgression;
                    var relativeProgress = new Progress<long>(fileProgressBytes =>
                        progress.Report((float)(fileProgressBytes + progression) / totalLength));
                    await entryStream.CopyToAsync(file, 81920, relativeProgress, cancellationToken);
                }
            }

            currentProgression += entry.Length;
        }
    }

    public static async Task ExtractToDirectoryAsync(
        Stream zipStream,
        string pathDestination,
        IProgress<float> progress,
        CancellationToken cancellationToken = default)
    {
        using var archive = new ZipArchive(zipStream);
        var totalLength = archive.Entries.Sum(entry => entry.Length);
        long currentProgression = 0;
        foreach (var entry in archive.Entries)
        {
            // Check if entry is a folder
            string filePath = Path.Combine(pathDestination, entry.FullName);
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\'))
            {
                Directory.CreateDirectory(filePath);
                continue;
            }

            // Create folder anyway since a folder may not have an entry
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await using var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using (var entryStream = entry.Open())
            {
                if (progress is null)
                {
                    await entryStream.CopyToAsync(file, cancellationToken);
                }
                else
                {
                    var progression = currentProgression;
                    var relativeProgress = new Progress<long>(fileProgressBytes =>
                        progress.Report((float)(fileProgressBytes + progression) / totalLength));
                    await entryStream.CopyToAsync(file, 81920, relativeProgress, cancellationToken);
                }
            }

            currentProgression += entry.Length;
        }
    }
}