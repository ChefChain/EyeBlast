using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX.Tests
{
    using ParserInput = CompoundOpParser.ParserInput;
    using ExternalInput = CompoundOpParser.ExternalInput;
    using SamplerStateInput = CompoundOpParser.SamplerStateInput;
    
    [TestFixture]
    public class CompoundOpParserTests
    {
        [Test]
        public void Test_Empty()
        {
            var nodeDefs = CompoundOpParser.Parse(new(), "");
            Assert.AreEqual(nodeDefs.Count, 0);

            nodeDefs = CompoundOpParser.Parse(new(),
@"
// Comments have no effect.
/* Out = A + B;
*/
");
            Assert.AreEqual(nodeDefs.Count, 0);
        }

        [Test]
        public void Test_Scalar_Arithmetic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["A"] = new ExternalInput(MtlxDataTypes.Float),
                ["B"] = new ExternalInput(MtlxDataTypes.Float),
                ["C"] = new ExternalInput(MtlxDataTypes.Float),
            };
            
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("A"),
                    ["in2"] = new ExternalInputDef("B"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A + B;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("A"),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("B"),
                        ["in2"] = new ExternalInputDef("C"),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A + B * C;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                    ["in2"] = new ExternalInputDef("C"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A * B + C;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new ExternalInputDef("B"),
                            ["in2"] = new ExternalInputDef("C"),
                        }),
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, -1.0f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = (A * (B + C) + (((-1.0f))));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["D"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                    {
                        ["in2"] = new ExternalInputDef("C")
                    }),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 5.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                    ["in2"] = new InternalInputDef("D"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float D = -C * 5; Out = A + B + D;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Mix, MtlxDataTypes.Float, new()
                {
                    ["bg"] = new ExternalInputDef("A"),
                    ["fg"] = new ExternalInputDef("B"),
                    ["mix"] = new ExternalInputDef("C"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = lerp(A, B, C);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = distance(A, B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = length(A - B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Float, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Float, 1.0e-16f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = 1.0e-16;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Power, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, -1.0f),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Power, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new ExternalInputDef("B"),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, -0.5f),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = rcp(A) * rsqrt(B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("A"),
                    ["in2"] = new ExternalInputDef("B"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(A, B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Vector_Arithmetic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["Normal"] = new ExternalInput(MtlxDataTypes.Vector3),
                ["ViewDir"] = new ExternalInput(MtlxDataTypes.Vector3),
                ["Power"] = new ExternalInput(MtlxDataTypes.Float),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Power, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Clamp, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, new()
                            {
                                ["in1"] = new InlineInputDef(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3, new()
                                {
                                    ["in"] = new ExternalInputDef("Normal"),
                                }),
                                ["in2"] = new InlineInputDef(MtlxNodeTypes.Normalize, MtlxDataTypes.Vector3, new()
                                {
                                    ["in"] = new ExternalInputDef("ViewDir"),
                                }),
                            }),
                        }),
                    }),
                    ["in2"] = new ExternalInputDef("Power"),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector4, 1.0f, 2.0f, 3.0f, 4.0f),
                    ["channels"] = new StringInputDef("xyy"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = float4(1.0, 2.0, 3.0, 4.0).rgg;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["__Tmp0"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Vector2, 1.0f, 2.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 1.0f, 2.0f),
                }),
                ["__Tmp1"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector2, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Vector2, 3.0f, 4.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector2, 3.0f, 4.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("x")
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("y")
                    }),
                    ["in3"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp1"),
                        ["channels"] = new StringInputDef("x")
                    }),
                    ["in4"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp1"),
                        ["channels"] = new StringInputDef("y")
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = float4(float2(1.0, 2.0) + float2(1.0, 2.0), float2(3.0, 4.0) * float2(3.0, 4.0));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Arctangent2, MtlxDataTypes.Vector3, new()
                {
                    ["iny"] = new ExternalInputDef("Normal"),
                    ["inx"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, 1.0f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = atan(Normal);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Add, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new ExternalInputDef("Normal"),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                        {
                            ["in1"] = new ExternalInputDef("ViewDir"),
                            ["in2"] = new ExternalInputDef("Normal"),
                        }),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 0.5f, 0.5f, 0.5f),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = lerp(Normal, ViewDir, float3(0.5, 0.5, 0.5));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.SmoothStep, MtlxDataTypes.Vector2, new()
                {
                    ["low"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("Normal"),
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["high"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("ViewDir"),
                        ["channels"] = new StringInputDef("zw"),
                    }),
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.5f, 0.5f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = smoothstep(Normal.xy, ViewDir.zw, float2(0.5, 0.5));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.RealityKitStep, MtlxDataTypes.Vector2, new()
                {
                    ["edge"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("Normal"),
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new ExternalInputDef("ViewDir"),
                        ["channels"] = new StringInputDef("zw"),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = step(Normal.rg, ViewDir.ba);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Length, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Subtract, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new ExternalInputDef("Normal"),
                        ["in2"] = new ExternalInputDef("ViewDir"),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = distance(Normal, ViewDir);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = length(Normal - ViewDir);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.SplitLR, MtlxDataTypes.Vector3, new()
                {
                    ["valuel"] = new ExternalInputDef("Normal"),
                    ["valuer"] = new ExternalInputDef("ViewDir"),
                    ["center"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                    ["texcoord"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.0f, 0.0f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = splitlr(Normal, ViewDir, 0.5, float2(0.0, 0.0));");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.DotProduct, MtlxDataTypes.Float, new()
                {
                    ["in1"] = new ExternalInputDef("Normal"),
                    ["in2"] = new ExternalInputDef("ViewDir"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(Normal, ViewDir);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Vector2, 0.0f, 1.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new InternalInputDef("tmp"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2 tmp = {0, 1}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["tmp"] = new(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in3"] = new ExternalInputDef("Power"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("tmp"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float3 tmp = {0, 1, Power}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["tmp"] = new(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.Combine3, MtlxDataTypes.Vector3, new()
                    {
                        ["in1"] = new ExternalInputDef("Power"),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in3"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                    }),
                    ["channels"] = new StringInputDef("x"),
                }),
                ["tmp3"] = new(MtlxNodeTypes.Convert, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("tmp"),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Vector3, new()
                {
                    ["in"] = new InternalInputDef("tmp3"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "float tmp = float3(Power, 1, 2); float3 tmp3 = tmp; Out = tmp3;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Matrix_Arithmetic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["M1"] = new ExternalInput(MtlxDataTypes.Matrix22),
                ["M2"] = new ExternalInput(MtlxDataTypes.Matrix22),
                ["A"] = new ExternalInput(MtlxDataTypes.Vector2),
                ["B"] = new ExternalInput(MtlxDataTypes.Vector2),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Matrix22, 1.0f, 0.0f, 0.0f, 1.0f),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = float2x2(1, 0, 0, 1);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Transpose, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InlineInputDef(MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22, new()
                    {
                        ["in1"] = new ExternalInputDef("A"),
                        ["in2"] = new ExternalInputDef("B"),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = float2x2(A, B);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ExternalInputDef("A"),
                    ["mat"] = new ExternalInputDef("M1"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(M1, A);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.TransformMatrix, MtlxDataTypes.Vector2, new()
                {
                    ["in"] = new ExternalInputDef("A"),
                    ["mat"] = new InlineInputDef(MtlxNodeTypes.Transpose, MtlxDataTypes.Matrix22, new()
                    {
                        ["in"] = new ExternalInputDef("M1"),
                    }),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(A, M1);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.Multiply, MtlxDataTypes.Matrix22, new()
                {
                    ["in1"] = new ExternalInputDef("M1"),
                    ["in2"] = new ExternalInputDef("M2"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = mul(M1, M2);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Matrix22, 0.0f, 1.0f, 1.0f, 0.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("tmp"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2x2 tmp = {0, 1, 1, 0}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["tmp"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new FloatInputDef(MtlxDataTypes.Matrix22, 1.0f, 2.0f, 3.0f, 4.0f),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("tmp"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2x2 tmp = {{1, 2}, {3, 4}}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["__Tmp0"] = new(MtlxNodeTypes.Combine4, MtlxDataTypes.Vector4, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new ExternalInputDef("A"),
                        ["channels"] = new StringInputDef("x"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                    {
                        ["in"] = new ExternalInputDef("A"),
                        ["channels"] = new StringInputDef("y"),
                    }),
                    ["in3"] = new FloatInputDef(MtlxDataTypes.Float, 3.0f),
                    ["in4"] = new FloatInputDef(MtlxDataTypes.Float, 4.0f),
                }),
                ["tmp"] = new(MtlxNodeTypes.RealityKitCombine2, MtlxDataTypes.Matrix22, new()
                {
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("xy"),
                    }),
                    ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Vector2, new()
                    {
                        ["in"] = new InternalInputDef("__Tmp0"),
                        ["channels"] = new StringInputDef("zw"),
                    }),
                }),
                ["Out"] = new(MtlxNodeTypes.Dot, MtlxDataTypes.Matrix22, new()
                {
                    ["in"] = new InternalInputDef("tmp"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "float2x2 tmp = {A, {3, 4}}; Out = tmp;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Boolean_Logic()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["A"] = new ExternalInput(MtlxDataTypes.Float),
                ["B"] = new ExternalInput(MtlxDataTypes.Float),
                ["C"] = new ExternalInput(MtlxDataTypes.Vector3),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new ExternalInputDef("A"),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = !A;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.Add, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                                {
                                    ["in1"] = new ExternalInputDef("A"),
                                    ["in2"] = new ExternalInputDef("B"),
                                }),
                                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            }),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Absolute, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                            {
                                ["value1"] = new InlineInputDef(MtlxNodeTypes.Length, MtlxDataTypes.Float, new()
                                {
                                    ["in"] = new ExternalInputDef("C"),
                                }),
                                ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                                ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            }),
                        }),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A && B || any(C);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                        {
                            ["in1"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new ExternalInputDef("C"),
                                ["channels"] = new StringInputDef("x"),
                            }),
                            ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                            {
                                ["in"] = new ExternalInputDef("C"),
                                ["channels"] = new StringInputDef("y"),
                            }),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.Swizzle, MtlxDataTypes.Float, new()
                        {
                            ["in"] = new ExternalInputDef("C"),
                            ["channels"] = new StringInputDef("z"),
                        }),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = all(C);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Float, new()
                    {
                        ["in1"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Float, new()
                        {
                            ["value1"] = new ExternalInputDef("A"),
                            ["value2"] = new ExternalInputDef("B"),
                            ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        }),
                        ["in2"] = new InlineInputDef(MtlxNodeTypes.IfGreater, MtlxDataTypes.Float, new()
                        {
                            ["value1"] = new ExternalInputDef("A"),
                            ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                            ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        }),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(inputs, "Out = A > B && A <= 1;");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.IfEqual, MtlxDataTypes.Vector3, new()
                {
                    ["value1"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                    {
                        ["value1"] = new ExternalInputDef("A"),
                        ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    }),
                    ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                    ["in1"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Vector3, new()
                    {
                        ["value1"] = new InlineInputDef(MtlxNodeTypes.IfEqual, MtlxDataTypes.Float, new()
                        {
                            ["value1"] = new ExternalInputDef("A"),
                            ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 2.0f),
                            ["in1"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                            ["in2"] = new FloatInputDef(MtlxDataTypes.Float, 1.0f),
                        }),
                        ["value2"] = new FloatInputDef(MtlxDataTypes.Float, 0.0f),
                        ["in1"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, 1.0f),
                        ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 0.0f, 0.0f, 0.0f),
                    }),
                    ["in2"] = new ExternalInputDef("C"),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = A == 1 ? C : A != 2 ? float3(0, 0, 0) : float3(1, 1, 1);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }

        [Test]
        public void Test_Texture_Sampling()
        {
            Dictionary<string, ParserInput> inputs = new()
            {
                ["Texture"] = new ExternalInput(MtlxDataTypes.Filename),
                ["UV"] = new ExternalInput(MtlxDataTypes.Vector2),
            };
            Dictionary<string, NodeDef> expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.RealityKitTexture2D, MtlxDataTypes.Vector4, new()
                {
                    ["file"] = new ExternalInputDef("Texture"),
                    ["texcoord"] = new ExternalInputDef("UV"),
                    ["mag_filter"] = new StringInputDef("linear"),
                    ["min_filter"] = new StringInputDef("linear"),
                    ["mip_filter"] = new StringInputDef("nearest"),
                    ["u_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["v_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["max_anisotropy"] = new FloatInputDef(MtlxDataTypes.Integer, 1),
                }),
            };
            var actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = SAMPLE_TEXTURE2D(Texture, samplerpolySpatial_Lightmap, UV);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);

            expectedNodeDefs = new()
            {
                ["Out"] = new(MtlxNodeTypes.RealityKitTexture2DLOD, MtlxDataTypes.Vector4, new()
                {
                    ["file"] = new ExternalInputDef("Texture"),
                    ["texcoord"] = new ExternalInputDef("UV"),
                    ["lod"] = new FloatInputDef(MtlxDataTypes.Float, 0.5f),
                    ["mag_filter"] = new StringInputDef("linear"),
                    ["min_filter"] = new StringInputDef("linear"),
                    ["mip_filter"] = new StringInputDef("nearest"),
                    ["u_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["v_wrap_mode"] = new StringInputDef("clamp_to_edge"),
                    ["max_anisotropy"] = new FloatInputDef(MtlxDataTypes.Integer, 1),
                }),
            };
            actualNodeDefs = CompoundOpParser.Parse(
                inputs, "Out = SAMPLE_TEXTURE2D_LOD(Texture, samplerpolySpatial_Lightmap, UV, 0.5);");
            Assert.AreEqual(expectedNodeDefs, actualNodeDefs);
        }
    }
}