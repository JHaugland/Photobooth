using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Photobooth.Editor.Capture
{
    internal static class CameraPngRenderer
    {
        internal static byte[] Render(
            Camera camera,
            int width,
            int height,
            Color backgroundColor,
            bool transparentBackground)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            if (width < 1 || height < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Capture dimensions must be greater than zero.");
            if (width > SystemInfo.maxTextureSize || height > SystemInfo.maxTextureSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    $"Capture dimensions cannot exceed {SystemInfo.maxTextureSize} pixels.");
            }

            var renderTexture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            CameraClearFlags previousClearFlags = camera.clearFlags;
            Color previousBackground = camera.backgroundColor;

            try
            {
                if (!renderTexture.Create())
                    throw new InvalidOperationException("Failed to create the capture render texture.");

                camera.targetTexture = renderTexture;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = transparentBackground
                    ? new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f)
                    : new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 1f);
                camera.Render();

                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply(false, false);
                byte[] png = texture.EncodeToPNG();
                if (png == null || png.Length == 0)
                    throw new InvalidOperationException("Unity failed to encode the capture as PNG.");
                return png;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.clearFlags = previousClearFlags;
                camera.backgroundColor = previousBackground;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
