using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

#if ENABLE_VIRTUALTEXTURES
namespace UnityEngine.Rendering.HighDefinition
{
    internal class DebugDisplayVirtualTexturing : IDebugDisplaySettingsData
    {
        internal class Settings 
        {
            public bool debugDisableResolving = false;
        }

        [DisplayInfo(name = "Virtual Texturing", order = 5)]
        private class Panel : DebugDisplaySettingsPanel
        {
            public Panel(Settings data)
            {
                AddWidget(new DebugUI.Container()
                {
                    displayName = "Virtual Texturing",
                    children =
                    {
                        new DebugUI.BoolField
                        {
                            displayName = "Debug disable Feedback Streaming",
                            getter = () => data.debugDisableResolving,
                            setter = value => data.debugDisableResolving = value 
                        },
                        new DebugUI.Value()
                        {
                            displayName  = "Textures with Preloaded Mips",
                            getter = () => VirtualTexturing.Debugging.mipPreloadedTextureCount
                        }
                    }
                });
            }
        }

        public Settings data = new Settings();

        bool IDebugDisplaySettingsQuery.AreAnySettingsActive => true;
        bool IDebugDisplaySettingsQuery.IsPostProcessingAllowed => true;
        bool IDebugDisplaySettingsQuery.IsLightingActive => true;
        bool IDebugDisplaySettingsQuery.TryGetScreenClearColor(ref Color color) => false;

        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new Panel(this.data);
        }
    }
}
#endif
