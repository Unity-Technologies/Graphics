using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal.Converters;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Diagnostics;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.Universal
{
    internal sealed class UniversalRenderPipeline2DMaterialUpgrader : RenderPipelineConverter
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

        bool DoesFileContainString(string path, string str)
        {
            if (str != null)
            {
                string file = File.ReadAllText(path);
                return file.Contains(str);
            }

            return false;
        }

        bool IsMaterialPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if(path.EndsWith(".mat"))
                return DoesFileContainString(path, m_SpritesDefaultShaderId);

            return false;
        }

        bool IsPrefabOrScenePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if(path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".mat"))
                return DoesFileContainString(path, m_SpritesDefaultMatId);

            return false;
        }

        void UpgradeGameObject(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
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

                if(updateMaterials)
                    renderer.sharedMaterials = newMaterials;
            }
        }

        void UpgradePrefab(string path)
        {
            UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int objIndex = 0; objIndex < objects.Length; objIndex++)
            {
                GameObject go = objects[objIndex] as GameObject;
                if (go != null)
                {
                    UpgradeGameObject(go);
                }
            }

            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }

        void UpgradeScene(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            GameObject[] gameObjects = scene.GetRootGameObjects();
            foreach(GameObject go in gameObjects)
                UpgradeGameObject(go);
            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }

        void UpgradeMaterial(string path)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material.shader == m_SpritesDefaultShader)
                material.shader = m_SpriteLitDefaultShader;
        }

        string GetObjectIDString(UnityEngine.Object obj)
        {
            string guid;
            long localId;
            if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj.GetInstanceID(), out guid, out localId))
                return "fileID: " + localId + ", guid: " + guid;

            return null;
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
            m_SpritesDefaultShaderId = GetObjectIDString(m_SpritesDefaultShader);
            m_SpritesDefaultMatId = GetObjectIDString(m_SpritesDefaultMat);

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
        }

        public override void OnClicked(int index)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Material>(m_AssetsToConvert[index]));
        }

        public override void OnPostRun()
        {
            UnityEngine.Debug.Log("OnPostRun");
            AssetDatabase.SaveAssets();
            Resources.UnloadUnusedAssets();
        }
    }
}
