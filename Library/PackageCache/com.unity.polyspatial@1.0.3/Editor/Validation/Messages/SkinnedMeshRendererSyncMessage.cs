namespace UnityEditor.PolySpatial.Validation
{
    class SkinnedMeshRendererSyncMessage : ITypeMessage
    {
        const string k_MessageFormat = "The <b>{0}</b> profile(s) only support the <b>Mesh</b>, <b>Root Bone</b> and <b>Materials</b> properties.";

        public string Message { get; } = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames);
        public MessageType MessageType => MessageType.Info;
        public ITypeMessage.LinkData Link { get; }
    }
}
