namespace UnityEditor.ShaderGraph
{
    class GraphContext
    {
        public readonly string graphInputStructName;
        public readonly int splatCount;
        public readonly bool conditional;

        public GraphContext(string inputStructName, int splatCount = 0, bool conditional = false)
        {
            graphInputStructName = inputStructName;
            this.splatCount = splatCount;
            this.conditional = conditional;
        }
    }
}
