using UnityEditor;
using System.IO;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal static class Light2DLookupTexture
    {
        static Texture2D s_PointLightLookupTexture;
        static Texture2D s_FalloffLookupTexture;

        static public Texture2D CreatePointLightLookupTexture()
        {
            const float WIDTH = 256;
            const float HEIGHT = 256;
            TextureFormat textureFormat = TextureFormat.ARGB32;
            if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf))
                textureFormat = TextureFormat.RGBAHalf;
            else if (SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat))
                textureFormat = TextureFormat.RGBAFloat;

            s_PointLightLookupTexture = new Texture2D((int)WIDTH, (int)HEIGHT, textureFormat, false);
            s_PointLightLookupTexture.filterMode = FilterMode.Bilinear;
            s_PointLightLookupTexture.wrapMode = TextureWrapMode.Clamp;
            if (s_PointLightLookupTexture != null)
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


                        s_PointLightLookupTexture.SetPixel(x, y, color);
                    }
                }
            }
            s_PointLightLookupTexture.Apply();

            return s_PointLightLookupTexture;
        }

        static public Texture2D CreateFalloffLookupTexture()
        {
            const float WIDTH = 2048;
            const float HEIGHT = 192;

            TextureFormat textureFormat = TextureFormat.ARGB32;
            s_FalloffLookupTexture = new Texture2D((int)WIDTH, (int)HEIGHT-64, textureFormat, false);
            s_FalloffLookupTexture.filterMode = FilterMode.Bilinear;
            s_FalloffLookupTexture.wrapMode = TextureWrapMode.Clamp;
            if (s_FalloffLookupTexture != null)
            {
                for(int y=0;y<HEIGHT;y++)
                {
                    float baseValue = (float)(y+32) / (float)(HEIGHT+64);
                    float lineValue = -baseValue + 1;
                    float exponent = Mathf.Log(lineValue) / Mathf.Log(baseValue);

                    if (y == HEIGHT - 1)
                        textureFormat = TextureFormat.ARGB32;

                    for (int x=0;x<WIDTH;x++)
                    {
                        float t = (float)x / (float)WIDTH;
                        float red = Mathf.Pow(t, exponent);
                        Color color = new Color(red, 0, 0, 1);
                        if(y >= 32 && y < 160)
                            s_FalloffLookupTexture.SetPixel(x, y-32, color);
                    }
                }
            }
            s_FalloffLookupTexture.Apply();

            return s_FalloffLookupTexture;
        }

//#if UNITY_EDITOR
//        [MenuItem("Light2D Debugging/Write Light Texture")]
//        static public void WriteLightTexture()
//        {
//            var path = EditorUtility.SaveFilePanel("Save texture as PNG", "", "LightLookupTexture.exr", "png");

//            CreatePointLightLookupTexture();

//            byte[] imgData = s_PointLightLookupTexture.EncodeToEXR(Texture2D.EXRFlags.CompressRLE);
//            if (imgData != null)
//                File.WriteAllBytes(path, imgData);
//        }

//        [MenuItem("Light2D Debugging/Write Falloff Texture")]
//        static public void WriteCurveTexture()
//        {
//            var path = EditorUtility.SaveFilePanel("Save texture as PNG", "", "FalloffLookupTexture.png", "png");

//            CreateFalloffLookupTexture();

//            byte[] imgData = s_FalloffLookupTexture.EncodeToPNG();
//            if (imgData != null)
//                File.WriteAllBytes(path, imgData);
//        }
//#endif
    }
}
