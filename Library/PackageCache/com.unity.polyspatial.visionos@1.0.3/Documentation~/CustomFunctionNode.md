---
uid: psl-vos-custom-function-node
---
# Unity PolySpatial Custom Function Node Support
PolySpatial's shader graph conversion provides partial support for the [Custom Function Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/Custom-Function-Node.html) (when using the `String` function type) by parsing a limited subset of HLSL and converting it into MaterialX nodes.  Note that this does *not* allow for any functionality beyond that which is possible by combining built-in shader graph nodes; it simply provides an alternate means of specification that may be more compact or convenient for some purposes.

The parser used for custom function nodes is primitive compared to a full HLSL implementation, and is under active development.

## Function Body Format
Custom function bodies should consist of a series of assignment statements for either temporary variables or outputs.  Variables may not be reassigned.

### Example
The following shows an example of a supported function body.  Note that the body consists only of assignment statements, assigning one temporary variable and using it to set the value of an output.
```hlsl
float2 xy = float2(In.r * In.a, In.g) * 2.0 - 1.0;
Out = float3(xy, max(1.0e-16, sqrt(1.0 - saturate(dot(xy, xy)))));
```

## Supported Types
The parser recognizes scalar, vector, and matrix floating point types: `float`, `float2`, `float3`, `float4`, `float2x2`, `float3x3`, `float4x4`

## Supported Operators
The parser supports basic arithmetic operators (`+`, `-`, `*`, `/`, `%`) for `float`, vector, and matrix values and logic/comparison operators (`!`, `&&`, `||`, `==`, `!=`, `>`, `<`, `>=`, `<=`) for `float` values.  The conditional operator (`?:`) is supported for `float` conditions and `float` or vector values.

## Swizzling
Swizzling vector values is supported.  For example, `float3(1, 2, 3).xz` is equivalent to `float2(1, 3)`.

## Supported Functions

### HLSL Intrinsic Functions
The parser supports a subset of HLSL's intrinsic functions: `abs`, `acos`, `all`, `any`, `asin`, `atan`, `atan2`, `ceil`, `clamp`, `cos`, `cosh`, `cross`, `degrees`, `distance`, `dot`, `exp`, `floor`, `fmod`, `frac`, `isinf`, `isnan`, `length`, `lerp`, `log`, `max`, `min`, `mul`, `normalize`, `pow`, `radians`, `rcp`, `reflect`, `refract`, `round`, `rsqrt`, `saturate`, `sign`, `sin`, `sinh`, `smoothstep`, `sqrt`, `step`, `tan`, `tanh`, `transpose`, `trunc`

### PolySpatial-Specific Functions
Additionally, the parser supports the custom `splitlr` function, which implements the splitlr function described in the [MaterialX Spec](https://materialx.org/assets/MaterialX.v1.38.Spec.pdf):
```hlsl
genType splitlr(genType valuel, genType valuer, float center, float2 texcoord);
```
(where `genType` is one of `float`, `float2`, `float3`, or `float4`)

### Unity Macros
For sampling textures, the parser supports Unity macros: `SAMPLE_TEXTURE2D`, `SAMPLE_TEXTURE2D_LOD`, `SAMPLE_TEXTURECUBE_LOD`

## Supported Globals
The parser supports several of Unity's built-in global variables: `_Time`, `_SinTime`, `_CosTime`, `unity_DeltaTime`, `unity_ObjectToWorld`, `unity_WorldToObject`, `UNITY_MATRIX_V`, `UNITY_MATRIX_I_V`, `UNITY_MATRIX_P`, `UNITY_MATRIX_I_P`, `UNITY_MATRIX_VP`, `UNITY_MATRIX_I_VP`

*Note*: Currently, the object-to-world matrix supplied by visionOS (and thus `unity_ObjectToWorld` and `unity_WorldToObject`) includes the transform of the rendered volume: that is, moving the (bounded) application around in space affects its world transform, unlike the world position output from the [Position Node](https://docs.unity3d.com/Packages/com.unity.shadergraph@17.0/manual/Position-Node.html), which is relative to the transform of the rendered volume and thus does not change when the application is repositioned.