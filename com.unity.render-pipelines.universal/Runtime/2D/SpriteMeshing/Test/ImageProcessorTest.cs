using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;


namespace UnityEngine.Experimental.Rendering.Universal
{
    [ExecuteInEditMode]
    public class ImageProcessorTest : MonoBehaviour
    {
        public Texture2D m_Input;
        public string m_OutputPath;
        public bool m_Detect = false;
        public short m_AlphaCutoff;
        public bool m_RunAsJob = false;

        // Update is called once per frame
        void Update()
        {
            if (m_Detect)
            {
                m_Detect = false;

                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                ImageAlpha processedImageAlpha = new ImageAlpha(m_Input.width, m_Input.height);
                ImageAlpha unprocessedImageAlpha = new ImageAlpha(m_Input.width, m_Input.height);
                unprocessedImageAlpha.Copy(m_Input, new Unity.Mathematics.int4(0, 0, m_Input.width, m_Input.height));

                ImageProcessorJob imageProcessorJob = new ImageProcessorJob(m_AlphaCutoff, unprocessedImageAlpha, processedImageAlpha);

                if (m_RunAsJob)
                    imageProcessorJob.Run();
                else
                    imageProcessorJob.Execute();

                imageProcessorJob.Dispose();
                stopwatch.Stop();
                Debug.Log("Time: " + (float)stopwatch.ElapsedMilliseconds / 1000.0f);


                Texture2D outTex = new Texture2D(processedImageAlpha.width, processedImageAlpha.height);
                Color[] colors = new Color[] { Color.black, Color.clear, Color.green, Color.red };
                for (int y = 0; y < processedImageAlpha.height; y++)
                {
                    for (int x = 0; x < processedImageAlpha.width; x++)
                    {
                        OutlineTypes.AlphaType alphaType = processedImageAlpha.GetImageAlphaType(processedImageAlpha.GetImageAlpha(x, y), m_AlphaCutoff);
                        outTex.SetPixel(x, y, colors[(int)alphaType]);
                    }
                }

                byte[] pngData = outTex.EncodeToPNG();
                System.IO.File.WriteAllBytes(m_OutputPath + "/image_alpha.png", pngData);
                processedImageAlpha.Dispose();
                unprocessedImageAlpha.Dispose();
            }
        }
    }
}
