---
uid: psl-vos-polyspatial-lighting
---

# Unity PolySpatial Lighting Support
Because visionOS itself supplies only image based lighting, PolySpatial includes a lighting solution available to shader graphs that provides a subset of the lighting features available in Unity.  The PolySpatial Lighting Node supports directional lightmaps, light probes, and up to four dynamic lights (point, spot, or directional).

## Limitations
Only static directional lightmaps with dLDR encoding are supported.

## PolySpatial Lighting Node
To add PolySpatial lighting to a shader graph, create an instance of the `PolySpatial Lighting Node` using the `Create Node` command in the shader graph editor.

### Inputs
The inputs to the lighting node are largely the same as the inputs to the `Lit` shader graph target--`Base Color` (albedo), `Normal` (in tangent space), `Metallic`, `Smoothness`, `Emission`, and `Ambient Occlusion`--with the additional of a `Lightmap UV` input for lightmap texture coordinates.

### Output
The output (`Out`) of the lighting node is a single color result.  Depending on your application, you may wish to supply this output directly to the `Base Color` input of an `Unlit` target (if you wish to use only PolySpatial lighting) or to the `Emission` input of a `Lit` target (if you wish to combine PolySpatial lighting with visionOS's image based lighting).

### Settings

#### Baked Lighting
The `Baked Lighting` setting has three options: `None` to omit baked lighting entirely, `Lightmap` to apply baked lightmaps, and `LightProbes` to obtain baked lighting from light probes.  Typically, static objects use lightmaps and dynamic objects use light probes.

#### Reflection Probes
The `Reflection Probes` setting has three options: `None` to omit contribution from reflection probes, `Simple` to use a single reflection probe, and `Blended` to blend the contributions of up to two reflection probes.

#### Dynamic Lighting
The `Dynamic Lighting` toggle determines whether dynamic point/spot/directional lights (that is, lights represented by non-baked [Light](https://docs.unity3d.com/ScriptReference/Light.html) components) affect the output.

### Light Selection
The [Render Mode](https://docs.unity3d.com/ScriptReference/Light-renderMode.html) property of Light components may be used to control which dynamic lights are applied.  Lights marked [Not Important](https://docs.unity3d.com/ScriptReference/LightRenderMode.ForceVertex.html) will never be included in the four lights used by the PolySpatial Lighting Node.  Lights marked [Important](https://docs.unity3d.com/ScriptReference/LightRenderMode.ForcePixel.html) will be prioritized over lights marked [Auto](https://docs.unity3d.com/ScriptReference/LightRenderMode.Auto.html) (the default).

## PolySpatial Environment Radiance Node
To access visionOS's image based lighting (in either `Lit` or `Unlit` shader graphs), create an instance of the `PolySpatial Environment Radiance Node`.  This accepts surface parameters (`BaseColor`, `Roughness`, `Specular`, `Metallic`, and `Normal`) and outputs `Diffuse Radiance` and `Specular Radiance` colors representing the lighting results.  For example, to get an approximation of the ambient light level, use a `BaseColor` of white, `Roughness` of 1.0, `Specular` and `Metallic` of 0.0, and the default `Normal`.  The `Diffuse Radiance` output will be approximately equal to the ambient light level.  Note that the output of `PolySpatial Enviroment Radiance` is entirely separate from the lighting applied to `Lit` targets; for instance, if you add the `Diffuse Radiance` and `Specular Radiance` outputs together and connect them to the `Emission` output of a `Lit` target, you will end up with twice the overall brightness.