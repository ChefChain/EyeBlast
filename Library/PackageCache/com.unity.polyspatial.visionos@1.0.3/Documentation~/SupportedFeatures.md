---
uid: psl-vos-supported-features
---
# Supported Unity Features & Components
<a name="supported-unity-features-and-components"></a>
The majority of Unity components will work without modification on this platform - including most custom MonoBehaviours, animation logic, physics, input handling, asset management, AI, and so forth. However, components that need to be rendered require special support. Consequently, some components offer a reduced feature set on this platform; others are not currently supported. The tables below summarize the current state of various rendering component support on this platform. 

For more information about converting legacy projects, see also [Porting Unity Projects to PolySpatial XR](PortingUnityProjectsToPolySpatialXR.md)

## Unity Simulation Components / Systems
It's impossible to list all the systems and packages that Unity exposes in this page but the table below lists the status of support for a range of core Unity features: 

| **Component**             | **Status**               |
|---------------------------|--------------------------|
| **Transform**             | Supported                |
| **Audio**                 | Not fully spatialized    |
| **MeshFilter**            | Supported                |
| **Animation / Animators** | Supported                |
| **2D Physics**            | Supported                |
| **3D Physics**            | Supported                |
| **Scripts**               | Supported                |
| **AI & Navmesh**          | Supported                |
| **Terrain**               | Experimental support     |

`MonoBehaviours` are expected to work but they will depend on a case by case basis depending on which other components your scripts interact with.

## Rendering Components / Systems
<a name="rendering-components-systems"></a>

| **Component**             | **Status**               |
|---------------------------|--------------------------|
| **MeshRenderer**          | Lighting must be set up via material      No support for this component in Immediate mode     No support for "Additional Settings" (dynamic occlusion, rendering layer) |
| **SkinnedMeshRenderer**   |  Unoptimized animation only (the **Optimize Game Objects** option on the Rig tab of the Model Import inspector must be ticked off if it appears.) |
| **Particle Systems**      | Partial support; see [Particle Systems](#particle-systems) below |
| **Trail Renderer**        | Partial support; see [Particle Systems](#particle-systems) below |
| **Light**                 | Up to 4 dynamic lights only, and requires each affected material to include a [PolySpatial Lighting Node](PolySpatialLighting.md) in its shadergraph |
| **Camera**                | Not supported |
| **Halo**                  | Not supported |
| **Lens Flare**            | Not supported |
| **Line Rendering**        | Not supported |
| **Projector**             | Not supported |
| **Visual Effects**        | Not supported |
| **Lens Flare**            | Not supported |
| **Level of Detail (LoD)** | Not supported |
| **Occlusion Area**        | Not supported |
| **Occlusion Portal**      | Not supported |
| **Skybox**                | Not supported |
| **URP Decal projector**   | Not supported |
| **Tilemap Renderer**      | Not supported |
| **Video Player**          | Limited support |
| **Graphics Raycaster**    | Not supported |
| **Shaderlab Shaders**     | Not supported |
| **Post Processors**       | Not supported |
| **Lightmapping**          | Limited support through [PolySpatial Lighting Node](PolySpatialLighting.md) |
| **Baked Lighting**        | Limited support through [PolySpatial Lighting Node](PolySpatialLighting.md) |
| **Enlighten**             | Not supported |
| **Light Probes**          | Limited support through [PolySpatial Lighting Node](PolySpatialLighting.md) |
| **Reflection Probes**     | Limited support through [PolySpatial Lighting Node](PolySpatialLighting.md) |
| **Trees**                 | Not supported |
| **Fog**                   | Not supported |

Some of these features are not supported due to platforms constraints (for example, full screen graphics post processors aren't compatible with the idea of a shared rendering system), while others are areas of ongoing or planned development.

### Particle systems
<a name="particle-systems"></a>
Support for Unity's built-in particles under PolySpatial is actively being developed. Currently, it offers several alternate modes, each with its own set of quality and performance tradeoffs. You can configure these modes project-wide under `Project Settings > PolySpatial > Particle Mode`.

### Supported Modes:
- **Replicate Properties**: This mode aims to map Unity particle system properties to the native particle systems offered by RealityKit. While it offers relatively good performance, visual quality can vary significantly, especially for particles that utilize advanced features. Custom shaders are not supported in this mode.

- **Bake to Mesh**: In this mode, a dynamic mesh is baked for every particle system every frame. It closely aligns the visuals with Unity rendering, allowing leverage of most features of Unity's built-in particle systems, including custom shaders authored with ShaderGraph. However, this mode currently imposes a significant performance overhead. We are actively working to improve performance for baked particles.

**Note:** Baked Mesh billboard particles only face the camera if there's an unbounded volume camera and ARSession in the scene. You can add an ARSession to the scene by right-clicking in the Editor **Hierarchy** window and selecting **XR &gt; AR Session** from the context menu.

**Note:** VFXGraph is not currently supported in PolySpatial.

#### Replicate Properties Support
When using the "Replicate Properties" particle mode in PolySpatial, Particle System modules are supported to varying degrees, as summarized in the table below:

| **Module**                       | **Status**          |
|----------------------------------|---------------------|
| **Emission**                     | Partially supported |
| **Shape**                        | Partially supported |
| **Velocity over lifetime**       | Partially supported |
| **Limit Velocity over lifetime** | Partially supported |
| **Inherit velocity**             | Partially supported |
| **Force over lifetime**          | Partially supported |
| **Color over lifetime**          | Partially supported |
| **Color by speed**               | Not Supported       |
| **Size over lifetime**           | Partially supported |
| **Size by speed**                | Not Supported       |
| **Rotation over lifetime**       | Partially supported |
| **Rotation by speed**            | Not Supported       |
| **External Forces**              | Not Supported       |
| **Noise**                        | Partially supported |
| **Collision**                    | Partially supported |
| **Triggers**                     | Not Supported       |
| **Sub Emitters**                 | Partially supported |
| **Texture sheet animation**      | Partially supported |
| **Lights**                       | Not Supported       |
| **Trails**                       | Not Supported       |
| **Custom Data**                  | Not Supported       |
| **Renderer**                     | Partially supported |

## User Interface (UI)
[Unity UI](https://docs.unity3d.com/Manual/com.unity.ugui.html) works in world space, but screen space UI and advanced visual features like masking, shadowing, etc do not currently work. The table below summarizes the supprot status for other UI features:

| **Component**        | **Status**                                                                         |
|----------------------|------------------------------------------------------------------------------------|
| **TextMesh**         | Supported                                                                          |
| **Canvas Renderer**  | Partially Supported                                                                |
| **Sprite Renderer**  | Supported                                                                          |
| **TextMesh Pro**     | &#8226; Partially Supported<br/>&#8226; SDF only<br/> &#8226; No custom shaders |
| **Rect Transform**   | No specific support for sizing                                                     |  
| **Platform Text**    | See [Platform Text Rendering](PlatformText.md)                                     |
# Final thoughts
Unity has many more components, but the main parts of the average XR app were covered in this section. Generally speaking, your existing Unity projects will likely require work to port to PolySpatial XR.

You will need to experiment, investigate, and adapt to the PolySpatial XR requirements and constraints by either writing your own PolySpatial XR-compatible systems or finding workarounds to these limitations to support your existing features.
