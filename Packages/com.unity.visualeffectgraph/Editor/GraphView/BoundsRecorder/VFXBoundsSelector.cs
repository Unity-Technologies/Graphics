using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXBoundsSelector : VisualElement, ISelection
    {
        [Obsolete("VFXBoundsSelectorFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        class VFXBoundsSelectorFactory : UxmlFactory<VFXBoundsSelector>
        { }

        public VFXBoundsSelector()
        {
            selection = new List<ISelectable>();
        }

        public void AddToSelection(ISelectable selectable)
        {
            if (selectable is VFXBoundsRecorderField boundsField)
            {
                if (selection.Contains(boundsField))
                {
                    return;
                }
                selection.Add(boundsField);
                boundsField.OnSelected();
            }
        }

        public void RemoveFromSelection(ISelectable selectable)
        {
            if (selectable is VFXBoundsRecorderField boundsField)
            {
                if (selection.Contains(boundsField))
                {
                    selection.Remove(boundsField);
                    boundsField.OnUnselected();
                }
            }
        }

        public void ClearSelection()
        {
            foreach (var selectable in selection)
            {
                if (selectable is VFXBoundsRecorderField boundsField)
                {
                    boundsField.OnUnselected();
                }
            }
            selection.Clear();
        }

        public List<ISelectable> selection { get; }
    }
}
