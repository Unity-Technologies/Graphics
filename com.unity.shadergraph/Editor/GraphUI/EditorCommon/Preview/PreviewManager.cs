using System;
using Editor.GraphUI.Utilities;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEditor.ShaderGraph.GraphUI.DataModel;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview
{

    using PreviewRenderMode = HeadlessPreviewManager.PreviewRenderMode;

    public class PreviewManager
    {
        private HeadlessPreviewManager m_PreviewHandlerInstance;

        public PreviewManager()
        {
            m_PreviewHandlerInstance = new HeadlessPreviewManager();
        }

        public void OnPreviewExpansionChanged(string nodeName, bool newExpansionState) { }

        public void OnPreviewModeChanged(string nodeName, PreviewRenderMode newPreviewMode) { }

        public void OnNodeFlowChanged(string nodeName) { }

        public void OnNodeAdded(string nodeName) { }

        public void OnGlobalPropertyChanged(string propertyName, object newValue) { }

        public void OnLocalPropertyChanged(string nodeName, string propertyName, object newValue) { }
    }
}
