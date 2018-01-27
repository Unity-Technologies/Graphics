using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;

    [CustomEditorForRenderPipeline(typeof(PlanarReflectionProbe), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class PlanarReflectionProbeEditor : Editor
    {
        static Dictionary<PlanarReflectionProbe, PlanarReflectionProbeUI> s_StateMap = new Dictionary<PlanarReflectionProbe, PlanarReflectionProbeUI>();
        const float k_PreviewHeight = 128;

        public static bool TryGetUIStateFor(PlanarReflectionProbe p, out PlanarReflectionProbeUI r)
        {
            return s_StateMap.TryGetValue(p, out r);
        }

        [DidReloadScripts]
        static void DidReloadScripts()
        {
            foreach (var probe in FindObjectsOfType<PlanarReflectionProbe>())
            {
                if (probe.enabled)
                    ReflectionSystem.RegisterProbe(probe);
            }
        }

        SerializedPlanarReflectionProbe m_SerializedAsset;
        PlanarReflectionProbeUI m_UIState = new PlanarReflectionProbeUI();
        PlanarReflectionProbeUI[] m_UIHandleState;
        PlanarReflectionProbe[] m_TypedTargets;

        List<Texture> m_PreviewedTextures = new List<Texture>();

        void OnEnable()
        {
            m_SerializedAsset = new SerializedPlanarReflectionProbe(serializedObject);
            m_UIState.Reset(m_SerializedAsset, Repaint);

            m_TypedTargets = new PlanarReflectionProbe[targets.Length];
            m_UIHandleState = new PlanarReflectionProbeUI[m_TypedTargets.Length];
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_TypedTargets[i] = (PlanarReflectionProbe)targets[i];
                m_UIHandleState[i] = new PlanarReflectionProbeUI();
                m_UIHandleState[i].Reset(m_SerializedAsset, null);

                s_StateMap[m_TypedTargets[i]] = m_UIHandleState[i];
            }
        }

        void OnDisable()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
                s_StateMap.Remove(m_TypedTargets[i]);
        }

        public override void OnInspectorGUI()
        {
            var s = m_UIState;
            var d = m_SerializedAsset;
            var o = this;

            s.Update();
            d.Update();

            PlanarReflectionProbeUI.Inspector.Draw(s, d, o);

            d.Apply();
        }

        void OnSceneGUI()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                m_UIHandleState[i].Update();
                m_UIHandleState[i].influenceVolume.showInfluenceHandles = m_UIState.influenceVolume.isSectionExpandedShape.target;
                m_UIHandleState[i].showCaptureHandles = m_UIState.isSectionExpandedCaptureSettings.target;
                PlanarReflectionProbeUI.DrawHandles(m_UIHandleState[i], m_TypedTargets[i], this);
            }

            SceneViewOverlay_Window(_.GetContent("Planar Probe"), OnOverlayGUI, -100, target);
        }

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            var previewSize = new Rect();
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                var p = m_TypedTargets[i];
                if (p.texture == null)
                    continue;

                var factor = k_PreviewHeight / p.texture.height;

                previewSize.x += p.texture.width * factor;
                previewSize.y = k_PreviewHeight;
            }

            // Get and reserve rect
            Rect cameraRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y);

            if (Event.current.type == EventType.Repaint)
            {
                var c = new Rect(cameraRect);
                for (var i = 0; i < m_TypedTargets.Length; i++)
                {
                    var p = m_TypedTargets[i];
                    if (p.texture == null)
                        continue;

                    var factor = k_PreviewHeight / p.texture.height;

                    c.width = p.texture.width * factor;
                    c.height = k_PreviewHeight;
                    Graphics.DrawTexture(c, p.texture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, CameraEditorUtils.GUITextureBlit2SRGBMaterial);

                    c.x += c.width;
                }
            }
        }

        public override bool HasPreviewGUI()
        {
            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                if (m_TypedTargets[i].texture != null)
                    return true;
            }
            return false;
        }

        public override GUIContent GetPreviewTitle()
        {
            return  _.GetContent("Planar Reflection");
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            m_PreviewedTextures.Clear();
            for (var i = 0; i < m_TypedTargets.Length; i++)
                m_PreviewedTextures.Add(m_TypedTargets[i].texture);

            var space = Vector2.one;
            var rowSize = Mathf.CeilToInt(Mathf.Sqrt(m_PreviewedTextures.Count));
            var size = r.size / rowSize - space * (rowSize - 1);

            for (var i = 0; i < m_TypedTargets.Length; i++)
            {
                var row = i / rowSize;
                var col = i % rowSize;
                var itemRect = new Rect(
                    r.x + size.x * row + ((row > 0) ? (row - 1) * space.x : 0),
                    r.y + size.y * col + ((col > 0) ? (col - 1) * space.y : 0),
                    size.x,
                    size.y);

                if (m_PreviewedTextures[i] != null)
                    EditorGUI.DrawPreviewTexture(itemRect, m_PreviewedTextures[i], CameraEditorUtils.GUITextureBlit2SRGBMaterial, ScaleMode.ScaleToFit, 0, 1);
                else
                    EditorGUI.LabelField(itemRect, _.GetContent("Not Available"));
            }
        }

        static Type k_SceneViewOverlay_WindowFunction = Type.GetType("UnityEditor.SceneViewOverlay+WindowFunction,UnityEditor");
        static Type k_SceneViewOverlay_WindowDisplayOption = Type.GetType("UnityEditor.SceneViewOverlay+WindowDisplayOption,UnityEditor");
        static MethodInfo k_SceneViewOverlay_Window = Type.GetType("UnityEditor.SceneViewOverlay,UnityEditor")
            .GetMethod(
                "Window",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                CallingConventions.Any,
                new[] { typeof(GUIContent), k_SceneViewOverlay_WindowFunction, typeof(int), typeof(Object), k_SceneViewOverlay_WindowDisplayOption },
                null);
        static void SceneViewOverlay_Window(GUIContent title, Action<Object, SceneView> sceneViewFunc, int order, Object target)
        {
            k_SceneViewOverlay_Window.Invoke(null, new[]
            {
                title, DelegateUtility.Cast(sceneViewFunc, k_SceneViewOverlay_WindowFunction),
                order,
                target,
                Enum.ToObject(k_SceneViewOverlay_WindowDisplayOption, 1)
            });
        }
    }
}
