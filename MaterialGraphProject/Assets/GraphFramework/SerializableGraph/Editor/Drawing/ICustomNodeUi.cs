using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public interface ICustomNodeUi
    {
        float GetNodeUiHeight(float width);
        GUIModificationType Render(Rect area);
        void SetNode(INode node);
        float GetNodeWidth();
    }
}
