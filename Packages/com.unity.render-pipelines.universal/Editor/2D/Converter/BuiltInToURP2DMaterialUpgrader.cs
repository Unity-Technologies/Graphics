using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;


namespace UnityEditor.Rendering.Universal
{
    internal sealed class BuiltInToURP2DMaterialUpgrader : RenderPipelineConverter
    {
        public override string name => "Material and Material Reference Upgrade";
        public override string info => "This will upgrade all materials and material references.";
        public override int priority => -1000;
        public override Type container => typeof(BuiltInToURP2DConverterContainer);

        List<string> m_AssetsToConvert = new List<string>();

        Material m_SpriteLitDefaultMat;
        Material m_SpritesDefaultMat;
        Material m_SpritesMaskMat;
        Material m_SpriteMaskDefaultMat;
        Shader m_SpriteLitDefaultShader;
        Shader m_SpritesDefaultShader;

        string m_SpritesDefaultShaderId;
        string m_SpritesDefaultMatId;
        string m_SpritesMaskMatId;

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
                        else if (renderer.sharedMaterials[matIndex] == m_SpritesMaskMat)
                        {
                            newMaterials[matIndex] = m_SpriteMaskDefaultMat;
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

        public override void OnInitialize(InitializeConverterContext context, Action callback)
        {
            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if (data != null)
                m_SpriteLitDefaultMat = data.GetDefaultMaterial(DefaultMaterialType.Sprite);
            else
                m_SpriteLitDefaultMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

            m_SpritesDefaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            m_SpriteMaskDefaultMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/SpriteMask-Default.mat");
            m_SpritesMaskMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Mask.mat");
            m_SpriteLitDefaultShader = m_SpriteLitDefaultMat.shader;
            m_SpritesDefaultShader = m_SpritesDefaultMat.shader;
            m_SpritesDefaultShaderId = URP2DConverterUtility.GetObjectIDString(m_SpritesDefaultShader);
            m_SpritesDefaultMatId = URP2DConverterUtility.GetObjectIDString(m_SpritesDefaultMat);
            m_SpritesMaskMatId = URP2DConverterUtility.GetObjectIDString(m_SpritesMaskMat);

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();

            foreach (string path in allAssetPaths)
            {
                if (URP2DConverterUtility.IsPSB(path) ||  URP2DConverterUtility.IsMaterialPath(path, m_SpritesDefaultShaderId) || URP2DConverterUtility.IsPrefabOrScenePath(path, new string[] { m_SpritesDefaultMatId, m_SpritesMaskMatId }))
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
                URP2DConverterUtility.UpgradeMaterial(context.item.descriptor.info, m_SpritesDefaultShader, m_SpriteLitDefaultShader);
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
