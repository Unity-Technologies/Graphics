using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class HeaderDrawData : NodeDrawData
    {
        protected HeaderDrawData()
        {}

        public INode node { get; private set; }

        public void Initialize(INode inNode)
        {
            node = inNode;
            name = inNode.name + " Header";
        }
    }
}