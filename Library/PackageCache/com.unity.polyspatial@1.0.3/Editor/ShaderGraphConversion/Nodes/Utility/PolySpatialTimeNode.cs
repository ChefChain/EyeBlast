using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.MaterialX
{
    // Refer to implementation of TimeNode:
    // https://github.cds.internal.unity3d.com/unity/unity/blob/93a364f095f55c0e7616dc8d1638d6c6c37b5ad5/Packages/com.unity.shadergraph/Editor/Data/Nodes/Input/Basic/TimeNode.cs
    [Title("Utility", "PolySpatial Time")]
    class PolySpatialTimeNode : AbstractMaterialNode, IMayRequireTime
    {
        public PolySpatialTimeNode()
        {
            name = "PolySpatial Time";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(0, "Out", "Out", SlotType.Output, 0));
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return "IN.TimeParameters.x";
        }

        public bool RequiresTime()
        {
            return true;
        }
    }
}