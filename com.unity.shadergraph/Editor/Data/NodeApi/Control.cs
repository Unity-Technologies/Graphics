namespace UnityEditor.ShaderGraph
{
    struct ControlDescriptor
    {
        public Identifier nodeId { get; set; }
        public string label { get; set; }
        public float value { get; set; }
    }

    public struct ControlRef
    {
        internal Identifier nodeId { get; set; }
        internal short controlId { get; set; }
        internal short controlVersion { get; set; }

        // TODO: Obviously something different
        public bool isValid => true;
    }
}
