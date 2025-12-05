using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEditor.AssetDatabase;

namespace UnityEditor.Rendering.Universal
{
    internal abstract class Base2DMaterialUpgrader : RenderPipelineConverter
    {
        public const string k_PackageMaterialsPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/";

        public struct MaterialConversionInfo
        {
            private Material  m_OldMaterial;
            private Material  m_NewMaterial;
            private string    m_OldMaterialId;

            public Material oldMaterial => m_OldMaterial;
            public Material newMaterial => m_NewMaterial;
            public string oldMaterialId => m_OldMaterialId;

            public MaterialConversionInfo(Material oldMaterial, Material newMaterial)
            {
                m_OldMaterial = oldMaterial;
                m_NewMaterial = newMaterial;
                m_OldMaterialId = URP2DConverterUtility.GetObjectIDString(oldMaterial);
            }
        }

        public struct ShaderConversionInfo
        {
            public Shader oldShader;
            public Shader newShader;
        }
        List<string> m_AssetsToConvert = new List<string>();

        MaterialConversionInfo[] m_MaterialConversionInfo;        

        public Material GetSpriteDefaultMaterial()
        {
            // Note: functions here are shortened versions using static AssetDatabase
            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if (data != null)
                return data.GetDefaultMaterial(DefaultMaterialType.Sprite);
            else
                return LoadAssetAtPath<Material>(k_PackageMaterialsPath + "Sprite-Lit-Default.mat");
        }

        public abstract MaterialConversionInfo[] InitializeMaterialConversionInfo();


        Material ReplaceMaterial(Material currentMaterial)
        {
            foreach(MaterialConversionInfo info in m_MaterialConversionInfo)
            {
                if (currentMaterial == info.oldMaterial)
                    return info.newMaterial;
            }

            return currentMaterial;
        }

        void UpgradeGameObject(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (!PrefabUtility.IsPartOfPrefabInstance(renderer))
                {
                    int materialCount = renderer.sharedMaterials.Length;
                    Material[] newMaterials = new Material[materialCount];
                    bool updateMaterials = false;

                    for (int matIndex = 0; matIndex < materialCount; matIndex++)
                    {
                        newMaterials[matIndex] = ReplaceMaterial(renderer.sharedMaterials[matIndex]);
                        updateMaterials |= newMaterials[matIndex] != renderer.sharedMaterials[matIndex];
                    }

                    if (updateMaterials)
                        renderer.sharedMaterials = newMaterials;
                }
            }
        }

        void UpgradeMaterial(Material material)
        {
            for (int i = 0; i < m_MaterialConversionInfo.Length; i++)
            {
                if (material.shader == m_MaterialConversionInfo[i].oldMaterial.shader)
                {
                    material.shader = m_MaterialConversionInfo[i].newMaterial.shader;
                    break;
                }
            }
        }

        string[] GetAllMaterialConversionIds()
        {
            string[] materialIds = new string[m_MaterialConversionInfo.Length];
            for (int i = 0; i < m_MaterialConversionInfo.Length; i++)
                materialIds[i] = m_MaterialConversionInfo[i].oldMaterialId;

            return materialIds;
        }

        string[] GetAllShaderConversionIds()
        {
            string[] shaderIds = new string[m_MaterialConversionInfo.Length];
            for (int i = 0; i < m_MaterialConversionInfo.Length; i++)
                shaderIds[i] = URP2DConverterUtility.GetObjectIDString(m_MaterialConversionInfo[i].oldMaterial.shader);
            return shaderIds;
        }

        public override void OnInitialize(InitializeConverterContext context, Action callback)
        {
            m_MaterialConversionInfo = InitializeMaterialConversionInfo();

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            string[] allMaterialConversionIds = GetAllMaterialConversionIds();
            string[] allShaderConversionIds = GetAllShaderConversionIds();

            foreach (string path in allAssetPaths)
            {
                if (URP2DConverterUtility.IsPSB(path) ||  URP2DConverterUtility.IsMaterialPath(path, allShaderConversionIds) || URP2DConverterUtility.IsPrefabOrScenePath(path, allMaterialConversionIds))
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
            else if (ext == ".mat")
                URP2DConverterUtility.UpgradeMaterial(context.item.descriptor.info, UpgradeMaterial);
            else if (ext == ".psb" || ext == ".psd")
                result = URP2DConverterUtility.UpgradePSB(context.item.descriptor.info);

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
