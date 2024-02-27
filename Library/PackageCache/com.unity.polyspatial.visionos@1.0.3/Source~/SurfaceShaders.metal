#include <metal_stdlib>
#include <RealityKit/RealityKit.h>

using namespace metal;

[[visible]]
void textShader(realitykit::surface_parameters params)
{
    half3 color = (half3)(params.material_constants().base_color_tint() * params.geometry().color().rgb);
    params.surface().set_emissive_color(color);
    
    // UV is flipped; see docs at
    // https://developer.apple.com/documentation/realitykit/modifying-realitykit-rendering-using-custom-materials#Write-a-Surface-Shader
    float2 uv = params.geometry().uv0();
    uv.y = 1.0 - uv.y;
    
    constexpr sampler textureSampler(mag_filter::linear, min_filter::linear);
    half distance = 0.5 - params.textures().base_color().sample(textureSampler, uv).a;
    
    // See reference implementation for mx_aastep, used in splitlr implementation:
    // https://github.com/AcademySoftwareFoundation/MaterialX/blob/main/libraries/stdlib/genglsl/mx_aastep.glsl
    half afwidth = length(float2(dfdx(distance), dfdy(distance))) * 0.70710678118654757;
    half inside = mix((half)1.0, (half)0.0, smoothstep(-afwidth, afwidth, distance));
    
    half vertexAlpha = (half)params.geometry().color().a;
    params.surface().set_opacity(inside * params.material_constants().opacity_scale() * vertexAlpha);
}

[[visible]]
void maskingShader(realitykit::surface_parameters params)
{
    // UV is flipped; see docs at
    // https://developer.apple.com/documentation/realitykit/modifying-realitykit-rendering-using-custom-materials#Write-a-Surface-Shader
    float2 uv = params.geometry().uv0();
    uv.y = 1.0 - uv.y;
    
    constexpr sampler textureSampler(mag_filter::linear, min_filter::linear);
    half4 textureColor = params.textures().base_color().sample(textureSampler, uv);
    params.surface().set_emissive_color((half3)params.material_constants().base_color_tint() * textureColor.rgb);
    
    float4 customParameter = params.uniforms().custom_parameter();
    float maskOperation = customParameter[0];
    float alphaCutoff = customParameter[1];
    
    // Because we only get a single vec4 for custom parameters, we have to pack the relevant
    // part of our uvTransform matrix into all the material properties we aren't using.
    float4x4 uvTransform;
    uvTransform[0][0] = customParameter[2];
    uvTransform[0][1] = customParameter[3];
    uvTransform[1][0] = params.material_constants().clearcoat_scale();
    uvTransform[1][1] = params.material_constants().clearcoat_roughness_scale();
    uvTransform[2][0] = params.material_constants().opacity_threshold();
    uvTransform[2][1] = params.material_constants().roughness_scale();
    uvTransform[3][0] = params.material_constants().metallic_scale();
    uvTransform[3][1] = params.material_constants().specular_scale();
    
    float4 uvPos = uvTransform * float4(params.geometry().model_position(), 1.0);
    
    half alpha = params.textures().custom().sample(textureSampler, saturate(uvPos.xy)).a;
    if (maskOperation)
        alpha = 1.0 - alpha;
    
    if (alpha < alphaCutoff)
        params.surface().set_opacity(0.0);
    else
        params.surface().set_opacity(textureColor.a * params.material_constants().opacity_scale());
}

[[visible]]
void unlitBakedMeshParticleShader(realitykit::surface_parameters params)
{
    // UV is flipped; see docs at
    // https://developer.apple.com/documentation/realitykit/modifying-realitykit-rendering-using-custom-materials#Write-a-Surface-Shader
    float2 uv = params.geometry().uv0();
    uv.y = 1.0 - uv.y;

    constexpr sampler textureSampler(mag_filter::linear, min_filter::linear);
    half4 color = (half4)params.geometry().color() * params.textures().base_color().sample(textureSampler, uv);

    params.surface().set_emissive_color(color.rgb);
    params.surface().set_opacity(color.a);
}
