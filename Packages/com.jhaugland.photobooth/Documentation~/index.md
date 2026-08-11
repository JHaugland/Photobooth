# Photobooth for Unity

Photobooth discovers prefab assets in a selected project folder and renders
each prefab from one or more configurable camera angles. It is intended for
inventory icons, asset catalogues, documentation, and visual review.

## Requirements

- Unity 6.0 or newer.
- Prefabs with at least one active, enabled child `Renderer`.
- Materials compatible with the destination project's active render pipeline.

The included stage uses only built-in Camera, Light, and Transform components.
Photobooth has been validated with URP 17.5; the stage itself has no URP, HDRP,
or third-party package dependency.

## Installation

### Unity Package Manager from Git

In **Window > Package Management > Package Manager**, choose
**Install package from git URL** and enter:

```text
https://github.com/JHaugland/Photobooth.git?path=/Packages/com.jhaugland.photobooth
```

You can pin a release by appending `#v1.0.0`.

### Unity package file

Download `Photobooth-1.0.0.unitypackage` from the GitHub release and import it
through **Assets > Import Package > Custom Package**.

## Quick start

1. Open **Tools > Photobooth**.
2. Assign a project folder containing prefabs to **Source Folder**.
3. Enable **Include Subfolders** if required.
4. Choose a project-relative or absolute output directory.
5. Configure image size, transparency, filename pattern, and camera presets.
6. Select **Refresh Queue**, then **Start Batch**.

Git-installed packages are read-only. On first use, Photobooth creates an
editable profile at:

```text
Assets/Photobooth/DefaultPhotoboothProfile.asset
```

This is the only project asset the UPM installation creates automatically.
The `.unitypackage` installation keeps its editable profile within the imported
Photobooth folder.

## Capture profile

| Setting | Purpose |
| --- | --- |
| Source Folder | Project folder whose prefab assets will be captured. |
| Include Subfolders | Recursively discovers nested prefabs. |
| Output Path Mode | Uses a project-relative or absolute destination. |
| Existing File Policy | Skips, overwrites, or generates a unique filename. |
| Filename Pattern | Supports `{prefab}` and `{preset}` tokens. |
| Width / Height | Output dimensions in pixels. |
| Transparent Background | Writes PNG alpha instead of the solid color. |
| Stage Prefab | Camera, anchors, and lighting used for capture. |
| Camera Presets | Angles, projection, framing padding, and fixed transforms. |

The default profile includes front, right, back, left, isometric, level
three-quarter, and elevated three-quarter views.

## Safety and project isolation

- All C# code is compiled into an Editor-only assembly.
- The package contains no runtime assembly or runtime component.
- The staging scene is not added to Build Settings.
- Captures never modify or save the user's open scenes.
- Each subject is instantiated only in a temporary additive staging scene.
- The previous active scene is restored immediately after staging opens.
- Subjects and staging scenes are destroyed or closed after capture and errors.
- Camera and render-texture state is restored after every image.
- Output is written only to the configured directory.
- Project-relative output is rejected if it escapes the project root.
- PNG writes are atomic, using a temporary file before replacement.
- Package assets are not stored in `Resources` and are not Addressables.

## Camera presets

Auto-frame presets interpret **Viewing Angles** as Euler angles around the
subject. Useful product-photo angles include:

- Level three-quarter: `(0, -45, 0)`
- Elevated three-quarter: `(15, -45, 0)`
- Low hero view: a small negative X angle with a three-quarter Y angle
- Top-down view: a larger positive X angle
- Rear three-quarter: a Y angle near `135` or `-135`

Increase **Framing Padding** for more empty space. Fixed-transform presets are
available when exact stage-relative camera placement is more important than
automatic framing.

## Failure handling

A malformed prefab fails only its current camera operation. The batch records
the prefab path, preset, and exception, resets the staging scene, and continues.
If staging recovery fails, the batch stops and reports the fatal error.
Cancellation finishes the current operation before cleaning up.

## Limitations

- Prefabs without enabled renderers cannot be framed.
- Scripts are not run in Play Mode; visuals that require runtime initialization
  will not appear unless already represented by serialized renderer state.
- Particle systems, trails, procedural meshes, and animations are not advanced
  before capture.
- Render results depend on the active render pipeline and material compatibility.

## Extending the tool

- `PrefabDiscoveryService` owns deterministic asset discovery.
- `SubjectPlacementService` calculates combined renderer bounds and grounding.
- `CameraFramingService` contains perspective and orthographic framing math.
- `PhotoboothStageSession` owns isolated scene and subject lifetime.
- `CameraPngRenderer` owns render state and PNG encoding.
- `CaptureOutputPathResolver` owns paths, filenames, and overwrite behavior.
- `PhotoboothCaptureSession` is the incremental batch state machine.
- `PhotoboothWindow` is the Editor UI.

Keep new production code inside the Editor assembly. Add EditMode coverage for
changes to framing, state restoration, paths, or staging lifetime.

## Troubleshooting

**The queue is empty:** verify that Source Folder references a Unity project
folder and that it contains prefab assets.

**A prefab fails:** expand **Failures** after the batch. Renderer-less prefabs
and invalid stage structures report explicit errors.

**The output is missing:** project-relative paths are resolved from the Unity
project root, not from the repository's primary checkout or another worktree.

**Materials render incorrectly:** confirm that the imported asset materials are
compatible with the destination project's active render pipeline.
