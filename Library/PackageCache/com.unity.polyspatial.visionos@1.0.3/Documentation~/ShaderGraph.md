---
uid: psl-vos-shader-graph
---
# Shader Graph Support
You can use the Unity Shader Graph to create custom materials for visionOS. These will previewed in their compiled form within Unity, but converted to MaterialX for display in simulator and on device. While MaterialX is very expressive, some Shader Graph nodes have no analog in materialX. Within the Shader Graph editor, unsupported nodes will be indicated by the presence of a `#` symbol.

For technical, security, and privacy reasons, visionOS does not allow Metal-based shaders or other low level shading languages to run when using AR passthrough. 

## Texture Limitations
When sampling textures in shader graphs, note that the sampler state (filter, wrap modes) associated with the texture itself is ignored.  Instead, you must use the `Sampler State` node to control how the texture is sampled if you want to use a mode other than the default (linear filtering, repeat wrap mode).

## Coordinate Space Notes
There are two caveats to be aware of when converting between coordinate spaces in shader graphs.  Because content is transformed according to the [Volume Camera](VolumeCamera.md), the "world space" geometry returned within a shader graph will not match that of the simulation scene.  Furthermore, visionOS is currently inconsistent with regards to the geometry it returns and the transformation matrices it supplies.

### Retrieving the Geometry of the Source Scene
To obtain positions, normals, tangents, or bitangents in the world space of the simulation scene, use the `Position`, `Normal Vector`, `Tangent Vector`, or `Bitangent Vector` nodes with `Space`: `World`, then transform them using the `PolySpatial Volume to World` node with a `Transform` appropriate to the type (typically `Position` for positions and `Direction` for the rest, which will be normalized after transformation).

### Notes on Transform and Transformation Matrix Nodes in VisionOS
The matrices returned by the `Transformation Matrix` node and used by the `Transform` node are obtained directly from visionOS and currently assume a world space that does not match either the simulation scene or the output of the `Position`, `Normal Vector`, `Tangent Vector`, or `Bitangent Vector` nodes.  The "world space" output of those nodes is relative to the transform of the output volume--that is, it does not change when a bounded app volume is dragged around.  The `Transform` and `Transformation Matrix` nodes, on the other hand, assume a world space that is shared between all app volumes.  To get geometry in this world space, use the geometry (e.g., `Position`) node with `Space`: `Object` and transform it with the `Transform` node set to `From`: `Object` and `To`: `World`.

## Input Properties
Shader graph properties must be set to `Exposed` in order to be set on a per-instance basis.  Globals must *not* be `Exposed`, and global values must be set in C# using the methods of [PolySpatialShaderGlobals](https://docs.unity3d.com/Packages/com.unity.polyspatial@latest?subfolder=/api/Unity.PolySpatial.PolySpatialShaderGlobals.html#methods).  

### Time-based Animation
Note that visionOS materials do not support global properties natively, and thus PolySpatial must apply global properties separately to all material instances, which may affect performance.  For animation, consider using the `PolySpatial Time` node rather than the standard Unity shader graph `Time`.  While `PolySpatial Time` will not be exactly synchronized with [Time.time](https://docs.unity3d.com/ScriptReference/Time-time.html) (notably, it will not reflect changes to [Time.timeScale](https://docs.unity3d.com/ScriptReference/Time-timeScale.html)), it is supported natively in visionOS and does not require per-frame property updates.

## Supported Targets
The `Universal` and `Built-In` targets are supported for conversion.  For both targets, the `Lit` and `Unlit` materials are supported, as well as the `Opaque` and `Transparent` surface types and the `Alpha Clipping` setting.  For `Transparent` surfaces, the `Alpha`, `Premultiply`, and `Additive` blending modes are supported.  No other target settings are currently supported for conversion.  Due to platform limitations, all materials will have `Front` render face, depth writes enabled, `LEqual` depth testing, and tangent space fragment normals.

## Shader Graph Nodes
The following tables show the [current support status for Shader Graph nodes](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Built-In-Blocks.html) in PolySpatial for visionOS including a list of supported nodes and their various caveats. 

If a node doesn't appear here it means that it's not currently supported. *Note that this list will be updated as we continue to add support for more nodes.*

### Artistic

| Section       | Node                  | Notes                                                                                                                         |
|---------------|-----------------------|-------------------------------------------------------------------------------------------------------------------------------|
| Adjustment    | Channel Mixer         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Contrast              | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Hue                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Invert Colors         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Replace Color         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Saturation            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | White Balance         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
| Blend         | Blend                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
| Filter        | Dither                | Only default `Screen Position` is supported.                                                                                  |
|               | Fade Transition       | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
| Mask          | Channel Mask          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Color Mask            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
| Normal        | Normal Blend          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Normal From Height    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Normal From Texture   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Normal Reconstruct Z  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Normal Strength       | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
|               | Normal Unpack         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |
| Utility       | Colorspace Conversion | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                      |

### Channel

| Section   | Node       | Notes                                                                          |
|-----------|------------|--------------------------------------------------------------------------------|
| Channel   | Combine    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>       |
|           | Flip       | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>       |
|           | Split      | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>       |
|           | Swizzle    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>       |

### Input

`Custom Interpolators` are limited to these specific/names types:
* `Color`: Vector4
* `UV0`: Vector2
* `UV1`: Vector2
* `UserAttribute`: Vector4

  | Section   | Node                     | Notes                                                                                                                  |
  |-----------|--------------------------|------------------------------------------------------------------------------------------------------------------------|
  | Basic     | Boolean                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Color                    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Constant                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Integer                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Slider                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Time                     | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Float                    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Vector2                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Vector3                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Vector4                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  | Geometry  | Bitangent Vector         | Tangent and View space options are not standard.                                                                       |
  |           | Normal Vector            | Tangent and View space options are not standard.                                                                       |
  |           | Position                 | Tangent and View space options are not standard.                                                                       |
  |           | Screen Position          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Tangent Vector           | Tangent and View space options are not standard.                                                                       |
  |           | UV                       | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Vertex Color             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Vertex ID                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | View Direction           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  | Gradient  | Blackbody                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Gradient                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Sample Gradient          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  | Lighting  | Ambient                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Main Light Direction     | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  | Matrix    | Matrix 2x2               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Matrix 3x3               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Matrix 4x4               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Transformation Matrix    | Tangent and View space options are not standard.                                                                       |
  | PBR       | Dielectric Specular      | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Metal Reflectance        | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  | Scene     | Camera                   | `Position` and `Direction` outputs supported (non-standard).                                                           |
  |           | Eye Index                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |  
  |           | Object                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Scene Depth              | Platform doesn't allow have access to the depth buffer, this is just the camera distance in either clip or view space. |
  |           | Screen                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  | Texture   | Cubemap Asset            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Sample Cubemap           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Sample Reflected Cubemap | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Sample Texture 2D        | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Sample Texture 2D LOD    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Sample Texture 3D        | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Sampler State            | `MirrorOnce` wrap mode not supported.                                                                                  |
  |           | Texture 2D Asset         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Texture 3D Asset         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |
  |           | Texture Size             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                               |

### Math

| Section       | Node                   | Notes                                                                                              |
|---------------|------------------------|----------------------------------------------------------------------------------------------------|
| Advanced      | Absolute               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Exponential            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Length                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Log                    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Modulo                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Negate                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Normalize              | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Posterize              | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Reciprocal             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Reciprocal Square Root | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
| Basic         | Add                    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Divide                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Multiply               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Power                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Square Root            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Subtract               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
| Interpolation | Inverse Lerp           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Lerp                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Smoothstep             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
| Matrix        | Matrix Construction    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Matrix Determinant     | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Matrix Split           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Matrix Transpose       | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
| Range         | Clamp                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Fraction               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Maximum                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Minimum                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | One Minus              | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Random Range           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Remap                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Saturate               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
| Round         | Ceiling                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Floor                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Round                  | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Sign                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Step                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Truncate               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
| Trigonometry  | Arccosine              | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Arcsine                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Arctangent             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Arctangent2            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Cosine                 | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Degrees to Radians     | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Hyperbolic Cosine      | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Hyperbolic Sine        | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Hyperbolic Tangent     | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Radians to Degrees     | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Sine                   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Tangent                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
| Vector        | Cross Product          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Distance               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Dot Product            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Fresnel Effect         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Projection             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Reflection             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Refract                | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Rejection              | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Rotate About Axis      | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Sphere Mask            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Transform              | Some spaces are simulated and not covered in tests.                                                |
| Wave          | Noise Sine Wave        | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Sawtooth Wave          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Square Wave            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |
|               | Triangle Wave          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                           |

### Procedural

| Section    | Node              | Notes                                                                                                                                                                 |
|------------|-------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Procedural | Checkerboard      | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                              |
| Noise      | Gradient Noise    | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                              |
|            | Voronoi           | `Cells` output not supported.                                                                                                                                         |
| Shapes     | Ellipse           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                              |
|            | Polygon           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                              |
|            | Rectangle         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                              |
|            | Rounded Polygon   | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                              |
|            | Rounded Rectangle | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                              |

### Utility

| Section | Node                             | Notes                                                                                                                                                                                |
|---------|----------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Utility | Custom Function                  | See [custom function node conversion notes](CustomFunctionNode.md).                                                                                                                  |
|         | PolySpatial Environment Radiance | See [lighting notes](PolySpatialLighting.md).                                                                                                                                        |
|         | PolySpatial Lighting             | See [lighting notes](PolySpatialLighting.md).                                                                                                                                        |
|         | PolySpatial Time                 | Non-standard shader graph node specific to PolySpatial. Implements the time function as described in the [MaterialX Spec](https://materialx.org/assets/MaterialX.v1.38.Spec.pdf).    |
|         | Preview                          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                                             |
|         | Split LR                         | Non-standard shader graph node specific to PolySpatial. Implements the splitlr function as described in the [MaterialX Spec](https://materialx.org/assets/MaterialX.v1.38.Spec.pdf). |
| Logic   | Branch                           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                                             |
|         | Comparison                       | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                                             |
|         | Is Infinite                      | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                                             |
|         | Is NaN                           | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                                             |
|         | Or                               | <span style="color: green; font-weight: bold;">&#x2713; Supported</span>                                                                                                             |

### UV

| Section | Node              | Notes                                                                    |
|---------|-------------------|--------------------------------------------------------------------------|
| UV      | Flipbook          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
|         | Polar Coordinates | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
|         | Radial Shear      | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
|         | Rotate            | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
|         | Spherize          | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
|         | Tiling and Offset | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
|         | Triplanar         | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
|         | Twirl             | <span style="color: green; font-weight: bold;">&#x2713; Supported</span> |
