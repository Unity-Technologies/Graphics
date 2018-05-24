using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    //This Editor window is a quick way to generate 3D Textures for the Volumetric system.
    //It will take a sourceTexture and slice it up into tiles that will fill a 3D Texture
    //The volumetric system has a hardcoded size of 32x32x32 volume texture atlas so this tool makes sure it fits that size.
    public class Texture3DCreationEditor : EditorWindow
    {
        private Texture2D sourceTexture = null;
        private string sourcePath = null;

        private int tileSize = DensityVolumeManager.volumeTextureSize;

        private int numXTiles
        {
            get { return sourceTexture != null ? sourceTexture.width / tileSize : 0; }
            set {}
        }

        private int numYTiles
        {
            get { return sourceTexture != null ? sourceTexture.height / tileSize : 0; }
            set {}
        }

        private bool validData
        {
            get { return numXTiles * numYTiles >= tileSize; }
            set {}
        }

        [MenuItem("Window/Render Pipeline/Create 3D Texture")]
        private static void Init()
        {
            Texture3DCreationEditor window = (Texture3DCreationEditor)EditorWindow.GetWindow(typeof(Texture3DCreationEditor));
            window.titleContent.text = "Create Texture3D Asset";
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.miniButton);
            GUILayout.Button(new GUIContent(" Create Texture3D Asset", ""), EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Source Texture");
            sourceTexture = (Texture2D)EditorGUILayout.ObjectField(sourceTexture, typeof(Texture2D), false);
            EditorGUILayout.HelpBox(String.Format("Volumetric system requires textures of size {0}x{0}x{0} so please ensure the source texture is at least this many pixels.", tileSize), MessageType.Info);

            EditorGUILayout.Separator();

            if (sourceTexture != null)
            {
                sourcePath = AssetDatabase.GetAssetPath(sourceTexture);
                if (validData)
                {
                    if (GUILayout.Button(new GUIContent("Generate 3D Texture", ""), EditorStyles.toolbarButton))
                    {
                        Generate3DTexture();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Invalid Source Texture: Source texture size is not enough to create " + tileSize + " depthSlices.", MessageType.Error);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void Generate3DTexture()
        {
            Texture3D texture = new Texture3D(tileSize, tileSize, tileSize, sourceTexture.format, false);
            texture.wrapMode = sourceTexture.wrapMode;
            texture.wrapModeU = sourceTexture.wrapModeU;
            texture.wrapModeV = sourceTexture.wrapModeV;
            texture.wrapModeW = sourceTexture.wrapModeW;
            texture.filterMode = sourceTexture.filterMode;
            texture.mipMapBias = 0;
            texture.anisoLevel = 0;

            Color[] colorArray = new Color[0];

            for (int i = numYTiles - 1; i >= 0; --i)
            {
                for (int j = 0; j < numXTiles; ++j)
                {
                    Color[] texColor = sourceTexture.GetPixels(j * tileSize, i * tileSize, tileSize, tileSize);

                    Array.Resize(ref colorArray, texColor.Length + colorArray.Length);
                    Array.Copy(texColor, 0, colorArray, colorArray.Length - texColor.Length, texColor.Length);
                }
            }


            texture.SetPixels(colorArray);
            texture.Apply();

            AssetDatabase.CreateAsset(texture, System.IO.Directory.GetParent(sourcePath) + "\\" + sourceTexture.name + "_Texture3D.asset");
        }
    }
}
