using System;
using System.IO;
using Photobooth.Editor.Configuration;

namespace Photobooth.Editor.Capture
{
    internal static class PhotoboothCapturePipeline
    {
        internal static CaptureBatchResult Run(PhotoboothProfile profile)
        {
            using var session = new PhotoboothCaptureSession(profile);
            while (session.Step())
            {
            }

            return session.Result;
        }

        internal static void ValidateProfile(PhotoboothProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (profile.SourceFolder == null)
                throw new InvalidOperationException("The capture profile has no source folder.");
            if (profile.StagePrefab == null)
                throw new InvalidOperationException("The capture profile has no stage prefab.");
            if (profile.CameraPresets.Count == 0)
                throw new InvalidOperationException("The capture profile has no camera presets.");
            if (profile.CaptureWidth < 1 || profile.CaptureHeight < 1)
                throw new InvalidOperationException("Capture dimensions must be greater than zero.");
        }

        internal static void WriteFile(
            string outputPath,
            byte[] contents,
            ExistingFilePolicy policy)
        {
            string temporaryPath = outputPath + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, contents);
                if (File.Exists(outputPath))
                {
                    if (policy != ExistingFilePolicy.Overwrite)
                    {
                        throw new IOException(
                            $"Output file unexpectedly exists: '{outputPath}'.");
                    }

                    File.Delete(outputPath);
                }

                File.Move(temporaryPath, outputPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}
