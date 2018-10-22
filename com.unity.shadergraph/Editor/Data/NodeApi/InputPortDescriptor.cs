namespace UnityEditor.ShaderGraph
{
    public struct InputPortDescriptor
    {
        public int id { get; set; }

        public string displayName { get; set; }

        public PortValue value { get; set; }
    }
}
