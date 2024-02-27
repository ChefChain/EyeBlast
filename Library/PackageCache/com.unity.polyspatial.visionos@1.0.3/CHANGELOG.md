---
uid: psvos-changelog
---
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

For general changes to PolySpatial, refer to the [PolySpatial Changelog](https://docs.unity3d.com/Packages/com.unity.polyspatial@latest?subfolder=/changelog/CHANGELOG.html).

## [1.0.3] - 2024-01-20

### Added

### Changed
- Updated documentation to address package version and Unity Editor version requirements.
- In the editor, PolySpatial preview and builds will only be enabled on platforms where it is supported (currently, visionOS MR).

### Deprecated

### Removed

### Fixed
- PolySpatial will only be enabled for the visionOS Mixed Reality build configuration.
- Fixed issue with not restarting ARSession when switching scenes.
- Fixed issue with incorrect transforms on volumes when Display -> Appearance -> Window Zoom setting was changed.
- Fixed issue with incorrect transforms on volumes when using a bounded Default Volume Camera Window Configuration.
- Fixed issue when certain skinned meshes would not show up when connecting to PlayToDevice.
- Fixed issues with building for visionOS MR on non-macOS editor platforms.

### Security

## [0.7.1] - 2023-12-13

### Added

### Changed
- All packages now require 2022.3.15f1 and later (rather than 2022.3.11f1 and later) to pick up fixes for various memory leaks made in 15f1.

### Deprecated

### Removed
- Removed Statistics docs and Asset finder docs since the tooling is not available anymore
- Support for Unity versions earlier than 2022.3.11f1.

### Fixed
- Fixed interaction ray direction on pointer events.
- Updated a note in the documentation about choosing Target SDK in Player Settings. Previously, the note explained that choosing the SDK was _not_ required, but now it is.

### Security

## [0.6.3] - 2023-11-28

### Added
- Added link from the Index page to the Requirements page for easier access to the required Unity versions for Polyspatial.
- Added instructions for upgrading/downgrading Play to Device Host app on TestFlight.

### Changed
- Moved the PolySpatial Unity Version Support matrix from the Changelog to the Requirements page in the docs.
- Play to device page no longer has a compatibility version matrix for each PolySpatial release but points to a google drive folder on where one can find the different Play To Device Host versions.

### Deprecated

### Removed

### Fixed
- Moving volume cameras will no longer recreate the window on every frame.
- Fixed crash due when Raycast Target is enabled on UGUI elements.
- Corrected docs for PolySpatial version in the Play To Device docs to 0.6.2 version (instead of 0.6.0).

### Security

## [0.6.2] - 2023-11-13

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.6.1] - 2023-11-09

Our latest release introduces a new feature called "Play To Device." Please read this [Discussion post](https://discussions.unity.com/t/play-to-device/309359) to learn more and visit the [documentation](https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@0.5/manual/PlayToDevice.html) page.

For those who are testing on devices at Apple's developer labs or via a developer kit, you should only be using the following **updated configuration**.
* Apple Silicon Mac for development
* Unity 2022 LTS (2022.3.11f1) and higher
* Xcode 15.1 beta 1
    * The Xcode 15 Release Candidate will _not_ work
* visionOS beta 4 (21N5259k) - SDK 

To learn more about Unity's visionOS beta program, please refer to [this post](https://discussions.unity.com/t/welcome-to-unitys-visionos-beta-program/270282). 

### Related Changelogs

- [com.unity.polyspatial](https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@0.6/changelog/CHANGELOG.html)
- [com.unity.polyspatial.xr](https://docs.unity3d.com/Packages/com.unity.polyspatial.xr@0.6/changelog/CHANGELOG.html)
- [com.unity.xr.visionos](https://docs.unity3d.com/Packages/com.unity.xr.visionos@0.6/changelog/CHANGELOG.html)


### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.6.0] - 2023-11-08

### Added
- Particle property transfer render mode now supports StretchedBillboard->LengthScale, and has more accurate emitter shape representation.  
- Added `PolySpatialWindowManagerAccess.entityForIdentifier` in Swift and `PolySpatialObjectUtils.GetPolySpatialIdentifier` to C#, to allow accessing the RealityKit Entity corresponding to a Unity GameObject from Swift code. No guarantees are made about whether there is a RealityKit Entity for any given GameObject, or about the lifetime of the Entity.

### Changed

### Deprecated

### Removed

### Fixed

- Device Position input values are now converted to RealityKit coordinates (meters, instead of points)

### Security

## [0.5.0] - 2023-10-26

### Added
- **Particle System Modes**: Developers can now select between particle system modes under `Project Settings > PolySpatial > Particle Mode`. The available modes offer tradeoffs between performance and quality:
  - **Bake to Mesh**: In this mode, a dynamic mesh is baked for every particle system every frame. It closely aligns the visuals with Unity rendering, allowing leverage of most features of Unity's built-in particle systems, including custom shaders authored with ShaderGraph. However, this mode currently imposes a significant performance overhead. We are actively working to improve performance for baked particles.
- Added support for platform base text rendering through the new UnityPolySpatialPlatformText component.

### Changed

### Deprecated

### Removed

### Fixed
- Fixed an issue where projects with `com.unity.polyspatial.visionos` would fail to build when App Mode is set to Virtual Reality. Device builds were failing with the error `Undefined symbol: _GetPolySpatialNativeAPI`, and simulator builds failed to run with a similar error.

### Security

## [0.4.3] - 2023-10-13

## Fixed
-- Slowdown in visionOS player introduced in 0.4.2 fixed.

## [0.4.2] - 2023-10-12
- Existing windows will now be reused if they are the correct configuration on scene load.
- Added fixes for host-side cleanup needed for PlayToDevice

## [0.4.1] - 2023-10-06

### Added
- Documentation for Volume Camera around configuration assets.

## [0.4.0] - 2023-10-04

## Added
- Documentation for Volume Camera around configuration assets.
- Build error if trying to build for Simulator SDK in Unity prior to 2022.3.11f1.
 
## [0.4.0] - 2023-10-04

### Added
- PolySpatial now supports Xcode 15.1 beta 1 and visionOS 1.0 beta 4

## [0.3.2] - 2023-09-18

## [0.3.1] - 2023-09-15

## [0.3.0] - 2023-09-13

## [0.2.2] - 2023-08-28

## [0.2.1] - 2023-08-25

## [0.2.0] - 2023-08-21

## [0.1.2] - 2023-08-16

## [0.1.2] - 2023-08-16

## [0.1.0] - 2023-07-19

## [0.0.4] - 2023-07-18

## [0.0.3] - 2023-07-18

## [0.0.2] - 2023-07-17

## [0.0.1] - 2023-07-14

### Added
- Initial PolySpatial visionOS package.
