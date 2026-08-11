using System;
using UnityEngine;

namespace Photobooth.Editor.Configuration
{
    internal enum CameraPlacementMode
    {
        AutoFrame,
        FixedTransform
    }

    internal enum CameraProjection
    {
        Perspective,
        Orthographic
    }

    [Serializable]
    internal sealed class CameraPreset
    {
        [SerializeField]
        string presetName = "Front";

        [SerializeField]
        CameraPlacementMode placementMode = CameraPlacementMode.AutoFrame;

        [SerializeField]
        Vector3 viewingAngles;

        [SerializeField, Min(1f)]
        float framingPadding = 1.15f;

        [SerializeField]
        Vector3 fixedPosition = new(0f, 1f, -5f);

        [SerializeField]
        Vector3 fixedRotation;

        [SerializeField]
        CameraProjection projection = CameraProjection.Perspective;

        [SerializeField, Range(1f, 179f)]
        float fieldOfView = 30f;

        [SerializeField, Min(0.01f)]
        float orthographicSize = 5f;

        internal string PresetName => presetName;
        internal CameraPlacementMode PlacementMode => placementMode;
        internal Vector3 ViewingAngles => viewingAngles;
        internal float FramingPadding => framingPadding;
        internal Vector3 FixedPosition => fixedPosition;
        internal Vector3 FixedRotation => fixedRotation;
        internal CameraProjection Projection => projection;
        internal float FieldOfView => fieldOfView;
        internal float OrthographicSize => orthographicSize;

        internal CameraPreset()
        {
        }

        internal CameraPreset(string name, Vector3 angles)
        {
            presetName = name;
            viewingAngles = angles;
        }

        internal static CameraPreset CreateAutoFrame(
            string name,
            Vector3 angles,
            CameraProjection cameraProjection = CameraProjection.Perspective,
            float padding = 1.15f)
        {
            return new CameraPreset(name, angles)
            {
                projection = cameraProjection,
                framingPadding = padding
            };
        }

        internal static CameraPreset CreateFixed(
            string name,
            Vector3 position,
            Vector3 rotation,
            CameraProjection cameraProjection = CameraProjection.Perspective)
        {
            return new CameraPreset
            {
                presetName = name,
                placementMode = CameraPlacementMode.FixedTransform,
                fixedPosition = position,
                fixedRotation = rotation,
                projection = cameraProjection
            };
        }
    }
}
