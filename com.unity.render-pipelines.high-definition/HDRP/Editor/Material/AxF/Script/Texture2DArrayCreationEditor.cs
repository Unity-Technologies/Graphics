using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // This Editor window is a quick way to generate Texture2DArrays for the AxF material
    // It will take a sourceTexture as well as the size of a single slice and automatically determine how many slices are needed
    // NOTE: Expects slices packed along X dimension first, then Y...
    //
    public class Texture2DArrayCreationEditor : EditorWindow, IComparer<Texture2D>
    {
        private Texture2D   m_sourceTexture = null;
        private string      m_sourcePath = null;

        [Range(1, 1024)]
        private int         m_sliceWidth = 480;
        [Range(1, 1024)]
        private int         m_sliceHeight = 480;

        private Texture2D[] m_textureWithMips = null;

        private int     slicesCountX
        {
            get { return m_sourceTexture != null ? m_sourceTexture.width / m_sliceWidth : 0; }
            set {}
        }

        private int     slicesCountY
        {
            get { return m_sourceTexture != null ? m_sourceTexture.height / m_sliceHeight : 0; }
            set {}
        }

        // Texture must be valid and have integer amounts of tiles
        private bool    validData
        {
            get { return m_textureWithMips != null && slicesCountX * m_sliceWidth == m_sourceTexture.width && slicesCountY * m_sliceHeight == m_sourceTexture.height; }
            set {}
        }

        [MenuItem("Window/Render Pipeline/Create Texture2DArray")]
        private static void Init()
        {
            Texture2DArrayCreationEditor window = (Texture2DArrayCreationEditor)EditorWindow.GetWindow(typeof(Texture2DArrayCreationEditor));
            window.titleContent.text = "Create Texture2DArray Asset";
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.miniButton);
            GUILayout.Button(new GUIContent(" Create Texture2DArray Asset", ""), EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("Source Texture");

//            EditorGUI.BeginChangeCheck();
            m_sourceTexture = (Texture2D)EditorGUILayout.ObjectField(m_sourceTexture, typeof(Texture2D), false);
//            if ( EditorGUI.EndChangeCheck() )
            {
                // Rebuild textures list with mips
                m_sourcePath = null;
                m_textureWithMips = null;
                if (m_sourceTexture != null)
                {
                    m_sourcePath = AssetDatabase.GetAssetPath(m_sourceTexture);

                    // Build
                    List<Texture2D> textures = new List<Texture2D>();
                    textures.Add(m_sourceTexture);

                    // Find mips
                    string      baseDirectory = System.IO.Path.GetDirectoryName(m_sourcePath);
                    string      strippedAssetName = System.IO.Path.GetFileNameWithoutExtension(m_sourcePath);
                    string[]    mipGUIDs = AssetDatabase.FindAssets(strippedAssetName + "_mip", new string[] { baseDirectory });

                    string  bisou = "\n";
                    foreach (string mipGUID in mipGUIDs)
                    {
                        string      mipFileName = AssetDatabase.GUIDToAssetPath(mipGUID);
                        Texture2D   mip = AssetDatabase.LoadAssetAtPath(mipFileName, typeof(Texture2D)) as Texture2D;
                        //                       bisou += "attempting to load " + mipFileName + "\n";
                        if (mip != null)
                        {
                            textures.Add(mip);
//                            bisou += "SUCCESS!\n";
                        }
//                        else
//                            bisou += "FAILED!\n";
                    }

                    textures.Sort(this);

//                     foreach ( Texture2D t in textures ) {
//                         bisou += t.name + " " + t.width + "x" + t.height + "\n";
//                     }

//                    EditorGUILayout.HelpBox( "Source Path: " + m_sourcePath + "\nDirectory Name: " + baseDirectory + "\nFile Name: " + strippedAssetName + "\nAsset Name: " + m_sourceTexture.name, MessageType.Info );
                    EditorGUILayout.HelpBox("Source Path: " + m_sourcePath + "\nAdditional Mip Assets Detected: " + mipGUIDs.Length + "\nTotal Mips Count: " + textures.Count + bisou, MessageType.Info);

                    m_textureWithMips = textures.ToArray();
                }
            }

//            EditorGUILayout.HelpBox(String.Format("Volumetric system requires textures of size {0}x{0}x{0} so please ensure the source texture is at least this many pixels.", tileSize), MessageType.Info);

            EditorGUILayout.LabelField("Slice Width");
            m_sliceWidth = EditorGUILayout.IntField(m_sliceWidth);

            EditorGUILayout.LabelField("Slice Height");
            m_sliceHeight = EditorGUILayout.IntField(m_sliceHeight);

            EditorGUILayout.Separator();

            if (validData)
            {
                if (GUILayout.Button(new GUIContent("Generate 2D Texture Array", ""), EditorStyles.toolbarButton))
                {
                    GenerateTexture();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Invalid Source Texture: slice dimensions must fit integrally into texture dimensions.", MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private void GenerateTexture()
        {
            int slicesCount = slicesCountX * slicesCountY;
            Texture2DArray texture = new Texture2DArray(m_sliceWidth, m_sliceHeight, slicesCount, m_sourceTexture.format, true, false);
            texture.wrapMode = m_sourceTexture.wrapMode;
            texture.wrapModeU = m_sourceTexture.wrapModeU;
            texture.wrapModeV = m_sourceTexture.wrapModeV;
            texture.wrapModeW = m_sourceTexture.wrapModeW;
            texture.filterMode = m_sourceTexture.filterMode;
            texture.mipMapBias = 0;
            texture.anisoLevel = 0;

            int sliceWidth = m_sliceWidth;
            int sliceHeight = m_sliceHeight;
            for (int mipIndex = 0; mipIndex < m_textureWithMips.Length; mipIndex++)
            {
                Texture2D mip = m_textureWithMips[mipIndex];
                for (int Y = 0; Y < slicesCountY; Y++)
                {
                    for (int X = 0; X < slicesCountX; X++)
                    {
                        Color[] texColor = mip.GetPixels(X * sliceWidth, (slicesCountY - 1 - Y) * sliceHeight, sliceWidth, sliceHeight);  // WARNING! Y is reversed here! First slices are at the top of the image!
                        texture.SetPixels(texColor, slicesCountX * Y + X, mipIndex);
                    }
                }

                // Next mip
                sliceWidth = Mathf.Max(1, sliceWidth >> 1);
                sliceHeight = Mathf.Max(1, sliceHeight >> 1);
            }

            texture.Apply();

            AssetDatabase.CreateAsset(texture, System.IO.Directory.GetParent(m_sourcePath) + "\\" + m_sourceTexture.name + "_Texture2DArray.asset");
        }

        public int Compare(Texture2D x, Texture2D y)
        {
            if (x == y)
                return 0;
            else if (x == null)
                return 1;
            else if (y == null)
                return -1;

            int areaX = x.width * x.height;
            int areaY = y.width * y.height;
            return areaX > areaY ? -1 : (areaX < areaY ? +1 : 0);
        }
    }
}
