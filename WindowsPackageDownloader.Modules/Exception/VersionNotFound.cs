namespace WindowsPackageDownloader.Modules.Exception
{
    [System.Serializable]
    public class VersionNotFoundException : System.Exception
    {
        public VersionNotFoundException() { }
        public VersionNotFoundException(string message) : base(message) { }
        public VersionNotFoundException(string message, System.Exception inner) : base(message, inner) { }
        protected VersionNotFoundException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}