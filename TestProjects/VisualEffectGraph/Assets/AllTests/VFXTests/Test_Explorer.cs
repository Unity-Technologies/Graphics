using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class Test_Explorer : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string[] m_scenes;

    private static readonly int kColumnCount = 8;
    private static readonly Vector2 kGridSize = new Vector2(1.25f, 1.25f);
    private static readonly Vector3 kScale = Vector3.one * 0.1f;
    private static readonly int kMainCullingID = 31;

    private RenderTexture[] m_PreviewRenderTextures;
    private GameObject[] m_PreviewPlanes;
    private GameObject m_PreviewSelectionQuad;
    private GameObject m_PreviewMainCamera;

    private int m_currentSceneIndex;
    private bool m_previewMode;
#if UNITY_EDITOR
    [PostProcessScene(666)]
    public static void OnPostprocessScene()
    {
        var listScene = Directory.GetFiles("Assets/AllTests/VFXTests/GraphicsTests/", "*.unity").Take(31).ToArray();
        var scene = SceneManager.GetActiveScene();
        foreach (var obj in scene.GetRootGameObjects())
        {
            var testExplorer = obj.GetComponent<Test_Explorer>();
            if (testExplorer != null)
            {
                testExplorer.m_scenes = listScene;
            }
        }
    }

#endif


    void Start()
    {
        m_currentSceneIndex = 0;
        m_previewMode = true;

        m_PreviewMainCamera = new GameObject("mainCamera");
        var mainCamera = gameObject.AddComponent<Camera>();

        if (mainCamera != null)
        {
            mainCamera = gameObject.GetComponent<Camera>();
        }

        mainCamera.cullingMask = 1 << kMainCullingID;

        mainCamera.GetComponent<Transform>().position = new Vector3(3.5f, 1, 2.25f);
        mainCamera.GetComponent<Transform>().eulerAngles = new Vector3(90.0f, 180.0f, 0);
        mainCamera.orthographicSize = 3.0f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
        mainCamera.orthographic = true;
        mainCamera.depth = -666;

        ResetScene();
    }

    private static GameObject CreatePlane(Texture texture, Color color)
    {
        var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        var material = new Material(Shader.Find("Unlit/Texture"));

        if (texture != null)
        {
            material.SetTexture("_MainTex", texture);
        }

        material.SetFloat("_Transparency", 1.0f);
        material.SetColor("_TintColor", color);
        plane.GetComponent<Renderer>().material = material;

        return plane;
    }

    static Vector3 GetPlanePosition(int i)
    {
        var currentColumn = i % kColumnCount;
        var currentRow = i / kColumnCount;
        return new Vector3(kColumnCount - currentColumn * kGridSize.x, 0, currentRow * kGridSize.y);
    }

    void OnObjectLoaded(GameObject obj, int sceneIndex)
    {
        obj.layer = sceneIndex;
        var currentCamera = obj.GetComponent<Camera>();
        if (currentCamera)
        {
            currentCamera.cullingMask = 1 << sceneIndex;
            currentCamera.renderingPath = RenderingPath.Forward;
            currentCamera.clearFlags = CameraClearFlags.SolidColor;
            currentCamera.targetTexture = m_PreviewRenderTextures[sceneIndex];
        }

        var audioListener = obj.GetComponent<AudioListener>();
        if (audioListener)
        {
            audioListener.enabled = false;
            Object.Destroy(audioListener);
        }

        var light = obj.GetComponent<Light>();
        if (light)
        {
            light.cullingMask = 1 << sceneIndex;
        }

        foreach (Transform subObj in obj.transform)
        {
            OnObjectLoaded(subObj.gameObject, sceneIndex);
        }
    }

    void ResetScene()
    {
        if (m_PreviewSelectionQuad)
        {
            Object.Destroy(m_PreviewSelectionQuad);
        }

        if (m_PreviewRenderTextures != null)
        {
            for (int i = 0; i < m_PreviewRenderTextures.Length; ++i)
            {
                m_PreviewRenderTextures[i].Release();
                m_PreviewRenderTextures[i] = null;
            }
            m_PreviewRenderTextures = null;
        }

        if (m_PreviewPlanes != null)
        {
            for (int i = 0; i < m_PreviewPlanes.Length; ++i)
            {
                Object.Destroy(m_PreviewPlanes[i]);
            }
            m_PreviewPlanes = null;
        }

        for (int sceneIndex = 1; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            var scene = SceneManager.GetSceneAt(sceneIndex);
            SceneManager.UnloadSceneAsync(scene);
        }

        if (!m_previewMode)
        {
            m_PreviewMainCamera.SetActive(false);
            SceneManager.LoadSceneAsync(m_scenes[m_currentSceneIndex], LoadSceneMode.Additive);
        }
        else
        {
            m_PreviewMainCamera.SetActive(true);
            m_PreviewSelectionQuad = CreatePlane(null, Color.red);
            m_PreviewSelectionQuad.name = "Plane_Selection";
            m_PreviewSelectionQuad.GetComponent<Transform>().localScale = kScale * 1.1f;
            m_PreviewSelectionQuad.layer = kMainCullingID;

            m_PreviewRenderTextures = new RenderTexture[m_scenes.Length];
            m_PreviewPlanes = new GameObject[m_scenes.Length];
            for (var i = 0; i < m_scenes.Length; ++i)
            {
                const int res = 256;
                m_PreviewRenderTextures[i] = new RenderTexture(res, res, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default) { hideFlags = HideFlags.HideAndDontSave };

                var plane = CreatePlane(m_PreviewRenderTextures[i], Color.white);
                plane.name = "Plane_" + i;
                plane.layer = kMainCullingID;

                plane.GetComponent<Transform>().position = GetPlanePosition(i);
                plane.GetComponent<Transform>().localScale = kScale;
                m_PreviewPlanes[i] = plane;
            }

            for (var i = 0; i < m_scenes.Length; ++i)
            {
                var sceneName = m_scenes[i];
                var sceneIndex = i;
                var asyncJob = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                asyncJob.completed += (a) =>
                {
                    var currentScene = SceneManager.GetSceneByPath(sceneName);
                    foreach (var obj in currentScene.GetRootGameObjects())
                    {
                        OnObjectLoaded(obj, sceneIndex);
                    }
                };
            }
        }
    }

    private float m_waitAbsoluteNextInput = 0.0f;
    private bool m_reset = false;
    void Update()
    {
        if (m_reset)
        {
            for (int i = 1; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    return;
            }
            ResetScene();
            m_reset = false;
        }

        if (m_waitAbsoluteNextInput != 0)
        {
            m_waitAbsoluteNextInput -= Time.deltaTime;
            if (m_waitAbsoluteNextInput < 0.0f)
            {
                m_waitAbsoluteNextInput = 0.0f;
            }
        }

        var horizontal = m_waitAbsoluteNextInput == 0.0f ? Input.GetAxis("Horizontal") : 0.0f;
        var vertical = m_waitAbsoluteNextInput == 0.0f ? Input.GetAxis("Vertical") : 0.0f;

        float ignoreZone = 0.5f;
        float waitTime = 0.2f;

        var lastSceneIndex = m_currentSceneIndex;
        if (horizontal > ignoreZone || Input.GetKeyUp(KeyCode.Joystick1Button5))
        {
            m_currentSceneIndex++; m_waitAbsoluteNextInput = waitTime;
        }
        else if (horizontal < -ignoreZone || Input.GetKeyUp(KeyCode.Joystick1Button4))
        {
            m_currentSceneIndex--; m_waitAbsoluteNextInput = waitTime;
        }

        if (m_previewMode)
        {
            if (vertical > ignoreZone)
            {
                m_currentSceneIndex -= kColumnCount; m_waitAbsoluteNextInput = waitTime;
            }
            else if (vertical < -ignoreZone)
            {
                m_currentSceneIndex += kColumnCount; m_waitAbsoluteNextInput = waitTime;
            }
        }

        if (m_currentSceneIndex < 0)
        {
            m_currentSceneIndex = m_scenes.Length + m_currentSceneIndex + 1;
        }
        else if (m_currentSceneIndex >= m_scenes.Length)
        {
            m_currentSceneIndex = m_currentSceneIndex % m_scenes.Length;
        }

        if (m_previewMode)
        {
            m_PreviewSelectionQuad.GetComponent<Transform>().position = GetPlanePosition(m_currentSceneIndex) - new Vector3(0, 0.1f, 0);
        }

        if (Input.GetButtonUp("Fire1")
            ||  Input.GetButtonUp("Fire2")
            ||  Input.GetButtonUp("Fire3"))
        {
            m_previewMode = !m_previewMode;
            m_reset = true;
        }
        else if (!m_previewMode && lastSceneIndex != m_currentSceneIndex)
        {
            m_reset = true;
        }
    }
}
