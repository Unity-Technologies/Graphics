using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Rendering;

namespace UnityEditor.MaterialGraph.Drawing
{
    public sealed class PreviewView : VisualElement
    {
        Texture m_Image;

        public Texture image
        {
            get { return m_Image; }
            set
            {
                if (value == m_Image)
                    return;
                m_Image = value;
                Dirty(ChangeType.Repaint);
            }
        }

        public override void DoRepaint()
        {
            EditorGUI.DrawPreviewTexture(contentRect, image);
        }
    }
}
