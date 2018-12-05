using UnityEditor.Graphing.Util;

namespace UnityEditor.ShaderGraph
{
    class TestMaterialGraph : AbstractMaterialGraph
    {
        public TestMaterialGraph()
        {
            messageManager = new MessageManager();
        }
    }
}
