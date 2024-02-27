namespace UnityEditor.PolySpatial.Validation
{
    class LightSyncMessage : ITypeMessage
    {
        const string k_MessageFormat = "The <b>{0}</b> profile(s) only support the <b>Type</b>, <b>Color</b> and <b>Intensity</b> properties.";

        public string Message { get; } = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames);
        public MessageType MessageType => MessageType.Info;
        public ITypeMessage.LinkData Link { get; }
    }
}
