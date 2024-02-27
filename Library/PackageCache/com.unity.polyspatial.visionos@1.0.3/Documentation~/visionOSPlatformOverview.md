---
uid: psl-vos-platform-overview
---
# visionOS Platform Overview

visionOS is the operating system of the Apple Vision Pro, Apple's latest spatial computing device. Unity developers can leverage existing 3D scenes and assets to build games or applications for visionOS. For more information about visionOS, see Apple's [visionOS Overview](https://developer.apple.com/visionos/learn).

visionOS provides a few different modes in which apps can be displayed: Windows, Volumes, and Spaces. You can use Windows to present 2D or 3D content (without stereo), or use Volumes to present 3D content and objects. When you use Volumes, users of your app have the flexibility to walk around and interact with 3D content from any angle.

Depending on application type, visionOS apps can run in either a **Shared Space** or a **Full Space**. The Shared Space is a multitasking environment similar to the desktop of a personal computer. In this mode, users can see and interact with Windows and Volumes from multiple applications simultaneously. To create a more immersive experiences, you can target your applications for a dedicated **Full Space**, which displays content exclusively from one app at time. Windowed apps developed in Unity always run in a Shared Space. Fully immersive (VR) content always runs in a Full Space, while Immersive (MR) content can switch between the Shared Space and a Full Space.

# PolySpatial on visionOS

<a name="visionos-platform-overview"></a>
PolySpatial's support for **visionOS** combines the full power of Unity's Editor and runtime engine with the rendering capabilities provided by **RealityKit**. Unity’s core features - including scripting, physics, animation blending, AI, scene management, and more - are supported without modification. This allows game and application logic to run on visionOS like any other Unity-supported platform, and the goal is to allow existing Unity games or applications to be able to bring over their logic without changes.

For rendering, visionOS support support is provided through RealityKit. Core features such as meshes, materials, textures should work as expected. More complex features like particles are subject to limitations. Advanced features like full screen post processing and decals are currently unsupported, though this may change in the future. For more details, see [visionOS PolySpatial Requirements & Limitations](Requirements.md) and [Supported Unity Features & Components](SupportedFeatures.md). 

Building for the visionOS platform using PolySpatial in Unity adds new functionality to support XR content creation that runs on separate devices, while also having a seamless and effective development experience. Most importantly, Unity PolySpatial for visionOS reacts to real-world and other AR content by default like any other XR Unity app.

### visionOS Application Types
Unity supports several different application types on visionOS, each with their own advantages:
* If you're interested in creating fully immersive virtual reality (VR) apps for visionOS, refer to [Fully Immersive VR apps on visionOS](VRApps.md) for more information.
* If you're interested in creating immersive mixed reality (MR) apps for visionOS, refer to [PolySpatial MR Apps on visionOS](PolySpatialMRApps.md) for more information. These apps are built with Unity's newly developed PolySpatial technology, where apps are simulated with Unity, but rendered with RealityKit, the system renderer of visionOS.
* If you're interested in creating content that will run in a window on visionOS, refer to [Windowed Apps on visionOS](WindowedApps.md) for more information.
