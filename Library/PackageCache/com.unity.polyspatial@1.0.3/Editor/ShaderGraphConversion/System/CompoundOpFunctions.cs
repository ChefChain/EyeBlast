using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    using ExternalInput = CompoundOpParser.ExternalInput;
    using Operator = CompoundOpParser.Operator;
    using ParseException = CompoundOpParser.ParseException;
    using ParserInput = CompoundOpParser.ParserInput;
    using SamplerStateInput = CompoundOpParser.SamplerStateInput;
    using Symbol = CompoundOpParser.Symbol;
    using SyntaxNode = CompoundOpParser.SyntaxNode;

    // Contains the implementations of the functions and operators that
    // expressions parsed by CompoundOpParser may reference.
    internal static class CompoundOpFunctions
    {
        // The "compiler" delegate type.  Compilation in this usage refers to processing a node in the
        // abstract syntax tree in order to convert it to a node definition.
        delegate InputDef Compiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output);
        
        static Dictionary<(string, Operator.VariantType), Compiler> s_OperatorCompilers = new()
        {
            [(";", Operator.VariantType.Default)] = SemicolonCompiler,
            [("=", Operator.VariantType.Default)] = AssignmentCompiler,
            [("+", Operator.VariantType.Prefix)] = IdentityCompiler,
            [("-", Operator.VariantType.Prefix)] = CreateUnaryOpCompiler(MtlxNodeTypes.Subtract, "in2"),
            [("+", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Add, true),
            [("-", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Subtract, true),
            [("*", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Multiply, true),
            [("/", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Divide, true),
            [("%", Operator.VariantType.Default)] = CreateBinaryOpCompiler(MtlxNodeTypes.Modulo, true),
            [(".", Operator.VariantType.Default)] = SwizzleCompiler,
            [("!", Operator.VariantType.Default)] = NotCompiler,
            [("==", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfEqual, 1.0f),
            [("!=", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfEqual, 0.0f),
            [("<=", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreater, 0.0f),
            [(">=", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreaterOrEqual, 1.0f),
            [(">", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreater, 1.0f),
            [("<", Operator.VariantType.Default)] = CreateComparisonCompiler(MtlxNodeTypes.IfGreaterOrEqual, 0.0f),
            [("&&", Operator.VariantType.Default)] = AndCompiler,
            [("||", Operator.VariantType.Default)] = OrCompiler,
            [("?", Operator.VariantType.Default)] = ConditionalCompiler,
            [("{", Operator.VariantType.Default)] = CreateConstructorCompiler(),
            [("abs", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Absolute),
            [("acos", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Arccosine),
            [("all", Operator.VariantType.FunctionCall)] = AllCompiler,
            [("any", Operator.VariantType.FunctionCall)] = AnyCompiler,
            [("asin", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Arcsine),
            [("atan", Operator.VariantType.FunctionCall)] = AtanCompiler,
            [("atan2", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.Arctangent2, "iny", "inx"),
            [("ceil", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Ceil),
            [("clamp", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.Clamp, "in", "low", "high"),
            [("cos", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Cosine),
            [("cosh", Operator.VariantType.FunctionCall)] = HyperbolicCosineCompiler,
            [("cross", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.CrossProduct),
            [("degrees", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Multiply, Mathf.Rad2Deg),
            [("distance", Operator.VariantType.FunctionCall)] = DistanceCompiler,
            [("dot", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(
                MtlxNodeTypes.DotProduct, false, MtlxDataTypes.Float),
            [("exp", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Exponential),
            [("float2", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Vector2),
            [("float2x2", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Matrix22),
            [("float3", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Vector3),
            [("float3x3", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Matrix33),
            [("float4", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Vector4),
            [("float4x4", Operator.VariantType.FunctionCall)] = CreateConstructorCompiler(MtlxDataTypes.Matrix44),
            [("floor", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Floor),
            [("fmod", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Modulo, true),
            [("frac", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.RealityKitFractional),
            [("isinf", Operator.VariantType.FunctionCall)] = IsInfCompiler,
            [("isnan", Operator.VariantType.FunctionCall)] = IsNanCompiler,
            [("length", Operator.VariantType.FunctionCall)] = LengthCompiler,
            [("lerp", Operator.VariantType.FunctionCall)] = LerpCompiler,
            [("log", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.NaturalLog),
            [("max", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Maximum, true),
            [("min", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Minimum, true),
            [("mul", Operator.VariantType.FunctionCall)] = MulCompiler,
            [("normalize", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Normalize),
            [("pow", Operator.VariantType.FunctionCall)] = CreateBinaryOpCompiler(MtlxNodeTypes.Power, true),
            [("radians", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Multiply, Mathf.Deg2Rad),
            [("rcp", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(MtlxNodeTypes.Power, -1.0f),
            [("reflect", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.RealityKitReflect, "in", "normal"),
            [("refract", Operator.VariantType.FunctionCall)] = RefractCompiler,
            [("round", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Round),
            [("rsqrt", Operator.VariantType.FunctionCall)] = CreateBinaryConstantOpCompiler(
                MtlxNodeTypes.Power, -0.5f),
            [("saturate", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Clamp),
            [("sign", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Sign),
            [("sin", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Sine),
            [("sinh", Operator.VariantType.FunctionCall)] = HyperbolicSineCompiler,
            [("smoothstep", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.SmoothStep, "low", "high", "in"),
            [("splitlr", Operator.VariantType.FunctionCall)] = SplitLRCompiler,
            [("sqrt", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.SquareRoot),
            [("step", Operator.VariantType.FunctionCall)] = CreateNaryOpCompiler(
                MtlxNodeTypes.RealityKitStep, "edge", "in"),
            [("tan", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Tangent),
            [("tanh", Operator.VariantType.FunctionCall)] = HyperbolicTangentCompiler,
            [("transpose", Operator.VariantType.FunctionCall)] = CreateUnaryOpCompiler(MtlxNodeTypes.Transpose),
            [("trunc", Operator.VariantType.FunctionCall)] = TruncCompiler,
            [("SAMPLE_TEXTURE2D", Operator.VariantType.FunctionCall)] = SampleTexture2DCompiler,
            [("SAMPLE_TEXTURE2D_LOD", Operator.VariantType.FunctionCall)] = SampleTexture2DLodCompiler,
            [("SAMPLE_TEXTURE3D", Operator.VariantType.FunctionCall)] = SampleTexture3DCompiler,
            [("SAMPLE_TEXTURE3D_LOD", Operator.VariantType.FunctionCall)] = SampleTexture3DLodCompiler,
            [("SAMPLE_TEXTURECUBE_LOD", Operator.VariantType.FunctionCall)] = SampleTextureCubeLodCompiler,
        };

        static FloatInputDef s_ZFlipMatrix = new(
            MtlxDataTypes.Matrix44,
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f);

        static InlineInputDef s_GeometryModifierFlippedWorldToModel = new(
            MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44, new()
        {
            ["in1"] = new InlineInputDef(
                MtlxNodeTypes.RealityKitGeometryModifierWorldToModel, MtlxDataTypes.Matrix44, new(), "worldToModel"),
            ["in2"] = s_ZFlipMatrix,
        });

        static InlineInputDef s_GeometryModifierViewToProjection = new(
            MtlxNodeTypes.RealityKitGeometryModifierViewToProjection,
            MtlxDataTypes.Matrix44, new(), "viewToProjection");

        static InlineInputDef s_SurfaceViewToProjection = new(
            MtlxNodeTypes.RealityKitSurfaceViewToProjection,
            MtlxDataTypes.Matrix44, new(), "viewToProjection");

        static Dictionary<string, Compiler> s_SymbolCompilers = new()
        {
            ["PI"] = CreateConstantCompiler(Mathf.PI),
            ["HALF_PI"] = CreateConstantCompiler(Mathf.PI * 0.5f),
            [PolySpatialShaderGlobals.Time] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.SinTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.CosTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.DeltaTime] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderGlobals.GlossyEnvironmentColor] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.VolumeToWorld] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            [PolySpatialShaderProperties.Lightmap] = CreateImplicitCompiler(MtlxDataTypes.Filename),
            [$"sampler{PolySpatialShaderProperties.Lightmap}"] = CreateSamplerCompiler(
                new() { wrap = TextureSamplerState.WrapMode.Clamp }),
            [PolySpatialShaderProperties.LightmapInd] = CreateImplicitCompiler(MtlxDataTypes.Filename),
            [$"sampler{PolySpatialShaderProperties.LightmapInd}"] = CreateSamplerCompiler(
                new() { wrap = TextureSamplerState.WrapMode.Clamp }),
            [PolySpatialShaderProperties.LightmapST] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHAr] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHAg] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHAb] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHBr] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHBg] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHBb] = CreateImplicitCompiler(MtlxDataTypes.Vector4),
            [PolySpatialShaderProperties.SHC] = CreateImplicitCompiler(MtlxDataTypes.Vector4),

#if DISABLE_MATERIALX_EXTENSIONS
            ["unity_ObjectToWorld"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["unity_WorldToObject"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_V"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_I_V"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_P"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_I_P"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_VP"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
            ["UNITY_MATRIX_I_VP"] = CreateImplicitCompiler(MtlxDataTypes.Matrix44),
#else
            ["unity_ObjectToWorld"] = CreateMatrixCompiler(
                s_ZFlipMatrix, MtlxNodeTypes.RealityKitGeometryModifierModelToWorld,
                MtlxNodeTypes.RealityKitSurfaceModelToWorld, "modelToWorld", s_ZFlipMatrix, false),
            ["unity_WorldToObject"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    s_ZFlipMatrix, MtlxNodeTypes.RealityKitGeometryModifierWorldToModel,
                    "worldToModel", s_ZFlipMatrix, false),
                CreateMatrixCompiler(
                    s_ZFlipMatrix, MtlxNodeTypes.RealityKitSurfaceModelToWorld,
                    "modelToWorld", s_ZFlipMatrix, true)),
            ["UNITY_MATRIX_V"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, false),
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, false)),
            ["UNITY_MATRIX_I_V"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, true),
                CreateMatrixCompiler(
                    null, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, true)),
            ["UNITY_MATRIX_P"] = CreateMatrixCompiler(
                null, MtlxNodeTypes.RealityKitGeometryModifierViewToProjection,
                MtlxNodeTypes.RealityKitSurfaceViewToProjection, "viewToProjection", null, false),
            ["UNITY_MATRIX_I_P"] = CreateMatrixCompiler(
                null, MtlxNodeTypes.RealityKitGeometryModifierProjectionToView,
                MtlxNodeTypes.RealityKitSurfaceProjectionToView, "projectionToView", null, false),
            ["UNITY_MATRIX_VP"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    s_GeometryModifierViewToProjection, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, false),
                CreateMatrixCompiler(
                    s_SurfaceViewToProjection, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, false)),
            ["UNITY_MATRIX_I_VP"] = CreatePerStageCompiler(
                CreateMatrixCompiler(
                    s_GeometryModifierViewToProjection, MtlxNodeTypes.RealityKitGeometryModifierModelToView,
                    "modelToView", s_GeometryModifierFlippedWorldToModel, true),
                CreateMatrixCompiler(
                    s_SurfaceViewToProjection, MtlxNodeTypes.RealityKitSurfaceWorldToView,
                    "worldToView", s_ZFlipMatrix, true)),
            ["polySpatial_TangentToWorld"] = CreateTangentMatrixCompiler(false),
            ["polySpatial_WorldToTangent"] = CreateTangentMatrixCompiler(true),
#endif
        };

        static CompoundOpFunctions()
        {
            for (var i = 0; i < PolySpatialShaderGlobals.LightCount; ++i)
            {
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.LightColorPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.LightPositionPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.SpotDirectionPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
                s_SymbolCompilers.Add(
                    PolySpatialShaderGlobals.LightAttenPrefix + i, CreateImplicitCompiler(MtlxDataTypes.Vector4));
            }
            for (var i = 0; i < PolySpatialShaderProperties.ReflectionProbeCount; ++i)
            {
                var reflectionProbeTextureProperty = PolySpatialShaderProperties.ReflectionProbeTexturePrefix + i;
                s_SymbolCompilers.Add(reflectionProbeTextureProperty, CreateImplicitCompiler(MtlxDataTypes.Filename));
                s_SymbolCompilers.Add(
                    $"sampler{reflectionProbeTextureProperty}",
                    CreateSamplerCompiler(new() { wrap = TextureSamplerState.WrapMode.Clamp }));
                s_SymbolCompilers.Add(
                    PolySpatialShaderProperties.ReflectionProbeWeightPrefix + i,
                    CreateImplicitCompiler(MtlxDataTypes.Float));
            }
        }

        internal static InputDef CompileOperator(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            var op = node.Lexeme as Operator;
            if (s_OperatorCompilers.TryGetValue((op.Span.contents, op.Variant), out var compiler))
                return compiler(node, inputs, output);
            
            throw new ParseException($"Unknown operator {op.Span.contents}", op.Span);
        }

        internal static InputDef CompileSymbol(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            var symbol = node.Lexeme as Symbol;
            if (inputs.ContainsKey(symbol.Span.contents))
                return new ExternalInputDef(symbol.Span.contents);
            
            if (output.ContainsKey(symbol.Span.contents))
                return new InternalInputDef(symbol.Span.contents);

            if (s_SymbolCompilers.TryGetValue(symbol.Span.contents, out var compiler))
                return compiler(node, inputs, output);
            
            throw new ParseException($"Unknown symbol {symbol.Span.contents}", symbol.Span);
        }

        static InputDef SemicolonCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.Children.ForEach(child => child.Compile(inputs, output));
            return null;
        }

        static InputDef AssignmentCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);
            InputDef inputDef = node.Children[1].Compile(inputs, output);

            string symbol;
            var leftChild = node.Children[0];
            if (leftChild.Lexeme is Symbol)
            {
                symbol = leftChild.Lexeme.Span.contents;
            }
            else if (leftChild.Lexeme is Operator op &&
                op.Variant == Operator.VariantType.VariableDefinition &&
                leftChild.Children.Count == 1 &&
                leftChild.Children[0].Lexeme is Symbol leftGrandchildSymbol)
            {
                symbol = leftGrandchildSymbol.Span.contents;

                var expectedType = MtlxDataTypes.GetTypeForHlsl(op.Span.contents);
                if (expectedType == null)
                    throw new ParseException("Unknown type", op.Span);
                    
                if (!TryCoerce(ref inputDef, inputs, output, expectedType))
                    throw new ParseException($"Expected {op.Span.contents} rvalue", node.Lexeme.Span);
            }
            else
            {
                throw new ParseException("Invalid lvalue for assignment", node.Lexeme.Span);
            }

            switch (inputDef)
            {
                case InlineInputDef inlineInputDef:
                    output[symbol] = inlineInputDef.Source;
                    break;
                
                default:
                    output[symbol] = new(MtlxNodeTypes.Dot, GetOutputType(inputDef, inputs, output), new()
                    {
                        ["in"] = inputDef,
                    });
                    break;
            }
            
            return new InternalInputDef(symbol);
        }

        static InputDef IdentityCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);
            return node.Children[0].Compile(inputs, output);
        }

        static InputDef AtanCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            // Note: If the atan2 node definitions had reasonable defaults (i.e., defaulting inx to a vector
            // of 1.0), we could just rely on that and use CreateUnaryOpCompiler.  However, the current node
            // definitions have 1.0 as the default inx for the float variant but zero vectors as the defaults for
            // the vector variants (and one vectors for the iny defaults, which makes me think it's probably
            // an oversight).
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["iny"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "iny");

            var ones = new float[MtlxDataTypes.GetLength(matchedType)];
            Array.Fill(ones, 1.0f);
            inputDefs.Add("inx", new FloatInputDef(matchedType, ones));

            return new InlineInputDef(MtlxNodeTypes.Arctangent2, matchedType, inputDefs);
        }

        static InputDef SwizzleCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);
            var leftInputDef = node.Children[0].Compile(inputs, output);
            var leftInputType = GetOutputType(leftInputDef, inputs, output);

            var leftLength = MtlxDataTypes.GetLength(leftInputType);
            if (leftLength > 4)
                throw new ParseException("Left side of . cannot be swizzled", node.Lexeme.Span);
            if (node.Children[1].Lexeme is not Symbol swizzle)
                throw new ParseException("Right side of . is not a swizzle", node.Lexeme.Span);

            var containsXYZW = false;
            var containsRGBA = false;
            var containsOther = false;
            foreach (var ch in swizzle.Span.contents)
            {
                if ("xyzw".Contains(ch))
                    containsXYZW = true;
                else if ("rgba".Contains(ch))
                    containsRGBA = true;
                else
                    containsOther = true;
            }
            if (!(containsXYZW ^ containsRGBA) || containsOther || swizzle.Span.contents.Length > 4)
                throw new ParseException("Invalid swizzle", swizzle.Span);

            var outputType = MtlxDataTypes.GetTypeOfLength(swizzle.Span.contents.Length);

            var channels = swizzle.Span.contents;
            if (containsRGBA)
            {
                StringBuilder builder = new();
                foreach (var ch in channels)
                {
                    builder.Append(ch switch
                    {
                        'r' => 'x',
                        'g' => 'y',
                        'b' => 'z',
                        _ => 'w',
                    });
                }
                channels = builder.ToString();
            }

            return new InlineInputDef(MtlxNodeTypes.Swizzle, outputType, new()
            {
                ["in"] = leftInputDef,
                ["channels"] = new StringInputDef(channels),
            });
        }

        static InputDef NotCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["value1"] = node.Children[0].Compile(inputs, output),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
            };
            CoerceToType(node, inputs, output, inputDefs, "value1", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, inputDefs);
        }

        static InputDef AndCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(inputs, output),
                ["in2"] = node.Children[1].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "in1", MtlxDataTypes.Float);
            CoerceToType(node, inputs, output, inputDefs, "in2", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, inputDefs),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef OrCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> leftInputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, leftInputDefs, "in", MtlxDataTypes.Float);

            Dictionary<string, InputDef> rightInputDefs = new()
            {
                ["in"] = node.Children[1].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, rightInputDefs, "in", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, leftInputDefs),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, rightInputDefs),
                }),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef ConditionalCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);

            var rightChild = node.Children[1];
            if (rightChild.Lexeme is not Operator rightOp || rightOp.Span.contents != ":")
                throw new ParseException("Expected ':'", node.Lexeme.Span);
            
            rightChild.RequireChildCount(2);
            
            var valueDef = node.Children[0].Compile(inputs, output);
            var trueDef = rightChild.Children[0].Compile(inputs, output);
            var falseDef = rightChild.Children[1].Compile(inputs, output);
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["value1"] = valueDef,
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = falseDef,
                ["in2"] = trueDef,
            };
            CoerceToType(node, inputs, output, inputDefs, "value1", MtlxDataTypes.Float);
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");

            return new InlineInputDef(MtlxNodeTypes.IfEqual, matchedType, inputDefs);
        }

        static InputDef AllCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var inputType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");

            if (inputType != MtlxDataTypes.Float)
            {
                var sharedInput = GetSharedInput(inputDefs["in"], output);
                
                var inputLength = MtlxDataTypes.GetLength(inputType);
                for (var i = 0; i < inputLength; ++i)
                {
                    var componentDef = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedInput,
                        ["channels"] = new StringInputDef("xyzw".Substring(i, 1)),
                    });
                    inputDefs["in"] = (i == 0) ? 
                        componentDef : new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = inputDefs["in"],
                        ["in2"] = componentDef,
                    });
                }
            }

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = inputDefs["in"],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef AnyCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var inputType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");

            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = inputType == MtlxDataTypes.Float ?
                    inputDefs["in"] : new InlineInputDef(MtlxNodeTypes.Length, MtlxDataTypes.Float, inputDefs),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });
        }

        static InputDef DistanceCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(inputs, output),
                ["in2"] = node.Children[1].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");

            var lengthNodeType = (matchedType == MtlxDataTypes.Float) ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            return new InlineInputDef(lengthNodeType, MtlxDataTypes.Float, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, inputDefs),
            });
        }

        static InputDef IsInfCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);
            
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "in", MtlxDataTypes.Float);

            // 1/0 == float.PositiveInfinity
            // (see https://www.gnu.org/software/libc/manual/html_node/Infinity-and-NaN.html)
            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, inputDefs),
                ["value2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                }),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
            });
        }

        static InputDef IsNanCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);
            
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "in", MtlxDataTypes.Float);

            // If the input is NaN, then both step(in, 0) and step(0, in) will return 0; otherwise, one
            // or both will return 1.  So, we add them together and compare the result to zero.
            // I arrived at this approach after failing to get other methods to work in visionOS, perhaps
            // because of NaN-ignoring optimizations in the Metal shader compiler.  For instance, in == in
            // should be false for NaN, but it evaluates to true in visionOS (and may simply be optimized out).
            var sharedIn = GetSharedInput(inputDefs["in"], output);
            return new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
            {
                ["value1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.RealityKitStep, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedIn,
                        ["edge"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.RealityKitStep, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["edge"] = sharedIn,
                    }),
                }),
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
            });
        }

        static InputDef LengthCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);
            
            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");

            var nodeType = (matchedType == MtlxDataTypes.Float) ? MtlxNodeTypes.Absolute : MtlxNodeTypes.Length;
            return new InlineInputDef(nodeType, MtlxDataTypes.Float, inputDefs);
        }

        static InputDef LerpCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(3);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["bg"] = node.Children[0].Compile(inputs, output),
                ["fg"] = node.Children[1].Compile(inputs, output),
                ["mix"] = node.Children[2].Compile(inputs, output),
            };
            
            // MaterialX's mix type requires a scalar parameter.  So, we use that for scalar parameters only
            // and use X + (Y - X)*S for vector parameters.
            if (GetOutputType(inputDefs["mix"], inputs, output) == MtlxDataTypes.Float)
            {
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "bg", "fg");
                return new InlineInputDef(MtlxNodeTypes.Mix, matchedType, inputDefs);
            }
            else
            {
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "bg", "fg", "mix");
                var sharedXInput = GetSharedInput(inputDefs["bg"], output);
                return new InlineInputDef(MtlxNodeTypes.Add, matchedType, new()
                {
                    ["in1"] = sharedXInput,
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, new()
                        {
                            ["in1"] = inputDefs["fg"],
                            ["in2"] = sharedXInput,
                        }),
                        ["in2"] = inputDefs["mix"],
                    }),
                });
            }
        }

        static InputDef MulCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(2);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in1"] = node.Children[0].Compile(inputs, output),
                ["in2"] = node.Children[1].Compile(inputs, output),
            };
            var leftType = GetOutputType(inputDefs["in1"], inputs, output);
            var rightType = GetOutputType(inputDefs["in2"], inputs, output);

            // Two vectors -> dot product.
            var leftIsVector = MtlxDataTypes.IsVector(leftType);
            var rightIsVector = MtlxDataTypes.IsVector(rightType);
            if (leftIsVector && rightIsVector)
            {
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");
                return new InlineInputDef(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, inputDefs);
            }

            // One vector, one matrix -> transform vector.
            var leftIsMatrix = MtlxDataTypes.IsMatrix(leftType);
            var rightIsMatrix = MtlxDataTypes.IsMatrix(rightType);
            if (leftIsMatrix && rightIsVector)
            {
                var elementLength = MtlxDataTypes.GetElementLength(leftType);
                var vectorType = MtlxDataTypes.GetTypeOfLength(elementLength);

                inputDefs["in"] = inputDefs["in2"];
                inputDefs["mat"] = inputDefs["in1"];
                inputDefs.Remove("in1");
                inputDefs.Remove("in2");

                CoerceToType(node, inputs, output, inputDefs, "in", vectorType);
                return new InlineInputDef(MtlxNodeTypes.TransformMatrix, vectorType, inputDefs);
            }
            else if (leftIsVector && rightIsMatrix)
            {
                var elementLength = MtlxDataTypes.GetElementLength(rightType);
                var vectorType = MtlxDataTypes.GetTypeOfLength(elementLength);

                inputDefs["in"] = inputDefs["in1"];
                inputDefs["mat"] = new InlineInputDef(MtlxNodeTypes.Transpose, rightType, new()
                {
                    ["in"] = inputDefs["in2"],
                });
                inputDefs.Remove("in1");
                inputDefs.Remove("in2");

                CoerceToType(node, inputs, output, inputDefs, "in", vectorType);
                return new InlineInputDef(MtlxNodeTypes.TransformMatrix, vectorType, inputDefs);
            }
            else
            {
                // Anything else -> multiply
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");
                return new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, inputDefs);
            }
        }

        static InputDef SplitLRCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(4);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["valuel"] = node.Children[0].Compile(inputs, output),
                ["valuer"] = node.Children[1].Compile(inputs, output),
                ["center"] = node.Children[2].Compile(inputs, output),
                ["texcoord"] = node.Children[3].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "valuel", "valuer");
            CoerceToType(node, inputs, output, inputDefs, "center", MtlxDataTypes.Float);
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector2);

            return new InlineInputDef(MtlxNodeTypes.SplitLR, matchedType, inputDefs);
        }

        static InputDef TruncCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");
            var sharedIn = GetSharedInput(inputDefs["in"], output);

            // trunc(in) = floor(abs(in)) * sign(in)
            return new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Floor, matchedType, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Absolute, matchedType, new()
                    {
                        ["in"] = sharedIn,
                    }),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Sign, matchedType, new()
                {
                    ["in"] = sharedIn,
                }),
            });
        }

        static InputDef SampleTexture2DCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(3);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = node.Children[0].Compile(inputs, output),
                ["texcoord"] = node.Children[2].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector2);

            var samplerInputDef = node.Children[1].Compile(inputs, output);
            var samplerState = RequireTextureSampler(node, inputs, samplerInputDef);

#if DISABLE_MATERIALX_EXTENSIONS
            AddImageSamplerState(inputDefs, samplerState);
            return new InlineInputDef(MtlxNodeTypes.Image, MtlxDataTypes.Vector4, inputDefs);
#else
            AddTexture2DSamplerState(inputDefs, samplerState);
            return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2D, MtlxDataTypes.Vector4, inputDefs);
#endif
        }

        static InputDef SampleTexture2DLodCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(4);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = node.Children[0].Compile(inputs, output),
                ["texcoord"] = node.Children[2].Compile(inputs, output),
                ["lod"] = node.Children[3].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector2);
            CoerceToType(node, inputs, output, inputDefs, "lod", MtlxDataTypes.Float);

            var samplerInputDef = node.Children[1].Compile(inputs, output);
            var samplerState = RequireTextureSampler(node, inputs, samplerInputDef);
            AddTexture2DSamplerState(inputDefs, samplerState);

            return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, inputDefs);
        }

        static InputDef SampleTexture3DCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(3);

            var fileInputDef = node.Children[0].Compile(inputs, output);
            var file = RequireExternalTexture(node, inputs, fileInputDef);

            var samplerInputDef = node.Children[1].Compile(inputs, output);
            var samplerState = RequireTextureSampler(node, inputs, samplerInputDef);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["texcoord"] = node.Children[2].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector3);

            var texCoords3D = CompileWrappedTexCoords3D(output, inputDefs["texcoord"], samplerState);
            
            return CompileSampleTexture3D(output, texCoords3D, file, samplerState, texCoords2D =>
            {
                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["file"] = file,
                    ["texcoord"] = texCoords2D,
                };
#if DISABLE_MATERIALX_EXTENSIONS
                AddImageSamplerState(inputDefs, samplerState);
                return new InlineInputDef(MtlxNodeTypes.Image, MtlxDataTypes.Vector4, inputDefs);
#else
                AddTexture2DSamplerState(inputDefs, samplerState);
                return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2D, MtlxDataTypes.Vector4, inputDefs);
#endif
            });
        }

        static InputDef SampleTexture3DLodCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(4);

            var fileInputDef = node.Children[0].Compile(inputs, output);
            var file = RequireExternalTexture(node, inputs, fileInputDef);

            var samplerInputDef = node.Children[1].Compile(inputs, output);
            var samplerState = RequireTextureSampler(node, inputs, samplerInputDef);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["texcoord"] = node.Children[2].Compile(inputs, output),
                ["lod"] = node.Children[3].Compile(inputs, output),
            };
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector3);
            CoerceToType(node, inputs, output, inputDefs, "lod", MtlxDataTypes.Float);
            
            var texCoords3D = CompileWrappedTexCoords3D(output, inputDefs["texcoord"], samplerState);
            var lod = inputDefs["lod"];
            
            return CompileSampleTexture3D(output, texCoords3D, file, samplerState, texCoords2D =>
            {
                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["file"] = file,
                    ["texcoord"] = texCoords2D,
                    ["lod"] = lod,
                };
                AddTexture2DSamplerState(inputDefs, samplerState);
                return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, inputDefs);
            });
        }

        // As a helper for SampleTexture3DCompiler and SampleTexture3DLodCompiler, this method converts raw 3D texture
        // coordinates to wrapped coordinates in [0, 1) according to the wrap mode.  Keeping the range open-ended helps
        // to avoid exceeding the boundaries of each slice later on, when we map 3D coordinates to 2D ones.  
        static InputDef CompileWrappedTexCoords3D(
            Dictionary<string, NodeDef> output, InputDef texCoords3D, TextureSamplerState samplerState)
        {
            const float kOneMinusEpsilon = 1.0f - Vector3.kEpsilon;
            const float kTwoMinusEpsilon = 2.0f - Vector3.kEpsilon;
            switch (samplerState.wrap)
            {
                case TextureSamplerState.WrapMode.Repeat:
                    // For repeat mode, we can simply take the fraction, which will be in [0, 1).
                    return new InlineInputDef(MtlxNodeTypes.RealityKitFractional, MtlxDataTypes.Vector3, new()
                    {
                        ["in"] = texCoords3D,
                    });

                case TextureSamplerState.WrapMode.Clamp:
                    // Clamp to [0, 1) to prevent overflow into next slice.
                    return new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Vector3, new()
                    {
                        ["in"] = texCoords3D,
                        ["low"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["high"] = new FloatInputDef(MtlxDataTypes.Float, kOneMinusEpsilon),
                    });

                case TextureSamplerState.WrapMode.Mirror:
                    // Start with 2 * fract(uvw / 2), which repeats 0 -> 1.999... every two units.
                    var sharedFraction = GetSharedInput(
                        new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.RealityKitFractional, MtlxDataTypes.Vector3, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector3, new()
                            {
                                ["in1"] = texCoords3D,
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                            }),
                        }),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                    }), output);
                    // Then take the smaller of the result and (1.999... - result),
                    // which repeats 0 -> 0.999... -> 0 every two units.
                    return new InlineInputDef(MtlxNodeTypes.Minimum, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = sharedFraction,
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new FloatInputDef(
                                MtlxDataTypes.Vector3, kTwoMinusEpsilon, kTwoMinusEpsilon, kTwoMinusEpsilon),
                            ["in2"] = sharedFraction,
                        }),
                    });

                case TextureSamplerState.WrapMode.MirrorOnce:
                    // Taking the absolute value mirrors the function; clamp the result to [0, 1).
                    return new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Vector3, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Vector3, new()
                        {
                            ["in"] = texCoords3D,
                        }),
                        ["low"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["high"] = new FloatInputDef(MtlxDataTypes.Float, kOneMinusEpsilon),
                    });
                
                default:
                    throw new NotSupportedException($"Unknown wrap mode {samplerState.wrap}");
            }
        }

        delegate InputDef CompileSampleTexture2D(InputDef texCoords2D);

        // As a helper for SampleTexture3DCompiler and SampleTexture3DLodCompiler, this method simulates a 3D texture
        // sample by sampling a 2D texture containing vertically stacked slices once (for point sampling) or twice
        // (for linear sampling, with the two samples being blended according to the fraction).  
        static InputDef CompileSampleTexture3D(
            Dictionary<string, NodeDef> output, InputDef texCoords3D, ExternalInputDef file,
            TextureSamplerState samplerState, CompileSampleTexture2D compileSampleTexture2D)
        {
            var sharedDepth = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = new TextureSizeInputDef(file.Source),
                ["channels"] = new StringInputDef("z"),
            }), output);
            var sharedTexCoords3D = GetSharedInput(texCoords3D, output);

            if (samplerState.filter == TextureSamplerState.FilterMode.Point)
            {
                return compileSampleTexture2D(new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                {
                    // U' = U
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedTexCoords3D,
                        ["channels"] = new StringInputDef("x"),
                    }),
                    // V' = (floor(W * depth) + V) / depth
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Floor, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                                {
                                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                                    {
                                        ["in"] = sharedTexCoords3D,
                                        ["channels"] = new StringInputDef("z"),
                                    }),
                                    ["in2"] = sharedDepth,
                                }),
                            }),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = sharedTexCoords3D,
                                ["channels"] = new StringInputDef("y"),
                            }),
                        }),
                        ["in2"] = sharedDepth,
                    }),
                }));
            }
            var sharedU = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedTexCoords3D,
                ["channels"] = new StringInputDef("x"),
            }), output);

            // To prevent bleeding between slices at their tops and bottoms, when we use linear filtering,
            // we need to clamp V to [0.5/height, 1 - 0.5/height].
            var sharedHalfTexelHeight = GetSharedInput(
                new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
            {
                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = new TextureSizeInputDef(file.Source),
                    ["channels"] = new StringInputDef("y"),
                }),
            }), output);
            var sharedV = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = sharedTexCoords3D,
                    ["channels"] = new StringInputDef("y"),
                }),
                ["low"] = sharedHalfTexelHeight,
                ["high"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = sharedHalfTexelHeight,
                }),
            }), output);

            var sharedWD = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = sharedTexCoords3D,
                        ["channels"] = new StringInputDef("z"),
                    }),
                    ["in2"] = sharedDepth,
                }),
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
            }), output);

            var sharedFloorWD = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Floor, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedWD,
            }), output);

            var sharedCeilWD = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Ceil, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedWD,
            }), output);

            var depthMinusOne = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
            {
                ["in1"] = sharedDepth,
                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
            });

            return new InlineInputDef(MtlxNodeTypes.Mix, MtlxDataTypes.Vector4, new()
            {
                ["bg"] = compileSampleTexture2D(new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                {
                    // U' = U
                    ["in1"] = sharedU,
                    // V' = (floor(W * depth - 0.5) + V) / depth
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            // If floor == -1, we either wrap around to depth - 1 or clamp to zero. 
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreaterOrEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = sharedFloorWD,
                                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in1"] = sharedFloorWD,
                                ["in2"] = samplerState.wrap switch
                                {
                                    TextureSamplerState.WrapMode.Repeat => depthMinusOne,
                                    _ => new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                },
                            }),
                            ["in2"] = sharedV,
                        }),
                        ["in2"] = sharedDepth,
                    }),
                })),
                ["fg"] = compileSampleTexture2D(new InlineInputDef(MtlxNodeTypes.Combine2, MtlxDataTypes.Vector2, new()
                {
                    // U' = U
                    ["in1"] = sharedU,
                    // V' = (ceil(W * depth - 0.5) + V) / depth
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            // If ceil == depth, we either wrap around to zero or clamp to depth - 1. 
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreaterOrEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = sharedCeilWD,
                                ["value2"] = sharedDepth,
                                ["in1"] = samplerState.wrap switch
                                {
                                    TextureSamplerState.WrapMode.Repeat => new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                    _ => depthMinusOne,
                                },
                                ["in2"] = sharedCeilWD,
                            }),
                            ["in2"] = sharedV,
                        }),
                        ["in2"] = sharedDepth,
                    }),
                })),
                // Final color = lerp(BG, FG, fract(W * depth - 0.5))
                ["mix"] = new InlineInputDef(MtlxNodeTypes.RealityKitFractional, MtlxDataTypes.Float, new()
                {
                    ["in"] = sharedWD,
                }),
            });
        }

        static InputDef SampleTextureCubeLodCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(4);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["file"] = node.Children[0].Compile(inputs, output),
                ["texcoord"] = node.Children[2].Compile(inputs, output),
                ["lod"] = node.Children[3].Compile(inputs, output),
                ["u_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                ["v_wrap_mode"] = new StringInputDef("clamp_to_edge"),
            };
            CoerceToType(node, inputs, output, inputDefs, "file", MtlxDataTypes.Filename);
            CoerceToType(node, inputs, output, inputDefs, "texcoord", MtlxDataTypes.Vector3);
            CoerceToType(node, inputs, output, inputDefs, "lod", MtlxDataTypes.Float);

            // Create shared intermediate values for the texcoord direction and its
            // components so that we can reference them in multiple places.
            var sharedDir = GetSharedInput(inputDefs["texcoord"], output);
            var sharedDirComps = "xyz".Select(comp =>
                GetSharedInput(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedDir,
                ["channels"] = new StringInputDef(comp.ToString()),
            }), output)).ToArray();
            
            // Likewise, create shared values for the absolute value of the direction and its components.
            var sharedAbsDir = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Vector3, new()
            {
                ["in"] = sharedDir,
            }), output);
            var sharedAbsDirComps = "xyz".Select(comp =>
                GetSharedInput(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
            {
                ["in"] = sharedAbsDir,
                ["channels"] = new StringInputDef(comp.ToString()),
            }), output)).ToArray();

            // Create a shared vector2 for the ZY/X component pair (with range [-1, 1]), used for the +X/-X faces.
            var sharedZY = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = sharedDir,
                    ["channels"] = new StringInputDef("zy"),
                }),
                ["in2"] = sharedAbsDirComps[0]
            }), output);

            // Create a shared vector2 for the XZ/Y component pair (with range [-1, 1]), used for the +Y/-Y faces.
            var sharedXZ = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = sharedDir,
                    ["channels"] = new StringInputDef("xz"),
                }),
                ["in2"] = sharedAbsDirComps[1]
            }), output);

            // Create a shared vector2 for the XY/Z component pair (with range [-1, 1]), used for the +Z/-Z faces.
            var sharedXY = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Divide, MtlxDataTypes.Vector2, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = sharedDir,
                    ["channels"] = new StringInputDef("xy"),
                }),
                ["in2"] = sharedAbsDirComps[2]
            }), output);

            // Create the value for the +X/-X faces.
            var valueX = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedDirComps[0],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                // dir.x > 0: +X
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedZY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, -0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of +X face: (0.5, 0.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 0.5f / 6.0f),
                }),
                // dir.x <= 0: -X
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedZY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of -X face: (0.5, 1.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 1.5f / 6.0f),
                }),
            });

            // Create the value for the +Y/-Y faces.
            var valueY = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedDirComps[1],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                // dir.y > 0: +Y
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXZ,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 0.5f / 6.0f),
                    }),
                    // Translate to center of +Y face: (0.5, 2.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 2.5f / 6.0f),
                }),
                // dir.y <= 0: -Y 
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXZ,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of -Y face: (0.5, 3.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 3.5f / 6.0f),
                }),
            });

            // Create the shared value for the +Z/-Z faces (shared because used in two places).
            var sharedValueZ = GetSharedInput(new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedDirComps[2],
                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                // dir.z > 0: +Z
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of +Z face: (0.5, 4.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 4.5f / 6.0f),
                }),
                // dir.z <= 0: -Z
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    // Scale from size (2, 2) to size (1, 1/6) (with sign changes to align with reference image).
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                    {
                        ["in1"] = sharedXY,
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, -0.5f, -0.5f / 6.0f),
                    }),
                    // Translate to center of -Z face: (0.5, 5.5 / 6.0)
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 5.5f / 6.0f),
                }),
            }), output);

            // Choose the final texcoord value based on the component with the greatest absolute value.
            inputDefs["texcoord"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
            {
                ["value1"] = sharedAbsDirComps[0],
                ["value2"] = sharedAbsDirComps[1],
                // dir.x > dir.y
                ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
                {
                    ["value1"] = sharedAbsDirComps[0],
                    ["value2"] = sharedAbsDirComps[2],
                    // dir.x > dir.z: +X/-X
                    ["in1"] = valueX,
                    // dir.x <= dir.z: +Z/-Z
                    ["in2"] = sharedValueZ,
                }),
                // dir.x <= dir.y
                ["in2"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Vector2, new()
                {
                    ["value1"] = sharedAbsDirComps[1],
                    ["value2"] = sharedAbsDirComps[2],
                    // dir.y > dir.z: +Y/-Y
                    ["in1"] = valueY,
                    // dir.y <= dir.z: +Z/-Z
                    ["in2"] = sharedValueZ,
                }),
            });

            var samplerInputDef = node.Children[1].Compile(inputs, output);
            var samplerState = RequireTextureSampler(node, inputs, samplerInputDef);
            
            StringInputDef minMagFilter = new(GetMinMagFilter(samplerState));
            inputDefs.Add("min_filter", minMagFilter);
            inputDefs.Add("mag_filter", minMagFilter);
            inputDefs.Add("mip_filter", new StringInputDef(GetMipFilter(samplerState)));

            inputDefs.Add("max_anisotropy", new FloatInputDef(MtlxDataTypes.Integer, GetMaxAnisotropy(samplerState)));

            return new InlineInputDef(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, inputDefs);
        }

        static InputDef RefractCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(3);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
                ["normal"] = node.Children[1].Compile(inputs, output),
                ["eta"] = node.Children[2].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in", "normal");
            CoerceToType(node, inputs, output, inputDefs, "eta", MtlxDataTypes.Float);

            return new InlineInputDef(MtlxNodeTypes.RealityKitRefract, matchedType, inputDefs);
        }

        static InputDef HyperbolicSineCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");
            var sharedInput = GetSharedInput(inputDefs["in"], output);

            // See https://en.wikipedia.org/wiki/Hyperbolic_functions#Exponential_definitions
            return new InlineInputDef(MtlxNodeTypes.Divide, matchedType, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                        {
                            ["in1"] = sharedInput,
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                        }),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = sharedInput,
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                }),
            });
        }

        static InputDef HyperbolicCosineCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");
            var sharedInput = GetSharedInput(inputDefs["in"], output);

            // See https://en.wikipedia.org/wiki/Hyperbolic_functions#Exponential_definitions
            return new InlineInputDef(MtlxNodeTypes.Divide, matchedType, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                        {
                            ["in1"] = sharedInput,
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                        }),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
                    {
                        ["in"] = sharedInput,
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                }),
            });
        }

        static InputDef HyperbolicTangentCompiler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            node.RequireChildCount(1);

            Dictionary<string, InputDef> inputDefs = new()
            {
                ["in"] = node.Children[0].Compile(inputs, output),
            };
            var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in");

            // See https://en.wikipedia.org/wiki/Hyperbolic_functions#Exponential_definitions
            var sharedExpInput2 = GetSharedInput(new InlineInputDef(MtlxNodeTypes.Exponential, matchedType, new()
            {
                ["in"] = new InlineInputDef(MtlxNodeTypes.Multiply, matchedType, new()
                {
                    ["in1"] = inputDefs["in"],
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                }),
            }), output);

            return new InlineInputDef(MtlxNodeTypes.Divide, matchedType, new()
            {
                ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, matchedType, new()
                {
                    ["in1"] = sharedExpInput2,
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
                ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, matchedType, new()
                {
                    ["in1"] = sharedExpInput2,
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
            });
        }

        static Compiler CreateUnaryOpCompiler(string nodeType, string inputPort = "in", string outputType = null)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(1);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    [inputPort] = node.Children[0].Compile(inputs, output),
                };
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, inputPort);

                return new InlineInputDef(nodeType, outputType ?? matchedType, inputDefs);
            };
        }

        static Compiler CreateBinaryOpCompiler(string nodeType, bool allowFloatRight = false, string outputType = null)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(2);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(inputs, output),
                    ["in2"] = node.Children[1].Compile(inputs, output),
                };

                // We allow the right hand side to be a float for the FA node variants, like vector * scalar.
                string matchedType;
                if (allowFloatRight && GetOutputType(inputDefs["in2"], inputs, output) == MtlxDataTypes.Float)
                    matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1");
                else
                    matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1", "in2");
                    
                return new InlineInputDef(nodeType, outputType ?? matchedType, inputDefs);
            };
        }

        static Compiler CreateBinaryConstantOpCompiler(string nodeType, float constant)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(1);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["in1"] = node.Children[0].Compile(inputs, output),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, constant),
                };
                var matchedType = CoerceToMatchedType(node, inputs, output, inputDefs, "in1");
                
                return new InlineInputDef(nodeType, matchedType, inputDefs);
            };
        }

        static Compiler CreateNaryOpCompiler(string nodeType, params string[] inputPorts)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(inputPorts.Length);

                Dictionary<string, InputDef> inputDefs = new();
                for (var i = 0; i < inputPorts.Length; ++i)
                {
                    inputDefs.Add(inputPorts[i], node.Children[i].Compile(inputs, output));
                }
                var outputType = CoerceToMatchedType(node, inputs, output, inputDefs, inputPorts);

                return new InlineInputDef(nodeType, outputType, inputDefs);
            };
        }

        static Compiler CreateComparisonCompiler(string nodeType, float trueValue)
        {
            return (node, inputs, output) =>
            {
                node.RequireChildCount(2);

                Dictionary<string, InputDef> inputDefs = new()
                {
                    ["value1"] = node.Children[0].Compile(inputs, output),
                    ["value2"] = node.Children[1].Compile(inputs, output),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, trueValue),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f - trueValue),
                };
                CoerceToType(node, inputs, output, inputDefs, "value1", MtlxDataTypes.Float);
                CoerceToType(node, inputs, output, inputDefs, "value2", MtlxDataTypes.Float);

                return new InlineInputDef(nodeType, MtlxDataTypes.Float, inputDefs);
            };
        }

        static TextureSamplerState RequireTextureSampler(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, InputDef inputDef)
        {
            if (inputDef is ExternalInputDef externalInputDef)
            {
                if (inputs.TryGetValue(externalInputDef.Source, out var input) &&
                    input is SamplerStateInput samplerStateInput)
                {
                    return samplerStateInput.SamplerState ?? new();
                }
            }
            throw new ParseException($"Expected texture sampler argument", node.Lexeme.Span);
        }

        static ExternalInputDef RequireExternalTexture(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, InputDef inputDef)
        {
            if (inputDef is ExternalInputDef externalInputDef &&
                inputs.TryGetValue(externalInputDef.Source, out var input) &&
                input is ExternalInput externalInput &&
                externalInput.InputType == MtlxDataTypes.Filename)
            {
                return externalInputDef;
            }
            throw new ParseException($"Expected texture argument", node.Lexeme.Span);
        }

        /// <summary>
        /// Attempts to coerce the inputs identified by inputNames within the inputDefs map (which represents the inputs
        /// to a node in a compound op) so that they are the same type, which may involve promotion (float to vector)
        /// or conversion (color to vector).
        /// </summary>
        /// <param name="node">The abstract syntax node being for which the coercion is being undertaken.</param>
        /// <param name="inputs">The map of all inputs to the parsed expression.</param>
        /// <param name="output">The map that will contain the generated node definitions for the parsed
        /// expression.</param>
        /// <param name="inputDefs">The mapping of input names to their definitions, which may be modified to include
        /// type conversions.</param>
        /// <param name="inputNames">The names of the inputs to be coerced within the inputDefs map.</param>
        /// <returns>The common type to which all named inputs were coerced.</returns>
        static string CoerceToMatchedType(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output,
            Dictionary<string, InputDef> inputDefs, params string[] inputNames)
        {
            var maxLength = 1;
            var maxElementLength = 1;
            foreach (var inputName in inputNames)
            {
                var inputDef = inputDefs[inputName];
                var inputType = GetOutputType(inputDef, inputs, output);
                maxLength = Math.Max(maxLength, MtlxDataTypes.GetLength(inputType));
                maxElementLength = Math.Max(maxElementLength, MtlxDataTypes.GetElementLength(inputType));
            }

            var matchedType = maxElementLength switch
            {
                2 => MtlxDataTypes.Matrix22,
                3 => MtlxDataTypes.Matrix33,
                4 => MtlxDataTypes.Matrix44,
                _ => MtlxDataTypes.GetTypeOfLength(maxLength),
            };

            foreach (var inputName in inputNames)
            {
                CoerceToType(node, inputs, output, inputDefs, inputName, matchedType);
            }

            return matchedType;
        }

        static void CoerceToType(
            SyntaxNode node, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output,
            Dictionary<string, InputDef> inputDefs, string inputName, string expectedType)
        {
            var inputDef = inputDefs[inputName];
            if (!TryCoerce(ref inputDef, inputs, output, expectedType))
            {
                var inputType = GetOutputType(inputDef, inputs, output);
                throw new ParseException(
                    $"Mismatched argument type ({inputType} vs. {expectedType})", node.Lexeme.Span);
            }
            inputDefs[inputName] = inputDef;
        }

        static bool TryCoerce(
            ref InputDef inputDef, Dictionary<string, ParserInput> inputs,
            Dictionary<string, NodeDef> output, string expectedType)
        {
            var outputType = GetOutputType(inputDef, inputs, output);
            if (outputType == expectedType)
                return true;
            
            var outputLength = MtlxDataTypes.GetLength(outputType);
            var expectedLength = MtlxDataTypes.GetLength(expectedType);
            if (inputDef is FloatInputDef floatInputDef)
            {
                var newValues = new float[expectedLength];
                if (outputLength == 1)
                    Array.Fill(newValues, floatInputDef.Values[0]);
                else
                    Array.Copy(floatInputDef.Values, newValues, Math.Min(outputLength, expectedLength));
                inputDef = new FloatInputDef(expectedType, newValues);
                return true;
            }

            // Handle the conversion between vector4s and matrix22s as a special case; it's the one
            // case where we can't distinguish between types by their length.
            // This is specifically for the case of compiling "float2x2 foo = {<four scalars or two vector2s>};",
            // since the right side will be initially compiled as a vector4 (simply because it has four elements).
            if (outputType == MtlxDataTypes.Vector4 && expectedType == MtlxDataTypes.Matrix22)
            {
                var sharedInputDef = GetSharedInput(inputDef, output);
                inputDef = new InlineInputDef(MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = sharedInputDef,
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = sharedInputDef,
                        ["channels"] = new StringInputDef("zw"),
                    }),
                });
                return true;
            } 

            // The conversion nodes can convert a float to anything and
            // can convert between colors and vectors of the same size.
            if (outputLength == 1 || outputLength == expectedLength)
            {
                inputDef = new InlineInputDef(MtlxNodeTypes.Convert, expectedType, new()
                {
                    ["in"] = inputDef,
                });
                return true;
            }

            // We can reduce the number of components of vectors by swizzling.
            var isColor = MtlxDataTypes.IsColor(outputType);
            var isVector = MtlxDataTypes.IsVector(outputType);
            if ((isColor || isVector) && expectedLength < outputLength)
            {
                inputDef = new InlineInputDef(MtlxNodeTypes.Swizzle, expectedType, new()
                {
                    ["in"] = inputDef,
                    ["channels"] = new StringInputDef((isColor ? "rgb" : "xyz").Substring(0, expectedLength)),
                });
                return true;
            }

            return false;
        }

        static void AddImageSamplerState(Dictionary<string, InputDef> inputDefs, TextureSamplerState samplerState)
        {
            inputDefs.Add("filtertype", new StringInputDef(GetFilterType(samplerState)));

            StringInputDef addressMode = new(GetAddressMode(samplerState));
            inputDefs.Add("uaddressmode", addressMode);
            inputDefs.Add("vaddressmode", addressMode);
        }

        static string GetAddressMode(TextureSamplerState samplerState)
        {
            return samplerState.wrap switch
            {
                TextureSamplerState.WrapMode.Clamp => "clamp",
                TextureSamplerState.WrapMode.Mirror => "mirror",
                _ => "periodic",
            };
        }

        static string GetFilterType(TextureSamplerState samplerState)
        {
            return samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Point => "closest",
                _ => "linear",
            };
        }

        static void AddTexture2DSamplerState(Dictionary<string, InputDef> inputDefs, TextureSamplerState samplerState)
        {
            StringInputDef minMagFilter = new(GetMinMagFilter(samplerState));
            inputDefs.Add("min_filter", minMagFilter);
            inputDefs.Add("mag_filter", minMagFilter);
            inputDefs.Add("mip_filter", new StringInputDef(GetMipFilter(samplerState)));

            StringInputDef wrapMode = new(GetWrapMode(samplerState));
            inputDefs.Add("u_wrap_mode", wrapMode);
            inputDefs.Add("v_wrap_mode", wrapMode);

            inputDefs.Add("max_anisotropy", new FloatInputDef(MtlxDataTypes.Integer, GetMaxAnisotropy(samplerState)));
        }

        static string GetMinMagFilter(TextureSamplerState samplerState)
        {
            return samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Point => "nearest",
                _ => "linear",
            };
        }

        static string GetMipFilter(TextureSamplerState samplerState)
        {
            return samplerState.filter switch
            {
                TextureSamplerState.FilterMode.Trilinear => "linear",
                _ => "nearest",
            };
        }

        static string GetWrapMode(TextureSamplerState samplerState)
        {
            return samplerState.wrap switch
            {
                TextureSamplerState.WrapMode.Clamp => "clamp_to_edge",
                TextureSamplerState.WrapMode.Mirror => "mirrored_repeat",
                _ => "repeat",
            };
        }

        static int GetMaxAnisotropy(TextureSamplerState samplerState)
        {
            return samplerState.anisotropic switch
            {
                TextureSamplerState.Anisotropic.x2 => 2,
                TextureSamplerState.Anisotropic.x4 => 4,
                TextureSamplerState.Anisotropic.x8 => 8,
                TextureSamplerState.Anisotropic.x16 => 16,
                _ => 1,
            };
        }

        // Creates a compiler to construct the specified data type
        // (or null to infer the type from the number of elements).
        static Compiler CreateConstructorCompiler(string fixedDataType = null)
        {
            return (node, inputs, output) =>
            {
                List<InputDef> inputDefs = new();
                List<string> outputTypes = new();
                var allFloatInputDefs = true;
                foreach (var child in node.Children)
                {
                    var inputDef = child.Compile(inputs, output);
                    if (inputDef is not FloatInputDef)
                        allFloatInputDefs = false;

                    inputDefs.Add(inputDef);
                    outputTypes.Add(GetOutputType(inputDef, inputs, output));
                }
                var totalLength = outputTypes.Select(MtlxDataTypes.GetLength).Sum();
                string dataType;
                if (fixedDataType == null)
                {
                    dataType = MtlxDataTypes.GetTypeOfLength(totalLength);
                    if (dataType == null)
                        throw new ParseException($"No type known of length {totalLength}", node.Lexeme.Span);
                }
                else 
                {
                    var expectedLength = MtlxDataTypes.GetLength(fixedDataType);
                    if (totalLength != expectedLength)
                    {
                        throw new ParseException(
                            $"Expected {expectedLength} components, found {totalLength}", node.Lexeme.Span);
                    }
                    dataType = fixedDataType;
                }

                if (inputDefs.Count == 1)
                {
                    return inputDefs[0];
                }
                else if (allFloatInputDefs)
                {
                    var values = inputDefs.SelectMany(inputDef => ((FloatInputDef)inputDef).Values).ToArray();
                    return new FloatInputDef(dataType, values);
                }

                if (MtlxDataTypes.IsVector(dataType))
                    return CompileVectorConstructor(output, dataType, inputDefs, outputTypes);
                else if (MtlxDataTypes.IsMatrix(dataType))
                    return CompileMatrixConstructor(output, dataType, inputDefs, outputTypes);
                else
                    throw new ParseException($"Cannot construct type {dataType}", node.Lexeme.Span);
            };
        }
        
        static InputDef CompileVectorConstructor(
            Dictionary<string, NodeDef> output, string dataType, List<InputDef> inputDefs, List<string> outputTypes)
        {
            Assert.IsTrue(MtlxDataTypes.IsVector(dataType));
            var dataTypeLength = MtlxDataTypes.GetLength(dataType);

            Dictionary<string, InputDef> inputDefsMap = new();
            var inIndex = 1;

            void AddScalar(InputDef inputDef)
            {
                Assert.IsTrue(inIndex <= dataTypeLength);
                inputDefsMap[$"in{inIndex++}"] = inputDef;
            }

            for (var i = 0; i < inputDefs.Count; ++i)
            {
                var inputLength = MtlxDataTypes.GetLength(outputTypes[i]);
                Assert.AreNotEqual(inputLength, 0);
                if (inputLength == 1)
                {
                    AddScalar(inputDefs[i]);
                    continue;
                }
                var inputDef = inputDefs[i];
                if (inputDef is FloatInputDef floatInputDef)
                {
                    foreach (var value in floatInputDef.Values)
                    {
                        AddScalar(new FloatInputDef(MtlxDataTypes.Float, value));
                    }
                }
                else
                {
                    var sharedInput = GetSharedInput(inputDef, output);
                    for (var j = 0; j < inputLength; ++j)
                    {
                        AddScalar(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = sharedInput,
                            ["channels"] = new StringInputDef("xyzw".Substring(j, 1)),
                        }));
                    }
                }
            }

            return new InlineInputDef($"combine{dataTypeLength}", dataType, inputDefsMap);
        }

        static InputDef CompileMatrixConstructor(
            Dictionary<string, NodeDef> output, string dataType, List<InputDef> inputDefs, List<string> outputTypes)
        {
            Assert.IsTrue(MtlxDataTypes.IsMatrix(dataType));

            Dictionary<string, InputDef> inputDefsMap = new();
            var inIndex = 1;
            var elementLength = MtlxDataTypes.GetElementLength(dataType);
            var vectorType = MtlxDataTypes.GetTypeOfLength(elementLength);
            Dictionary<string, InputDef> vectorInputDefsMap = null;
            var vectorInIndex = 1;

            void AddVector(InputDef inputDef)
            {
                Assert.IsTrue(inIndex <= elementLength);
                inputDefsMap[$"in{inIndex++}"] = inputDef;
            }

            void AddScalar(InputDef inputDef)
            {
                if (vectorInputDefsMap == null)
                    AddVector(new InlineInputDef($"combine{elementLength}", vectorType, vectorInputDefsMap = new()));
                
                Assert.IsTrue(vectorInIndex <= elementLength);
                vectorInputDefsMap[$"in{vectorInIndex++}"] = inputDef;
                if (vectorInIndex > elementLength)
                {
                    vectorInputDefsMap = null;
                    vectorInIndex = 1;
                }
            }

            for (var i = 0; i < inputDefs.Count; ++i)
            {
                var outputType = outputTypes[i];
                if (outputType == vectorType && vectorInputDefsMap == null)
                {
                    AddVector(inputDefs[i]);
                    continue;
                }
                var outputLength = MtlxDataTypes.GetLength(outputType);
                Assert.AreNotEqual(outputLength, 0);
                if (outputLength == 1)
                {
                    AddScalar(inputDefs[i]);
                    continue;
                }
                var inputDef = inputDefs[i];
                if (inputDef is FloatInputDef floatInputDef)
                {
                    foreach (var value in floatInputDef.Values)
                    {
                        AddScalar(new FloatInputDef(MtlxDataTypes.Float, value));
                    }
                }
                else
                {
                    var sharedInput = GetSharedInput(inputDef, output);
                    for (var j = 0; j < outputLength; ++j)
                    {
                        AddScalar(new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = sharedInput,
                            ["channels"] = new StringInputDef("xyzw".Substring(j, 1)),
                        }));
                    }
                }
            }

            return new InlineInputDef(MtlxNodeTypes.Transpose, dataType, new()
            {
                ["in"] = new InlineInputDef($"realitykit_combine{elementLength}", dataType, inputDefsMap),
            });
        }

        static InputDef GetSharedInput(InputDef inputDef, Dictionary<string, NodeDef> output)
        {
            if (inputDef is not InlineInputDef inlineInputDef)
                return inputDef;
            
            var temporaryName = $"__Tmp{output.Count}";
            output.Add(temporaryName, inlineInputDef.Source);
            return new InternalInputDef(temporaryName);
        }

        static string GetOutputType(
            InputDef inputDef, Dictionary<string, ParserInput> inputs, Dictionary<string, NodeDef> output)
        {
            if (inputDef is PerStageInputDef perStageInputDef)
            {
                // Ensure that both stages have the same type.
                var vertexType = GetOutputType(perStageInputDef.Vertex, inputs, output);
                var fragmentType = GetOutputType(perStageInputDef.Fragment, inputs, output);
                Assert.AreEqual(vertexType, fragmentType);
                return vertexType;
            }
            return inputDef switch
            {
                FloatInputDef floatInputDef => floatInputDef.PortType,
                InternalInputDef internalInputDef => output[internalInputDef.Source].OutputType,
                ExternalInputDef externalInputDef => inputs[externalInputDef.Source].InputType,
                InlineInputDef inlineInputDef => inlineInputDef.Source.OutputType,
                ImplicitInputDef implicitInputDef => implicitInputDef.DataType,
                TextureSizeInputDef => MtlxDataTypes.Vector3,
                _ => MtlxDataTypes.String,
            };
        }

        static Compiler CreateConstantCompiler(float value)
        {
            return (node, inputs, output) => new FloatInputDef(MtlxDataTypes.Float, value);
        }

        static Compiler CreateImplicitCompiler(string dataType)
        {
            return (node, inputs, output) => new ImplicitInputDef(node.Lexeme.Span.contents, dataType);
        }

        static Compiler CreateSamplerCompiler(TextureSamplerState samplerState)
        {
            return (node, inputs, output) =>
            {
                var name = node.Lexeme.Span.contents;
                inputs[name] = new SamplerStateInput(samplerState);
                return new ExternalInputDef(name);
            };
        }

        // Convenience function to create space transforms more conveniently. 
        // Returns (preMult * input * postMult) ^ (invert ? -1 : 1), using either the vertex node type
        // or the fragment node type for the input depending on the active stage.
        static Compiler CreateMatrixCompiler(
            InputDef preMult, string vertexNodeType, string fragmentNodeType,
            string outputName, InputDef postMult, bool invert)
        {
            return CreatePerStageCompiler(
                CreateMatrixCompiler(preMult, vertexNodeType, outputName, postMult, invert),
                CreateMatrixCompiler(preMult, fragmentNodeType, outputName, postMult, invert));
        }

        // Convenience function to create separate compilers for each shader stage.
        static Compiler CreatePerStageCompiler(Compiler vertexCompiler, Compiler fragmentCompiler)
        {
            return (node, inputs, output) => new PerStageInputDef(
                vertexCompiler(node, inputs, output), fragmentCompiler(node, inputs, output));
        }

        // Convenience function to create space transforms more conveniently. 
        // Returns (preMult * input * postMult) ^ (invert ? -1 : 1).
        static Compiler CreateMatrixCompiler(
            InputDef preMult, string nodeType, string outputName, InputDef postMult, bool invert)
        {
            return (node, inputs, output) =>
            {
                var inputDef = new InlineInputDef(nodeType, MtlxDataTypes.Matrix44, new(), outputName);

                if (preMult != null)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44, new()
                    {
                        ["in1"] = preMult,
                        ["in2"] = inputDef,
                    });
                }
                if (postMult != null)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix44, new()
                    {
                        ["in1"] = inputDef,
                        ["in2"] = postMult,
                    });
                }
                if (invert)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Inverse, MtlxDataTypes.Matrix44, new()
                    {
                        ["in"] = inputDef,
                    });
                }

                return inputDef;
            };
        }

        static Compiler CreateTangentMatrixCompiler(bool invert)
        {
            return (node, inputs, output) =>
            {
                // Local function to create column from geometry position/vector.
                InlineInputDef CreateColumn(string nodeType, bool position = false)
                {
                    // Start with the object space geometry converted to a vector4: (x, y, z, 1)
                    var columnDef = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Vector4, new()
                    {
                        ["in"] = new InlineInputDef(nodeType, MtlxDataTypes.Vector3, new()
                        {
                            ["space"] = new StringInputDef("object"),
                        }),
                    });

                    // For vectors, clear the w component before transforming.
                    if (!position)
                    {
                        columnDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4, new()
                        {
                            ["in1"] = columnDef,
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 1.0f, 1.0f, 0.0f),
                        });
                    }

                    // Transform by the model-to-world matrix and invert the resulting z to convert to Unity space.
                    columnDef = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector4, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector4, new()
                        {
                            ["in"] = columnDef,
                            ["mat"] = new PerStageInputDef(
                                new InlineInputDef(
                                    MtlxNodeTypes.RealityKitGeometryModifierModelToWorld,
                                    MtlxDataTypes.Matrix44, new(), "modelToWorld"),
                                new InlineInputDef(
                                    MtlxNodeTypes.RealityKitSurfaceModelToWorld,
                                    MtlxDataTypes.Matrix44, new(), "modelToWorld")),
                        }),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 1.0f, -1.0f, 1.0f),
                    });

                    if (position)
                        return columnDef;
                    
                    // Normalize vectors after transformation.
                    return new InlineInputDef(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector4, new()
                    {
                        ["in"] = columnDef,
                    });
                }

                var inputDef = new InlineInputDef(MtlxNodeTypes.RealityKitCombine4, MtlxDataTypes.Matrix44, new()
                {
                    ["in1"] = CreateColumn(MtlxNodeTypes.GeomTangent),
                    ["in2"] = CreateColumn(MtlxNodeTypes.GeomBitangent),
                    ["in3"] = CreateColumn(MtlxNodeTypes.GeomNormal),
                    ["in4"] = CreateColumn(MtlxNodeTypes.GeomPosition, true),
                });

                if (invert)
                {
                    inputDef = new InlineInputDef(MtlxNodeTypes.Inverse, MtlxDataTypes.Matrix44, new()
                    {
                        ["in"] = inputDef,
                    });
                }

                return inputDef;
            };
        }
    }
}