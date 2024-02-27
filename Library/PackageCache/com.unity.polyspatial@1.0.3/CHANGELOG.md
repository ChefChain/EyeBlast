---
uid: ps-changelog
---
# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2024-01-20

### Added
- Added support for PolySpatial Time shader graph node.
- Added support for inline sprites in TextMeshPro strings.
- Added support for Built-in Render Pipeline's Unlit and Lit Particle shaders.
- Added support for Universal Render Pipeline's Lit Particle shader.
- Added support for Radians setting of Rotate shader graph node.
- Added links to PolySpatial API docs/changelog from PolySpatial visionOS docs.
- Added runtime validations option to PolySpatial project settings.
- Added instructions panel to MixedReality and Image Tracking sample scenes as an example of an object that always faces the camera, and to provide an affordance for returning to the ProjectLauncher scene.
- Added device rotation and distinct device position (was a copy of interaction position) to events sent by UnityPlayModeInput script.
- Added a list of `MaterialSwapSet`s to `PolySpatialSettings`. `MaterialSwapSet`s must now be explicitly added to this list in order to be used.
- Added StackTrace and Categories logging options to the PolySpatial settings `Project Settings > PolySpatial > Logging`.

### Changed
- PolySpatial Sorting Group components can now override sprite/UI sorting.
- Manipulation sample now supports grabbing two cubes.
- Sample panel in ProjectLauncher scene now blocks gaze interactions so you don't accidentally click the bubble behind the panel.
- Changed the layer of the Default AR Plane in samples so that it no longer blocks input.
- We changed the names of a few classes and provided automatic API updaters for them to help with the transition.
    - `PolySpatialHoverEffect` is now `VisionOSHoverEffect`
    - `PolySpatialGroundingShadow` is now `VisionOSGroundingShadow`
    - `PolySpatialImageBasedLight` is now `VisionOSImageBasedLight`
    - `PolySpatialImageBasedLightReceiver` is now `VisionOSImageBasedLightReceiver`
    - `PolySpatialSortingGroup` is now `VisionOSSortingGroup`
    - `PolySpatialVideoComponent` is now `VisionOSVideoComponent`
    - `UnityPolySpatialPlatformText` is now `VisionOSNativeText`
    - `PolySpatialRaycaster` is now `PolySpatialUIRaycaster`
- PolySpatial-specific logging settings will be respected in player builds.
- Changed shader graph test input data folder to Data, rather than Resources.
- Renamed `devicePosition` and `deviceRotation` input controls to `inputDevicePosition` and `inputDeviceRotation`.

### Deprecated

### Removed
- Removed a call to `Resources.LoadAll` which caused all assets in Resources to be loaded into memory.
- Removed deprecated PolySpatialTouchspace input device.

### Fixed
- Fixed invalid MaterialX generation for Vertex ID shader graph node.
- Fixed issue with sprite flipping (x flips and y flips were swapped).
- Fixed issue with TextMeshPro vertex colors in UI components.
- Fixed warnings from shader graph imports in URP samples.
- Fixed issue with toggle visuals not reflecting state.
- Improved text rendering at distance/in peripheral vision.
- Fixed frequency/amplitude of shader graph Gradient Noise node.
- Fixed issue with pink materials temporarily appearing on BakeToMesh particle systems.
- Fixed crash when using PolySpatial Image Based Light Receiver on Play to Device.
- Fixed error logged on first import: `A new PolySpatialSettings asset was initialized when its asset already exists. Was PolySpatialSettings.instance used by an asset importer?`
- Fixed crash caused by meshes with tangents but no normals.
- Fixed colliders to update when mesh updates occur in Unity host environment.
- Fixed baked mesh particles not scaling relative to volume camera transform in unity backend.
- Fixed issue with transparent textures on unlit materials rendering as opaque.
- Fixed issue with meshes set to `None` rendering as spheres.
- Fixed issue with Renderers set to `Shadows Only` being visible.
- Fixed two minor NativeArray leaks.
- Fixed scale of platform text.
- Fixed handling of device rotation in manipulation sample.
- Fixed pink UI/text in Unity play mode when using built-in render pipeline.
- Fixed issue with lowered frame rate when using an Animator on UI element.
- Fixed issue with URP unlit particle material not incorporating base color.
- Fixed `HandVisualizer` script in samples to properly hide visuals for hand skeleton joints when they lose tracking.
- Fixed an issue where the virtual keyboard would not show when selecting UI text fields.
- Fixed issue where setting empty text on TextMeshProUGUI component would still render previous text.
- Fixed issue with hit tests and Built-In Render Pipeline
- Fixed issue with validation processing scene objects with HideAndDontSave hideFlags.
- Fixed issue with using RenderTextures in RawImage components.
- Fixed potential failure caused by font manager asset remaining after a failed build.

### Security

## [0.7.1] - 2023-12-13

### Added
- Added PolySpatial Environment Radiance shader graph node.
- Added support for more shader graph nodes: Blackbody, Dielectric Specular, Checkerboard, Rounded Polygon, Rounded Rectangle, and Ambient.
- Added "Unseparated" option for PolySpatial Sorting Group depth pass property.
- Explicitly present the dimensions of output volume configurations as meters.
- Added support for bold text in Text Mesh Pro components.
- Added a list of available devices in your network to the **Play To Device** window (menu **Window > PolySpatial > Play To Device**). You can select the desired device to connect or add direct connections. 
- Added PolySpatial Image Based Light/Image Based Light Receiver components.
- Added support for blended tilesheet animations for BakeToMesh Particle Systems.
- Added support for custom user assigned font for PolySpatialTextComponent.
- Added backup implementation for Text Component to support macOS and iOS.
- Added support for Emission map and color for unlit URP particle shader.
- Added additional documentation for RenderTexture usage.
- Added documentation for shader graph properties and targets.
- Added 10 volume camera configurations to PlayToDevice project. If a connecting app requests a camera configuration that is not available, it will be rerouted based on cubic volume and aspect ratio. 
- Volume Cameras now have opened/closed/resized/focused events that code can respond to.
- Volume Cameras can have their volumetric windows explicitly opened and closed.

### Changed
- Renamed VolumeCameraConfiguration to VolumeCameraWindowConfiguration
- All packages now require 2022.3.15f1 and later (rather than 2022.3.11f1 and later) to pick up fixes for various memory leaks made in 15f1.

### Deprecated

### Removed
- Support for Unity versions earlier than 2022.3.11f1.

### Fixed
- Fixed issue with opaque shader graphs not being rendered before transparent ones.
- Fixed issue with shader graph view space positions/normals/tangents/bitangents being read in vertex stage.
- Fixed issue with shader graphs sampling vertex colors in meshes that lack explicit ones.
- Fixed issue with clipped UI elements (such as scroll view contents) with non-identity rotations.
- Fixed issue with Lit shaders being generated when material type set to Unlit.
- Fixed ArgumentNullException when using non-directional lightmaps.
- Fix for visual discontinuities when updating meshes/textures.
- Fixed issue with "Fix This Object" button in the Inspector view throwing errors.
- Fixed issue with `VolumeCamera` scene handles being drawn in the wrong position when not in the origin.
- Fixed issue with input and UI objects when outside the viewable area of the main camera.
- Fixed performance regression related to light and reflection probe tracking.
- Fixed issue with persisting Particle Systems GameObject post-deletion.
- Fixed issue with wrapping on 3D textures sampled in clamp mode in shader graphs.
- Fixed issue with VolumeToWorld transform being overwritten when keywords/globals were changed.
- Fixed warnings from shader graph imports in URP package.
- Fixed issue with standard shaders (e.g. URP/Lit, URP/Unlit) using alpha-to-coverage when semi-transparent.
- Fixed Recording & Playback framerate limiting using built-in framerate controls
- Fixed issue with PolySpatialSortingGroup items not being sorted correctly if their activation state changes.
- Fixed crash on disabling GameObjects containing particle systems.
- Fixed color space issue with HDR shader graph properties.

### Security

## [0.6.3] - 2023-11-28

### Added

### Changed

### Deprecated

### Removed

### Fixed
- Fixed typo on project settings on the "Hide PolySpatial Preview Objects In Scene" field.
- Fix one frame pink textures on scene transition.
- Cleanup BakeToMesh Particle and Trail meshes when Gameobject set inactive.
- Fixed issue with shader graph dependencies of prefabs in Resources not being included in the build.
- Fixed KeyNotFoundException when using multiple instances of the same input property in a shader subgraph.
- Fixed MaterialX generation for shader graph Swizzle nodes that input and output floats.
- Fixed default values for shader graph custom interpolators.
- Fixed failure to display warnings for unsupported nodes in shader subgraphs.
- Fix for shader graph input properties connected directly to outputs.
- Fixed issue with using Transform/Transformation Matrix nodes in subgraphs outputting to vertex stage.
- Fixed offset for bounded volumes using PolySpatial Volume to World/PolySpatial Lighting nodes.
- Fixed offset z coordinates for box/sphere/capsule colliders.
- Fixed crash related to overlapping colliders.
- Fixed issue where textures were not being unregistered after material unregistration due to a scene unload.

### Security

## [0.6.2] - 2023-11-13

### Added

### Changed

### Deprecated

### Removed

### Fixed
- Fixed issue where simulation app may crash when connecting to PlayToDevice.
- Fixed pink materials in Play Mode for AR Foundation simulation environments.
- Fixed color spaces for shader graph color properties and PolySpatialPlatformText.

### Security

## [0.6.1] - 2023-11-09

### Added

### Changed

### Deprecated

### Removed

### Fixed
- Baked Particles are now rendered with respect to AR head pose (when available, such as with an unbounded volume camera), and otherwise falls back to the main camera (if available)

### Security

## [0.6.0] - 2023-11-08

### Added
- Added partial support for Particle System's custom vertex streams.
- Added PolySpatialGroundingShadow component.
- Added support for Normal From Texture shader graph node.

### Changed

### Deprecated

### Removed

### Fixed
- Fixed Volume Camera Configuration Handles for rotated Volume Cameras
- Fix over-release of shader assets.
- Fixed failure to use native texture/render texture path on visionOS.
- Fix initial UGUI display issue when properties don't change at start.
- Fixed an issue with editor input not releasing a touch when mouse is out of bound.
- Samples: Fixed issue with keeping the target position from going outside the platform in Character Navigation sample.
- Fixed "Unparseable GUID" error when loading assets from still-loaded asset bundles. 
- Fixed "Non shader graph shader" errors for standard materials (URP/Lit, etc.) loaded from asset bundles.
- Fixed issue with PolySpatial Lighting Node normals for rotated volume cameras.
- Fixed issue with Transform node in Tangent space.
- Fixed an issue with particle system renderer module not change-tracking correctly.
- Fixed application of Vector2 shader graph properties in visionOS.
- Fixed BakeToMesh Particle System depth sorting
- Fixed shader compilation error with PolySpatial Lighting Node with reflection probes enabled and baked lighting disabled.
- Fixed NullReferenceException when building asset bundles containing scenes.
- Fixed an issue where touch screen input would report more active touches than expected if a user initiates a pinch/gaze and the hand that initiated the interaction is no longer tracked.
- Fixed issue where input in Game View would not work if the EventSystem was switched to use the new Input system UI module.
- Fixed KeyNotFoundException when using multiple instances of the same input property in a shader subgraph.
- Fixed MaterialX generation for shader graph Swizzle nodes that input and output floats.
- Fixed color spaces for shader graph color properties and PolySpatialPlatformText.
- Fixed default values for shader graph custom interpolators.
- Fixed issue with using Transform/Transformation Matrix nodes in subgraphs outputting to vertex stage.
- Fix for shader graph input properties connected directly to outputs.
- Fixed failure to display warnings for unsupported nodes in shader subgraphs.

### Security

## [0.5.0] - 2023-10-26

### Added
- "Disable Tracking Mask" in PolySpatial settings allows omitting objects from tracking based on their layer at creation.
- Added support for new shader graph nodes: Channel Mixer, Replace Color, White Balance, Fade Transition, Channel Mask, Color Mask, Flip
- Added support for platform base text rendering through the new UnityPolySpatialPlatformText component.
- Added Trail support for BakeToMesh Particle Systems.
- Added support for RGB/linear conversions to shader graph Colorspace Conversion node.
- Enhanced Volume camera scene view interaction by adding several handles that let you visualize the position and volume of a volume camera when not selected in the hierarchy.
- Added support for the Refract shader graph node.

### Changed
- Fixed a typo in PolySpatialPointerKind: `indDirectPinch` -> `indirectPinch`
- Fixed an issue accessing UV1 in shader graphs.
- Changed the access modifier of the serialized fields in the PolySpatial settings (class `PolySpatialSettings`) from public to private and renamed these members to include the `m_` prefix.
- Renamed the PolySpatial settings `Enable Default Volume Camera` to `Auto-Create Volume Camera`.
- Updated XRTouchSpaceInteractor to use SpatialPointerDevice. Fixed XRIDebug sample scene.

### Deprecated
- Uses of PolySpatialTouchspace upgraded from warning to error.

### Removed
- Removed obsolete UI and hand tracking building blocks.

### Fixed
- Fixed an issue where deleting a canvas renderer instance at runtime could cause an OOB exception in the tracker.
- Fixed an issue causing incorrect mesh index buffer sizes.
- Fixed shader graph world position node outputs for output volumes with heights greater than 2.
- Fixed inconsistent RGB/HSV conversion in shader graph Colorspace Conversion node.
- Fixed an issue with updating texture contents.
- Fixed invalid transforms for shader graph Transform and Transformation Matrix nodes used in vertex stage.
- Fixed issue with sprite textures wrapping at edges.
- Fixed an issue where `SpatialPointerDevice` events reported the Began phase for more than one frame in a row.
- Fixed KeyNotFoundException when using CustomRenderTextures.
- Fixed an issue with shader graphs failing to load if their filenames contained hyphens.
- Fixed output of Normal Vector/Tangent Vector/Bitangent Vector nodes in Tangent space.
- Fixed crash in visionOS when there were more submeshes in a Mesh than materials in the MeshRenderer.
- Fixed shader graph Dither node.
- Fixed an issue where `UnityPlayModeInput` would override the current `UIInputModule`'s action asset to null, breaking UI input in play mode.
- Fixed failure to transfer float shader graph properties in "Slider" mode.
- Fixed issue with invalid properties when materials/shader graphs had duplicate names.
- Fixed an issue with the SDF Text Shader that was causing brightening in text coloration.
- Fixed issue with global texture properties in shader graphs.

### Security

## [0.4.3] - 2023-10-13

## [0.4.2] - 2023-10-12

## Added
- Documentation for Volume Camera around configuration assets.

## Fixed
- Build error if trying to build for Simulator SDK in Unity prior to 2022.3.11f1.
- Fixed an issue causing incorrect negative scales.
- Sprite performance improvements.
- Improved CanvasRenderer/UI performance. Best performance metrices will only be attained with Unity 2022.3.12f1 or later.

## [0.4.1] - 2023-10-06

## [0.4.0] - 2023-10-04

### Added
- PolySpatial now supports Xcode 15.1 beta 1 and visionOS 1.0 beta 4
- PolySpatial now supports transferring 3D textures and sampling them in shader graphs.
- PolySpatial Lighting Node now supports reflection probes.

### Changed
- Updated dependency on `com.unity.collections` to version 2.1.4 to resolve conflicts with `com.unity.entities`

### Fixed
- Native texture pointers for RenderTextures no longer cached, which may fix issues with RenderTextures that are released and recreated.
- Sprite performance improvements.

## [0.3.2] - 2023-09-18

## [0.3.1] - 2023-09-15

## [0.3.0] - 2023-09-13

## [0.2.2] - 2023-08-28

## [0.2.1] - 2023-08-25

## [0.2.0] - 2023-08-21

### Added
- PolySpatial now supports Xcode 15 beta 5 & 6
- Lighting
    - Adds PolySpatial Lighting node, which allows developers to opt into using Unity's internal lighting in addition to the IBL lighting model provided by RealityKit
    - Lighting parameter shader globals (dynamic lights, light probe volumes, and baked lightmaps) are now accessible via Unity ShaderGraph & MaterialX

- Materials/ShaderGraph/MaterialX
    - Adds limited support for the custom funtion node in Unity ShaderGraph (see documentation for details)
    - Additional materialX nodes are now supported (see documentation for details)
    - Unity PBR materials now map to the RealityKitPBR shader node rather than the UsdPreviewSurface shader node, as the RealityKitPBR supports additional useful features


### Fixed
- Input
    - XRI now works with QuantumTouchSpace
    - Other assorted input fixes
- UI
- Other Bug fixes
    - Native Texture & UI texture fixes
    - Fixes for sprite UVs
    - ARKit meshes now appear in MR mode
    - Fixes null object error related to particle systems
    - Fixes bad reference caused by inactive skinned mesh renderers
    - Fixes crash if mesh collider was disabled at scene load.
    - Other sssorted bug fixes

## [0.1.2] - 2023-08-16

### Added
- `WorldTouchState.colliderObject`, which provides the GameObject targeted by input. 

### Fixed

- Crash when loading a scene containing a disabled mesh collider.
- In-use skinned meshes were removed from scenes when calling `Resourced.UnloadUnusedAssets()`. 
- Project Validation entries not displaying when targeting visionOS. These validation entries no longer appear when targeting Standalone, iOS, or Android.
- Samples: Input now uses `worldTouch.colliderObject` to identify the hit object, fixing issues where input would occasionally miss the targeted object. 
- Samples: Objects reset position after being manipulated. 
- Samples: Missing script warnings resolved. 
- Samples: Settings assets removed from samples, fixing settings-related issues. 
- Samples: Samples now import to the Assets/Samples/ folder rather than Assets/ directly. 
- Documentation: Fixed broken links.
- Documentation: Component reference images displaying. 
- Documentation: Various typo fixes and clarity improvements.

## [0.1.2] - 2023-08-16

### Added
- `WorldTouchState.colliderObject`, which provides the GameObject targeted by input. 

### Fixed

- Crash when loading a scene containing a disabled mesh collider.
- In-use skinned meshes were removed from scenes when calling `Resourced.UnloadUnusedAssets()`. 
- Project Validation entries not displaying when targeting visionOS. These validation entries no longer appear when targeting Standalone, iOS, or Android.
- Samples: Input now uses `worldTouch.colliderObject` to identify the hit object, fixing issues where input would occasionally miss the targeted object. 
- Samples: Objects reset position after being manipulated. 
- Samples: Missing script warnings resolved. 
- Samples: Settings assets removed from samples, fixing settings-related issues. 
- Samples: Samples now import to the Assets/Samples/ folder rather than Assets/ directly. 
- Documentation: Fixed broken links.
- Documentation: Component reference images displaying. 
- Documentation: Various typo fixes and clarity improvements.

## [0.1.0] - 2023-07-19

## [0.0.4] - 2023-07-18

## [0.0.3] - 2023-07-18

## [0.0.2] - 2023-07-17

### Added
- Initial PolySpatial package.

### Changed (from most recent pre-release)
- The codename `Quantum` has been changed to its release name `PolySpatial` with corresponding name changes throughout the package. 

- The volume camera, previously called `QuantumVolumeCamera` is now just called `VolumeCamera` and existing instances will need to be fixed. Other changed class names include:
    - `QuantumHoverEffect` is now `PolySpatialHoverEffect`

- The settings asset has been renamed, so you will need to recreate your `PolySpatial` project settings.
