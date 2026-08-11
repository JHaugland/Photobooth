using System;
using System.Collections.Generic;
using System.IO;
using Photobooth.Editor.Configuration;
using Photobooth.Editor.Discovery;
using Photobooth.Editor.Placement;
using Photobooth.Editor.Stage;
using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Capture
{
    internal sealed class PhotoboothCaptureSession : IDisposable
    {
        readonly PhotoboothProfile profile;
        readonly PrefabDiscoveryResult prefabs;
        readonly string outputDirectory;
        readonly bool refreshAssetDatabase;
        readonly List<CaptureFileResult> results = new();
        PhotoboothStageSession stage;
        PlacedSubject? subject;
        int prefabIndex;
        int presetIndex;
        int completedOperations;
        bool wroteFile;
        bool refreshed;
        bool cancellationRequested;
        bool disposed;

        internal int TotalOperations { get; }
        internal int CompletedOperations => completedOperations;
        internal float Progress =>
            TotalOperations == 0
                ? IsCompleted ? 1f : 0f
                : (float)completedOperations / TotalOperations;
        internal string CurrentPrefabPath =>
            prefabIndex < prefabs.Count
                ? prefabs.Entries[prefabIndex].AssetPath
                : string.Empty;
        internal string CurrentPresetName =>
            prefabIndex < prefabs.Count && presetIndex < profile.CameraPresets.Count
                ? profile.CameraPresets[presetIndex].PresetName
                : string.Empty;
        internal bool IsCompleted { get; private set; }
        internal bool IsCancelled { get; private set; }
        internal CaptureBatchResult Result => new(results.AsReadOnly());

        internal PhotoboothCaptureSession(PhotoboothProfile captureProfile)
        {
            profile = captureProfile;
            PhotoboothCapturePipeline.ValidateProfile(profile);
            prefabs = PrefabDiscoveryService.Discover(
                profile.SourceFolder,
                profile.IncludeSubfolders);
            outputDirectory =
                CaptureOutputPathResolver.ResolveOutputDirectory(profile);
            Directory.CreateDirectory(outputDirectory);
            refreshAssetDatabase =
                CaptureOutputPathResolver.IsInsideAssets(outputDirectory);
            TotalOperations = prefabs.Count * profile.CameraPresets.Count;
            stage = new PhotoboothStageSession(profile.StagePrefab);
        }

        internal bool Step()
        {
            ThrowIfDisposed();
            if (IsCompleted)
                return false;
            if (cancellationRequested)
            {
                IsCancelled = true;
                Complete();
                return false;
            }
            if (prefabIndex >= prefabs.Count)
            {
                Complete();
                return false;
            }

            PrefabDiscoveryEntry prefabEntry = prefabs.Entries[prefabIndex];
            CameraPreset preset = profile.CameraPresets[presetIndex];
            try
            {
                if (!subject.HasValue)
                    subject = stage.PlaceSubject(prefabEntry.LoadPrefab());

                CaptureFilePlan filePlan = CaptureOutputPathResolver.ResolveFile(
                    outputDirectory,
                    prefabEntry.Name,
                    preset.PresetName,
                    profile.FilenamePattern,
                    profile.ExistingFilePolicy);

                if (filePlan.ShouldCapture)
                {
                    stage.FrameSubject(
                        subject.Value,
                        preset,
                        (float)profile.CaptureWidth / profile.CaptureHeight);
                    byte[] png = CameraPngRenderer.Render(
                        stage.CaptureCamera,
                        profile.CaptureWidth,
                        profile.CaptureHeight,
                        profile.BackgroundColor,
                        profile.TransparentBackground);
                    PhotoboothCapturePipeline.WriteFile(
                        filePlan.Path,
                        png,
                        profile.ExistingFilePolicy);
                    wroteFile = true;
                    results.Add(new CaptureFileResult(
                        prefabEntry,
                        preset.PresetName,
                        filePlan.Path,
                        CaptureFileStatus.Captured));
                }
                else
                {
                    results.Add(new CaptureFileResult(
                        prefabEntry,
                        preset.PresetName,
                        filePlan.Path,
                        CaptureFileStatus.SkippedExisting));
                }
            }
            catch (Exception exception)
            {
                results.Add(new CaptureFileResult(
                    prefabEntry,
                    preset.PresetName,
                    string.Empty,
                    CaptureFileStatus.Failed,
                    exception.Message));
                Debug.LogError(
                    $"Photobooth capture failed for '{prefabEntry.AssetPath}' " +
                    $"with preset '{preset.PresetName}'.\n{exception}");
                ResetStageAfterFailure(exception);
            }

            completedOperations++;
            AdvanceQueue();
            if (prefabIndex >= prefabs.Count)
                Complete();
            return !IsCompleted;
        }

        void ResetStageAfterFailure(Exception operationException)
        {
            subject = null;
            try
            {
                stage?.Dispose();
                stage = new PhotoboothStageSession(profile.StagePrefab);
            }
            catch (Exception recoveryException)
            {
                throw new InvalidOperationException(
                    "The Photobooth stage could not recover after a capture failure.",
                    new AggregateException(operationException, recoveryException));
            }
        }

        internal void Cancel()
        {
            ThrowIfDisposed();
            cancellationRequested = true;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            UnloadSubject();
            stage?.Dispose();
            stage = null;
            RefreshAssetDatabase();
        }

        void AdvanceQueue()
        {
            presetIndex++;
            if (presetIndex < profile.CameraPresets.Count)
                return;

            UnloadSubject();
            presetIndex = 0;
            prefabIndex++;
        }

        void Complete()
        {
            IsCompleted = true;
            UnloadSubject();
            stage?.Dispose();
            stage = null;
            RefreshAssetDatabase();
        }

        void UnloadSubject()
        {
            if (!subject.HasValue)
                return;

            stage?.UnloadSubject(subject.Value);
            subject = null;
        }

        void RefreshAssetDatabase()
        {
            if (refreshed || !refreshAssetDatabase || !wroteFile)
                return;

            refreshed = true;
            AssetDatabase.Refresh();
        }

        void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PhotoboothCaptureSession));
        }
    }
}
