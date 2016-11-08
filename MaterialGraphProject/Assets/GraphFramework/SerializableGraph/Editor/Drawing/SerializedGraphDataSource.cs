using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class SerializedGraphDataSource : AbstractGraphDataSource
    {
        protected SerializedGraphDataSource()
        {}

        protected override void AddTypeMappings()
        {
            AddTypeMapping(typeof(SerializableNode), typeof(NodeDrawData));
        }
    }
}
