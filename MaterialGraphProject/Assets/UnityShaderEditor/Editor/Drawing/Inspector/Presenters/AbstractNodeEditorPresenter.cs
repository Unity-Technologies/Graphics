using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public abstract class AbstractNodeEditorPresenter : ScriptableObject
    {
        public abstract void Initialize(INode node);
    }
}
