namespace UnityEditor.PolySpatial.Validation
{
    class ImageSyncMessage : ITypeMessage
    {
        const string k_MessageFormat = "The <b>{0}</b> profile(s) only support the <b>Source Image</b> and <b>Color</b> properties.";

        public string Message { get; } = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames);
        public MessageType MessageType => MessageType.Info;
        public ITypeMessage.LinkData Link { get; }
    }
}
