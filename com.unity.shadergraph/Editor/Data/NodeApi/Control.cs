namespace UnityEditor.ShaderGraph
{
    struct ControlDescriptor
    {
        public string label { get; set; }
        public float value { get; set; }
    }

    public struct ControlRef
    {
        internal int id { get; set; }

        public bool isValid => id > 0;
    }
}
