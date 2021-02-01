using System;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardSectionViewModel : ISGViewModel
    {
        public VisualElement ParentView { get; set; }

        // Wipes all data in this view-model
        public void Reset()
        {

        }

        // Title of the section
        internal string Name { get; set; }

        internal bool IsExpanded { get; set; }

        internal Action<IGraphDataAction> RequestModelChangeAction { get; set; }
    }
}
