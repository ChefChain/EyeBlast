---
uid: vosxr-changelog
---
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2024-01-20

### Added
- Added a Project Validation rule to check for UniversalRenderData with `Depth Texture Mode` set to anything other than `After Opaques`, which will cause rendering glitches when no opaque objects are visible.
- Added a workaround to build the post processor for `ARM64 branch out of range` error which can occur when building in Xcode.
- Added `interactionRayRotation` control which exposes a gaze ray which can be used for draggable UI elements. It begins with a rotation pointing in the direction of the gaze ray, and follows a position which is offset by the change in `devicePosition`. In practice, users can gaze at a slider, pinch their fingers and move their hand right and left to drag it side-to-side.
- Added a UI canvas to `Main` sample scene, configured to use the `XRUIInputModule` from `com.unity.xr.interaction.toolkit`.
- Added an `InputSystem UI` scene configured to use the `InputSystemUIInputModule` from `com.unity.inputsystem`.
- Added an affordance to the Apple visionOS settings UI to install PolySpatial packages if the user switches AppMode to Mixed Reality or clicks the Install Packages button visible when AppMode is set to Mixed Reality.
- Add Windowed AppMode.

### Changed
- Updated Xcode version used to build native libraries to 15.2 (15C500b)
- Renamed `devicePosition` and `deviceRotation` input controls to `inputDevicePosition` and `inputDeviceRotation`.

### Deprecated

### Removed

### Fixed
- Use the correct deployment target version `1.0` when invoking `actool` to compile image marker libraries.
- Fixed an issue in samples where the world anchor that is placed by user input used an empty GameObject instead of a visible prefab.
- Fixed the `HandVisualizer` script in package samples to properly disable joint visual GameObjects when the joint is not tracked.

### Security

## [0.7.1] - 2023-12-13

### Added
- Added a step to the build pre-processor which disables splash screen on visionOS player builds.
- Enabled foveated rendering for VR builds on Unity 2022.3.16f1 and above.
- Added extension method `TryGetVisionOSRotation` to `XRHandJoint` when using the `UnityEngine.XR.VisionOS` namespace. If you depended on the rotations reported before this version, use this `TryGetVisionOSRotaiton` instead of the rotation reported from `XRHandJoint.TryGetPose`.

### Changed
- Changed the platforms behavior to report rotations of hand joints through `XRHandSubsystem` that align more closely with OpenXR's rotations. If you depended on the previous reporting of rotations, use the rotation reported by `TryGetVisionOSRotation`, a new extension method to `XRHandJoint`.
- All packages now require 2022.3.15f1 and later (rather than 2022.3.11fa and later) to pick up fixes for various memory leaks made in 15f1.

### Deprecated

### Removed
- Support for Unity versions earlier than 2022.3.11f1.
- Removed gray "Loading..." window in VR builds. VR apps now launch directly into the immersive space.

### Fixed
- Fixed a linker error in Xcode when building the visionOS player with App Mode set to VR, but the visionOS loader is not enabled.
- Fixed a memory leak in `VisionOSHandProvider`.
- Fixed a memory leak caused by using particle systems in VR mode.
- Implemented lifecycle management. Unity now suspends and resumes properly when the home menu is brought forward.
- Fixed an issue where closing the gray "Loading..." window would mute audio.
- Fixed an issue where spatial audio would use the gray "Loading..." window as its source location.
- XRHMD Input device now properly reports HMD input. This enables existing VR projects and templates to properly track head movement in visionOS VR builds.

### Security

## [0.6.3] - 2023-11-28

### Added

### Changed
- Changed license check modal option from "See Pricing" to "Learn about a 30-day trial".

### Deprecated

### Removed

### Fixed

### Security

## [0.6.2] - 2023-11-13

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.6.1] - 2023-11-09

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.6.0] - 2023-11-08

### Added
- Added additional input controls on `VisionOSSpatialPointerDevice` which are needed to drive an XR Ray Interactor.
- Added VR samples for both Built-in and Universal Render Pipelines.

### Changed

### Deprecated

### Removed

### Fixed
- Fixed compile errors when the project has `com.unity.render-pipelines.core` but not `com.unity.render-pipelines.universal`.
- Fixed issue with over releasing material references for canvas items.
 
### Security

## [0.5.0] - 2023-10-26

### Added
- `VisionOSSpatialPointerDevice` for pinch/gaze input support in VR mode.

### Changed

### Deprecated

### Removed

### Fixed

### Security

## [0.4.3] - 2023-10-13

## [0.4.2] - 2023-10-12

## Fixed
- Fixed an issue where VR builds would only render to the left eye in device builds when using the built-in pipeline.

## [0.4.1] - 2023-10-06

### Added
- PolySpatial now supports Xcode 15.1 beta 1 and visionOS 1.0 beta 4
- Project Validation rules for Linear Color Space, ARSession + ARInputManager components, and cameras generating depth textures inside of the VisionOS XR package

### Removed

- Removed `VisionOSSettings.renderMode`, `VisionOSSettings.deviceTarget`, and related `visionos_config.h` file that was generated during builds. The XR plugin will automatically switch between single-pass and multi-pass rendering depending on whether the app was built for the visionOS simulator or a device.

### Fixed

- Fixed an issue where VR builds would only render to the left eye in device builds when using the built-in pipeline.

## [0.3.3] - 2023-09-28

### Changed

- Revert changes that were mistakenly included in 0.3.2

## [0.3.2] - 2023-09-27

### Changed

- Use renamed `ar_skeleton_get_anchor_from_joint_transform_for_joint` API. This fixes an issue where builds are rejected on TestFlight for using deprecated `ar_skeleton_get_skeleton_root_transform_for_joint` API.

## [0.3.1] - 2023-09-13

### Fixed

- Fixed linker errors in Xcode when building without visionOS loader enabled.

## [0.3.0] - 2023-09-12

### Added

- VisionOSSessionSubsystem now returns a structure including the native session pointer from the `nativePtr` property.

### Changed

- Xcode beta 8 and visionOS beta 3 compatibility.
- Static libraries were rebuilt with Xcode Version 15.0 beta 8 (15A5229m).

### Fixed

- Fixed an issue where plane detection would be disabled if meshing was not enabled.

## [0.2.0] - 2023-08-21

### Changed

- Xcode beta 5 and visionOS beta 2 compatibility
- Static libraries were rebuilt with Xcode Version 15.0 beta 2 (15A5161b).

### Fixed

- Fixed issues with AR mesh alignment.
- Fixed issues with AR anchor position.
- Fixed issues with AR authorization and session startup.
- Fixed an issue where Plane alignment values would not match the values expected by AR Foundation.

## [0.1.3] - 2023-07-19

### This is the first release of *Unity Package Apple visionOS XR Plugin*.

*Provides XR support for visionOS*
