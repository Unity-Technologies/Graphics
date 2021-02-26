using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardSectionViewModel : ISGViewModel
    {
        public VisualElement parentView { get; set; }

        // Wipes all data in this view-model
        public void Reset()
        {
            parentView = null;
            name = String.Empty;
            isExpanded = false;
            requestModelChangeAction = null;
        }

        internal string name { get; set; }

        internal Guid associatedCategoryGuid { get; set; }

        internal bool isExpanded { get; set; }

        internal Action<IGraphDataAction> requestModelChangeAction { get; set; }
    }
}
