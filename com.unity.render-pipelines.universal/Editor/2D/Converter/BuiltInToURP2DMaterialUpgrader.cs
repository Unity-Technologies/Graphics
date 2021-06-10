using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal.Converters;
using UnityEngine;
using UnityEngine.Rendering.Universal;


namespace UnityEditor.Rendering.Universal
{
    internal sealed class BuiltInToURP2DMaterialUpgrader : RenderPipelineConverter
    {
        public override string name => "Material and Material Reference Upgrade";
        public override string info => "This will upgrade your materials and all material references.";
        public override int priority => - 1000;
        public override Type container => typeof(BuiltInToURP2DConverterContainer);

        List<string> m_AssetsToConvert = new List<string>();

        Material m_SpriteLitDefaultMat;
        Material m_SpritesDefaultMat;
        Shader   m_SpriteLitDefaultShader;
        Shader   m_SpritesDefaultShader;

        string m_SpritesDefaultShaderId;
        string m_SpritesDefaultMatId;


        bool IsMaterialPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if(path.EndsWith(".mat"))
                return URP2DConverterUtility.DoesFileContainString(path, m_SpritesDefaultShaderId);

            return false;
        }

        bool IsPrefabOrScenePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            if(path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".mat"))
                return URP2DConverterUtility.DoesFileContainString(path, m_SpritesDefaultMatId);

            return false;
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
                        if (renderer.sharedMaterials[matIndex] == m_SpritesDefaultMat)
                        {
                            newMaterials[matIndex] = m_SpriteLitDefaultMat;
                            updateMaterials = true;
                        }
                        else
                            newMaterials[matIndex] = renderer.sharedMaterials[matIndex];
                    }

                    if (updateMaterials)
                        renderer.sharedMaterials = newMaterials;
                }
            }
        }

        public override void OnInitialize(InitializeConverterContext context, Action calback)
        {
            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if(data != null)
                m_SpriteLitDefaultMat = data.GetDefaultMaterial(DefaultMaterialType.Sprite);
            else
                m_SpriteLitDefaultMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

            m_SpritesDefaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            m_SpriteLitDefaultShader = m_SpriteLitDefaultMat.shader;
            m_SpritesDefaultShader = m_SpritesDefaultMat.shader;
            m_SpritesDefaultShaderId = URP2DConverterUtility.GetObjectIDString(m_SpritesDefaultShader);
            m_SpritesDefaultMatId = URP2DConverterUtility.GetObjectIDString(m_SpritesDefaultMat);

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            
            foreach (string path in allAssetPaths)
            {
                if (IsMaterialPath(path) || IsPrefabOrScenePath(path))
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

            calback.Invoke();
        }

        public override void OnRun(ref RunItemContext context)
        {
            string ext = Path.GetExtension(context.item.descriptor.info);
            if (ext == ".prefab")
                URP2DConverterUtility.UpgradePrefab(context.item.descriptor.info, UpgradeGameObject);
            else if (ext == ".unity")
                URP2DConverterUtility.UpgradeScene(context.item.descriptor.info, UpgradeGameObject);
            else if(ext == ".mat")
                URP2DConverterUtility.UpgradeMaterial(context.item.descriptor.info, m_SpritesDefaultShader, m_SpriteLitDefaultShader);
        }

        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Material>(m_AssetsToConvert[index]));
        }

        public override void OnPostRun()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
