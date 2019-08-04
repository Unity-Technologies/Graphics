namespace UnityEditor.ShaderGraph
{
    class GraphContext
    {
        public readonly string graphInputStructName;
        public readonly bool conditional;

        public GraphContext(string inputStructName, bool conditional = false)
        {
            graphInputStructName = inputStructName;
            this.conditional = conditional;
        }
    }
}
