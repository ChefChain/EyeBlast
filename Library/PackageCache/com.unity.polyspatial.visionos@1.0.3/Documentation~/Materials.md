---
uid: psl-vos-materials
---
# PolySpatial Material support

Several important standard shaders for each pipeline have been mapped to their closest available RealityKit analog. Current support includes:
* Standard URP shaders: Lit, Simple Lit, Unlit, (+TBD - more coming)
* Standard Builtin shaders: Standard, (+TBD – more coming)

## Custom shaders
Shaders and materials can be authored for visionOS via the Unity Shader Graph. Behind the scenes, these shader graphs are converted into MaterialX. While MaterialX is very expressive, some Shader Graph nodes have no analog in MaterialX. Within the ShaderGraph editor, unsupported nodes will be indicated by the presence of a `#` symbol, but please also refer to [Shader Graph Support](ShaderGraph.md). Both Lit and Unlit root nodes are supported.

ShaderLab, Metal, and other code-based shaders are not available, as RealityKit for visionOS does not currently expose a low level shading language.

## Unlit Materials

### Universal Render Pipeline/Unlit
For the URP unlit material, PolySpatial supports the `Base Map` color and texture properties as well as the `Opaque` and `Transparent` (with blending mode `Alpha`) surface types. `Alpha Clipping` may be enabled; if the `Threshold` is greater than zero, no blending will be performed (only alpha testing). If `Threshold` is equal to zero, the material will use [alpha-to-coverage](https://docs.unity3d.com/Manual/SL-AlphaToMask.html) mode on visionOS.

### Unlit/Color
For the built-in renderer pipeline unlit color material, the `Main Color` property is supported.

### Unlit/Texture
For the built-in renderer pipeline unlit texture material, the `Base (RGB)` texture is supported.

### Unlit/Transparent
For the built-in renderer pipeline unlit transparent material, the `Base (RGBA)` texture is supported.

## Lit Materials
By default, using a lit material will only apply the host system's illumination to your materials. For example, on visionOS, this only applies the system IBL that encodes real world illumination. If you also want to apply Unity's internal illumination to your materials (e.g., light maps, light probes, and/or dynamic lights), you need to opt in to add these more expensive lighting calculations on a per-material basis by wiring up a [PolySpatial Lighting Node](PolySpatialLighting.md).

### Universal Render Pipeline/Lit
For the URP lit material, the `Base Map` color and texture are respected, as is the `Render Face` option and the `Surface Inputs` `Tiling` and `Offset` properties.

When in the `Specular` workflow and `Specular Highlights` are enabled, PolySpatial supports the `Specular Map` texture or the (grayscale) intensity of the associated color. When in the `Metallic` workflow, PolySpatial supports the `Metallic Map` texture or intensity and respects the `Specular Highlights` toggle.

For both workflows, the `Smoothness` intensity is respected, but smoothness from texture channel (e.g., from `Metallic Alpha`) is not supported. `Normal Map`[^1] is supported, but the normal scale is not. Likewise, `Occlusion Map` is supported, but occlusion intensity is not. `Emission` color *or* texture may be specified; if they are both given, the color is reduced to grayscale and acts as a multiplier.

The `Opaque` and `Transparent` (with `Alpha` blend mode) surface types are supported. In `Transparent` mode, the `Preserve Specular` flag is respected. `Alpha Clipping` may be enabled; if the `Threshold` is greater than zero, no blending will be performed (only alpha testing). If `Threshold` is equal to zero, the material will use [alpha-to-coverage](https://docs.unity3d.com/Manual/SL-AlphaToMask.html) mode on visionOS. 

### Universal Render Pipeline/Simple Lit
For the URP simple lit material, the options supported are the same as for the lit material, except that there are no `Metallic` properties and no `Occlusion Map`. 

### Universal Render Pipeline/Complex Lit
For the URP complex lit material, the options supported are the same as for the lit material, with the addition of the `Clear Coat` option and its `Mask` and `Smoothness` properties.

### Standard
The built-in standard lit material is supported in much the same way as the URP lit material in `Metallic` workflow. The `Albedo` texture and color are similarly respected, as is the `Metallic` map or intensity and the `Smoothness` intensity (but not the smoothness `Source`). `Normal Map`[^1] and `Occlusion` are supported (but not their corresponding intensities), as is `Emission` color or texture, `Main Maps` `Tiling` and `Offset`, and the `Specular Highlights` flag. All rendering modes are supported: `Opaque`, `Transparent`, `Fade`, and `Cutout`.

### Standard (Specular setup)
The built-in lit specular material is supported in the same way as the URP lit material in `Specular` workflow. The `Albedo` texture and color are similarly respected, as is the `Specular` map or (grayscale) intensity and the `Smoothness` intensity (but not the smoothness `Source`). `Normal Map`[^1] and `Occlusion` are supported (but not their corresponding intensities), as is `Emission` color or texture, `Main Maps` `Tiling` and `Offset`, and the `Specular Highlights` flag. All rendering modes are supported: `Opaque`, `Transparent`, `Fade`, and `Cutout`.

[^1]: Currently, normal maps used in non-shader-graph materials must be imported as the `Default` texture type--that is, *not* the `Normal map` type--with the `sRGB (Color Texture)` option unchecked.

## Special Purpose Materials

### TextMeshPro/Distance Field

### TextMeshPro/Mobile/Distance Field
The TMP distance field materials are converted to shader graph materials that respect the `Face Color` and `Face Texture` properties only.

### UI/Default
The UI default material is converted to an unlit material that respects the tint and texture properties.

### Universal Render Pipeline/Particles/Unlit
The URP unlit particles material is converted to an unlit material that respects the `Base Map` texture and color and the `Surface Type`.

### Universal Render Pipeline/Particles/Lit
The URP lit particles material is converted to a lit material that respects the `Base Map` texture and color, the `Metallic Map` and intensity, the `Normal Map`, the `Smoothness` intensity, the `Normal` intensity, `Emission Map` texture and color, and the `Surface Type`.

### Particles/Standard Surface
The Built-in Render Pipeline lit particles material is converted to a lit material that respects the `Albedo` texture and color, the `Metallic Map` and intensity, the `Normal Map`, the `Smoothness` intensity, the `Normal` intensity, `Emission Map` texture and color, and the `Surface Type`.

### AR/Basic Occlusion
### AR/Occlusion
The occlusion materials are converted to basic equivalents.

## Shader Graph materials
Shader Graphs may use the Builtin or URP targets and the Unlit or Lit materials. All output blocks are supported. For more information about shader graph support, see the [shader graph conversion notes](ShaderGraph.md).

## Fallback Materials
If an unsupported shader is used PolySpatial will attempt to fallback to a basic Lit or Unlit shader. This behaviour can be toggled with the "Enable Fallback Shader Conversion" in the PolySpatial settings. If this is disabled unsupported shaders will be rendered as magenta on device.
