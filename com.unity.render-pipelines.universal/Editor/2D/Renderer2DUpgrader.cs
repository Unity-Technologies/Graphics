using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    internal static class Renderer2DUpgrader
    {
        delegate void Upgrader<T>(T toUpgrade) where T : Object;

        static void ProcessAssetDatabaseObjects<T>(string searchString, Upgrader<T> upgrader) where T : Object
        {
            string[] prefabNames = AssetDatabase.FindAssets(searchString);
            foreach (string prefabName in prefabNames)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabName);
                if (path.StartsWith("Assets"))
                {
                    T obj = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (obj != null)
                    {
                        upgrader(obj);
                    }
                }
            }
        }

        public static void UpgradeObjectWithParametricLights(GameObject obj)
        {
            Light2D[] lights = obj.GetComponents<Light2D>();
            if (lights.Length > 0)
            {
                foreach (var light in lights)
                    UpgradeParametricLight(light);
            }
        }

        public static void UpgradeParametricLight(Light2D light)
        {
            if (light.lightType == (Light2D.LightType)Light2D.DeprecatedLightType.Parametric)
            {
                light.lightType = Light2D.LightType.Freeform;

                float radius = light.shapeLightParametricRadius;
                float angle = light.shapeLightParametricAngleOffset;
                int   sides = light.shapeLightParametricSides;

                var angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
                if (sides < 3)
                {
                    radius = 0.70710678118654752440084436210485f * radius;
                    sides = 4;
                }

                if (sides == 4)
                {
                    angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
                }

                var radiansPerSide = 2 * Mathf.PI / sides;
                var min = new Vector3(float.MaxValue, float.MaxValue, 0);
                var max = new Vector3(float.MinValue, float.MinValue, 0);


                Vector3[] shapePath = new Vector3[sides];
                for (var i = 0; i < sides; i++)
                {
                    var endAngle = (i + 1) * radiansPerSide;
                    var extrudeDir = new Vector3(Mathf.Cos(endAngle + angleOffset), Mathf.Sin(endAngle + angleOffset), 0);
                    var endPoint = radius * extrudeDir;

                    shapePath[i] = endPoint;
                }

                light.shapePath = shapePath;
                light.UpdateMesh(true);
            }
        }

        static void UpgradeGameObject(GameObject go)
        {
            Renderer[] spriteRenderers = go.GetComponentsInChildren<Renderer>(true);
            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if (data != null)
            {
                Material defaultMat = data.GetDefaultMaterial(DefaultMaterialType.Sprite);

                bool upgraded = false;
                foreach (Renderer renderer in spriteRenderers)
                {
                    int materialCount = renderer.sharedMaterials.Length;
                    Material[] newMaterials = new Material[materialCount];

                    for (int i = 0; i < materialCount; i++)
                    {
                        Material mat = renderer.sharedMaterials[i];

                        if (mat != null && mat.shader.name == "Sprites/Default")
                        {
                            newMaterials[i] = defaultMat;
                            upgraded = true;
                        }
                        else
                        {
                            newMaterials[i] = renderer.sharedMaterials[i];
                        }
                    }

                    if (upgraded)
                        renderer.sharedMaterials = newMaterials;
                }

                if (upgraded)
                {
                    Debug.Log(go.name + " was upgraded.", go);
                    EditorSceneManager.MarkSceneDirty(go.scene);
                }
            }
        }

        static void UpgradeMaterial(Material mat)
        {
            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if (data != null)
            {
                Material defaultMat = data.GetDefaultMaterial(DefaultMaterialType.Sprite);

                if (mat.shader.name == "Sprites/Default")
                {
                    mat.shader = defaultMat.shader;
                }
            }
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Materials to 2D Renderer Materials", false)]
        static void UpgradeProjectTo2DRenderer()
        {
            if (!EditorUtility.DisplayDialog("2D Renderer Upgrader", "The upgrade will search for all prefabs in your project that use Sprite Renderers and change the material references of those Sprite Renderers to a lit material. You can't undo this operation. It's highly recommended to backup your project before proceeding.", "Proceed", "Cancel"))
                return;

            ProcessAssetDatabaseObjects<GameObject>("t: Prefab", UpgradeGameObject);
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Scene Materials to 2D Renderer Materials", false)]
        static void UpgradeSceneTo2DRenderer()
        {
            if (!EditorUtility.DisplayDialog("2D Renderer Upgrader", "The upgrade will change the material references of Sprite Renderers in currently open scene(s) to a lit material. You can't undo this operation. Make sure you save the scene(s) before proceeding.", "Proceed", "Cancel"))
                return;

            GameObject[] gameObjects = Object.FindObjectsOfType<GameObject>();
            if (gameObjects != null && gameObjects.Length > 0)
            {
                foreach (GameObject go in gameObjects)
                {
                    UpgradeGameObject(go);
                }
            }
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Materials to 2D Renderer Materials", true)]
        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Scene Materials to 2D Renderer Materials", true)]
        static bool MenuValidation()
        {
            return Light2DEditorUtility.IsUsing2DRenderer();
        }

        public static void UpgradeParametricLightsInScene(bool prompt)
        {
            if (prompt)
            {
                if (!EditorUtility.DisplayDialog("Parametric Light Upgrader", "The upgrade will change all game objects which use Parametric Light2D to Freeform Light2D in currently open scene(s). You can't undo this operation. Make sure you save the scene(s) before proceeding.", "Proceed", "Cancel"))
                    return;
            }

            GameObject[] gameObjects = Object.FindObjectsOfType<GameObject>();
            if (gameObjects != null && gameObjects.Length > 0)
            {
                foreach (GameObject go in gameObjects)
                {
                    UpgradeObjectWithParametricLights(go);
                }
            }
        }

        public static void UpgradeParametricLightsInProject(bool prompt = true)
        {
            if (prompt)
            {
                if (!EditorUtility.DisplayDialog("Parametric Light Upgrader", "The upgrade will search for all prefabs in your project that use Parametric Light2D and change them to Freeform Light2D. You can't undo this operation. It's highly recommended to backup your project before proceeding.", "Proceed", "Cancel"))
                    return;
            }

            ProcessAssetDatabaseObjects<GameObject>("t: Prefab", UpgradeObjectWithParametricLights);
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }

        // Set priority to get these to the bottom. Add test for pipeline enabled

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Project Parametric Lights to Freeform", false)]
        public static void UpgradeParametricLightsInProject()
        {
            UpgradeParametricLightsInProject(true);
        }

        [MenuItem("Edit/Render Pipeline/Universal Render Pipeline/Upgrade Scene Parametric Lights to Freeform", false)]
        public static void UpgradeParametricLightsInScene()
        {
            UpgradeParametricLightsInScene(true);
        }
    }
}
