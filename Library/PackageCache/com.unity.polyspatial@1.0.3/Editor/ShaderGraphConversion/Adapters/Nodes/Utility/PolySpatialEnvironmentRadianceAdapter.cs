using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class PolySpatialEnvironmentRadianceAdapter : ANodeAdapter<PolySpatialEnvironmentRadianceNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            Dictionary<string, InputDef> inputs = new()
            {
                ["baseColor"] = new ExternalInputDef("BaseColor"),
                ["roughness"] = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Half, new()
                {
                    ["in"] = new ExternalInputDef("Roughness"),
                }),
                ["specular"] = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Half, new()
                {
                    ["in"] = new ExternalInputDef("Specular"),
                }),
                ["metallic"] = new InlineInputDef(MtlxNodeTypes.Convert, MtlxDataTypes.Half, new()
                {
                    ["in"] = new ExternalInputDef("Metallic"),
                }),
            };

            // Only add the mapping for the normal if it's connected.  Otherwise,
            // we want the default of the RealityKit world space normal.
            var normalSlot = NodeUtils.GetInputByName(node, "Normal");
            if (normalSlot.isConnected)
            {
                // Flip Z to convert to RealityKit coordinates.
                inputs["normal"] = new InlineInputDef(MtlxNodeTypes.Multiply, MtlxDataTypes.Vector3, new()
                {
                    ["in1"] = new ExternalInputDef("Normal"),
                    ["in2"] = new FloatInputDef(MtlxDataTypes.Vector3, 1.0f, 1.0f, -1.0f),
                });
            }

            // The realitykit_environment_radiance node has two outputs, but all of our conversion code assumes a
            // single output per MaterialX node, so we have to duplicate the node for each output.
            // TODO (LXR-2880): Add support for nodes with multiple outputs to conversion code.
            QuickNode.CompoundOp(node, graph, externals, sgContext, "PolySpatialEnvironmentRadiance", new()
            {
                ["Diffuse Radiance"] = new(
                    MtlxNodeTypes.RealityKitEnvironmentRadiance, MtlxDataTypes.Color3, inputs, "diffuseRadiance"),
                ["Specular Radiance"] = new(
                    MtlxNodeTypes.RealityKitEnvironmentRadiance, MtlxDataTypes.Color3, inputs, "specularRadiance"),
            });
        }
    }
}