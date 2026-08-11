using System;
using System.IO;
using NUnit.Framework;
using Photobooth.Editor.Capture;
using Photobooth.Editor.Configuration;
using UnityEditor;
using UnityEngine;

namespace Photobooth.Editor.Tests
{
    internal sealed class ConfigurationAndOutputTests
    {
        string temporaryDirectory;
        PhotoboothProfile profile;

        [SetUp]
        public void SetUp()
        {
            temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "PhotoboothConfigurationTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (profile != null)
                UnityEngine.Object.DestroyImmediate(profile);
            if (Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, true);
        }

        [Test]
        public void DefaultProfile_HasUsableCaptureDefaults()
        {
            PhotoboothProfile defaultProfile =
                AssetDatabase.LoadAssetAtPath<PhotoboothProfile>(
                    PhotoboothAssetPaths.DefaultProfilePath);

            Assert.That(defaultProfile, Is.Not.Null);
            Assert.That(defaultProfile.StagePrefab, Is.Not.Null);
            Assert.That(defaultProfile.CameraPresets, Has.Count.EqualTo(7));
            Assert.That(
                defaultProfile.CameraPresets[5].PresetName,
                Is.EqualTo("Front Three-Quarter"));
            Assert.That(
                defaultProfile.CameraPresets[5].ViewingAngles,
                Is.EqualTo(new Vector3(0f, -45f, 0f)));
            Assert.That(
                defaultProfile.CameraPresets[6].PresetName,
                Is.EqualTo("Elevated Front Three-Quarter"));
            Assert.That(
                defaultProfile.CameraPresets[6].ViewingAngles,
                Is.EqualTo(new Vector3(15f, -45f, 0f)));
            Assert.That(defaultProfile.CaptureWidth, Is.GreaterThan(0));
            Assert.That(defaultProfile.CaptureHeight, Is.GreaterThan(0));
            Assert.That(defaultProfile.FilenamePattern, Does.Contain("{prefab}"));
        }

        [Test]
        public void ValidateProfile_WithoutSourceFolder_ThrowsClearError()
        {
            profile = CreateProfile();

            var exception = Assert.Throws<InvalidOperationException>(
                () => PhotoboothCapturePipeline.ValidateProfile(profile));

            StringAssert.Contains("no source folder", exception.Message);
        }

        [Test]
        public void ValidateProfile_WithoutStagePrefab_ThrowsClearError()
        {
            profile = CreateProfile();
            SetObjectReference(
                profile,
                "sourceFolder",
                AssetDatabase.LoadAssetAtPath<DefaultAsset>(
                    "Assets/__PhotoboothProject/Tests"));

            var exception = Assert.Throws<InvalidOperationException>(
                () => PhotoboothCapturePipeline.ValidateProfile(profile));

            StringAssert.Contains("no stage prefab", exception.Message);
        }

        [Test]
        public void ResolveOutputDirectory_ProjectRelativePathStaysInsideProject()
        {
            profile = CreateProfile();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("projectRelativeOutputPath").stringValue =
                "Assets/__PhotoboothProject/TestCaptures";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string resolved = CaptureOutputPathResolver.ResolveOutputDirectory(profile);

            Assert.That(
                resolved,
                Is.EqualTo(Path.GetFullPath(
                    Path.Combine(
                        Application.dataPath,
                        "__PhotoboothProject",
                        "TestCaptures"))));
        }

        [Test]
        public void ResolveOutputDirectory_ProjectRelativePathCannotEscapeProject()
        {
            profile = CreateProfile();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("projectRelativeOutputPath").stringValue =
                "../OutsideProject";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var exception = Assert.Throws<InvalidOperationException>(
                () => CaptureOutputPathResolver.ResolveOutputDirectory(profile));

            StringAssert.Contains("cannot leave", exception.Message);
        }

        [Test]
        public void ResolveOutputDirectory_AbsoluteModeRequiresFullyQualifiedPath()
        {
            profile = CreateProfile();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("outputPathMode").enumValueIndex =
                (int)OutputPathMode.Absolute;
            serialized.FindProperty("absoluteOutputPath").stringValue =
                "relative/output";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var exception = Assert.Throws<InvalidOperationException>(
                () => CaptureOutputPathResolver.ResolveOutputDirectory(profile));

            StringAssert.Contains("fully qualified", exception.Message);
        }

        [Test]
        public void WriteFile_OverwriteReplacesContentsWithoutLeavingTemporaryFile()
        {
            string outputPath = Path.Combine(temporaryDirectory, "capture.png");
            File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3 });

            PhotoboothCapturePipeline.WriteFile(
                outputPath,
                new byte[] { 9, 8 },
                ExistingFilePolicy.Overwrite);

            Assert.That(File.ReadAllBytes(outputPath), Is.EqualTo(new byte[] { 9, 8 }));
            Assert.That(File.Exists(outputPath + ".tmp"), Is.False);
        }

        [Test]
        public void WriteFile_NonOverwriteFailurePreservesOriginalAndCleansTemporaryFile()
        {
            string outputPath = Path.Combine(temporaryDirectory, "capture.png");
            File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3 });

            Assert.Throws<IOException>(
                () => PhotoboothCapturePipeline.WriteFile(
                    outputPath,
                    new byte[] { 9, 8 },
                    ExistingFilePolicy.Skip));

            Assert.That(File.ReadAllBytes(outputPath), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(File.Exists(outputPath + ".tmp"), Is.False);
        }

        static PhotoboothProfile CreateProfile() =>
            ScriptableObject.CreateInstance<PhotoboothProfile>();

        static void SetObjectReference(
            PhotoboothProfile target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
