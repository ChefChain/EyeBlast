namespace Unity.PolySpatial.Internals
{
    // Helper class for the public assembly to have data from the private one
    internal static class PolySpatialBridge
    {
        public static bool? s_RuntimeEnabled = null;

        public static bool RuntimeEnabled
        {
            get
            {
                return s_RuntimeEnabled ?? false;
            }

            internal set
            {
                s_RuntimeEnabled = value;
            }
        }
    }
}
