using System;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    /// <summary>
    /// A place to store any tools for testing and tech-art needs
    /// </summary>
    public class Tools
    {
        [MenuItem("Tests/Shader Graph/Generate All Nodes Graph", false)]
        static void GenerateAllNodesGraph()
        {
            string testAssetPath = $"Assets\\AllNodes.{ShaderGraphStencil.GraphExtension}";
            var graphWindowTest = new BaseGraphWindowTest();
            var window = graphWindowTest.CreateWindow();

            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateGraphAssetAction>();
            newGraphAction.Action(0, testAssetPath, "");
            var graphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(testAssetPath);

            if (graphAsset != null)
            {
                window.GraphTool.Dispatch(new LoadGraphCommand(graphAsset.GraphModel));
                window.GraphTool.Update();
            }

            static int RoundOff(int i)
            {
                return ((int)Math.Round(i / 10.0)) * 10;
            }

            int Mod(int a, int n) => (a % n + n) % n;

            if (window.GraphView.GraphModel is ShaderGraphModel shaderGraphModel)
            {
                var shaderGraphStencil = shaderGraphModel.Stencil as ShaderGraphStencil;
                var searcherDatabaseProvider = new ShaderGraphSearcherDatabaseProvider(shaderGraphStencil);
                var searcherDatabases = searcherDatabaseProvider.GetGraphElementsDatabases(shaderGraphModel);
                foreach (var database in searcherDatabases.Skip(1))
                {
                    var allNodes = database.Search("");
                    var roundedSize = RoundOff(allNodes.Count);

                    var gridWidth = roundedSize / 10;
                    var yIndex = 0;
                    for (var i = 0; i < allNodes.Count; ++i)
                    {
                        var searcherItem = allNodes[i];
                        var xIndex = Mod(i, gridWidth);
                        var position = new Vector2(500 * xIndex, yIndex * 500);
                        if (xIndex == gridWidth - 1)
                            yIndex++;
                        var createNodeCommand = new CreateNodeCommand().WithNodeOnGraph(searcherItem as GraphNodeModelLibraryItem, position);
                        window.GraphView.Dispatch(createNodeCommand);

                        window.GraphView.DispatchFrameAllCommand();
                    }
                }
            }

            // Save asset
            ShaderGraphAssetUtils.HandleSave(testAssetPath, graphAsset);
        }

        [MenuItem("Tests/Shader Graph/Create Texture 2D Array")]
        static void CreateTexture2DArray()
        {
            var texture1 = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Shaders/Tests/Textures/bone_02.png", typeof(Texture2D));
            var texture2 = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Shaders/Tests/Textures/cobblestone_d.tga", typeof(Texture2D));

            var textures = new [] {texture1, texture2};

            var array = new Texture2DArray(textures[0].width, textures[0].height, textures.Length, textures[0].format, false);
            for (int i = 0; i < textures.Length; i++)
                array.SetPixels(textures[i].GetPixels(), i);

            array.Apply();
            AssetDatabase.CreateAsset(array, "Assets/TextureArray.asset");
        }

        [MenuItem("Tests/Shader Graph/Create Texture 3D")]
        static void CreateTexture3D()
        {
            // Configure the texture
            int size = 32;
            TextureFormat format = TextureFormat.RGBA32;
            TextureWrapMode wrapMode =  TextureWrapMode.Clamp;

            // Create the texture and apply the configuration
            Texture3D texture = new Texture3D(size, size, size, format, false);
            texture.wrapMode = wrapMode;

            // Create a 3-dimensional array to store color data
            Color[] colors = new Color[size * size * size];

            // Populate the array so that the x, y, and z values of the texture will map to red, blue, and green colors
            float inverseResolution = 1.0f / (size - 1.0f);
            for (int z = 0; z < size; z++)
            {
                int zOffset = z * size * size;
                for (int y = 0; y < size; y++)
                {
                    int yOffset = y * size;
                    for (int x = 0; x < size; x++)
                    {
                        colors[x + yOffset + zOffset] = new Color(x * inverseResolution,
                            y * inverseResolution, z * inverseResolution, 1.0f);
                    }
                }
            }

            // Copy the color values to the texture
            texture.SetPixels(colors);

            // Apply the changes to the texture and upload the updated texture to the GPU
            texture.Apply();

            // Save the texture to your Unity Project
            AssetDatabase.CreateAsset(texture, "Assets/Texture3D.asset");
        }
    }
}
