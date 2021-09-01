namespace UnityEditor.ShaderGraph.GraphDelta
{
    public static class GraphUtil
    {
        public static IGraphHandler CreateGraph()
        {
            return new GraphDelta();
        }

        public static IGraphHandler OpenGraph(string assetPath)
        {
            throw new System.NotImplementedException();
        }

        public static void DestroyGraph(string assetPath)
        {
            throw new System.NotImplementedException();
        }

        public static bool SaveGraph(IGraphHandler graph, string assetPath, bool overwrite)
        {
            throw new System.NotImplementedException();
        }

        public static bool GraphExists(string assetPath)
        {
            throw new System.NotImplementedException();
        }
    }
}
