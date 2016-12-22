using UnityEditorInternal;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing
{
    internal class MaterialGraphPreviewGenerator
    {
        private PreviewRenderUtility m_PreviewUtility;
        private static readonly Mesh[] s_Meshes = {null, null, null, null, null};
        private static Mesh s_PlaneMesh;
        private static readonly GUIContent[] s_MeshIcons = {null, null, null, null, null};
        private static readonly GUIContent[] s_LightIcons = {null, null};
        private static readonly GUIContent[] s_TimeIcons = {null, null};

        private PreviewRenderUtility utility
        {
            get
            {
                if (m_PreviewUtility == null)
                    m_PreviewUtility = new PreviewRenderUtility();

                return m_PreviewUtility;
            }
        }

        public void Reset()
        {
            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();

            m_PreviewUtility = null;
        }

        public MaterialGraphPreviewGenerator()
        {
            EditorUtility.SetCameraAnimateMaterials(utility.m_Camera, true);

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

        static Mesh s_Quad;
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

        public Texture DoRenderPreview(Material mat, PreviewMode mode, Rect size, float time)
        {
            if (mat == null || mat.shader == null)
                return Texture2D.blackTexture;

            utility.BeginPreview(size, GUIStyle.none);

            if (mode == PreviewMode.Preview3D)
            {
                utility.m_Camera.transform.position = -Vector3.forward * 5;
                utility.m_Camera.transform.rotation = Quaternion.identity;
            }
            else
            {
                utility.m_Camera.projectionMatrix = Matrix4x4.identity;
            }

            EditorUtility.SetCameraAnimateMaterialsTime(utility.m_Camera, time);
            utility.m_Light[0].intensity = 1.0f;
            utility.m_Light[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
            utility.m_Light[1].intensity = 1.0f;
            InternalEditorUtility.SetCustomLighting(utility.m_Light, Color.black);
            var oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
            utility.m_Camera.clearFlags = CameraClearFlags.Depth;

            utility.DrawMesh(
                mode == PreviewMode.Preview3D ? s_Meshes[0] : quad,
                Vector3.zero,
                Quaternion.identity,
                mat,
                0);
            utility.m_Camera.Render();

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
            InternalEditorUtility.RemoveCustomLighting();

            return utility.EndPreview();
        }
    }
}
