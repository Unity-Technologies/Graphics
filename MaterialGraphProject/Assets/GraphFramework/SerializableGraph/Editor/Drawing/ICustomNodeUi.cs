using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.Drawing
{
    public interface ICustomNodeUi
    {
        float GetNodeUiHeight(float width);
        GUIModificationType Render(Rect area);
        INode node { get; set; }
        float GetNodeWidth();
    }
}
