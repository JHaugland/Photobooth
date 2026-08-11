using System;
using Photobooth.Editor.Configuration;
using Photobooth.Editor.Placement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Photobooth.Editor.Stage
{
    internal sealed class PhotoboothStageSession : IDisposable
    {
        readonly Scene stageScene;
        readonly Scene originalActiveScene;
        GameObject stageInstance;
        bool disposed;

        internal Scene StageScene => stageScene;
        internal Camera CaptureCamera { get; }
        internal Transform SpawnPoint { get; }
        internal Transform CameraTarget { get; }
        internal Transform LightingRig { get; }

        internal PhotoboothStageSession(GameObject stagePrefab)
        {
            if (stagePrefab == null)
                throw new ArgumentNullException(nameof(stagePrefab));
            string stageScenePath =
                PhotoboothAssetPaths.EnsureWritableStageScene();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(stageScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"The Photobooth staging scene is missing at '{stageScenePath}'.");
            }

            originalActiveScene = SceneManager.GetActiveScene();
            stageScene = EditorSceneManager.OpenScene(
                stageScenePath,
                OpenSceneMode.Additive);
            RestoreActiveScene();

            try
            {
                stageInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                    stagePrefab,
                    stageScene);
                CaptureCamera = FindRequiredComponent<Camera>(
                    stageInstance.transform,
                    "CameraRig/CaptureCamera",
                    stagePrefab.name);
                SpawnPoint = FindRequiredTransform(
                    stageInstance.transform,
                    "Anchors/SpawnPoint",
                    stagePrefab.name);
                CameraTarget = FindRequiredTransform(
                    stageInstance.transform,
                    "Anchors/CameraTarget",
                    stagePrefab.name);
                LightingRig = FindRequiredTransform(
                    stageInstance.transform,
                    "Lighting",
                    stagePrefab.name);
                if (CaptureCamera == null ||
                    SpawnPoint == null ||
                    CameraTarget == null ||
                    LightingRig == null)
                {
                    throw new InvalidOperationException(
                        $"Stage prefab '{stagePrefab.name}' has missing references.");
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal PlacedSubject PlaceSubject(GameObject prefab)
        {
            ThrowIfDisposed();
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stageScene);
            try
            {
                instance.transform.SetParent(SpawnPoint, false);
                Bounds bounds = SubjectPlacementService.CenterAndGround(
                    instance,
                    SpawnPoint.position);
                CameraTarget.position = bounds.center;
                return new PlacedSubject(instance, bounds);
            }
            catch
            {
                Object.DestroyImmediate(instance);
                throw;
            }
        }

        internal void FrameSubject(
            PlacedSubject subject,
            CameraPreset preset,
            float aspectRatio)
        {
            ThrowIfDisposed();
            CameraFramingService.Apply(
                CaptureCamera,
                SpawnPoint,
                CameraTarget,
                subject.Bounds,
                preset,
                aspectRatio);
            AlignLightingToCamera();
        }

        internal void UnloadSubject(PlacedSubject subject)
        {
            ThrowIfDisposed();
            if (subject.Instance != null)
                Object.DestroyImmediate(subject.Instance);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (stageInstance != null)
                Object.DestroyImmediate(stageInstance);
            if (!stageScene.IsValid() || !stageScene.isLoaded)
                return;

            if (SceneManager.GetActiveScene() == stageScene)
                RestoreActiveScene();
            EditorSceneManager.CloseScene(stageScene, true);
        }

        void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PhotoboothStageSession));
        }

        void RestoreActiveScene()
        {
            if (originalActiveScene.IsValid() &&
                originalActiveScene.isLoaded &&
                originalActiveScene != stageScene)
            {
                SceneManager.SetActiveScene(originalActiveScene);
                return;
            }

            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene candidate = SceneManager.GetSceneAt(index);
                if (candidate.IsValid() &&
                    candidate.isLoaded &&
                    candidate != stageScene)
                {
                    SceneManager.SetActiveScene(candidate);
                    return;
                }
            }
        }

        void AlignLightingToCamera()
        {
            Vector3 viewingDirection = CaptureCamera.transform.forward;
            viewingDirection.y = 0f;
            if (viewingDirection.sqrMagnitude < Mathf.Epsilon)
                return;

            LightingRig.rotation = Quaternion.LookRotation(
                viewingDirection.normalized,
                Vector3.up);
        }

        static Transform FindRequiredTransform(
            Transform root,
            string path,
            string prefabName)
        {
            Transform found = root.Find(path);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"Stage prefab '{prefabName}' is missing '{path}'.");
            }

            return found;
        }

        static T FindRequiredComponent<T>(
            Transform root,
            string path,
            string prefabName)
            where T : Component
        {
            Transform found = FindRequiredTransform(root, path, prefabName);
            if (!found.TryGetComponent(out T component))
            {
                throw new InvalidOperationException(
                    $"Stage prefab '{prefabName}' has no {typeof(T).Name} at '{path}'.");
            }

            return component;
        }
    }
}
