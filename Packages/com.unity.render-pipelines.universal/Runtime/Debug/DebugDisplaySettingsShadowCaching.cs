#if URP_SHADOW_CACHING

using System;
using NameAndTooltip = UnityEngine.Rendering.DebugUI.Widget.NameAndTooltip;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class DebugDisplaySettingsShadowCaching : IDebugDisplaySettingsData, ISerializedDebugDisplaySettings
    {
        public ShadowCachingDebugState forceState { get; set; }

        internal static class Strings
        {
            public static readonly NameAndTooltip ForceState = new() { name = "Force State", tooltip = "Use the drop-down to paint all surfaces within the main light's shadow distance with the color of one cache state." };
        }

        internal static class WidgetFactory
        {
            internal static DebugUI.Widget CreateForceState(DebugDisplaySettingsShadowCaching data) => new DebugUI.EnumField
            {
                nameAndTooltip = Strings.ForceState,
                autoEnum = typeof(ShadowCachingDebugState),
                getter = () => (int)data.forceState,
                setter = (value) => data.forceState = (ShadowCachingDebugState)value,
                getIndex = () => (int)data.forceState,
                setIndex = (value) => data.forceState = (ShadowCachingDebugState)value
            };
        }

        [DisplayInfo(name = "Shadow Caching", order = 5)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsShadowCaching>
        {
            public SettingsPanel(DebugDisplaySettingsShadowCaching data)
                : base(data)
            {
                AddWidget(new DebugUI.Foldout
                {
                    displayName = "Advanced",
                    opened = true,
                    children =
                    {
                        WidgetFactory.CreateForceState(data)
                    }
                });
            }
        }

        public bool AreAnySettingsActive => forceState != ShadowCachingDebugState.None;

        IDebugDisplaySettingsPanelDisposable IDebugDisplaySettingsData.CreatePanel()
        {
            return new SettingsPanel(this);
        }
    }
}

#endif
