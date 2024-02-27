---
uid: psl-vos-porting-xr
---
# Porting existing Unity projects into PolySpatial XR
When porting an existing project to a new platform, it is important to know what potential technical risks and challenges you will face; PolySpatial XR is no exception. This section discusses several aspects of porting Unity projects, with emphasis on their support in PolySpatial XR.

Depending on the target platform some Unity features may not supported, or supported in a limited way, so you will need to plan your project with this info at hand. 

## Input
The Input System allows users to control your game or app using a device, touch, or gestures. In projects developed for PolySpatial XR, the supported Input system is the [New Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html). This input system is intended to be a replacement for Unity's classic Input Manager.

Projects that use [Unity's classic input system](https://docs.unity3d.com/ScriptReference/Input.html) will not work and are required to be ported to use the new input system as mentioned above.

## Rendering

### Render Pipelines
By default Unity PolySpatial XR supports both [Universal Render Pipeline (URP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html) and [Unity's Built-in Render Pipeline](https://docs.unity3d.com/Manual/built-in-render-pipeline.html).

Unity PolySpatial XR doesn't support **custom ShaderLab shaders** on any render pipeline, and if your project uses custom shaders, all of them will have to be authored using [Unity's Shader graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html). The ParticleSystem component only supports materials with Unity's built-in shaders. Unity's Shader Graph support is an ongoing work in progress for particles.

### Rendering Components
See [Supported Rendering Components](SupportedFeatures.md#rendering-components-systems) for information about which rendering features and components are supported.

