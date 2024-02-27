using System;
using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using Unity.PolySpatial;

namespace UnityEditor.ShaderGraph.MaterialX
{
    enum TransformMode
    {
        Position,
        Direction,
        Vector,
    }

    [Title("Utility", "PolySpatial Volume to World")]
    class PolySpatialVolumeToWorldNode : CodeFunctionNode
    {
        public PolySpatialVolumeToWorldNode()
        {
            name = "PolySpatial Volume to World";
        }

        [SerializeField]
        private TransformMode m_TransformMode = TransformMode.Position;

        [EnumControl("Transform")]
        public TransformMode transformMode
        {
            get { return m_TransformMode; }
            set
            {
                if (m_TransformMode == value)
                    return;

                m_TransformMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public override bool hasPreview => false;
        
        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);

            properties.AddShaderProperty(new Matrix4ShaderProperty()
            {
                overrideReferenceName = PolySpatialShaderProperties.VolumeToWorld,
                generatePropertyBlock = false,
            });
        }

        internal string GetFunctionBody()
        {
            return m_TransformMode switch
            {
                TransformMode.Position =>
                    $"Out = mul({PolySpatialShaderProperties.VolumeToWorld}, float4(In, 1)).xyz;",
                TransformMode.Direction => 
                    $"Out = normalize(mul({PolySpatialShaderProperties.VolumeToWorld}, float4(In, 0)).xyz);",
                TransformMode.Vector => $"Out = mul({PolySpatialShaderProperties.VolumeToWorld}, float4(In, 0)).xyz;",
                _ => throw new NotSupportedException($"Unsupported transform mode {m_TransformMode}"),
            };
        }
        
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("PolySpatial_VolumeToWorld", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        string PolySpatial_VolumeToWorld(
            [Slot(0, Binding.None)] Vector3 In,
            [Slot(1, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;

            return
$@"
{{
    {GetFunctionBody()}
}}";
        }
    }
}