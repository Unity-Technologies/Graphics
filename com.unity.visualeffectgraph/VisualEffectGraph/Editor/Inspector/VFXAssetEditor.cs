using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;
using UnityEditor.VFX;
using UnityEditor.VFX.UI;
using System.IO;
using UnityEditor.Callbacks;

using UnityObject = UnityEngine.Object;

[CustomEditor(typeof(VisualEffectAsset))]
public class VisualEffectAssetEditor : Editor
{
    [OnOpenAsset(1)]
    public static bool OnOpenVFX(int instanceID, int line)
    {
        var obj = EditorUtility.InstanceIDToObject(instanceID);
        if (obj is VisualEffectAsset)
        {
            VFXViewWindow.GetWindow<VFXViewWindow>().LoadAsset(obj as VisualEffectAsset, null);
            return true;
        }
        return false;
    }

    static Mesh s_CubeWireFrame;
    void OnEnable()
    {
        if (m_VisualEffectGO == null)
        {
            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = 60.0f;
            m_PreviewUtility.camera.allowHDR = true;
            m_PreviewUtility.camera.allowMSAA = false;
            m_PreviewUtility.camera.farClipPlane = 10000.0f;
            m_PreviewUtility.ambientColor = new Color(.1f, .1f, .1f, 1.0f);
            m_PreviewUtility.lights[0].intensity = 1.4f;
            m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
            m_PreviewUtility.lights[1].intensity = 1.4f;

            m_VisualEffectGO = new GameObject("VisualEffect (Preview)");

            m_VisualEffectGO.hideFlags = HideFlags.DontSave;
            m_VisualEffect = m_VisualEffectGO.AddComponent<VisualEffect>();
            m_PreviewUtility.AddManagedGO(m_VisualEffectGO);

            m_VisualEffectGO.transform.localPosition = Vector3.zero;
            m_VisualEffectGO.transform.localRotation = Quaternion.identity;
            m_VisualEffectGO.transform.localScale = Vector3.one;

            m_VisualEffect.visualEffectAsset = target as VisualEffectAsset;
            m_OriginalBounds.size = Vector3.zero;

            m_FrameCount = 0;
            m_Distance = 10;
            m_Origin = Vector3.zero;
            m_Direction = Vector3.forward;

            if (s_CubeWireFrame == null)
            {
                s_CubeWireFrame = new Mesh();

                var vertices = new Vector3[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, 0.5f),
                    new Vector3(-0.5f, 0.5f, -0.5f),

                    new Vector3(0.5f, -0.5f, -0.5f),
                    new Vector3(0.5f, -0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, 0.5f),
                    new Vector3(0.5f, 0.5f, -0.5f)
                };


                var indices = new int[]
                {
                    0, 1,
                    0, 3,
                    0, 4,

                    6, 2,
                    6, 5,
                    6, 7,

                    1, 2,
                    1, 5,

                    3, 7,
                    3, 2,

                    4, 5,
                    4, 7
                };
                s_CubeWireFrame.vertices = vertices;
                s_CubeWireFrame.SetIndices(indices, MeshTopology.Lines, 0);
            }
        }
    }

    PreviewRenderUtility m_PreviewUtility;

    GameObject m_VisualEffectGO;
    VisualEffect m_VisualEffect;
    Vector3 m_Direction;
    Vector3 m_Origin;
    float m_Distance;
    Bounds m_OriginalBounds;

    int m_FrameCount = 0;

    const int kSafeFrame = 2;

    public override bool HasPreviewGUI()
    {
        return true;
    }

    void ComputeFarNear()
    {
        if (m_OriginalBounds.size != Vector3.zero)
        {
            float maxBounds = Mathf.Sqrt(m_OriginalBounds.size.x * m_OriginalBounds.size.x + m_OriginalBounds.size.y * m_OriginalBounds.size.y + m_OriginalBounds.size.z * m_OriginalBounds.size.z);
            m_PreviewUtility.camera.farClipPlane = m_Distance + maxBounds * 1.1f;
            m_PreviewUtility.camera.nearClipPlane = Mathf.Max(0.0001f, (m_Distance - maxBounds));
        }
    }

    public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
    {
        if (m_VisualEffectGO == null)
        {
            OnEnable();
        }

        bool isRepaint = (Event.current.type == EventType.Repaint);

        m_Direction = VFXPreviewGUI.Drag2D(m_Direction, r);
        Renderer renderer = m_VisualEffectGO.GetComponent<Renderer>();

        if (m_FrameCount == kSafeFrame) // wait to frame before asking the renderer bounds as it is a computed value.
        {
            if (renderer != null)
            {
                m_OriginalBounds = renderer.bounds;
                float maxBounds = Mathf.Sqrt(m_OriginalBounds.size.x * m_OriginalBounds.size.x + m_OriginalBounds.size.y * m_OriginalBounds.size.y + m_OriginalBounds.size.z * m_OriginalBounds.size.z);
                m_Distance = Mathf.Max(0.01f, maxBounds * 1.25f);

                m_Origin = m_OriginalBounds.center;
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
            m_PreviewUtility.BeginPreview(r, background);

            Quaternion rot = Quaternion.Euler(m_Direction.y, 0, 0) * Quaternion.Euler(0, m_Direction.x, 0);
            m_PreviewUtility.camera.transform.position = m_OriginalBounds.center + rot * new Vector3(0, 0, -m_Distance);
            m_PreviewUtility.camera.transform.localRotation = rot;


            m_PreviewUtility.Render();
            if (renderer != null)
            {
                var bounds = renderer.bounds;

                m_PreviewUtility.DrawMesh(s_CubeWireFrame, Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.size), (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat"), 0);
            }

            m_PreviewUtility.EndAndDrawPreview(r);

            // Ask for repaint so the effect is animated.
            Repaint();
        }
    }

    void OnDisable()
    {
        if (!Object.ReferenceEquals(m_VisualEffectGO, null))
        {
            Object.DestroyImmediate(m_VisualEffectGO);
        }
        if (m_PreviewUtility != null)
        {
            m_PreviewUtility.Cleanup();
        }
    }

    public override void OnInspectorGUI()
    {
        VisualEffectAsset asset = (VisualEffectAsset)target;

        VisualEffectResource resource = asset.GetResource();

        if (resource == null) return;

        UnityObject[] objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset));


        bool enable = GUI.enabled; //Everything in external asset is disabled by default
        GUI.enabled = true;

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
            }
        }
        GUI.enabled = false;
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
                    scrollPosition -= -evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(position.width, position.height) * 140.0f;
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
