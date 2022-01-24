using System;

namespace UnityEngine.Rendering
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public class RenderingDebuggerPanelAttribute : Attribute
    {
        public string panelName { get; set; }
        public string uiDocumentPath { get; set; } // TODO: Split into a different attribute

        public RenderingDebuggerPanelAttribute(string panelName, string uiDocumentPath)
        {
            this.panelName = panelName;
            this.uiDocumentPath = uiDocumentPath;
        }
    }
}
