using GraphViewIconBadge = UnityEditor.Experimental.GraphView.IconBadge;

namespace UnityEditor.ShaderGraph
{
    // Wrapper class for internal access to GraphView's IconBadge.
    // Allows for creation of badges which don't manually layout their label's dimensions. Works around
    // a GraphView bug.
    internal static class IconBadgeWrapper
    {
        public static GraphViewIconBadge CreateError(string message)
        {
            var result = GraphViewIconBadge.CreateError(message);
            result.m_ComputeTextBoundingBox = false;
            return result;
        }

        public static GraphViewIconBadge CreateComment(string message)
        {
            var result = GraphViewIconBadge.CreateComment(message);
            result.m_ComputeTextBoundingBox = false;
            return result;
        }
    }
}
