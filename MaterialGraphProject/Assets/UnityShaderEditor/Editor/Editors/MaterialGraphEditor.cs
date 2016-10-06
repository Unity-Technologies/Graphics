using UnityEditorInternal;
using UnityEngine;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph
{
    internal class MaterialGraphPreviewGenerator
    {
        private PreviewRenderUtility m_PreviewUtility;
        private static readonly Mesh[] s_Meshes = {null, null, null, null, null};
        private static Mesh s_PlaneMesh;
        private static readonly GUIContent[] s_MeshIcons = {null, null, null, null, null};
        private static readonly GUIContent[] s_LightIcons = {null, null};
        private static readonly GUIContent[] s_TimeIcons = {null, null};

        public MaterialGraphPreviewGenerator()
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                //EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.m_Camera, true);
            }

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

        public Texture DoRenderPreview(Material mat, PreviewMode mode, Rect size, float time)
        {
            if (mat == null || mat.shader == null)
                return Texture2D.blackTexture;

            m_PreviewUtility.BeginPreview(size, GUIStyle.none);

            if (mode == PreviewMode.Preview3D)
            {
                m_PreviewUtility.m_Camera.transform.position = -Vector3.forward * 5;
                m_PreviewUtility.m_Camera.transform.rotation = Quaternion.identity;
                //EditorUtility.SetCameraAnimateMaterialsTime(m_PreviewUtility.m_Camera, time);
                var amb = new Color(.2f, .2f, .2f, 0);
                m_PreviewUtility.m_Light[0].intensity = 1.0f;
                m_PreviewUtility.m_Light[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
                m_PreviewUtility.m_Light[1].intensity = 1.0f;

                InternalEditorUtility.SetCustomLighting(m_PreviewUtility.m_Light, amb);
                m_PreviewUtility.DrawMesh(s_Meshes[0], Vector3.zero, Quaternion.Euler(-20, 0, 0) * Quaternion.Euler(0, 0, 0), mat, 0);
                var oldFog = RenderSettings.fog;
                Unsupported.SetRenderSettingsUseFogNoDirty(false);
                m_PreviewUtility.m_Camera.Render();
                Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
                InternalEditorUtility.RemoveCustomLighting();
            }
            else
            {
                //EditorUtility.UpdateGlobalShaderProperties(Time.realtimeSinceStartup);
                Graphics.Blit(null, mat);
            }
            return m_PreviewUtility.EndPreview();
        }
    }
}
