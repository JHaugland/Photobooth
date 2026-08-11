using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Photobooth.Editor.Capture;
using Photobooth.Editor.Configuration;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Photobooth.Editor.Tests
{
    internal sealed class CapturePipelineTests
    {
        string temporaryDirectory;
        GameObject cameraObject;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "PhotoboothTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (cameraObject != null)
                UnityEngine.Object.DestroyImmediate(cameraObject);
            if (Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, true);
        }

        [Test]
        public void CameraRenderer_ProducesPngWithRequestedDimensions()
        {
            cameraObject = new GameObject("CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;

            byte[] png = CameraPngRenderer.Render(
                camera,
                64,
                32,
                Color.gray,
                false);

            Assert.That(png[0], Is.EqualTo(0x89));
            Assert.That(png[1], Is.EqualTo((byte)'P'));
            Assert.That(ReadBigEndianInt32(png, 16), Is.EqualTo(64));
            Assert.That(ReadBigEndianInt32(png, 20), Is.EqualTo(32));
            Assert.That(camera.targetTexture, Is.Null);
        }

        [Test]
        public void CameraRenderer_TransparentBackgroundWritesZeroAlpha()
        {
            cameraObject = new GameObject("CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);

            try
            {
                byte[] png = CameraPngRenderer.Render(
                    camera,
                    8,
                    8,
                    Color.magenta,
                    true);
                Assert.That(texture.LoadImage(png), Is.True);
                Assert.That(texture.GetPixel(4, 4).a, Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void CameraRenderer_RestoresExistingRenderTargets()
        {
            cameraObject = new GameObject("CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            var cameraTarget = new RenderTexture(16, 16, 0);
            var activeTarget = new RenderTexture(16, 16, 0);

            try
            {
                camera.targetTexture = cameraTarget;
                RenderTexture.active = activeTarget;

                CameraPngRenderer.Render(camera, 8, 8, Color.gray, false);

                Assert.That(camera.targetTexture, Is.SameAs(cameraTarget));
                Assert.That(RenderTexture.active, Is.SameAs(activeTarget));
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = null;
                cameraTarget.Release();
                activeTarget.Release();
                UnityEngine.Object.DestroyImmediate(cameraTarget);
                UnityEngine.Object.DestroyImmediate(activeTarget);
            }
        }

        [Test]
        public void ResolveFile_SkipPolicy_DoesNotOverwriteExistingFile()
        {
            string existing = Path.Combine(temporaryDirectory, "Cube_Front.png");
            File.WriteAllBytes(existing, new byte[] { 1 });

            CaptureFilePlan plan = CaptureOutputPathResolver.ResolveFile(
                temporaryDirectory,
                "Cube",
                "Front",
                "{prefab}_{preset}",
                ExistingFilePolicy.Skip);

            Assert.That(plan.Path, Is.EqualTo(existing));
            Assert.That(plan.ShouldCapture, Is.False);
        }

        [Test]
        public void ResolveFile_UniquePolicy_AddsNumericSuffix()
        {
            File.WriteAllBytes(
                Path.Combine(temporaryDirectory, "Cube_Front.png"),
                new byte[] { 1 });

            CaptureFilePlan plan = CaptureOutputPathResolver.ResolveFile(
                temporaryDirectory,
                "Cube",
                "Front",
                "{prefab}_{preset}",
                ExistingFilePolicy.GenerateUniqueName);

            Assert.That(
                Path.GetFileName(plan.Path),
                Is.EqualTo("Cube_Front_1.png"));
            Assert.That(plan.ShouldCapture, Is.True);
        }

        [Test]
        public void ResolveFile_UniquePolicy_SkipsUsedNumericSuffixes()
        {
            File.WriteAllBytes(
                Path.Combine(temporaryDirectory, "Cube_Front.png"),
                new byte[] { 1 });
            File.WriteAllBytes(
                Path.Combine(temporaryDirectory, "Cube_Front_1.png"),
                new byte[] { 1 });

            CaptureFilePlan plan = CaptureOutputPathResolver.ResolveFile(
                temporaryDirectory,
                "Cube",
                "Front",
                "{prefab}_{preset}",
                ExistingFilePolicy.GenerateUniqueName);

            Assert.That(
                Path.GetFileName(plan.Path),
                Is.EqualTo("Cube_Front_2.png"));
        }

        [Test]
        public void ResolveFile_SanitizesAssetAndPresetNames()
        {
            CaptureFilePlan plan = CaptureOutputPathResolver.ResolveFile(
                temporaryDirectory,
                "Pack/Model",
                "Front:Close",
                "{prefab}_{preset}",
                ExistingFilePolicy.Skip);

            Assert.That(
                Path.GetFileName(plan.Path),
                Is.EqualTo("Pack_Model_Front_Close.png"));
        }

        [Test]
        public void Pipeline_CapturesPrefabThroughTemporaryStage()
        {
            const string fixtureFolder =
                "Assets/__PhotoboothProject/Tests/CaptureFixtures";
            const string prefabPath = fixtureFolder + "/TestCube.prefab";
            PhotoboothProfile profile = null;

            try
            {
                AssetDatabase.DeleteAsset(fixtureFolder);
                AssetDatabase.CreateFolder(
                    "Assets/__PhotoboothProject/Tests",
                    "CaptureFixtures");
                CreateCubePrefab(prefabPath);

                profile = ScriptableObject.CreateInstance<PhotoboothProfile>();
                var serializedProfile = new SerializedObject(profile);
                serializedProfile.FindProperty("sourceFolder").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<DefaultAsset>(fixtureFolder);
                serializedProfile.FindProperty("outputPathMode").enumValueIndex =
                    (int)OutputPathMode.Absolute;
                serializedProfile.FindProperty("absoluteOutputPath").stringValue =
                    temporaryDirectory;
                serializedProfile.FindProperty("existingFilePolicy").enumValueIndex =
                    (int)ExistingFilePolicy.Overwrite;
                serializedProfile.FindProperty("captureWidth").intValue = 48;
                serializedProfile.FindProperty("captureHeight").intValue = 48;
                serializedProfile.FindProperty("stagePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        PhotoboothAssetPaths.StagePrefabPath);
                serializedProfile.FindProperty("cameraPresets").arraySize = 1;
                serializedProfile.ApplyModifiedPropertiesWithoutUndo();

                CaptureBatchResult result = PhotoboothCapturePipeline.Run(profile);

                Assert.That(result.CapturedCount, Is.EqualTo(1));
                Assert.That(result.SkippedCount, Is.Zero);
                Assert.That(File.Exists(result.Files[0].OutputPath), Is.True);
                byte[] png = File.ReadAllBytes(result.Files[0].OutputPath);
                Assert.That(ReadBigEndianInt32(png, 16), Is.EqualTo(48));
                Assert.That(ReadBigEndianInt32(png, 20), Is.EqualTo(48));
                AssertSubjectIsVisible(png, 48, 48);
            }
            finally
            {
                if (profile != null)
                    UnityEngine.Object.DestroyImmediate(profile);
                AssetDatabase.DeleteAsset(fixtureFolder);
            }
        }

        [Test]
        public void CaptureSession_StepProcessesOneOperationAndCleansUpOnCompletion()
        {
            const string fixtureFolder =
                "Assets/__PhotoboothProject/Tests/CaptureSessionFixtures";
            const string prefabPath = fixtureFolder + "/TestCube.prefab";
            PhotoboothProfile profile = null;
            int initialSceneCount = SceneManager.sceneCount;
            Scene initialActiveScene = SceneManager.GetActiveScene();

            try
            {
                CreateFixtureFolder(fixtureFolder);
                CreateCubePrefab(prefabPath);
                profile = CreateProfile(fixtureFolder, 2);

                using var session = new PhotoboothCaptureSession(profile);

                Assert.That(session.TotalOperations, Is.EqualTo(2));
                Assert.That(session.CompletedOperations, Is.Zero);
                Assert.That(session.Progress, Is.Zero);
                Assert.That(SceneManager.sceneCount, Is.EqualTo(initialSceneCount + 1));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(initialActiveScene));

                Assert.That(session.Step(), Is.True);
                Assert.That(session.CompletedOperations, Is.EqualTo(1));
                Assert.That(session.Progress, Is.EqualTo(0.5f));
                Assert.That(session.Result.Files, Has.Count.EqualTo(1));

                Assert.That(session.Step(), Is.False);
                Assert.That(session.IsCompleted, Is.True);
                Assert.That(session.CompletedOperations, Is.EqualTo(2));
                Assert.That(session.Progress, Is.EqualTo(1f));
                Assert.That(session.Result.CapturedCount, Is.EqualTo(2));
                Assert.That(SceneManager.sceneCount, Is.EqualTo(initialSceneCount));
                Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(initialActiveScene));
            }
            finally
            {
                if (profile != null)
                    UnityEngine.Object.DestroyImmediate(profile);
                AssetDatabase.DeleteAsset(fixtureFolder);
            }
        }

        [Test]
        public void CaptureSession_CancelStopsBeforeNextOperation()
        {
            const string fixtureFolder =
                "Assets/__PhotoboothProject/Tests/CaptureSessionCancelFixtures";
            const string prefabPath = fixtureFolder + "/TestCube.prefab";
            PhotoboothProfile profile = null;

            try
            {
                CreateFixtureFolder(fixtureFolder);
                CreateCubePrefab(prefabPath);
                profile = CreateProfile(fixtureFolder, 2);

                using var session = new PhotoboothCaptureSession(profile);
                Assert.That(session.Step(), Is.True);

                session.Cancel();

                Assert.That(session.Step(), Is.False);
                Assert.That(session.IsCompleted, Is.True);
                Assert.That(session.IsCancelled, Is.True);
                Assert.That(session.CompletedOperations, Is.EqualTo(1));
                Assert.That(session.Result.Files, Has.Count.EqualTo(1));
            }
            finally
            {
                if (profile != null)
                    UnityEngine.Object.DestroyImmediate(profile);
                AssetDatabase.DeleteAsset(fixtureFolder);
            }
        }

        [Test]
        public void CaptureSession_EmptyQueueCompletesWithoutWork()
        {
            const string fixtureFolder =
                "Assets/__PhotoboothProject/Tests/CaptureSessionEmptyFixtures";
            PhotoboothProfile profile = null;

            try
            {
                CreateFixtureFolder(fixtureFolder);
                profile = CreateProfile(fixtureFolder, 1);

                using var session = new PhotoboothCaptureSession(profile);

                Assert.That(session.TotalOperations, Is.Zero);
                Assert.That(session.Step(), Is.False);
                Assert.That(session.IsCompleted, Is.True);
                Assert.That(session.Progress, Is.EqualTo(1f));
                Assert.That(session.Result.Files, Is.Empty);
            }
            finally
            {
                if (profile != null)
                    UnityEngine.Object.DestroyImmediate(profile);
                AssetDatabase.DeleteAsset(fixtureFolder);
            }
        }

        [Test]
        public void CaptureSession_FailedPrefabDoesNotStopRemainingQueue()
        {
            const string fixtureFolder =
                "Assets/__PhotoboothProject/Tests/CaptureSessionFailureFixtures";
            PhotoboothProfile profile = null;

            try
            {
                CreateFixtureFolder(fixtureFolder);
                CreateEmptyPrefab(fixtureFolder + "/Bad.prefab");
                CreateCubePrefab(fixtureFolder + "/Good.prefab");
                profile = CreateProfile(fixtureFolder, 1);
                LogAssert.Expect(
                    LogType.Error,
                    new Regex(
                        "Photobooth capture failed for .*Bad\\.prefab.*",
                        RegexOptions.Singleline));

                using var session = new PhotoboothCaptureSession(profile);

                Assert.That(session.Step(), Is.True);
                Assert.That(session.CompletedOperations, Is.EqualTo(1));
                Assert.That(session.Result.FailedCount, Is.EqualTo(1));
                Assert.That(session.Result.Files[0].ErrorMessage, Is.Not.Empty);

                Assert.That(session.Step(), Is.False);
                Assert.That(session.IsCompleted, Is.True);
                Assert.That(session.Result.CapturedCount, Is.EqualTo(1));
                Assert.That(session.Result.FailedCount, Is.EqualTo(1));
                Assert.That(session.Result.Files, Has.Count.EqualTo(2));
            }
            finally
            {
                if (profile != null)
                    UnityEngine.Object.DestroyImmediate(profile);
                AssetDatabase.DeleteAsset(fixtureFolder);
            }
        }

        PhotoboothProfile CreateProfile(string fixtureFolder, int presetCount)
        {
            var profile = ScriptableObject.CreateInstance<PhotoboothProfile>();
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("sourceFolder").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(fixtureFolder);
            serializedProfile.FindProperty("outputPathMode").enumValueIndex =
                (int)OutputPathMode.Absolute;
            serializedProfile.FindProperty("absoluteOutputPath").stringValue =
                temporaryDirectory;
            serializedProfile.FindProperty("existingFilePolicy").enumValueIndex =
                (int)ExistingFilePolicy.Overwrite;
            serializedProfile.FindProperty("captureWidth").intValue = 32;
            serializedProfile.FindProperty("captureHeight").intValue = 32;
            serializedProfile.FindProperty("stagePrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    PhotoboothAssetPaths.StagePrefabPath);
            serializedProfile.FindProperty("cameraPresets").arraySize = presetCount;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        static void CreateFixtureFolder(string path)
        {
            AssetDatabase.DeleteAsset(path);
            string folderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(
                "Assets/__PhotoboothProject/Tests",
                folderName);
        }

        static void CreateCubePrefab(string path)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                PrefabUtility.SaveAsPrefabAsset(cube, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(cube);
            }
        }

        static void CreateEmptyPrefab(string path)
        {
            var empty = new GameObject("NoRenderers");
            try
            {
                PrefabUtility.SaveAsPrefabAsset(empty, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(empty);
            }
        }

        static int ReadBigEndianInt32(byte[] bytes, int offset) =>
            bytes[offset] << 24 |
            bytes[offset + 1] << 16 |
            bytes[offset + 2] << 8 |
            bytes[offset + 3];

        static void AssertSubjectIsVisible(byte[] png, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(texture.LoadImage(png), Is.True);
                Color center = texture.GetPixel(width / 2, height / 2);
                Assert.That(
                    Vector3.Distance(
                        new Vector3(center.r, center.g, center.b),
                        new Vector3(Color.gray.r, Color.gray.g, Color.gray.b)),
                    Is.GreaterThan(0.05f),
                    "The staged subject was not visible at the center of the capture.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
