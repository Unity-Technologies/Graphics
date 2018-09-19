using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Utilities
{
    public partial class PointCacheBakeTool : EditorWindow
    {
        public enum DecimationThresholdMode
        {
            None,
            Alpha,
            Luminance,
            R,
            G,
            B
        }

        Texture2D m_Texture;
        bool m_RandomizePixels = false;
        DecimationThresholdMode m_DecimationThresholdMode = DecimationThresholdMode.Alpha;
        float m_Threshold = 0.33333f;

        void OnGUI_Texture()
        {
            GUILayout.Label("Texture baking", EditorStyles.boldLabel);

            m_Texture = (Texture2D)EditorGUILayout.ObjectField("Texture", m_Texture, typeof(Texture2D), false);
            
            m_DecimationThresholdMode = (DecimationThresholdMode)EditorGUILayout.EnumPopup("Decimation Threshold", m_DecimationThresholdMode);
            if(m_DecimationThresholdMode != DecimationThresholdMode.None)
                m_Threshold = EditorGUILayout.Slider("Threshold",m_Threshold, 0.0f, 1.0f);

            m_RandomizePixels = EditorGUILayout.Toggle("Randomize Pixels", m_RandomizePixels);
            m_ExportColors = EditorGUILayout.Toggle("Export Colors", m_ExportColors);

            m_OutputFormat = (PCache.Format)EditorGUILayout.EnumPopup("File Format", m_OutputFormat);

            if (GUILayout.Button("Save to pCache file..."))
            {
                string fileName = EditorUtility.SaveFilePanelInProject("pCacheFile", m_Texture.name, "pcache", "Save PCache");
                if (fileName != null)
                {
                    PCache file = new PCache();
                    file.AddVector3Property("position");
                    if (m_ExportColors) file.AddColorProperty("color");

                    List<Vector3> positions = new List<Vector3>();
                    List<Vector4> colors = null;

                    if (m_ExportColors) colors = new List<Vector4>();

                    GetTextureData(m_Texture, m_RandomizePixels, m_DecimationThresholdMode, m_Threshold, positions, colors);
                    file.SetVector3Data("position", positions);
                    if (m_ExportColors)
                        file.SetColorData("color", colors);

                    file.SaveToFile(fileName, m_OutputFormat);
                }
            }
        }

        void GetTextureData(Texture2D texture, bool randomize, DecimationThresholdMode mode, float threshold, List<Vector3> positions, List<Vector4> colors = null )
        {
            bool decimate = mode != DecimationThresholdMode.None;
            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;
            int i = 0;

            foreach (var color in pixels)
            {
                var x = i % width;
                var y = i / width;
                var fx = (float)x / width;
                var fy = (float)y / height;
                i++;

                if(decimate)
                {
                    float value;
                    switch(mode)
                    {
                        default: throw new System.NotImplementedException();
                        case DecimationThresholdMode.R: value = color.r; break;
                        case DecimationThresholdMode.G: value = color.g; break;
                        case DecimationThresholdMode.B: value = color.b; break;
                        case DecimationThresholdMode.Alpha: value = color.a; break;
                        case DecimationThresholdMode.Luminance: value = color.grayscale; break;
                    }
                    if (value < threshold)
                        continue;
                }

                positions.Add(new Vector3(fx-0.5f,fy-0.5f,0));

                if(colors != null)
                {
                    colors.Add(color);
                }

            }

            if(randomize)
            {
                int total = positions.Count;
                int[] indices = new int[total];

                for (int k = 0; k < total; k++)
                    indices[k] = k;

                indices = indices.OrderBy(a => System.Guid.NewGuid()).ToArray();

                List<Vector3> newPositions = new List<Vector3>();
                List<Vector4> newColors = new List<Vector4>();

                foreach(int index in indices)
                {
                    newPositions.Add(positions[index]);
                    if (colors != null)
                        newColors.Add(colors[index]);
                }

                positions = newPositions;
                if (colors != null)
                    colors = newColors;
            }
        }

        static partial class Contents
        {

        }

    }
}


