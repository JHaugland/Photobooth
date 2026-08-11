using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Configuration
{
    internal enum OutputPathMode
    {
        ProjectRelative,
        Absolute
    }

    internal enum ExistingFilePolicy
    {
        Skip,
        Overwrite,
        GenerateUniqueName
    }

    [CreateAssetMenu(
        fileName = "PhotoboothProfile",
        menuName = "Photobooth/Capture Profile")]
    internal sealed class PhotoboothProfile : ScriptableObject
    {
        [Header("Input")]
        [SerializeField, Tooltip("Folder containing the prefabs to capture.")]
        DefaultAsset sourceFolder;

        [SerializeField, Tooltip("Search for prefabs in nested folders.")]
        bool includeSubfolders = true;

        [Header("Output")]
        [SerializeField]
        OutputPathMode outputPathMode = OutputPathMode.ProjectRelative;

        [SerializeField, Tooltip("Path relative to the Unity project root.")]
        string projectRelativeOutputPath = "PhotoboothCaptures";

        [SerializeField, Tooltip("Absolute output path used when Output Path Mode is Absolute.")]
        string absoluteOutputPath = string.Empty;

        [SerializeField]
        ExistingFilePolicy existingFilePolicy = ExistingFilePolicy.Skip;

        [SerializeField, Tooltip("Supported tokens are {prefab} and {preset}.")]
        string filenamePattern = "{prefab}_{preset}";

        [Header("Capture")]
        [SerializeField, Min(1)]
        int captureWidth = 1000;

        [SerializeField, Min(1)]
        int captureHeight = 1000;

        [SerializeField]
        bool transparentBackground;

        [SerializeField]
        Color backgroundColor = Color.gray;

        [Header("Stage")]
        [SerializeField, Tooltip("Prefab containing the capture camera, floor, and lighting.")]
        GameObject stagePrefab;

        [Header("Camera Presets")]
        [SerializeField]
        List<CameraPreset> cameraPresets = new()
        {
            new CameraPreset("Front", Vector3.zero),
            new CameraPreset("Right", new Vector3(0f, 90f, 0f)),
            new CameraPreset("Back", new Vector3(0f, 180f, 0f)),
            new CameraPreset("Left", new Vector3(0f, -90f, 0f)),
            new CameraPreset("Isometric", new Vector3(20f, 45f, 0f)),
            new CameraPreset("Front Three-Quarter", new Vector3(0f, -45f, 0f)),
            new CameraPreset(
                "Elevated Front Three-Quarter",
                new Vector3(15f, -45f, 0f))
        };

        internal DefaultAsset SourceFolder => sourceFolder;
        internal bool IncludeSubfolders => includeSubfolders;
        internal OutputPathMode OutputPathMode => outputPathMode;
        internal string ProjectRelativeOutputPath => projectRelativeOutputPath;
        internal string AbsoluteOutputPath => absoluteOutputPath;
        internal ExistingFilePolicy ExistingFilePolicy => existingFilePolicy;
        internal string FilenamePattern => filenamePattern;
        internal int CaptureWidth => captureWidth;
        internal int CaptureHeight => captureHeight;
        internal bool TransparentBackground => transparentBackground;
        internal Color BackgroundColor => backgroundColor;
        internal GameObject StagePrefab => stagePrefab;
        internal IReadOnlyList<CameraPreset> CameraPresets => cameraPresets;

        void OnValidate()
        {
            captureWidth = Mathf.Max(1, captureWidth);
            captureHeight = Mathf.Max(1, captureHeight);
            projectRelativeOutputPath = projectRelativeOutputPath?.Trim() ?? string.Empty;
            absoluteOutputPath = absoluteOutputPath?.Trim() ?? string.Empty;
            filenamePattern = filenamePattern?.Trim() ?? string.Empty;
        }
    }
}
