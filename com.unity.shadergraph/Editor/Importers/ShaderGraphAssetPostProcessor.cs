using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.ShaderGraph.Drawing;
using Unity.Collections;

namespace UnityEditor.ShaderGraph
{
    class ShaderGraphAssetPostProcessor : AssetPostprocessor
    {
        static void RegisterShaders(string[] paths)
        {
            foreach (var path in paths)
            {
                if (!path.EndsWith(ShaderGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var mainObj = AssetDatabase.LoadMainAssetAtPath(path);
                if (mainObj is Shader)
                    ShaderUtil.RegisterShader((Shader)mainObj);

                var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
                foreach (var obj in objs)
                {
                    if (obj is Shader)
                        ShaderUtil.RegisterShader((Shader)obj);
                }
            }
        }

        static void UpdateAfterAssetChange(string[] newNames)
        {
            // This will change the title of the window.
            MaterialGraphEditWindow[] windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
            foreach (var matGraphEditWindow in windows)
            {
                for (int i = 0; i < newNames.Length; ++i)
                {
                    if (matGraphEditWindow.selectedGuid == AssetDatabase.AssetPathToGUID(newNames[i]))
                        matGraphEditWindow.assetName = Path.GetFileNameWithoutExtension(newNames[i]).Split('/').Last();
                }
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            RegisterShaders(importedAssets);

            bool anyShaders = movedAssets.Any(val => val.EndsWith(ShaderGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase));
            anyShaders |= movedAssets.Any(val => val.EndsWith(ShaderSubGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase));
            if (anyShaders)
                UpdateAfterAssetChange(movedAssets);
            
            var changedFiles = movedAssets.Union(importedAssets)
                .Where(x => x.EndsWith(ShaderSubGraphImporter.Extension, StringComparison.InvariantCultureIgnoreCase)
                || CustomFunctionNode.s_ValidExtensions.Contains(Path.GetExtension(x)))
                .Select(AssetDatabase.AssetPathToGUID)
                .Distinct()
                .ToList();

            if (changedFiles.Count > 0)
            {
                var windows = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
                foreach (var window in windows)
                {
                    window.ReloadSubGraphsOnNextUpdate(changedFiles);
                }
            }
        }

        void PostProcessSkinnedMeshes(GameObject g)
        {
            // Only operate on FBX files
            if (assetPath.IndexOf(".fbx") == -1) {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            // only post process models with greater than 4 bones
            if((importer.maxBonesPerVertex > 4) && (importer.maxBonesPerVertex < 9))
            {            
                SkinnedMeshRenderer[] skinnedMeshRenderers = g.GetComponentsInChildren<SkinnedMeshRenderer>();
                if(skinnedMeshRenderers != null)
                {
                    // add extra bone indices/weights to uv channels 4 and 5, to be used with linear blend skinning shader graph node
                    for(int skinnedMeshRendererIndex = 0; skinnedMeshRendererIndex < skinnedMeshRenderers.Length; skinnedMeshRendererIndex++)
                    {               
                        Mesh mesh = skinnedMeshRenderers[skinnedMeshRendererIndex].sharedMesh;
                        NativeArray<BoneWeight1> boneWeights = mesh.GetAllBoneWeights();
                        NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();                   
                        Vector4[] newUV4 = new Vector4[mesh.vertexCount];
                        Vector4[] newUV5 = new Vector4[mesh.vertexCount];
                        int boneWeightsOffest = 0;
                        for(int vertexIndex = 0; vertexIndex < mesh.vertexCount; vertexIndex++)
                        {               
                            newUV4[vertexIndex] = Vector4.zero;
                            newUV5[vertexIndex] = Vector4.zero;
                            for(int boneIndex = 4; boneIndex < bonesPerVertex[vertexIndex]; boneIndex++)
                            {
                                switch(boneIndex)
                                {
                                    case 4:
                                        newUV4[vertexIndex].x = boneWeights[boneWeightsOffest + boneIndex].boneIndex;  
                                        newUV5[vertexIndex].x = boneWeights[boneWeightsOffest + boneIndex].weight;  
                                        break;
                                    case 5:
                                        newUV4[vertexIndex].y = boneWeights[boneWeightsOffest + boneIndex].boneIndex;  
                                        newUV5[vertexIndex].y = boneWeights[boneWeightsOffest + boneIndex].weight;  
                                        break;
                                    case 6:
                                        newUV4[vertexIndex].z = boneWeights[boneWeightsOffest + boneIndex].boneIndex;  
                                        newUV5[vertexIndex].z = boneWeights[boneWeightsOffest + boneIndex].weight;  
                                        break;
                                    case 7:
                                        newUV4[vertexIndex].w = boneWeights[boneWeightsOffest + boneIndex].boneIndex;  
                                        newUV5[vertexIndex].w = boneWeights[boneWeightsOffest + boneIndex].weight;  
                                        break;
                                }
                            }
                            boneWeightsOffest += bonesPerVertex[vertexIndex];
                        }                  
                        mesh.SetUVs(4, newUV4);
                        mesh.SetUVs(5, newUV5);
                        mesh.UploadMeshData(false);
                    }
                }
            }
        }

        void OnPostprocessModel(GameObject g)
        {
            PostProcessSkinnedMeshes(g);            
        }
    }
}
