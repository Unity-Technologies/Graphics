using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class SerializedGraphPresenter : AbstractGraphPresenter
    {
        protected SerializedGraphPresenter()
        {
            typeMapper[typeof(SerializableNode)] = typeof(GraphNodePresenter);
        }
    }
}
