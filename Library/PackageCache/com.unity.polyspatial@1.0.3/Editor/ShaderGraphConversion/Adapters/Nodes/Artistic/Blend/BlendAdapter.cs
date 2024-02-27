
namespace UnityEditor.ShaderGraph.MaterialX
{
    class BlendAdapter : ANodeAdapter<BlendNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (node is not BlendNode blendNode)
                return;

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Blend-Node.html
            var tmpType = MtlxDataTypes.GetLength(NodeUtils.GetDataTypeName(node)) switch
            {
                1 => "float",
                var dim => $"float{dim}",
            };
            var tmpExpr = blendNode.blendMode switch
            {
                BlendMode.HardLight => $@"
{tmpType} result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
{tmpType} result2 = 2.0 * Base * Blend;
{tmpType} zeroOrOne = step(Blend, 0.5);",
                BlendMode.Overlay => $@"
{tmpType} result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
{tmpType} result2 = 2.0 * Base * Blend;
{tmpType} zeroOrOne = step(Base, 0.5);",
                BlendMode.PinLight => $@"
{tmpType} check = step(0.5, Blend);
{tmpType} result1 = check * max(2.0 * (Base - 0.5), Blend);",
                BlendMode.SoftLight => $@"
{tmpType} result1 = 2.0 * Base * Blend + Base * Base * (1.0 - 2.0 * Blend);
{tmpType} result2 = sqrt(Base) * (2.0 * Blend - 1.0) + 2.0 * Base * (1.0 - Blend);
{tmpType} zeroOrOne = step(0.5, Blend);",
                BlendMode.VividLight => $@"
{tmpType} clampedBase = clamp(Base, 0.000001, 0.999999);
{tmpType} result1 = 1.0 - (1.0 - Blend) / (2.0 * clampedBase);
{tmpType} result2 = Blend / (2.0 * (1.0 - clampedBase));
{tmpType} zeroOrOne = step(0.5, clampedBase);",
                _ => "",
            };
            var blendExpr = blendNode.blendMode switch
            {
                BlendMode.Burn => "1.0 - (1.0 - Blend)/(Base + 0.000000000001)",
                BlendMode.Darken => "min(Blend, Base)",
                BlendMode.Difference => "abs(Blend - Base)",
                BlendMode.Dodge => "Base / (1.0 - clamp(Blend, 0.000001, 0.999999))",
                BlendMode.Divide => "Base / (Blend + 0.000000000001)",
                BlendMode.Exclusion => "Blend + Base - (2.0 * Blend * Base)",
                BlendMode.HardLight => "result2 * zeroOrOne + (1 - zeroOrOne) * result1",
                BlendMode.HardMix => "step(1 - Base, Blend)",
                BlendMode.Lighten => "max(Blend, Base)",
                BlendMode.LinearBurn => "Base + Blend - 1.0",
                BlendMode.LinearDodge => "Base + Blend",
                BlendMode.LinearLight => @"
lerp(max(Base + (2 * Blend) - 1, 0), min(Base + 2 * (Blend - 0.5), 1), step(0.5, Blend))",
                BlendMode.LinearLightAddSub => "Blend + 2.0 * Base - 1.0",
                BlendMode.Multiply => "Base * Blend",
                BlendMode.Negation => "1.0 - abs(1.0 - Blend - Base)",
                BlendMode.Overlay => "result2 * zeroOrOne + (1 - zeroOrOne) * result1",
                BlendMode.PinLight => "result1 + (1.0 - check) * min(2.0 * Base, Blend)",
                BlendMode.Screen => "1.0 - (1.0 - Blend) * (1.0 - Base)",
                BlendMode.SoftLight => "result2 * zeroOrOne + (1 - zeroOrOne) * result1",
                BlendMode.Subtract => "Base - Blend",
                BlendMode.VividLight => "result2 * zeroOrOne + (1 - zeroOrOne) * result1",
                BlendMode.Overwrite or _ => "Blend",
            };
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Blend", $"{tmpExpr}Out = lerp(Base, {blendExpr}, Opacity);");
        }
    }
}
