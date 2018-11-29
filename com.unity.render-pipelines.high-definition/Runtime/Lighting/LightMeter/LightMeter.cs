using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;using System.Reflection;
using System.Linq.Expressions;
using System;
#endif

[ExecuteInEditMode]
public class LightMeter : MonoBehaviour
{
    static Mesh m_sphere;
    static Mesh sphere
    {
        get
        {
            if (m_sphere == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_sphere = tmp.GetComponent<MeshFilter>().sharedMesh;
                DestroyImmediate(tmp, false);
            }
            
            return m_sphere;
        }
    }

    static Mesh m_diskMesh;

    static Mesh diskMesh
    {
        get
        {
            if (m_diskMesh == null)
            {
                m_diskMesh = new Mesh();
                const int division = 64;
                Vector3[] vertices = new Vector3[division + 1];
                vertices[0] = Vector3.zero;
                int[] triangles = new int[division * 3];
                Vector3[] normals = new Vector3[division + 1];
                normals[0] = Vector3.back;
                float angle = 0f;
                for (int i = 0; i < division; ++i)
                {
                    angle = Mathf.PI * 2f * (i) / division;
                    vertices[i+1] = new Vector3( Mathf.Cos(angle), Mathf.Sin(angle), 0f ) * 0.5f;
                    normals[i+1] = Vector3.back;

                    triangles[i * 3] = 0;
                    triangles[i * 3 + 2] = i+1;
                    triangles[i * 3 + 1] =  (i==division-1)? 1 : i+2;
                }

                m_diskMesh.name = "Disk";
                m_diskMesh.vertices = vertices;
                m_diskMesh.triangles = triangles;
                m_diskMesh.normals = normals;
                m_diskMesh.RecalculateBounds();
            }

            return m_diskMesh;
        }
    }

    static RenderTexture m_rt;
    static RenderTexture rt
    {
        get
        {
            if (m_rt == null)
                m_rt = new RenderTexture(1, 1, 0, GraphicsFormat.R32G32B32A32_SFloat);
            
            return m_rt;
        }
    }

    static Texture2D m_sampleTex;
    static Texture2D sampleTex
    {
        get
        {
            if (m_sampleTex == null)
                m_sampleTex = new Texture2D(rt.width, rt.height, GraphicsFormat.R32G32B32A32_SFloat, TextureCreationFlags.None);

            return m_sampleTex;
        }
    }

    static Material m_previewMaterial;
    static Material previewMaterial
    {
        get
        {
            if (m_previewMaterial == null)
            {
                m_previewMaterial = new Material(Shader.Find("HDRenderPipeline/Lit"));
                m_previewMaterial.SetColor("_BaseColor", Color.white);
                m_previewMaterial.SetFloat("_Metallic", 0f);
                m_previewMaterial.SetFloat("_Smoothness", 0f);
            }

            return m_previewMaterial;
        }
    }

    static Quaternion HalfRoundRotationX = Quaternion.AngleAxis(180f, Vector3.right);

    static GUIStyle m_labelStyle;

    static GUIStyle labelStyle
    {
        get
        {
            if (m_labelStyle == null)
            {
                m_labelStyle = new GUIStyle()
                {
                    normal = new GUIStyleState()
                    {
                        textColor = Color.white
                    }
                };
            }
            
            return m_labelStyle;
        }
    }

    const float k_CameraOffset = 0.001f;
    
    const int k_Layer = 3;

    [HideInInspector] public float sampledValue = 0f;
    GUIContent sampledContent;
    Vector2 sampledContentSize = Vector2.zero;

    void Start()
    {
        if (sampledContent == null)
            sampledContent = new GUIContent();
    }

    void Update()
    {
        sampledValue = Sample();

        sampledContent.text = sampledValue.ToString();
        sampledContentSize = labelStyle.CalcSize(sampledContent);

        //Graphics.DrawMesh(sphere, Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one * gizmoScale ), previewMaterial, k_Layer);
    }

    void OnGUI()
    {
        var screenPos = Vector3.zero;
        var cam = GetCurrentCamera();
        if ( cam != null && GetTextScreenPos(ref screenPos, transform.position + cam.transform.up * gizmoScale * 0.5f))
        {   
            GUI.Label(new Rect(screenPos.x - sampledContentSize.x * 0.5f, screenPos.y - sampledContentSize.y , sampledContentSize.x, sampledContentSize.y), Sample().ToString(), labelStyle );
        }
    }

    float Sample()
    {
        var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
        if (hdrp == null) return -1f;
        
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.position = transform.position;
        quad.transform.rotation = transform.rotation;
        quad.layer = k_Layer;

        var cam = new GameObject().AddComponent<Camera>();
        cam.transform.position = transform.TransformPoint(Vector3.back * k_CameraOffset);
        cam.transform.rotation = transform.rotation;
        
        var hdCam = cam.gameObject.AddComponent<HDAdditionalCameraData>();
        cam.targetTexture = rt;

        cam.nearClipPlane = k_CameraOffset * 0.5f;
        cam.farClipPlane = k_CameraOffset * 1.5f;

        cam.fieldOfView = 1f;
        
        hdrp.debugDisplaySettings.SetDebugLightingMode(DebugLightingMode.LuxMeter);
        
        cam.Render();

        cam.targetTexture = null;

        RenderTexture.active = rt;
        
        sampleTex.ReadPixels( new Rect(0f, 0f,  rt.width, rt.height ), 0, 0);
        sampleTex.Apply();

        RenderTexture.active = null;
        
        hdrp.debugDisplaySettings.SetDebugLightingMode(DebugLightingMode.None);

        var sampleValue = sampleTex.GetPixelBilinear(.5f, .5f);
        
        #if UNITY_EDITOR
        DestroyImmediate(cam.gameObject, false);
        DestroyImmediate(quad, false);
        #else
        Destroy(cam.gameObject, false);
        Destroy(quad, false);
        #endif

        return sampleValue.r;
    }

    void OnDrawGizmos()
    {
        if (!enabled) return;
        
        Vector3 diskPosition = transform.position + transform.forward * -k_CameraOffset * 1.5f;
        
        Gizmos.DrawMesh(diskMesh, diskPosition , transform.rotation, Vector3.one * gizmoScale);
        Gizmos.DrawLine(transform.position , transform.position + transform.forward * -gizmoScale * 0.5f);
        
        Gizmos.color = new Color(1f,1f,1f, 0.75f);
        Gizmos.DrawMesh(diskMesh, diskPosition, transform.rotation * HalfRoundRotationX, Vector3.one * gizmoScale);
    }

    static Camera GetCurrentCamera( bool sceneView = false )
    {
        var cam = Camera.main;
        if (cam == null)
        {
            cam = Camera.allCameras
                .Where(c => c.targetTexture == null)
                .OrderBy(c => c.depth)
                .Last();
        }
#if UNITY_EDITOR
        if (sceneView)
        {
            var view = UnityEditor.SceneView.currentDrawingSceneView;
            cam = view.camera;
        }
#endif
        return cam;
    }

    static bool GetTextScreenPos( ref Vector3 screenPos, Vector3 worldPos, bool sceneView = false )
    {
        var cam = GetCurrentCamera(sceneView);
        
        if (cam == null) return false;
        
        screenPos = cam.WorldToScreenPoint(worldPos);

        if (!sceneView) screenPos.y = Screen.height - screenPos.y;
        
        if (IsPosOnScreen(screenPos))
            return false;

        return true;
    }

    static bool IsPosOnScreen(Vector3 screenPos)
    {
        return screenPos.y < 0 || screenPos.y > Screen.height || screenPos.x < 0 || screenPos.x > Screen.width || screenPos.z < 0;
    }
    
#if UNITY_EDITOR
    static public void DrawString(string text, Vector3 worldPos, Color? colour = null) {
        UnityEditor.Handles.BeginGUI();
 
        var restoreColor = GUI.color;
 
        if (colour.HasValue) GUI.color = colour.Value;
        var view = UnityEditor.SceneView.currentDrawingSceneView;

        Vector3 screenPos = Vector3.zero;
        if (GetTextScreenPos(ref screenPos, worldPos, true))
        {
            Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
            GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height + 4, size.x, size.y), text);
        }
 
        
        GUI.color = restoreColor;
        UnityEditor.Handles.EndGUI();
    }
    
    static Func<float> s_GizmoScale = GizmoScaleGetter();
    static Func<float> GizmoScaleGetter()
    {
        var type = Type.GetType("UnityEditor.AnnotationUtility,UnityEditor");
        var property = type.GetProperty("iconSize", BindingFlags.Static | BindingFlags.NonPublic);
        var lambda = Expression.Lambda<Func<float>>(
            Expression.Multiply(
                Expression.Property(null, property),
                Expression.Constant(30.0f)
            )
        );
        return lambda.Compile();
    }

    void OnEnable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        SceneView.onSceneGUIDelegate += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
    }

    #if UNITY_EDITOR_WIN
    const int topBarPixelOffset = 42;
    #else
    const int topBarPixelOffset = 42;
    #endif

    void OnSceneGUI( SceneView sceneView )
    {
        var screenPos = sceneView.camera.WorldToScreenPoint(transform.position + sceneView.camera.transform.up * gizmoScale * 0.5f );
        
        screenPos.x += sampledContentSize.x * 0.5f;
        screenPos.y = Screen.height - screenPos.y - topBarPixelOffset - sampledContentSize.y - 5;
        
        if (true || IsPosOnScreen(screenPos))
        {
            Handles.BeginGUI();
            GUI.Label(new Rect(screenPos.x - sampledContentSize.x, screenPos.y, sampledContentSize.x, sampledContentSize.y), sampledContent, labelStyle );
            Handles.EndGUI();
        }
    }
#endif

    static float gizmoScale
    {
        get
        {
#if UNITY_EDITOR
            return s_GizmoScale();
#else
            return 0.5f;
#endif
        }
    }
}
