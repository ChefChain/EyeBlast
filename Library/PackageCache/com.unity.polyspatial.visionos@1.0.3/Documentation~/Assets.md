---
uid: psl-vos-assets
---
# PolySpatial Asset Support

## Meshes
RealityKit offers a limited set of predefined vertex formats. Meshes can supply a position, a normal, a tangent, a color, blend weight and blend indices. Unity will supply up to 8 texture coordinates to RealityKit, but note that only the first two UV channels are useable within its MaterialX implementation, limiting the utility of the extra geometric data. 

As Unity and RealityKit use different coordinate systems, some vertex attributes are modified when passing between systems. Handedness swapping is performed for position, normal, and tangent. UVs are flipped for all UV channels. 

## Materials
Please refer to [PolySpatial Material Support](Materials.md) for detailed information about material and shader support on visionOS.

### Unity Shader Graphs
Please refer to [Shader Graph Support](ShaderGraph.md) for detailed information about how custom shaders defined via Unity Shader Graph are converted to MaterialX to interop with RealityKit.

## Textures
Unity provides support for 2D textures on visionOS, and takes advantage of native texture compression options. 

RealityKit for visionOS does not support 3D textures or cubemaps, so PolySpatial emulates these using 2D textures.  This carries two limitations:
* First, the height times the depth (where cube maps have a fixed depth of 6) must be less than or equal to visionOS's maximum texture height (currently 8192).  This means that cube maps with power-of-two dimensions must be sized 1024 or smaller, and 3D textures with equal power-of-two dimensions cannot be larger than 64.
* Second, due to the way that 3D texture coordinates are mapped to slices within a 2D texture, artifacts may appear when displaying texture mipmap levels whose sizes are not evenly divisible by their compressed texture block size (e.g., 6 for ASTC6x6).  If this becomes apparent, consider switching to an uncompressed texture format.

### Render Textures
Please refer to [PolySpatial Render Texture Support](RenderTextures.md) for detailed information about support for render textures.

## Fonts
Both rasterized and SDF fonts are supported on visionOS, but we highly recommend using SDF fonts to ensure sharpness at all viewing distances.
