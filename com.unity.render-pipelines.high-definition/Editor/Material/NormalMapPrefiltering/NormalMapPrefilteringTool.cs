using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    public class NormalMapPrefilteringTool : EditorWindow
    {
        enum NormalMapSpace
        {
            TangentSpace = 0,
            ObjectSpace  = 1,
            Count
        };

        enum TextureChannel
        {
            Red   = 0,
            Green = 1,
            Blue  = 2,
            Alpha = 3,
            Count
        };

        static GUIContent   s_WindowTitle                = new GUIContent("Normal Map Prefiltering Tool");
        static GUIContent   s_NormalMapLabel             = new GUIContent("Normal Map");
        static GUIContent   s_NormalMapSpaceLabel        = new GUIContent("Normal Map Space");
        static GUIContent   s_TangentSpaceLabel          = new GUIContent("Tangent Space");
        static GUIContent   s_ObjectSpaceLabel           = new GUIContent("Object Space");
        static GUIContent[] s_NormalMapSpaceOptions      = new GUIContent[]{s_TangentSpaceLabel, s_ObjectSpaceLabel};
        static GUIContent   s_SmoothnessMapLabel         = new GUIContent("Smoothness Map");
        static GUIContent   s_SmoothnessMapChannelLabel  = new GUIContent("Smoothness Map Channel");
        static GUIContent   s_RedChannelLabel            = new GUIContent("Red");
        static GUIContent   s_GreenChannelLabel          = new GUIContent("Green");
        static GUIContent   s_BlueChannelLabel           = new GUIContent("Blue");
        static GUIContent   s_AlphaChannelLabel          = new GUIContent("Alpha");
        static GUIContent[] s_TextureChannelOptions      = new GUIContent[]{s_RedChannelLabel, s_GreenChannelLabel, s_BlueChannelLabel, s_AlphaChannelLabel};
        static GUIContent   s_OutputNormalMapSuffixLabel = new GUIContent("Output Normal Map Suffix");
        static GUIContent   s_GenerateLabel              = new GUIContent("Generate");

        static ComputeShader s_NormalMapPrefilteringCS;

        Texture2D m_NormalMap;     // Bump and height maps must be converted to normal maps
        Texture2D m_SmoothnessMap; // Roughness maps must be converted to smoothness maps

        string m_OutputNormalMapSuffix = "_fnm";

        bool m_GeneratedOnce = false;

        NormalMapSpace m_NormalMapSpace       = NormalMapSpace.TangentSpace;
        TextureChannel m_SmoothnessMapChannel = TextureChannel.Alpha;

        [MenuItem("Window/Rendering/Normal Map Prefiltering Tool")]
        static void Init()
        {
            string fullPath = HDUtils.GetHDRenderPipelinePath() + "Editor/Material/NormalMapPrefiltering/NormalMapPrefiltering.compute";
            s_NormalMapPrefilteringCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(fullPath);
            Debug.Assert(s_NormalMapPrefilteringCS != null);

            var window = GetWindow(typeof(NormalMapPrefilteringTool));
            window.titleContent = s_WindowTitle;
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(s_NormalMapLabel);

            EditorGUI.indentLevel++;
            m_NormalMap = EditorGUILayout.ObjectField(m_NormalMap, typeof(Texture2D), false) as Texture2D;
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(s_NormalMapSpaceLabel);

            EditorGUI.indentLevel++;
            m_NormalMapSpace = (NormalMapSpace)EditorGUILayout.Popup((int)m_NormalMapSpace, s_NormalMapSpaceOptions);
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(s_SmoothnessMapLabel);

            EditorGUI.indentLevel++;
            m_SmoothnessMap = EditorGUILayout.ObjectField(m_SmoothnessMap, typeof(Texture2D), false) as Texture2D;
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(s_SmoothnessMapChannelLabel);

            EditorGUI.indentLevel++;
            m_SmoothnessMapChannel = (TextureChannel)EditorGUILayout.Popup((int)m_SmoothnessMapChannel, s_TextureChannelOptions);
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField(s_OutputNormalMapSuffixLabel);

            EditorGUI.indentLevel++;
            m_OutputNormalMapSuffix = EditorGUILayout.TextField(m_OutputNormalMapSuffix);
            EditorGUI.indentLevel--;

            bool generate = GUILayout.Button(s_GenerateLabel);

            m_GeneratedOnce = m_GeneratedOnce || generate;

            if (m_GeneratedOnce)
            {
                // Basic error checking. Has to run every frame, else the HelpBox does not show up.

                if (!m_NormalMap)
                {
                    EditorGUILayout.HelpBox("Please assign a normal map.", MessageType.Warning);
                    return;
                }

                if ((int)m_NormalMapSpace >= (int)NormalMapSpace.Count)
                {
                    EditorGUILayout.HelpBox("Please select the normal map space.", MessageType.Warning);
                    return;
                }

                if (!m_SmoothnessMap)
                {
                    EditorGUILayout.HelpBox("Please assign a smoothness map.", MessageType.Warning);
                    return;
                }

                if ((int)m_SmoothnessMapChannel >= (int)TextureChannel.Count)
                {
                    EditorGUILayout.HelpBox("Please select the smoothness map channel.", MessageType.Warning);
                    return;
                }

                if (m_OutputNormalMapSuffix == "")
                {
                    EditorGUILayout.HelpBox("Please input the suffix of the output normal map.", MessageType.Warning);
                    return;
                }
            }

            if (generate)
            {
                // No errors, so let's generate the new texture.

                // Domain reload kills static variables. :-(
                if (s_NormalMapPrefilteringCS == null)
                {
                    string fullPath = HDUtils.GetHDRenderPipelinePath() + "Editor/Material/NormalMapPrefiltering/NormalMapPrefiltering.compute";
                    s_NormalMapPrefilteringCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(fullPath);
                }

                // Since both the roughness and the normal map use the same UV set,
                // it is safe to rescale them to the max size.
                int maxWidth  = Math.Max(m_NormalMap.width,  m_SmoothnessMap.width);
                int maxHeight = Math.Max(m_NormalMap.height, m_SmoothnessMap.height);

                RenderTexture prefilteredNormalMap = new RenderTexture(maxWidth, maxHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear); // TODO: reduce the size
                prefilteredNormalMap.enableRandomWrite = true;
                prefilteredNormalMap.useMipMap         = true;
                prefilteredNormalMap.autoGenerateMips  = false;
                prefilteredNormalMap.Create();

                CommandBuffer cmd = new CommandBuffer();
                cmd.SetComputeIntParam(    s_NormalMapPrefilteringCS,    "_NormalMapSpace",       (int)m_NormalMapSpace);
                cmd.SetComputeIntParam(    s_NormalMapPrefilteringCS,    "_SmoothnessMapChannel", (int)m_SmoothnessMapChannel);
                cmd.SetComputeFloatParam(  s_NormalMapPrefilteringCS,    "_RcpWidth",             1.0f / maxWidth);
                cmd.SetComputeFloatParam(  s_NormalMapPrefilteringCS,    "_RcpHeight",            1.0f / maxHeight);
                cmd.SetComputeTextureParam(s_NormalMapPrefilteringCS, 0, "_NormalMap",            m_NormalMap);
                cmd.SetComputeTextureParam(s_NormalMapPrefilteringCS, 0, "_SmoothnesMap",         m_SmoothnessMap);
                cmd.SetComputeTextureParam(s_NormalMapPrefilteringCS, 0, "_OutputNormalMap",      prefilteredNormalMap);
                cmd.DispatchCompute(       s_NormalMapPrefilteringCS, 0, HDUtils.DivRoundUp(maxWidth, 8), HDUtils.DivRoundUp(maxHeight, 8), 1);
                cmd.GenerateMips(prefilteredNormalMap);
                Graphics.ExecuteCommandBuffer(cmd);

                Texture2D outputNormalMap = new Texture2D(maxWidth, maxHeight, TextureFormat.RGBAHalf, true);

                // Dump the data from the RenderTexture to the Texture2D.
                RenderTexture.active = prefilteredNormalMap;
                outputNormalMap.ReadPixels(new Rect(0, 0, maxWidth, maxHeight), 0, 0); // Can't read MIP maps from the GPU
                outputNormalMap.Apply();

                // Put it in the same folder as the original normal map.
                // This function returns the path WITH the file name AND the extension.
                string outputNormalMapPath = AssetDatabase.GetAssetPath(m_NormalMap);
                outputNormalMapPath = Path.GetDirectoryName(outputNormalMapPath) + "/"
                                    + Path.GetFileNameWithoutExtension(outputNormalMapPath)
                                    + m_OutputNormalMapSuffix + ".exr";

                File.WriteAllBytes(outputNormalMapPath, outputNormalMap.EncodeToEXR()); // Can't store MIP maps to disk
            }
        }
    }
}
