using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public partial class RenderGraphViewer
    {
        internal class PassTitleLabel : Label
        {
            public PassTitleLabel(string text) : base(text) { }

            public override bool ContainsPoint(Vector2 localPoint)
            {
                return localPoint.x >= 0 && localPoint.x <= resolvedStyle.width &&
                       localPoint.y >= 0 && localPoint.y <= resolvedStyle.height;
            }
        }
    }
}
