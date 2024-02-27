namespace UnityEditor.PolySpatial.Validation
{
    class MeshRendererSyncMessage : ITypeMessage
    {
        const string k_MessageFormat = "The <b>{0}</b> profile(s) only support the <b>Materials</b> property and the <b>Mesh Filter's Mesh</b> attached to this same Game Object.";

        public string Message { get; } = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames);
        public MessageType MessageType => MessageType.Info;
        public ITypeMessage.LinkData Link { get; }
    }
}
