using System;

namespace UnityEditor.ShaderGraph.MaterialX
{
    internal static class MtlxNodeTypes // TODO: Maybe enum.
    {
        internal const string UsdPreviewSurface = "UsdPreviewSurface";
        internal const string UsdPrimvarReader = "UsdPrimvarReader";

        internal const string Material = "surfacematerial";

        // basic
        internal const string Dot = "dot"; // this is reroute/passthrough node, not DotProduct
        internal const string Extract = "extract";
        internal const string Combine2 = "combine2";
        internal const string Combine3 = "combine3";
        internal const string Combine4 = "combine4";
        internal const string Convert = "convert";
        internal const string Swizzle = "swizzle";
        internal const string Constant = "constant";

        // image
        internal const string TiledImage = "tiledimage";
        internal const string Image = "image";
        internal const string Triplanar = "triplanarprojection";

        // logic
        internal const string IfEqual = "ifequal";
        internal const string IfGreaterOrEqual = "ifgreatereq";
        internal const string IfGreater = "ifgreater";
        internal const string Switch = "switch";

        // math
        internal const string Add = "add";
        internal const string Subtract = "subtract";
        internal const string Multiply = "multiply";
        internal const string Divide = "divide";
        internal const string Power = "power";
        internal const string Modulo = "modulo";
        internal const string Exponential = "exp";
        internal const string NaturalLog = "ln";
        internal const string SquareRoot = "sqrt";
        internal const string Absolute = "absval";

        // range
        internal const string Sign = "sign";
        internal const string Ceil = "ceil";
        internal const string Round = "round";
        internal const string Floor = "floor";
        internal const string Clamp = "clamp";
        internal const string Minimum = "min";
        internal const string Maximum = "max";

        // trig
        internal const string Sine = "sin";
        internal const string Cosine = "cos";
        internal const string Tangent = "tan";
        internal const string Arcsine = "asin";
        internal const string Arccosine = "acos";
        internal const string Arctangent2 = "atan2";

        // vector
        internal const string Normalize = "normalize";
        internal const string Length = "magnitude";
        internal const string DotProduct = "dotproduct";
        internal const string CrossProduct = "crossproduct";

        // interpolation
        internal const string CurveLookup = "curvelookup";
        internal const string SmoothStep = "smoothstep";
        internal const string Mix = "mix";
        internal const string Remap = "remap";

        // vertex attributes
        internal const string GeomColor = "geomcolor";
        internal const string GeomPosition = "position";
        internal const string GeomNormal = "normal";
        internal const string GeomBitangent = "bitangent";
        internal const string GeomTangent = "tangent";
        internal const string GeomTexCoord = "texcoord";

        // matrices
        internal const string Rotate2d = "rotate2d";
        internal const string Rotate3d = "rotate3d";
        internal const string Transpose = "transpose";
        internal const string Determinant = "determinant";
        internal const string Inverse = "invertmatrix";
        internal const string TransformPoint = "transformpoint";
        internal const string TransformNormal = "transformnormal";
        internal const string TransformVector = "transformvector";
        internal const string TransformMatrix = "transformmatrix";

        // normals
        internal const string HeightToNormal = "heighttonormal";
        internal const string NormalMap = "normalmap";

        // Adjustment
        internal const string HsvAdjust = "hsvadjust";
        internal const string Saturate = "saturate";
        internal const string Contrast = "contrast";
        internal const string Range = "range";
        internal const string RgbToHsv = "rgbtohsv";
        internal const string HsvToRgb = "hsvtorgb";
        internal const string Blend = "blend";
        internal const string Mask = "mask";

        internal const string CellNoise2d = "cellnoise2d";
        internal const string PerlinNoise2d = "noise2d";
        internal const string WorleyNoise2d = "worleynoise2d";

        internal const string SplitLR = "splitlr";
        internal const string Time = "time";

        // Non-standard RealityKit nodes.
        internal const string RealityKitCameraPosition = "realitykit_cameraposition";
        internal const string RealityKitViewDirection = "realitykit_viewdirection";
        internal const string RealityKitCombine2 = "realitykit_combine2";
        internal const string RealityKitCombine3 = "realitykit_combine3";
        internal const string RealityKitCombine4 = "realitykit_combine4";
        internal const string RealityKitFractional = "realitykit_fractional";
        internal const string RealityKitSurfaceModelToWorld = "realitykit_surface_model_to_world";
        internal const string RealityKitSurfaceModelToView = "realitykit_surface_model_to_view";
        internal const string RealityKitSurfaceWorldToView = "realitykit_surface_world_to_view";
        internal const string RealityKitSurfaceViewToProjection = "realitykit_surface_view_to_projection";
        internal const string RealityKitSurfaceProjectionToView = "realitykit_surface_projection_to_view";
        internal const string RealityKitSurfaceScreenPosition = "realitykit_surface_screen_position";
        internal const string RealityKitGeometryModifierModelToWorld = "realitykit_geometry_modifier_model_to_world";
        internal const string RealityKitGeometryModifierWorldToModel = "realitykit_geometry_modifier_world_to_model";
        internal const string RealityKitGeometryModifierModelToView = "realitykit_geometry_modifier_model_to_view";
        internal const string RealityKitGeometryModifierViewToProjection = "realitykit_geometry_modifier_view_to_projection";
        internal const string RealityKitGeometryModifierProjectionToView = "realitykit_geometry_modifier_projection_to_view";
        internal const string RealityKitGeometryModifierVertexID = "realitykit_geometry_modifier_vertex_id";
        internal const string RealityKitSurfaceCustomAttribute = "realitykit_surface_custom_attribute";
        internal const string RealityKitUnlit = "realitykit_unlit";
        internal const string RealityKitPbr = "realitykit_pbr";
        internal const string RealityKitReflect = "realitykit_reflect";
        internal const string RealityKitRefract = "realitykit_refract";
        internal const string RealityKitStep = "realitykit_step";
        internal const string RealityKitGeometrySwitchCameraIndex = "realitykit_geometry_switch_cameraindex";
        internal const string RealityKitEnvironmentRadiance = "realitykit_environment_radiance";
        internal const string RealityKitTexture2D = "RealityKitTexture2D";
        internal const string RealityKitTexture2DLOD = "RealityKitTexture2DLOD";
    }
    
    internal static class MtlxDataTypes
        // TODO: Maybe enum.
    {
        internal const string Vertex = "vertexshader"; // this is not supported.
        internal const string Surface = "surfaceshader";
        internal const string Material = "material";

        internal const string Integer = "integer";
        internal const string Boolean = "boolean";
        internal const string Float = "float";
        internal const string Vector2 = "vector2";
        internal const string Vector3 = "vector3";
        internal const string Vector4 = "vector4";
        internal const string Color3 = "color3";
        internal const string Color4 = "color4";
        internal const string Matrix22 = "matrix22";
        internal const string Matrix33 = "matrix33";
        internal const string Matrix44 = "matrix44";
        internal const string Filename = "filename";
        internal const string String = "string";

        // TODO (LXR-2897): Support half precision types throughout the conversion process when the shader graph
        // precision is set to "half".  Currently, we only use this type for a node that specifically requires
        // it (realitykit_environment_radiance).  
        internal const string Half = "half";

        internal const string FloatArray = "floatarray"; // For Gradients only.
        internal const string Color4Array = "color4array"; // For Gradients only.

        internal static int GetLength(string datatype) => datatype switch
        {
            Integer => 1,
            Boolean => 1,
            Float => 1,
            Half => 1,
            Vector2 => 2,
            Vector3 => 3,
            Color3 => 3,
            Vector4 => 4,
            Color4 => 4,
            Matrix22 => 4,
            Matrix33 => 9,
            Matrix44 => 16,
            _ => 0,
        };

        internal static string GetTypeOfLength(int length) => length switch
        {
            1 => Float,
            2 => Vector2,
            3 => Vector3,
            4 => Vector4,
            9 => Matrix33,
            16 => Matrix44,
            _ => null,
        };

        internal static int GetElementLength(string datatype) => datatype switch
        {
            Matrix22 => 2,
            Matrix33 => 3,
            Matrix44 => 4,
            Color4Array => 4,
            _ => 1,
        };

        internal static string GetHlslForType(string datatype) => datatype switch
        {
            Vector2 => "float2",
            Vector3 or Color3 => "float3",
            Vector4 or Color4 => "float4",
            Matrix22 => "float2x2",
            Matrix33 => "float3x3",
            Matrix44 => "float4x4",
            _ => "float",
        };

        internal static string GetTypeForHlsl(string hlsl) => hlsl switch
        {
            "float" => MtlxDataTypes.Float,
            "float2" => MtlxDataTypes.Vector2,
            "float3" => MtlxDataTypes.Vector3,
            "float4" => MtlxDataTypes.Vector4,
            "float2x2" => MtlxDataTypes.Matrix22,
            "float3x3" => MtlxDataTypes.Matrix33,
            "float4x4" => MtlxDataTypes.Matrix44,
            _ => null,
        };

        internal static bool IsColor(string datatype) => datatype.Contains("color");
        internal static bool IsVector(string datatype) => datatype.Contains("vector");
        internal static bool IsString(string datatype) => (datatype == MtlxDataTypes.String || datatype == MtlxDataTypes.Filename);
        internal static bool IsMatrix(string datatype) => datatype.Contains("matrix");
        internal static bool IsArray(string datatype) => datatype.Contains("array");
    }
}
