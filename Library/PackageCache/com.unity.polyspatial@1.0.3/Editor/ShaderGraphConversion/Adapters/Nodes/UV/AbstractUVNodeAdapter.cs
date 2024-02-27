namespace UnityEditor.ShaderGraph.MaterialX
{
    internal abstract class AbstractUVNodeAdapter<T> : ANodeAdapter<T>
        where T : AbstractMaterialNode
    {
        public override string SupportDetails(AbstractMaterialNode node)
        {
            return QuickNode.GetUVSupportDetails((UVMaterialSlot)NodeUtils.GetSlotByName(node, "UV"));
        }
    }
}