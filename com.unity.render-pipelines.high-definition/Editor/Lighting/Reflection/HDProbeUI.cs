using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    static partial class HDProbeUI
    {
        public enum Expandable
        {
            Influence = 1 << 0,
            Capture = 1 << 1,
            Projection = 1 << 2,
            Custom = 1 << 3,
        }
        internal readonly static ExpandedState<Expandable, HDProbe> k_ExpandedState = new ExpandedState<Expandable, HDProbe>(Expandable.Projection | Expandable.Capture | Expandable.Influence, "HDRP");

        [System.Flags]
        public enum AdditionalProperties
        {
            Capture = 1 << 0,
        }
        internal readonly static AdditionalPropertiesState<AdditionalProperties, HDProbe> k_AdditionalPropertiesState = new AdditionalPropertiesState<AdditionalProperties, HDProbe>(0, "HDRP");

        internal static void RegisterEditor<TProvider, TSerialized>(HDProbeEditor<TProvider, TSerialized> editor)
            where TProvider : struct, HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
            where TSerialized : SerializedHDProbe
        {
            k_AdditionalPropertiesState.RegisterEditor(editor);
        }

        internal static void UnregisterEditor<TProvider, TSerialized>(HDProbeEditor<TProvider, TSerialized> editor)
            where TProvider : struct, HDProbeUI.IProbeUISettingsProvider, InfluenceVolumeUI.IInfluenceUISettingsProvider
            where TSerialized : SerializedHDProbe
        {
            k_AdditionalPropertiesState.UnregisterEditor(editor);
        }

        [SetAdditionalPropertiesVisibility]
        internal static void SetAdditionalPropertiesVisibility(bool value)
        {
            if (value)
                k_AdditionalPropertiesState.ShowAll();
            else
                k_AdditionalPropertiesState.HideAll();
        }
    }
}
