using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using System.IO;

using UnityObject = UnityEngine.Object;
using UnityEditorInternal;


public class VisualEffectAssetEditorStyles
{
    public static Texture2D errorIcon = EditorGUIUtility.LoadIcon("console.erroricon.sml");
    public static Texture2D warningIcon = EditorGUIUtility.LoadIcon("console.warnicon.sml");
}

[CustomEditor(typeof(VisualEffectAsset))]
public class VisualEffectAssetEditor : Editor
{
    void OnEnable()
    {
    }

    PreviewRenderUtility m_PreviewUtility;

    private PreviewRenderUtility previewUtility
    {
        get
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                m_PreviewUtility.camera.fieldOfView = 60.0f;
                m_PreviewUtility.camera.allowHDR = false;
                m_PreviewUtility.camera.allowMSAA = false;
                m_PreviewUtility.camera.farClipPlane = 10000.0f;
                m_PreviewUtility.ambientColor = new Color(.1f, .1f, .1f, 0);
                m_PreviewUtility.lights[0].intensity = 1.4f;
                m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
                m_PreviewUtility.lights[1].intensity = 1.4f;
                InitPreviewVisualEffect();

                m_FrameCount = 0;
                m_Distance = 10;
                m_Origin = Vector3.zero;
                m_Direction = Vector3.forward;
            }
            return m_PreviewUtility;
        }
    }


    void InitPreviewVisualEffect()
    {
        if (m_VisualEffectGO == null)
        {
            m_VisualEffectGO = new GameObject("VisualEffect (Preview)");
            m_VisualEffect = m_VisualEffectGO.AddComponent<VisualEffect>();
            m_PreviewUtility.AddManagedGO(m_VisualEffectGO);

            m_VisualEffectGO.transform.localPosition = Vector3.zero;
            m_VisualEffectGO.transform.localRotation = Quaternion.identity;
            m_VisualEffectGO.transform.localScale = Vector3.one;

            m_VisualEffect.visualEffectAsset = target as VisualEffectAsset;
            m_Bounds.size = Vector3.zero;
        }
    }

    GameObject m_VisualEffectGO;
    VisualEffect m_VisualEffect;
    Vector3 m_Direction;
    Vector3 m_Origin;
    float m_Distance;
    Bounds m_Bounds;

    int m_FrameCount = 0;

    const int kSafeFrame = 2;

    public override bool HasPreviewGUI()
    {
        return true;
    }

    void ComputeFarNear()
    {
        if (m_Bounds.size != Vector3.zero)
        {
            float maxBounds = Mathf.Max(m_Bounds.size.x, Mathf.Max(m_Bounds.size.y, m_Bounds.size.z));
            m_PreviewUtility.camera.farClipPlane = m_Distance + maxBounds * 1.1f;
            m_PreviewUtility.camera.nearClipPlane = Mathf.Max(0.0001f, (m_Distance - maxBounds) * 0.9f);
        }
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        bool isRepaint = (Event.current.type == EventType.Repaint);


        m_Direction = VFXPreviewGUI.Drag2D(m_Direction, r);

        if (m_FrameCount == kSafeFrame && m_VisualEffectGO != null) // wait to frame before asking the renderer bounds as it is a computed value.
        {
            Renderer renderer = m_VisualEffectGO.GetComponent<Renderer>();
            if (renderer != null)
            {
                var m_Bounds = renderer.bounds;
                float maxBounds = Mathf.Max(m_Bounds.size.x, Mathf.Max(m_Bounds.size.y, m_Bounds.size.z));
                m_Distance = Mathf.Max(0.01f, maxBounds * 0.5f);

                m_Origin = m_Bounds.center;
                ComputeFarNear();
            }
        }
        m_FrameCount++;
        if (Event.current.isScrollWheel)
        {
            m_Distance *= 1 + (Event.current.delta.y * .015f);
            ComputeFarNear();
        }

        if (isRepaint)
        {
            previewUtility.BeginPreview(r, background);

            Quaternion rot = Quaternion.Euler(-m_Direction.y, 0, 0) * Quaternion.Euler(0, -m_Direction.x, 0);
            m_PreviewUtility.camera.transform.position = m_Bounds.center + rot * new Vector3(0, 0, -m_Distance);
            m_PreviewUtility.camera.transform.localRotation = rot;


            previewUtility.Render();

            previewUtility.EndAndDrawPreview(r);

            // Ask for repaint so the effect is animated.
            Repaint();
        }
    }

    private void OnDisable()
    {
        if (m_PreviewUtility != null)
        {
            m_PreviewUtility.Cleanup();
        }
    }

    public override void OnInspectorGUI()
    {
        VisualEffectAsset asset = (VisualEffectAsset)target;


        bool enabled = GUI.enabled;
        GUI.enabled = true;
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open Editor"))
        {
            EditorWindow.GetWindow<VFXViewWindow>();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        VisualEffectResource resource = asset.GetResource();

        if (resource == null) return;


        UnityObject[] objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));

        foreach (var shader in objects)
        {
            if (shader is Shader || shader is ComputeShader)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(shader.name);
                if (GUILayout.Button("Reveal"))
                {
                    OpenTempFile(shader);
                }
                GUILayout.EndHorizontal();
                /*
                var errors = ShaderUtil.GetShaderErrors(shader as Shader);

                foreach (var error in errors)
                {
                    GUILayout.Label(new GUIContent(error.message, error.warning != 0 ? VisualEffectAssetEditorStyles.warningIcon : VisualEffectAssetEditorStyles.errorIcon, string.Format("{0} line:{1} shader:{2}", error.messageDetails, error.line, shaderSource.name)));
                }*/
            }
        }
        GUI.enabled = enabled;
    }

    void OpenTempFile(UnityObject shader)
    {
        string source = GetShaderSource(shader);

        if (!string.IsNullOrEmpty(source))
        {
            string path = AssetDatabase.GetAssetPath(target);
            string name = Path.GetFileNameWithoutExtension(path);
            string fileName = "Temp/" + name + "_" + shader.name.Replace("/", "_");
            File.WriteAllText(fileName, source);
            EditorUtility.RevealInFinder(fileName);
        }
    }

    string GetShaderSource(UnityObject shader)
    {
        VisualEffectAsset asset = (VisualEffectAsset)target;
        VisualEffectResource resource = asset.GetResource();

        int index = resource.GetShaderIndex(shader);
        if (index < 0 || index >= resource.shaderSources.Length)
            return "";

        return resource.shaderSources[index].source;
    }
}


static class VFXPreviewGUI
{
    static int sliderHash = "Slider".GetHashCode();
    static Rect s_ViewRect, s_Position;
    static Vector2 s_ScrollPos;

    internal static void BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
    {
        s_ScrollPos = scrollPosition;
        s_ViewRect = viewRect;
        s_Position = position;
        GUIClip.Push(position, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x - (viewRect.width - position.width) * .5f), Mathf.Round(-scrollPosition.y - viewRect.y - (viewRect.height - position.height) * .5f)), Vector2.zero, false);
    }

    internal class Styles
    {
        public static GUIStyle preButton;
        public static void Init()
        {
            preButton = "preButton";
        }
    }

    public static int CycleButton(int selected, GUIContent[] options)
    {
        Styles.Init();
        return EditorGUILayout.CycleButton(selected, options, Styles.preButton);
    }

    public static Vector2 EndScrollView()
    {
        GUIClip.Pop();

        Rect clipRect = s_Position, position = s_Position, viewRect = s_ViewRect;

        Vector2 scrollPosition = s_ScrollPos;
        switch (Event.current.type)
        {
            case EventType.Layout:
                GUIUtility.GetControlID(sliderHash, FocusType.Passive);
                GUIUtility.GetControlID(sliderHash, FocusType.Passive);
                break;
            case EventType.Used:
                break;
            default:
                bool needsVerticalScrollbar = ((int)viewRect.width > (int)clipRect.width);
                bool needsHorizontalScrollbar = ((int)viewRect.height > (int)clipRect.height);
                int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);

                if (needsHorizontalScrollbar)
                {
                    GUIStyle horizontalScrollbar = "PreHorizontalScrollbar";
                    GUIStyle horizontalScrollbarThumb = "PreHorizontalScrollbarThumb";
                    float offset = (viewRect.width - clipRect.width) * .5f;
                    scrollPosition.x = GUI.Slider(new Rect(position.x, position.yMax - horizontalScrollbar.fixedHeight, clipRect.width - (needsVerticalScrollbar ? horizontalScrollbar.fixedHeight : 0), horizontalScrollbar.fixedHeight),
                            scrollPosition.x, clipRect.width + offset, -offset, viewRect.width,
                            horizontalScrollbar, horizontalScrollbarThumb, true, id);
                }
                else
                {
                    // Get the same number of Control IDs so the ID generation for childrent don't depend on number of things above
                    scrollPosition.x = 0;
                }

                id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);

                if (needsVerticalScrollbar)
                {
                    GUIStyle verticalScrollbar = "PreVerticalScrollbar";
                    GUIStyle verticalScrollbarThumb = "PreVerticalScrollbarThumb";
                    float offset = (viewRect.height - clipRect.height) * .5f;
                    scrollPosition.y = GUI.Slider(new Rect(clipRect.xMax - verticalScrollbar.fixedWidth, clipRect.y, verticalScrollbar.fixedWidth, clipRect.height),
                            scrollPosition.y, clipRect.height + offset, -offset, viewRect.height,
                            verticalScrollbar, verticalScrollbarThumb, false, id);
                }
                else
                {
                    scrollPosition.y = 0;
                }
                break;
        }

        return scrollPosition;
    }

    public static Vector2 Drag2D(Vector2 scrollPosition, Rect position)
    {
        int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);
        Event evt = Event.current;
        switch (evt.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (position.Contains(evt.mousePosition) && position.width > 50)
                {
                    GUIUtility.hotControl = id;
                    evt.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    scrollPosition -= evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(position.width, position.height) * 140.0f;
                    scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90, 90);
                    evt.Use();
                    GUI.changed = true;
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                    GUIUtility.hotControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                break;
        }
        return scrollPosition;
    }
}
