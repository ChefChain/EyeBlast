using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph.MaterialX
{
    class PropertyAdapter : ANodeAdapter<PropertyNode>
    {
        public override bool IsNodeSupported(AbstractMaterialNode node)
        {
            if (base.IsNodeSupported(node) && node is PropertyNode pnode)
            {
                try
                {
                    return SlotUtils.GetDataTypeName(pnode.FindSlot<MaterialSlot>(0)) != null;
                }
                catch
                {
                }
            }
            return false;
        }

        public override void BuildInstance(AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals)
        {
            var pnode = (PropertyNode)node;
            var property = pnode.property;

            // Gradients are special and can be statically evaluated, so we do not need them.
            if (property is GradientShaderProperty)
                return;

            // Unique and stable property name, should be identical to the material property name.
            string nodeName = pnode.property.referenceName;

            var slot = node.FindSlot<MaterialSlot>(0);
            externals.AddExternalPort(slot.slotReference, nodeName);

            // Property Nodes are special, in that we only need 1 to exist for each property.
            if (graph.HasNode(nodeName))
                return;
                
            string nodeType = MtlxNodeTypes.Constant;
            string dataType = (pnode.property.propertyType == PropertyType.Color) ?
                MtlxDataTypes.Color4 : SlotUtils.GetDataTypeName(slot);

            var nodeData = graph.AddNode(nodeName, nodeType, dataType, true);

            // TODO: abstract the value container to just have both.
            if (dataType == MtlxDataTypes.Filename)
            {
                try
                {
                    // TODO: Support other texture types.
                    var tprop = (Texture2DShaderProperty)property;
                    var root = $"{Application.streamingAssetsPath}/{slot.owner.owner.path}";
                    var filename = System.IO.Path.GetRelativePath(root, UnityEditor.AssetDatabase.GetAssetPath(tprop.value.texture));

                    nodeData.AddPortString("value", dataType, filename);
                }
                catch
                {
                    // FNF or no texture file was referenced.
                    nodeData.AddPortString("value", dataType, "placeholder.png");
                }
            }
            else
            {
                var values = GetDefaultValue(property);
                nodeData.AddPortValue("value", dataType, values);
            }
        }


        internal static float[] GetDefaultValue(AbstractShaderProperty property)
        {
            switch (property)
            {
                case ColorShaderProperty c4: return new float[4] { c4.value.r, c4.value.g, c4.value.b, c4.value.a };
                case BooleanShaderProperty b: return new float[1] { b.value ? 1f : 0f };
                case Vector1ShaderProperty f: return new float[1] { f.value };
                case Vector2ShaderProperty v2: return new float[2] { v2.value.x, v2.value.y };
                case Vector3ShaderProperty v3: return new float[3] { v3.value.x, v3.value.y, v3.value.z };
                case Vector4ShaderProperty vd: return new float[4] { vd.value.x, vd.value.y, vd.value.z, vd.value.w };
                case Matrix2ShaderProperty m2:
                    return new float[4]
                    {
                        m2.value.m00, m2.value.m01,
                        m2.value.m10, m2.value.m11,
                    };
                case Matrix3ShaderProperty m3:
                    return new float[9]
                    {
                        m3.value.m00, m3.value.m01, m3.value.m02,
                        m3.value.m10, m3.value.m11, m3.value.m12,
                        m3.value.m20, m3.value.m21, m3.value.m22,
                    };
                case Matrix4ShaderProperty m4:
                    return new float[16]
                    {
                        m4.value.m00, m4.value.m01, m4.value.m02, m4.value.m03,
                        m4.value.m10, m4.value.m11, m4.value.m12, m4.value.m13,
                        m4.value.m20, m4.value.m21, m4.value.m22,m4.value.m23,
                        m4.value.m30, m4.value.m31, m4.value.m32,m4.value.m33,
                    };
                default: return null;
            }
        }
    }
}
