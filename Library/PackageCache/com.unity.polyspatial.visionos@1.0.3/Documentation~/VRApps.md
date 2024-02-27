---
uid: psl-vos-vr-apps
---
# Fully Immersive VR on visionOS

With Unity, users can leverage on familiar workflows to build [fully immersive experiences for VisionOS](https://developer.apple.com/documentation/visionOS/creating-fully-immersive-experiences), including virtual reality games or fully virtual environments. Today, Unity provides a wide [range of features and APIs](https://docs.unity3d.com/Manual/VROverview.html) that can be used to develop fully immersive experiences for visionOS. Such packages include:

* [visionOS plug-in]()
* [XR Interaction Toolkit](https://docs.unity3d.com/Manual/VROverview.html#xr-interaction-toolkit)
* [XR Core Utilities](https://docs.unity3d.com/Manual/VROverview.html#xr-core-utilities)
* [Input System](https://docs.unity3d.com/Manual/VROverview.html#input-system)
* [VR project template](https://docs.unity3d.com/Manual/VROverview.html#vr-template)
* [Hand tracking](https://docs.unity3d.com/Manual/VROverview.html#hand-tracking)

Once you’ve built your VR content in Unity, simply select **visionOS - Fully Immersive** on the XR Plug-in Management window, select and build for the visionOS platform, recompile native plugins and a Unity XCode Project file will be generated. From here on, [you’ll continue your development process in XCode](https://developer.apple.com/documentation/visionOS/creating-fully-immersive-experiences), where you can explore concepts like transitioning between windowed content and fully immersive content. 

>Please note that Unity is still building towards feature parity with the Metal API on XCode, so you might observe warnings from Metal’s API validation layer. To work around this, you can turn off the Metal API Validation Layer via XCode’s scheme menu.

## Porting VR experiences to visionOS 
For users who are looking to port existing VR titles looking to visionOS as a fully immersive experience there are a few things you can do to make the transition smoother, in addition to the build workflow elaborated in the above section. 

### 1. Prepare your graphics 
**Use Universal Render Pipeline (URP)** - visionOS supports Foveated Rendering, a technique that provides a higher-quality visual experience for users on the Vision Pro. To take advantage of the Foveated Rendering feature, we recommend URP, which allows for Foveated Rendering to be applied throughout the entire pipeline. We also plan to focus future improvements on URP specifically.

**Leverage Single-Pass Instanced Rendering** - Unity’s Single-Pass Instanced Rendering now supports the Metal Graphics API and will be enabled by default. This reduces the overhead of certain parts of the rendering pipeline like culling and shadows, and also helps to reduce CPU overhead when rendering your scenes in stereo. 

**Ensure depth-buffer for each pixel is non-zero** - on visionOS, the depth buffer is used for reprojection. To ensure visual effects like skyboxes and shaders are displayed beautifully, ensure that some value is written to the depth for each pixel.

**Check Project Validation** - there are a number of settings and scene configuration details that you will need for your app to function properly. They will be listed under `Project Settings > XR Plug-in Management > Project Validation` under the visionOS platform tab. If you see any issues, you can click `Fix` or `Fix All` to automatically resolve them. Some specific issues to look out for:
- The `Apple visionOS` loader must be enabled
- Your scene needs an `ARSession` in addition to the normally required `TrackedPoseDriver` for head tracking to work
- All cameras need a depth texture (you should enable `Depth Texture` in your URP settings)
- The only supported `Depth Texture Mode` (setting on `UniversalRenderer`) is `AfterOpaques`

### 2. Adapt your controller-based interactions to hands 
**Leverage the XR Interaction Toolkit (XRI)** - With visionOS, people will use their hands and eyes to interact with content. XRI provides a high-level interaction that abstracts implementation of hand-tracking and makes it easy to implement interactions like hover, grab and select in 3D space and in the UI for 3D spatial worlds. With XRI, you can also implement visual feedback for input, and its abstraction of input also makes it possible to author once, for multiple platforms.

**Dive into Unity Hands package** - If you’re looking to access even more flexibility in your input actions, Unity’s Hands Subsystem provides access to all the raw hand joint data via the Unity Hands Package.You can also learn about leveraging the Unity Hands Package for immersive experiences above. 

For more information on porting your VR experience to visionOS, we recommend watching this guide to “[Bring your Unity VR app to a fully immersive space](https://developer.apple.com/videos/play/wwdc2023/10093)”. 
