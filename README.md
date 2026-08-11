# Photobooth

Photobooth is an Editor-only Unity tool for batch-rendering prefab assets into
consistently framed PNG images. It automatically discovers prefabs, grounds and
centers subjects of widely varying sizes, frames them from configurable camera
angles, and captures them in an isolated staging scene.

## Features

- Deterministic folder-based prefab discovery
- Automatic placement and perspective or orthographic framing
- Configurable product-photo camera presets
- Neutral camera-relative three-point lighting
- Transparent or solid-color PNG output
- Atomic writes with skip, overwrite, and unique-name policies
- Incremental batches with progress, cancellation, and per-item recovery
- No changes to open scenes, prefabs, Build Settings, or runtime assemblies
- 43 EditMode tests covering capture behavior and distribution safety

## Install

### Git URL

In Unity's Package Manager, choose **Install package from git URL**:

```text
https://github.com/JHaugland/Photobooth.git?path=/Packages/com.jhaugland.photobooth
```

Append `#v1.0.2` to pin the latest patched release.

### Unity package

Download `Photobooth-1.0.2.unitypackage` from the matching GitHub release and
import it through **Assets > Import Package > Custom Package**.

## Use

1. Open **Tools > Photobooth**.
2. Select a folder containing prefab assets.
3. Configure output, dimensions, background, and camera presets.
4. Refresh the queue and start the batch.

See the [complete documentation](Packages/com.jhaugland.photobooth/Documentation~/index.md)
for safety guarantees, all settings, camera guidance, limitations,
troubleshooting, architecture, and extension points.

## Development

The distributable lives in `Packages/com.jhaugland.photobooth`. EditMode tests
and development fixtures live under `Assets/__PhotoboothProject/Tests`.

Build the importable package from PowerShell:

```powershell
./Distribution/Build-UnityPackage.ps1
```

The versioned artifact and SHA-256 checksum are written to `Dist/`.

## License

[MIT](LICENSE.md)
