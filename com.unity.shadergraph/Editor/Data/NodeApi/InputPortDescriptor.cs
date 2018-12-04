namespace UnityEditor.ShaderGraph
{
    public struct InputPortDescriptor
    {
        public string id { get; set; }

        public string displayName { get; set; }

        public PortValue value { get; set; }

        public override string ToString()
        {
            return $"id={id}, displayName={displayName}, {value}";
        }
    }
}
