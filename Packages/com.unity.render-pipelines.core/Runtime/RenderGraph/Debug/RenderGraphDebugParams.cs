using System.Collections.Generic;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.RenderGraphModule
{
    class RenderGraphDebugParams : IDebugDisplaySettingsQuery
    {
        DebugUI.Widget[] m_DebugItems;
        DebugUI.Panel m_DebugPanel;

        public bool clearRenderTargetsAtCreation;
        public bool clearRenderTargetsAtRelease;
        public bool disablePassCulling;
        public bool disablePassMerging;
        public bool immediateMode;
        public bool logFrameInformation;
        public bool logResources;

        public bool enableLogging => logFrameInformation || logResources;

        public void ResetLogging()
        {
            logFrameInformation = false;
            logResources = false;
        }

        internal void Reset()
        {
            clearRenderTargetsAtCreation = false;
            clearRenderTargetsAtRelease = false;
            disablePassCulling = false;
            disablePassMerging = false;
            immediateMode = false;

            ResetLogging();
        }

        private static class Strings
        {
            public static readonly NameAndTooltip ClearRenderTargetsAtCreation = new() { name = "Clear Render Targets At Creation", tooltip = "Enable to clear all render textures before any rendergraph passes to check if some clears are missing." };
            public static readonly NameAndTooltip ClearRenderTargetsAtFree = new() { name = "Clear Render Targets When Freed", tooltip = "Enable to clear all render textures when textures are freed by the graph to detect use after free of textures." };
            public static readonly NameAndTooltip DisablePassCulling = new() { name = "Disable Pass Culling", tooltip = "Enable to temporarily disable culling to assess if a pass is culled." };
            public static readonly NameAndTooltip DisablePassMerging = new() { name = "Disable Pass Merging", tooltip = "Enable to temporarily disable pass merging to diagnose issues or analyze performance." };
            public static readonly NameAndTooltip ImmediateMode = new() { name = "Immediate Mode", tooltip = "Enable to force render graph to execute all passes in the order you registered them." };
            public static readonly NameAndTooltip EnableLogging = new() { name = "Enable Logging", tooltip = "Enable to allow HDRP to capture information in the log." };
            public static readonly NameAndTooltip LogFrameInformation = new() { name = "Log Frame Information", tooltip = "Enable to log information output from each frame." };
            public static readonly NameAndTooltip LogResources = new() { name = "Log Resources", tooltip = "Enable to log the current render graph's global resource usage." };
        }

        internal List<DebugUI.Widget> GetWidgetList(string name)
        {
            var list = new List<DebugUI.Widget>
            {
                new DebugUI.Container
                {
                    displayName = $"{name} Render Graph",
                    children =
                    {
                        new DebugUI.BoolField
                        {
                            nameAndTooltip = Strings.ClearRenderTargetsAtCreation,
                            getter = () => clearRenderTargetsAtCreation,
                            setter = value => clearRenderTargetsAtCreation = value
                        },
                        new DebugUI.BoolField
                        {
                            nameAndTooltip = Strings.ClearRenderTargetsAtFree,
                            getter = () => clearRenderTargetsAtRelease,
                            setter = value => clearRenderTargetsAtRelease = value
                        },
                        // We cannot expose this option as it will change the active render target and the debug menu won't know where to render itself anymore.
                        //    list.Add(new DebugUI.BoolField { displayName = "Clear Render Targets at release", getter = () => clearRenderTargetsAtRelease, setter = value => clearRenderTargetsAtRelease = value });
                        new DebugUI.BoolField
                        {
                            nameAndTooltip = Strings.DisablePassCulling,
                            getter = () => disablePassCulling,
                            setter = value => disablePassCulling = value
                        },
                        new DebugUI.BoolField
                        {
                            nameAndTooltip = Strings.DisablePassMerging,
                            getter = () => disablePassMerging,
                            setter = value => disablePassMerging = value,
                            isHiddenCallback = () => !RenderGraph.hasAnyRenderGraphWithNativeRenderPassesEnabled
                        },
                        new DebugUI.BoolField
                        {
                            nameAndTooltip = Strings.ImmediateMode,
                            getter = () => immediateMode,
                            setter = value => immediateMode = value,
                            // [UUM-64948] Temporarily disable for URP while we implement support for Immediate Mode in the RenderGraph
                            isHiddenCallback = () => !IsImmediateModeSupported()
                        },
                        new DebugUI.Button
                        {
                            nameAndTooltip = Strings.LogFrameInformation,
                            action = () =>
                            {
                                logFrameInformation = true;
#if UNITY_EDITOR
                                UnityEditor.SceneView.RepaintAll();
#endif
                            }
                        },
                        new DebugUI.Button
                        {
                            nameAndTooltip = Strings.LogResources,
                            action = () =>
                            {
                                logResources = true;
#if UNITY_EDITOR
                                UnityEditor.SceneView.RepaintAll();
#endif
                            }
                        }
                    }
                }
            };

            return list;
        }

        private bool IsImmediateModeSupported()
        {
            return GraphicsSettings.currentRenderPipeline is IRenderGraphEnabledRenderPipeline rgPipeline &&
                   rgPipeline.isImmediateModeSupported;
        }

        public void RegisterDebug(string name, DebugUI.Panel debugPanel = null)
        {
            var list = GetWidgetList(name);
            m_DebugItems = list.ToArray();
            m_DebugPanel = debugPanel != null ? debugPanel : DebugManager.instance.GetPanel(name.Length == 0 ? "Rendering" : name, true);

            var foldout = new DebugUI.Foldout() { displayName = name, };
            foldout.children.Add(m_DebugItems);
            m_DebugPanel.children.Add(foldout);
        }

        public void UnRegisterDebug(string name)
        {
            //DebugManager.instance.RemovePanel(name.Length == 0 ? "Render Graph" : name);
            if ( m_DebugPanel != null ) m_DebugPanel.children.Remove(m_DebugItems);
            m_DebugPanel = null;
            m_DebugItems = null;
        }

        public bool AreAnySettingsActive
        {
            get
            {
                return clearRenderTargetsAtCreation ||
                       clearRenderTargetsAtRelease ||
                       disablePassCulling ||
                       disablePassMerging ||
                       immediateMode ||
                       enableLogging;
            }
        }
    }
}
