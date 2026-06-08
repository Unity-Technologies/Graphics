#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.Universal
{
    static class ShadowCaster2DAssetPipeline
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnSceneSaving(Scene scene, string path)
        {
            // Only the save path needs SetDirty: it ensures Unity persists the updated mesh data
            // to the .unity file. Play-mode and PostProcessScene paths do not write to disk.
            BakeScene(scene, markDirty: true);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.IsValid() && scene.isLoaded)
                        BakeScene(scene, markDirty: false);
                }
            }
        }

        [PostProcessScene]
        static void OnPostProcessScene()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                    BakeScene(scene, markDirty: false);
            }
        }

        // Reused per-call to avoid per-root allocations from GetComponentsInChildren.
        // Safe as a static field: editor callbacks fire on the main thread and BakeCaster
        // does not trigger reentrant scene baking.
        static readonly List<ShadowCaster2D> s_CastersBuffer = new List<ShadowCaster2D>();

        static void BakeScene(Scene scene, bool markDirty)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                s_CastersBuffer.Clear();
                roots[r].GetComponentsInChildren(true, s_CastersBuffer);
                for (int c = 0; c < s_CastersBuffer.Count; c++)
                    BakeCaster(s_CastersBuffer[c], markDirty);
            }
            s_CastersBuffer.Clear();
        }

        static readonly Bounds s_InfiniteBounds = new Bounds(Vector3.zero, new Vector3(1e9f, 1e9f, 1e9f));

        static void BakeCaster(ShadowCaster2D caster, bool markDirty)
        {
            if (caster == null)
                return;

            if (caster.shadowCastingSource != ShadowCaster2D.ShadowCastingSources.ShapeProvider)
            {
                // ShapeEditor mode bakes its mesh inside SetShadowShape via EnsureMeshInitialized.
                caster.EnsureMeshInitialized();
                if (markDirty)
                    EditorUtility.SetDirty(caster);
                return;
            }

            var source = caster.shadowShape2DComponent;
            var provider = caster.shadowShape2DProvider;
            if (source == null || provider == null)
                return;

            ForceProviderSourceReady(source);
            caster.EnsureMeshInitialized();

            // For Collider providers (and others that defer to render time), OnInitialized
            // only resets internal state — it doesn't populate the mesh. The actual SetShape
            // happens via OnBeforeRender → CalculateShadows. Drive it here with infinite
            // culling bounds so all shapes are included.
            if (caster.m_ShadowMesh != null)
            {
                // Asset baking runs outside any camera context, so pass null Camera.
                try { provider.OnBeforeRender(null, source, s_InfiniteBounds, caster.m_ShadowMesh); }
                catch { /* defensive — never let one broken caster abort the whole bake */ }
            }

            if (markDirty)
                EditorUtility.SetDirty(caster);
        }

        static void ForceProviderSourceReady(Component source)
        {
            if (source == null)
                return;

#if USING_PHYSICS2D_MODULE
            if (source is CompositeCollider2D composite)
            {
                try { composite.GenerateGeometry(); } catch { }
            }
#endif
        }
    }
}
#endif
