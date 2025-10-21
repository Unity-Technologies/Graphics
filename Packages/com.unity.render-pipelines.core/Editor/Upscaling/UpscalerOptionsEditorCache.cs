#if ENABLE_UPSCALER_FRAMEWORK
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering
{
#nullable enable
    /// <summary>
    /// Manages the lifecycle of cached editors for UpscalerOptions ScriptableObjects.
    /// </summary>
    public class UpscalerOptionsEditorCache
    {
        private readonly Dictionary<ScriptableObject, Editor> m_EditorCache = new Dictionary<ScriptableObject, Editor>();

        /// <summary>
        /// Gets a cached editor for the given @options. If the editor is not
        /// found in the cache, a new editor is created and then cached.
        /// </summary>
        public Editor? GetOrCreateEditor(ScriptableObject options)
        {
            if (options == null)
                return null;

            if (!m_EditorCache.TryGetValue(options, out var editor) || editor == null)
            {
                editor = Editor.CreateEditor(options);
                m_EditorCache[options] = editor;
            }

            return editor;
        }

        /// <summary>
        /// Destroys all cached editor instances.
        /// Call this from the parent editor's OnDisable method.
        /// </summary>
        public void Cleanup()
        {
            foreach (var editor in m_EditorCache.Values)
            {
                if (editor != null)
                    Object.DestroyImmediate(editor);
            }
            m_EditorCache.Clear();
        }
    }

#nullable disable
}
#endif
#endif // ENABLE_UPSCALER_FRAMEWORK
