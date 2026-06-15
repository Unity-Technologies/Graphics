using System;
using System.Collections.Generic;
using Unity.RenderPipelines.Core.Runtime.Shared;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// URP Rendering Debugger Display Stats.
    /// </summary>
    class UniversalRenderPipelineDebugDisplayStats : DebugDisplayStats
    {
        readonly List<ProfilingSampler> m_URPSamplers = GetProfilingSamplersToDisplay(typeof(URPProfilingSamplers));

        /// <inheritdoc/>
        public override void EnableProfilingRecorders()
        {
            AddAndEnableProfilingSamplers(m_URPSamplers);
            base.EnableProfilingRecorders();
        }

        /// <inheritdoc/>
        public override void RegisterDebugUI(List<DebugUI.Widget> list)
        {
#if UNITY_ANDROID || UNITY_IPHONE || UNITY_TVOS
            list.Add(new DebugUI.MessageBox
            {
                displayName = "Warning: GPU timings may not be accurate on mobile devices that have tile-based architectures.",
                style = DebugUI.MessageBox.Style.Warning,
                flags = DebugUI.Flags.RuntimeOnly
            });
#endif

           base.RegisterDebugUI(list);

           var detailedStats = DebugUIUtilities.GetFoldoutByName("Detailed Stats", list);
           var detailedFoldout = detailedStats != null ? DebugUIUtilities.GetFoldoutByName("Profiling Scopes", detailedStats.children) : null;
           detailedFoldout?.children.InsertRange(0,  BuildProfilingSamplerWidgetList(m_URPSamplers));
        }
    }
}
