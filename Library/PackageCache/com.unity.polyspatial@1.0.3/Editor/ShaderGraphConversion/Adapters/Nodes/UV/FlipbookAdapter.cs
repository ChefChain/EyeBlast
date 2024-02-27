namespace UnityEditor.ShaderGraph.MaterialX
{
    class FlipbookAdapter : AbstractUVNodeAdapter<FlipbookNode>
    {
        public override void BuildInstance(
            AbstractMaterialNode node, MtlxGraphData graph, ExternalEdgeMap externals, SubGraphContext sgContext)
        {
            if (node is not FlipbookNode flipbookNode)
                return;

            // Reference implementation:
            // https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Flipbook-Node.html
            var tileYExpr = flipbookNode.invertY.isOn ? "(Height - 1) - tileFloor" : "tileFloor";
            var tileXExpr = flipbookNode.invertX.isOn ?
                "(Width - 1) - (wrappedTile - Width * tileFloor)" : "wrappedTile - Width * tileFloor";
            QuickNode.CompoundOp(
                node, graph, externals, sgContext, "Flipbook", $@"
float wrappedTile = fmod(floor(Tile), Width * Height);
float2 tileCount = float2(1.0, 1.0) / float2(Width, Height);
float tileFloor = floor(wrappedTile * tileCount.x);
float tileY = abs({tileYExpr});
float tileX = abs({tileXExpr});
Out = (UV + float2(tileX, tileY)) * tileCount;");
        }
    }
}