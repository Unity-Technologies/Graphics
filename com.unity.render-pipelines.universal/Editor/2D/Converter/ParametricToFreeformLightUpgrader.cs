using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor.SceneManagement;

namespace UnityEditor.Rendering.Universal
{
    internal sealed class ParametricToFreeformLightUpgrader : RenderPipelineConverter
    {
        const float k_EnscribedSquareDiagonalLength = 0.70710678118654752440084436210485f;

        public override string name => "Parametric to Freeform Light Upgrade";
        public override string info => "This will upgrade all parametric lights to freeform lights.";
        public override int priority => -1000;
        public override Type container => typeof(UpgradeURP2DAssetsContainer);

        List<string> m_AssetsToConvert = new List<string>();

        string m_Light2DId;

        public static void UpgradeParametricLight(Light2D light)
        {
            if (light.lightType == (Light2D.LightType)Light2D.DeprecatedLightType.Parametric)
            {
                light.lightType = Light2D.LightType.Freeform;

                // Parametric radius has to be > 0 in order mesh tessellation to be valid
                if (light.shapeLightParametricRadius == 0)
                    light.shapeLightParametricRadius = 0.01f;

                float radius = light.shapeLightParametricRadius;
                float angle = light.shapeLightParametricAngleOffset;
                int sides = light.shapeLightParametricSides;

                var angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
                if (sides < 3)
                {
                    radius = k_EnscribedSquareDiagonalLength * radius;
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
                light.UpdateMesh();
                EditorSceneManager.MarkSceneDirty(light.gameObject.scene);
            }
        }

        void UpgradeGameObject(GameObject go)
        {
            Light2D[] lights = go.GetComponentsInChildren<Light2D>();
            foreach (Light2D light in lights)
            {
                if (light.lightType == Light2D.LightType.Parametric && !PrefabUtility.IsPartOfPrefabInstance(light))
                    UpgradeParametricLight(light);
            }
        }

        public override void OnInitialize(InitializeConverterContext context, Action callback)
        {
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

            foreach (string path in allAssetPaths)
            {
                if (URP2DConverterUtility.IsPrefabOrScenePath(path, "m_LightType: 0"))
                {
                    ConverterItemDescriptor desc = new ConverterItemDescriptor()
                    {
                        name = Path.GetFileNameWithoutExtension(path),
                        info = path,
                        warningMessage = String.Empty,
                        helpLink = String.Empty
                    };

                    // Each converter needs to add this info using this API.
                    m_AssetsToConvert.Add(path);
                    context.AddAssetToConvert(desc);
                }
            }

            callback.Invoke();
        }

        public override void OnRun(ref RunItemContext context)
        {
            string result = string.Empty;
            string ext = Path.GetExtension(context.item.descriptor.info);
            if (ext == ".prefab")
                result = URP2DConverterUtility.UpgradePrefab(context.item.descriptor.info, UpgradeGameObject);
            else if (ext == ".unity")
                URP2DConverterUtility.UpgradeScene(context.item.descriptor.info, UpgradeGameObject);

            if (result != string.Empty)
            {
                context.didFail = true;
                context.info = result;
            }
            else
            {
                context.hasConverted = true;
            }
        }

        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(m_AssetsToConvert[index]));
        }

        public override void OnPostRun()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
