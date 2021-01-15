namespace UnityEditor.Rendering
{
    /// <summary>
    /// Interface for editors using Additional Properties with EditorPrefBoolFlags
    /// </summary>
    public interface IAdditionalPropertiesBoolFlagsHandler
    {
        /// <summary>
        /// Implement to show or hide all additional properties for this editor.
        /// </summary>
        /// <param name="value"></param>
        void SetAddditionalPropertiesVisibility(bool value);
    }
}
