namespace UnityEditor.XR.VisionOS
{
    abstract class ARResource
    {
        protected abstract string extension { get; }

        public string name { get; protected set; }

        public string filename => $"{name}.{extension}";

        public abstract void Write(string pathToResourceGroup);
    }
}
