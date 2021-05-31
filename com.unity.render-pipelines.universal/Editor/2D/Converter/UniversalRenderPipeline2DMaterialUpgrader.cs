using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal.Converters;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Diagnostics;
using UnityEditor.SceneManagement;


namespace UnityEditor.Rendering.Universal
{
    internal sealed class UniversalRenderPipeline2DMaterialUpgrader : RenderPipelineConverter
    {
        const string k_SpriteDefaultShaderId = "fileID: 10753, guid: 0000000000000000f000000000000000";
        const string k_SpriteDefaultMatId = "fileID: 10754, guid: 0000000000000000f000000000000000";

        public override string name => "Material Upgrade";
        public override string info => "This will upgrade your materials.";
        public override int priority => - 1000;
        public override Type container => typeof(BuiltInToURP2DConverterContainer);

        List<string> m_AssetsToConvert = new List<string>();

        Material m_SpriteLitDefaultMat;
        Material m_SpritesDefaultMat;
        Shader   m_SpriteLitDefaultShader;

        bool DoesFileContainString(string path, string str)
        {
            string file = File.ReadAllText(path);
            return file.Contains(str);
        }

        bool IsMaterialPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if(path.EndsWith(".mat"))
                return DoesFileContainString(path, k_SpriteDefaultShaderId);

            return false;
        }

        bool IsPrefabOrScenePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if(path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".mat"))
                return DoesFileContainString(path, k_SpriteDefaultMatId);

            return false;
        }

        void UpgradePrefab(string path)
        {
            UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int objIndex = 0; objIndex < objects.Length; objIndex++)
            {
                GameObject go = objects[objIndex] as GameObject;
                if (go != null)
                {
                    Renderer renderer = go.GetComponent<Renderer>();
                    int materialCount = renderer.sharedMaterials.Length;
                    Material[] newMaterials = new Material[materialCount];

                    for (int matIndex = 0; matIndex < materialCount; matIndex++)
                    {
                        if (renderer.sharedMaterials[matIndex] == m_SpritesDefaultMat)
                            newMaterials[matIndex] = m_SpriteLitDefaultMat;
                        else
                            newMaterials[matIndex] = renderer.sharedMaterials[matIndex];
                    }

                    renderer.sharedMaterials = newMaterials;
                }
            }

            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }

        void UpgradeScene(string path)
        {
            //SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            //EditorSceneManagement.
        }

        void UpgradeMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        }

        public override void OnInitialize(InitializeConverterContext context, Action calback)
        {
            //string spriteDefaultGUID;
            //long spriteDefaultLocalId;
            //Material mat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            //AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat.GetInstanceID(), out spriteDefaultGUID, out spriteDefaultLocalId);

            Renderer2DData data = Light2DEditorUtility.GetRenderer2DData();
            if(data != null)
                m_SpriteLitDefaultMat = data.GetDefaultMaterial(DefaultMaterialType.Sprite);
            else
                m_SpriteLitDefaultMat = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

            m_SpritesDefaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            
            Stopwatch stopwatch = Stopwatch.StartNew();
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
            stopwatch.Stop();

            UnityEngine.Debug.Log("Initialization Time (ms): " + stopwatch.ElapsedMilliseconds);

            calback.Invoke();
        }

        public override void OnRun(ref RunItemContext context)
        {
            string ext = Path.GetExtension(context.item.descriptor.info);
            if (ext == ".prefab")
                UpgradePrefab(context.item.descriptor.info);
            else if (ext == ".unity")
                UpgradeScene(context.item.descriptor.info);
            else if(ext == ".mat")
                UpgradeMaterial(context.item.descriptor.info);

            //if(ext == ".mat")

            //    Material mat = AssetDatabase.LoadAssetAtPath<Material>(context.item.descriptor.info);
        }

        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Material>(m_AssetsToConvert[index]));
        }

        public override void OnPostRun()
        {
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }
    }
}
