using Photobooth.Editor.Discovery;

namespace Photobooth.Editor.Capture
{
    internal enum CaptureFileStatus
    {
        Captured,
        SkippedExisting,
        Failed
    }

    internal readonly struct CaptureFileResult
    {
        internal PrefabDiscoveryEntry Prefab { get; }
        internal string PresetName { get; }
        internal string OutputPath { get; }
        internal CaptureFileStatus Status { get; }
        internal string ErrorMessage { get; }

        internal CaptureFileResult(
            PrefabDiscoveryEntry prefab,
            string presetName,
            string outputPath,
            CaptureFileStatus status,
            string errorMessage = "")
        {
            Prefab = prefab;
            PresetName = presetName;
            OutputPath = outputPath;
            Status = status;
            ErrorMessage = errorMessage;
        }
    }
}
