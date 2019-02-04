using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    public class Light2DLookupTexture
    {
        static Texture2D m_LightLookupTexture;

        static public Texture2D CreateLightLookupTexture()
        {
            const float WIDTH = 256;
            const float HEIGHT = 256;
            TextureFormat textureFormat = TextureFormat.ARGB32;
            if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
                textureFormat = TextureFormat.RGBAHalf;
            else if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
                textureFormat = TextureFormat.RGBAFloat;

            m_LightLookupTexture = new Texture2D((int)WIDTH, (int)HEIGHT, textureFormat, false);
            m_LightLookupTexture.filterMode = FilterMode.Bilinear;
            m_LightLookupTexture.wrapMode = TextureWrapMode.Clamp;
            if (m_LightLookupTexture != null)
            {
                Vector2 center = new Vector2(WIDTH / 2, HEIGHT / 2);

                for (int y = 0; y < HEIGHT; y++)
                {
                    for (int x = 0; x < WIDTH; x++)
                    {
                        Vector2 pos = new Vector2(x, y);
                        float distance = Vector2.Distance(pos, center);
                        Vector2 relPos = pos - center;
                        Vector2 direction = center - pos;
                        direction.Normalize();

                        // red   = 1-0 distance
                        // green  = 1-0 angle
                        // blue = direction.x
                        // alpha = direction.y

                        float red;
                        if (x == WIDTH - 1 || y == HEIGHT - 1)
                            red = 0;
                        else
                            red = Mathf.Clamp(1 - (2.0f * distance / WIDTH), 0.0f, 1.0f);

                        float cosAngle = Vector2.Dot(Vector2.down, relPos.normalized);
                        float angle = Mathf.Acos(cosAngle) / Mathf.PI; // 0-1 

                        float green = Mathf.Clamp(1 - angle, 0.0f, 1.0f);
                        float blue = direction.x;
                        float alpha = direction.y;

                        Color color = new Color(red, green, blue, alpha);

                        
                        m_LightLookupTexture.SetPixel(x, y, color);
                    }
                }
            }
            m_LightLookupTexture.Apply();

            return m_LightLookupTexture;
        }

//#if UNITY_EDITOR
//        [MenuItem("Light2D Debugging/Write Light Texture")]
//        static public void WriteLightTexture()
//        {
//            var path = EditorUtility.SaveFilePanel("Save texture as EXR", "", "LightLookupTexture.exr", "exr");

//            CreateLightLookupTexture();

//            byte[] imgData = m_LightLookupTexture.EncodeToEXR(Texture2D.EXRFlags.CompressRLE);
//            if (imgData != null)
//                File.WriteAllBytes(path, imgData);
//        }
//#endif
    }
}
