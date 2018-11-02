namespace UnityEditor.ShaderGraph
{
    public struct OutputPortDescriptor
    {
        public int id { get; set; }

        public string displayName { get; set; }

        public PortValueType type { get; set; }

        public override string ToString()
        {
            return $"id={id}, displayName={displayName}, type={type}";
        }
    }
}
