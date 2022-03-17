using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookMainToolbar : MainToolbar
    {
        public MathBookMainToolbar(BaseGraphTool graphTool, GraphView graphView)
            : base(graphTool, graphView)
        {
            m_SaveAllButton.clickable.clicked += OnSaveAllButton;
            m_SaveAllButton.tooltip = "Save All and Reload Assets";
        }

        static void OnSaveAllButton()
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
