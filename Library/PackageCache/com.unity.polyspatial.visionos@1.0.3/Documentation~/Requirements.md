---
uid: psl-vos-requirements
---
# visionOS PolySpatial Requirements & Limitations

## Requirements
### Unity Version Support 
The current version of this package has been tested for Unity 2022.3.18f1 or higher.

Your Unity install must include the **visionOS Build Support** module. You can download this module from the Unity Hub.

> [!NOTE]
> Versions of Unity before 2022.3 aren't supported.

### Hardware, OS, and Xcode
- Compiling for visionOS currently requires Xcode 15.2 
- You must currently use an Apple to compile for visionOS, and can only use the visionOS Simulator on Apple Silicon (M1, M2, etc.) Mac.
- Make sure **visionOS Build Support** and **iOS Build Support** are both installed.
- Currently support visionOS beta 4 `21N5259k`
- If you don't have visionOS hardware, you can use the simulator built into XCode.

For more information about development setup, refer to [Development & Iteration](DevelopmentAndIteration.md)

### Graphics 
On visionOS, Unity delegates all rendering to the platform so that the OS can provide the best performance, battery life, and rendering quality taking into account all currently running mixed reality applications. This imposes significant constraints on the graphics features that are available.

Rendering on RealityKit will most likely have visual differences over in Unity rendering. We are constantly working to improve visual equivalency between Unity and RealityKit but note there are differences.

#### Render Pipeline
Your project must use either the Universal Render Pipeline (URP) or the Built-in Render Pipeline. URP is preferred; if you are considering migrating your project, this would be a good opportunity to do so. Migration documentation is available for moving to URP from the Built-in pipeline: [Move on over to the Universal Render Pipeline with our advanced guide | Unity Blog](https://blog.unity.com/technology/move-on-over-to-the-universal-render-pipeline-with-our-advanced-guide)

#### Color Space
Your project must use [Linear color space](https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html).

#### Shaders and Materials
You can author custom shaders for visionOS using a subset of the Unity ShaderGraph. Behind the scenes, this is converted to MaterialX. ShaderLab and other custom coded shaders are not supported, as RealityKit for visionOS doesn't expose a low-level shading language. 

Several important standard shaders for each pipeline have been mapped to their closest available RealityKit analog. Current support includes:
* Standard URP shaders: Lit, Simple Lit, Unlit, (+TBD - more coming)
* Standard Builtin shaders: Standard, (+TBD – more coming)

For more information, see [PolySpatial Material Support](Materials.md)

## Known Limitations
Currently Unity PolySpatial XR is shipped as an alpha product. Since this is an early release, expect documentation, workflows, and especially API changes to occur, so plan projects with this in mind.

See [Supported Unity Features & Components](SupportedFeatures.md) for information about which Unity features will work without modification, and which need to be reconsidered. 

For information about materials supported on this platform, see [PolySpatial Material Support](Materials.md), and [Shader Graph Support](ShaderGraph.md) for details about implementing custom shaders via Unity Shader Graph and MaterialX.
