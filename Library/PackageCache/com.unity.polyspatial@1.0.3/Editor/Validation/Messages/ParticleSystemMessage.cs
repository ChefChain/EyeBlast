namespace UnityEditor.PolySpatial.Validation
{
    class ParticleSystemMessage : ITypeMessage
    {
        const string k_MessageFormat = "The <b>Bake To Mesh</b> PolySpatial particle mode setting can impact performance.The <b>{0}</b> profile(s) do not fully " +
                                       "support all <b>Particle System</b> properties." +
            "For more information, see: ";
        public string Message { get; } = string.Format(k_MessageFormat, PolySpatialSceneValidator.CachedCapabilityProfileNames);
        public MessageType MessageType => MessageType.Info;

        public ITypeMessage.LinkData Link { get; } = new ITypeMessage.LinkData("Documentation",
            "https://docs.unity3d.com/Packages/com.unity.polyspatial.visionos@latest/index.html?subfolder=/manual/SupportedFeatures.html%23particle-systems");
    }
}
