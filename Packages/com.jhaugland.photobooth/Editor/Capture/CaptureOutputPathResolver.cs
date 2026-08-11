using System;
using System.IO;
using System.Linq;
using Photobooth.Editor.Configuration;
using UnityEngine;

namespace Photobooth.Editor.Capture
{
    internal readonly struct CaptureFilePlan
    {
        internal string Path { get; }
        internal bool ShouldCapture { get; }

        internal CaptureFilePlan(string path, bool shouldCapture)
        {
            Path = path;
            ShouldCapture = shouldCapture;
        }
    }

    internal static class CaptureOutputPathResolver
    {
        internal static string ResolveOutputDirectory(PhotoboothProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile.OutputPathMode == OutputPathMode.Absolute)
            {
                if (string.IsNullOrWhiteSpace(profile.AbsoluteOutputPath) ||
                    !Path.IsPathFullyQualified(profile.AbsoluteOutputPath))
                {
                    throw new InvalidOperationException(
                        "The profile requires a fully qualified absolute output path.");
                }

                return Path.GetFullPath(profile.AbsoluteOutputPath);
            }

            string relativePath = profile.ProjectRelativeOutputPath;
            if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathFullyQualified(relativePath))
            {
                throw new InvalidOperationException(
                    "The profile requires an output path relative to the Unity project.");
            }

            string projectRoot = GetProjectRoot();
            string outputPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            if (!IsPathInside(projectRoot, outputPath))
            {
                throw new InvalidOperationException(
                    "The project-relative output path cannot leave the Unity project.");
            }

            return outputPath;
        }

        internal static CaptureFilePlan ResolveFile(
            string outputDirectory,
            string prefabName,
            string presetName,
            string filenamePattern,
            ExistingFilePolicy policy)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("An output directory is required.", nameof(outputDirectory));
            if (string.IsNullOrWhiteSpace(filenamePattern))
                throw new ArgumentException("A filename pattern is required.", nameof(filenamePattern));

            string filename = filenamePattern
                .Replace("{prefab}", prefabName)
                .Replace("{preset}", presetName);
            filename = SanitizeFilename(filename);
            if (string.IsNullOrWhiteSpace(filename))
                throw new InvalidOperationException("The filename pattern produced an empty filename.");

            string path = Path.Combine(outputDirectory, filename + ".png");
            if (!File.Exists(path))
                return new CaptureFilePlan(path, true);

            return policy switch
            {
                ExistingFilePolicy.Skip => new CaptureFilePlan(path, false),
                ExistingFilePolicy.Overwrite => new CaptureFilePlan(path, true),
                ExistingFilePolicy.GenerateUniqueName =>
                    new CaptureFilePlan(GenerateUniquePath(path), true),
                _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
            };
        }

        internal static bool IsInsideAssets(string path)
        {
            string assetsPath = Path.GetFullPath(Application.dataPath);
            return IsPathInside(assetsPath, Path.GetFullPath(path));
        }

        static string GenerateUniquePath(string originalPath)
        {
            string directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            for (int suffix = 1; suffix < int.MaxValue; suffix++)
            {
                string candidate = Path.Combine(
                    directory,
                    $"{name}_{suffix}{extension}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            throw new IOException($"Could not generate a unique path for '{originalPath}'.");
        }

        static string SanitizeFilename(string filename)
        {
            char[] invalidCharacters = Path.GetInvalidFileNameChars()
                .Concat(new[] { '/', '\\', '<', '>', ':', '"', '|', '?', '*' })
                .Distinct()
                .ToArray();
            foreach (char invalidCharacter in invalidCharacters)
                filename = filename.Replace(invalidCharacter, '_');
            return filename.Trim();
        }

        static string GetProjectRoot() =>
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        static bool IsPathInside(string parentPath, string candidatePath)
        {
            string parent = TrimEndingDirectorySeparators(
                Path.GetFullPath(parentPath));
            string candidate = TrimEndingDirectorySeparators(
                Path.GetFullPath(candidatePath));
            return string.Equals(parent, candidate, StringComparison.OrdinalIgnoreCase) ||
                   candidate.StartsWith(
                       parent + Path.DirectorySeparatorChar,
                       StringComparison.OrdinalIgnoreCase);
        }

        static string TrimEndingDirectorySeparators(string path) =>
            path.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);
    }
}
