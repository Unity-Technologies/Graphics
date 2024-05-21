using System;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    // This file can't be in the editor assembly as we need to access it in runtime-editor-specific

#if UNITY_EDITOR
    static class HDRenderPipelinePreferences
    {
        public class MatCapModeEditorPreferences
        {
            static class Keys
            {
                internal const string matcapViewMixAlbedo = "HDRP.SceneView.MatcapMixAlbedo";
                internal const string matcapViewScale = "HDRP.SceneView.MatcapViewScale";
            }

            public Observable<bool> mixAlbedo = new(true);
            public Observable<float> viewScale = new(1.0f);

            public MatCapModeEditorPreferences()
            {
                mixAlbedo.value = EditorPrefs.GetBool(Keys.matcapViewMixAlbedo, true);
                mixAlbedo.onValueChanged += value => EditorPrefs.SetBool(Keys.matcapViewMixAlbedo, value);

                viewScale.value = EditorPrefs.GetFloat(Keys.matcapViewScale, 1.0f);
                viewScale.onValueChanged += value => EditorPrefs.SetFloat(Keys.matcapViewScale, value);
            }
        }

        private static Lazy<MatCapModeEditorPreferences> s_MatCapModeEditorPreferences = new(() => new MatCapModeEditorPreferences());
        public static MatCapModeEditorPreferences matCapMode => s_MatCapModeEditorPreferences.Value;
    }
#endif
}
