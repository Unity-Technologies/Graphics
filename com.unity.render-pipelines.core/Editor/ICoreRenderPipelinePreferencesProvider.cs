using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Interface to extend to provide UI
    /// </summary>
    public interface ICoreRenderPipelinePreferencesProvider
    {
        List<string> keywords { get; }
        GUIContent header { get; }
        int height { get; }
        void PreferenceGUI();
    }
}
