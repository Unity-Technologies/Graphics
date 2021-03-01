using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System.Linq;
using UnityEditorInternal;
#endif
namespace UnityEngine.Rendering
{
    /// NOTE: Should be initialized by a Scriptable Render Pipeline implementation.
    /// <summary>
    /// Core rendering resources shared between pipelines.
    /// </summary>
    public sealed class CoreResources : ScriptableObject
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Runtime/ShaderLibrary/Blit.shader")]
            public Shader blitPS;

            [Reload("Runtime/ShaderLibrary/BlitColorAndDepth.shader")]
            public Shader blitColorAndDepthPS;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
        }

        internal ShaderResources shaders;
        internal TextureResources textures;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(CoreResources))]
    class CoreResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (UnityEditor.EditorPrefs.GetBool("DeveloperMode")
                && GUILayout.Button("Reload All"))
            {
                foreach (var field in typeof(CoreResources).GetFields())
                    field.SetValue(target, null);

                ResourceReloader.ReloadAllNullIn(target, CoreUtils.GetCorePath());
            }
        }
    }
#endif
}
