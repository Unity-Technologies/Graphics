using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public class SerializedGraphPresenter : AbstractGraphPresenter
    {
        protected SerializedGraphPresenter()
        {
            dataMapper[typeof(SerializableNode)] = typeof(GraphNodePresenter);
        }
    }
}
