# Changelog

All notable changes to this package are documented here.

## [1.0.3] - 2026-08-11

### Fixed

- Add render-layer isolation in addition to camera scene isolation for render
  pipelines that do not honor `Camera.scene` during explicit rendering.
- Show only the output path field selected by Output Path Mode.
- Migrate a fully qualified path previously entered in the project-relative
  field when Absolute mode is selected.

## [1.0.2] - 2026-08-11

### Fixed

- Restrict the capture camera to the isolated staging scene so objects in open
  user scenes cannot appear in generated images.
- Redirect retained package-template profiles to the writable project copy.
- Accept fully qualified Windows drive and UNC paths, including paths pasted
  with surrounding quotes.

## [1.0.1] - 2026-08-11

### Fixed

- Copy the empty staging scene to a writable project folder before opening it
  when Photobooth is installed as a read-only UPM package.

## [1.0.0] - 2026-08-11

### Added

- Deterministic recursive and root-only prefab discovery.
- Bounds-based subject placement and grounding.
- Perspective and orthographic auto-framing.
- Configurable camera presets, output paths, backgrounds, and overwrite rules.
- Isolated additive staging with camera-relative three-point lighting.
- Incremental batch capture with progress, cancellation, and per-item failures.
- Atomic PNG writes and render-state restoration.
- Editor window and editable capture profiles.
- EditMode coverage for configuration, discovery, framing, staging, capture,
  output handling, cancellation, and recovery.
