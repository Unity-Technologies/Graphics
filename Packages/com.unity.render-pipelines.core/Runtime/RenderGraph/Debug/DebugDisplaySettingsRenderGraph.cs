using System;
using System.Reflection;
using UnityEngine.Rendering.RenderGraphModule;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Render Graph-related Rendering Debugger settings.
    /// </summary>
    [CurrentPipelineHelpURL(pageName: "features/rendering-debugger-reference", pageHash: "render-graph")]
    class DebugDisplaySettingsRenderGraph : IDebugDisplaySettingsData
    {
        public DebugDisplaySettingsRenderGraph()
        {
            foreach (var graph in RenderGraph.GetRegisteredRenderGraphs())
            {
                graph.debugParams.Reset();
            }
        }

        [DisplayInfo(name = "Rendering", order = 10)]
        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            public SettingsPanel(DebugDisplaySettingsRenderGraph _)
            {
                var foldout = new DebugUI.Foldout()
                {
                    displayName = "Render Graph",
                    documentationUrl = typeof(DebugDisplaySettingsRenderGraph).GetCustomAttribute<HelpURLAttribute>()?.URL
                };
                AddWidget(foldout);

                bool usingRenderGraph = false;
                foreach (var graph in RenderGraph.GetRegisteredRenderGraphs())
                {
                    usingRenderGraph = true;
                    var list = graph.GetWidgetList();
                    foreach (var item in list)
                        foldout.children.Add(item);
                }

                if (!usingRenderGraph)
                {
                    foldout.children.Add(new DebugUI.MessageBox()
                    {
                        displayName =
                            "Warning: The current render pipeline does not have Render Graphs Registered",
                        style = DebugUI.MessageBox.Style.Warning
                    });
                }
            }
        }

        #region IDebugDisplaySettingsQuery

        /// <inheritdoc/>
        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }

        public bool AreAnySettingsActive
        {
            get
            {
                foreach (var graph in RenderGraph.GetRegisteredRenderGraphs())
                {
                    if (graph.areAnySettingsActive)
                        return true;
                }

                return false;
            }
        }

        #endregion
    }
}
