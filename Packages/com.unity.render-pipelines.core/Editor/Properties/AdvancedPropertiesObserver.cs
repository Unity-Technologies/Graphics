using UnityEditorInternal;

namespace UnityEditor.Rendering
{
    class AdvancedPropertiesObserver
    {
        [InitializeOnLoadMethod]
        static void SubscribeToAdvancedPropertiesChanges()
        {
            AdvancedProperties.advancedPreferenceChanged += OnShowAdvancedPropertiesChanged;
        }

        static void OnShowAdvancedPropertiesChanged(bool newValue)
        {
            if (newValue)
            {
                AdvancedProperties.ResetHighlight();
                EditorApplication.update += RepaintUntilAnimFinish;
            }
        }

        static void RepaintUntilAnimFinish()
        {
            if (AdvancedProperties.IsHighlightActive())
                InternalEditorUtility.RepaintAllViews();
            else
                EditorApplication.update -= RepaintUntilAnimFinish;
        }
    }
}
