using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.MaterialGraph;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace UnityEditor.MaterialGraph.Drawing
{
    /*internal class PreviewScene : IDisposable
    {
       public PreviewScene(string sceneName)
        {
            camera.cameraType = CameraType.Preview;
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth;
            camera.fieldOfView = 15;
            camera.farClipPlane = 10.0f;
            camera.nearClipPlane = 2.0f;
            camera.backgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);

            // Explicitly use forward rendering for all previews
            // (deferred fails when generating some static previews at editor launch; and we never want
            // vertex lit previews if that is chosen in the player settings)
            camera.renderingPath = RenderingPath.Forward;
            camera.useOcclusionCulling = false;
            camera.scene = m_Scene;
        }

        public void AddGameObject(GameObject go)
        {
            if (m_GameObjects.Contains(go))
                return;

            m_GameObjects.Add(go);
        }

        public void Dispose() {

        }
        
    }*/

    /*internal class SavedRenderTargetState
    {
        RenderTexture renderTexture;
        Rect viewport;
        Rect scissor;

        internal SavedRenderTargetState()
        {
            GL.PushMatrix();
            if (ShaderUtil.hardwareSupportsRectRenderTexture)
                renderTexture = RenderTexture.active;
            viewport = ShaderUtil.rawViewportRect;
            scissor = ShaderUtil.rawScissorRect;
        }

        internal void Restore()
        {
            if (ShaderUtil.hardwareSupportsRectRenderTexture)
                EditorGUIUtility.SetRenderTextureNoViewport(renderTexture);
            ShaderUtil.rawViewportRect = viewport;
            ShaderUtil.rawScissorRect = scissor;
            GL.PopMatrix();
        }
    }*/

    internal class MaterialGraphPreviewGenerator : IDisposable
    {
        private readonly Scene m_Scene;
        static Mesh s_Quad;
        private Camera m_Camera;
        private RenderTexture m_RenderTexture;
        private Light Light0 { get; set; }
        private Light Light1 { get; set; }

        //private SavedRenderTargetState m_SavedState;

        private Material m_CheckerboardMaterial;

        public Light[] Lights
        {
            get
            {
                return new[] {Light0, Light1};
            }
        }

        private static readonly Mesh[] s_Meshes = {null, null, null, null, null};
        private static Mesh s_PlaneMesh;
        private static readonly GUIContent[] s_MeshIcons = {null, null, null, null, null};
        private static readonly GUIContent[] s_LightIcons = {null, null};
        private static readonly GUIContent[] s_TimeIcons = {null, null};

        protected static GameObject CreateLight()
        {
            GameObject lightGO = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, typeof(Light));
            var light = lightGO.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            light.enabled = false;
            return lightGO;
        }

        public MaterialGraphPreviewGenerator()
        {
            m_Scene = EditorSceneManager.NewPreviewScene();
            var camGO = EditorUtility.CreateGameObjectWithHideFlags("Preview Scene Camera", HideFlags.HideAndDontSave, typeof(Camera));
            SceneManager.MoveGameObjectToScene(camGO, m_Scene);

            m_Camera = camGO.GetComponent<Camera>();
            EditorUtility.SetCameraAnimateMaterials(m_Camera, true);

            m_Camera.cameraType = CameraType.Preview;
            m_Camera.enabled = false;
            m_Camera.clearFlags = CameraClearFlags.Depth;
            m_Camera.fieldOfView = 15;
            m_Camera.farClipPlane = 10.0f;
            m_Camera.nearClipPlane = 2.0f;
            m_Camera.backgroundColor = new Color(49.0f / 255.0f, 49.0f / 255.0f, 49.0f / 255.0f, 1.0f);

            // Explicitly use forward rendering for all previews
            // (deferred fails when generating some static previews at editor launch; and we never want
            // vertex lit previews if that is chosen in the player settings)
            m_Camera.renderingPath = RenderingPath.Forward;
            m_Camera.useOcclusionCulling = false;
            m_Camera.scene = m_Scene;

            var l0 = CreateLight();
            SceneManager.MoveGameObjectToScene(l0, m_Scene);

            //previewScene.AddGameObject(l0);
            Light0 = l0.GetComponent<Light>();

            var l1 = CreateLight();
            SceneManager.MoveGameObjectToScene(l1, m_Scene);
            //previewScene.AddGameObject(l1);
            Light1 = l1.GetComponent<Light>();

            Light0.color = new Color(0.769f, 0.769f, 0.769f, 1); // SceneView.kSceneViewFrontLight
            Light1.transform.rotation = Quaternion.Euler(340, 218, 177);
            Light1.color = new Color(.4f, .4f, .45f, 0f) * .7f;

            m_CheckerboardMaterial = new Material(Shader.Find("Hidden/Checkerboard"));
            m_CheckerboardMaterial.SetFloat("_X", 8);
            m_CheckerboardMaterial.SetFloat("_Y", 8);

            if (s_Meshes[0] == null)
            {
                var handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
                // @TODO: temp workaround to make it not render in the scene
                handleGo.SetActive(false);
                foreach (Transform t in handleGo.transform)
                {
                    var meshFilter = t.GetComponent<MeshFilter>();
                    switch (t.name)
                    {
                        case "sphere":
                            s_Meshes[0] = meshFilter.sharedMesh;
                            break;
                        case "cube":
                            s_Meshes[1] = meshFilter.sharedMesh;
                            break;
                        case "cylinder":
                            s_Meshes[2] = meshFilter.sharedMesh;
                            break;
                        case "torus":
                            s_Meshes[3] = meshFilter.sharedMesh;
                            break;
                        default:
                            Debug.Log("Something is wrong, weird object found: " + t.name);
                            break;
                    }
                }

                s_MeshIcons[0] = EditorGUIUtility.IconContent("PreMatSphere");
                s_MeshIcons[1] = EditorGUIUtility.IconContent("PreMatCube");
                s_MeshIcons[2] = EditorGUIUtility.IconContent("PreMatCylinder");
                s_MeshIcons[3] = EditorGUIUtility.IconContent("PreMatTorus");
                s_MeshIcons[4] = EditorGUIUtility.IconContent("PreMatQuad");

                s_LightIcons[0] = EditorGUIUtility.IconContent("PreMatLight0");
                s_LightIcons[1] = EditorGUIUtility.IconContent("PreMatLight1");

                s_TimeIcons[0] = EditorGUIUtility.IconContent("PlayButton");
                s_TimeIcons[1] = EditorGUIUtility.IconContent("PauseButton");

                Mesh quadMesh = Resources.GetBuiltinResource(typeof(Mesh), "Quad.fbx") as Mesh;
                s_Meshes[4] = quadMesh;
                s_PlaneMesh = quadMesh;
            }
        }

        public Texture DoRenderPreview(Material mat, PreviewMode mode, Rect size)
        {
            return DoRenderPreview(mat, mode, size, Time.realtimeSinceStartup);
        }

        public static Mesh quad
        {
            get
            {
                if (s_Quad != null)
                    return s_Quad;

                var vertices = new[]
                {
                    new Vector3(-1f, -1f, 0f),
                    new Vector3(1f,  1f, 0f),
                    new Vector3(1f, -1f, 0f),
                    new Vector3(-1f,  1f, 0f)
                };

                var uvs = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f)
                };

                var indices = new[] { 0, 1, 2, 1, 0, 3 };

                s_Quad = new Mesh
                {
                    vertices = vertices,
                    uv = uvs,
                    triangles = indices
                };
                s_Quad.RecalculateNormals();
                s_Quad.RecalculateBounds();

                return s_Quad;
            }
        }

        public float GetScaleFactor(float width, float height)
        {
            float scaleFacX = Mathf.Max(Mathf.Min(width * 2, 1024), width) / width;
            float scaleFacY = Mathf.Max(Mathf.Min(height * 2, 1024), height) / height;
            float result = Mathf.Min(scaleFacX, scaleFacY) * EditorGUIUtility.pixelsPerPoint;
            //if (m_PixelPerfect)  //m_PixelPerfect = false; in PreviewRenderUtility default constructor.
            //    result = Mathf.Max(Mathf.Round(result), 1f);
            return result;
        }

        public Texture DoRenderPreview(Material mat, PreviewMode mode, Rect size, float time)
        {
            if (mat == null || mat.shader == null)
                return Texture2D.blackTexture;

            

            //utility.BeginPreview(size, GUIStyle.none);

            float scaleFac = GetScaleFactor(size.width, size.height);

            int rtWidth = (int)(size.width * scaleFac);
            int rtHeight = (int)(size.height * scaleFac);

            if (!m_RenderTexture || m_RenderTexture.width != rtWidth || m_RenderTexture.height != rtHeight)
            {
                if (m_RenderTexture)
                {
                    UnityEngine.Object.DestroyImmediate(m_RenderTexture);
                    m_RenderTexture = null;
                }

                // Do not use GetTemporary to manage render textures. Temporary RTs are only
                // garbage collected each N frames, and in the editor we might be wildly resizing
                // the inspector, thus using up tons of memory.
                RenderTextureFormat format = m_Camera.allowHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
                m_RenderTexture = new RenderTexture(rtWidth, rtHeight, 16, format, RenderTextureReadWrite.Default);
                m_RenderTexture.hideFlags = HideFlags.HideAndDontSave;

                m_Camera.targetTexture = m_RenderTexture;

                foreach (var light in Lights) //redundant?
                    light.enabled = true;
            }
            
            //Blit checkerboard background:
            Graphics.Blit(null, m_RenderTexture, m_CheckerboardMaterial);
            
            //m_SavedState = new SavedRenderTargetState();
            //EditorGUIUtility.SetRenderTextureNoViewport(m_RenderTexture);
            GL.LoadOrtho();
            //GL.LoadPixelMatrix(0, m_RenderTexture.width, m_RenderTexture.height, 0);
            //ShaderUtil.rawViewportRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            //ShaderUtil.rawScissorRect = new Rect(0, 0, m_RenderTexture.width, m_RenderTexture.height);
            //GL.Clear(true, true, m_Camera.backgroundColor);

            foreach (var light in Lights)
                light.enabled = true;

            var oldProbe = RenderSettings.ambientProbe;
            //Unsupported.SetOverrideRenderSettings(previewScene.scene);
            // Most preview windows just want the light probe from the main scene so by default we copy it here. It can then be overridden if user wants.
            RenderSettings.ambientProbe = oldProbe;

            
            if (mode == PreviewMode.Preview3D)
            {
                m_Camera.transform.position = -Vector3.forward * 5;
                m_Camera.transform.rotation = Quaternion.identity;
                m_Camera.orthographic = false;
            }
            else
            {
                m_Camera.transform.position = -Vector3.forward * 2;
                m_Camera.transform.rotation = Quaternion.identity;
                m_Camera.orthographicSize = 1;
                m_Camera.orthographic = true;
            }

            EditorUtility.SetCameraAnimateMaterialsTime(m_Camera, time);
            Lights[0].intensity = 1.0f;
            Lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
            Lights[1].intensity = 1.0f;
            m_Camera.clearFlags = CameraClearFlags.Depth;

            DrawMesh(
                mode == PreviewMode.Preview3D ? s_Meshes[0] : quad,
                Vector3.zero,
                Quaternion.identity,
                mat,
                0);
            Render(true, false);

            return EndPreview();
        }

        public void Dispose()
        {
            //EditorSceneManager.ClosePreviewScene(m_Scene); //Removing this line makes it work in the shader graph viewer (checkerboard disappears after windows loses and regains focus)

            /*foreach (var go in m_GameObjects)
                Object.DestroyImmediate(go);

            m_GameObjects.Clear();*/
        }

        public void Reset() {
            if (m_RenderTexture)
            {
                UnityEngine.Object.DestroyImmediate(m_RenderTexture);
                m_RenderTexture = null;
            }

            /*if (m_InvisibleMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(m_InvisibleMaterial);
                m_InvisibleMaterial = null;
            }*/

            Dispose();
        }

        public Texture EndPreview()
        {
            //Unsupported.RestoreOverrideRenderSettings();

            //m_SavedState.Restore();
            FinishFrame();
            return m_RenderTexture;
        }

        private void FinishFrame()
        {
            foreach (var light in Lights)
                light.enabled = false;
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex)
        {
            DrawMesh(mesh, pos, rot, mat, subMeshIndex, null, null, false);
        }

        public void DrawMesh(Mesh mesh, Vector3 pos, Quaternion rot, Material mat, int subMeshIndex, MaterialPropertyBlock customProperties, Transform probeAnchor, bool useLightProbe)
        {
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(pos, rot, Vector3.one), mat, 1, m_Camera, subMeshIndex, customProperties, ShadowCastingMode.Off, false, probeAnchor, useLightProbe);
        }

        public void Render(bool allowScriptableRenderPipeline = false, bool updatefov = true)
        {
            foreach (var light in Lights)
                light.enabled = true;

            //var oldAllowPipes = Unsupported.useScriptableRenderPipeline;
            //Unsupported.useScriptableRenderPipeline = allowScriptableRenderPipeline;

            float saveFieldOfView = m_Camera.fieldOfView;

            if (updatefov)
            {
                // Calculate a view multiplier to avoid clipping when the preview width is smaller than the height.
                float viewMultiplier = (m_RenderTexture.width <= 0 ? 1.0f : Mathf.Max(1.0f, (float)m_RenderTexture.height / m_RenderTexture.width));
                // Multiply the viewing area by the viewMultiplier - it requires some conversions since the camera view is expressed as an angle.
                m_Camera.fieldOfView = Mathf.Atan(viewMultiplier * Mathf.Tan(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad)) * Mathf.Rad2Deg * 2.0f;
            }

            m_Camera.Render();

            m_Camera.fieldOfView = saveFieldOfView;
            //Unsupported.useScriptableRenderPipeline = oldAllowPipes;
        }
    }
}
