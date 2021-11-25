using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal static class Light2DLookupTexture
    {
        private static Texture2D s_PointLightLookupTexture;

        public static Texture GetLightLookupTexture()
        {
            if (s_PointLightLookupTexture == null)
                s_PointLightLookupTexture = CreatePointLightLookupTexture();
            return s_PointLightLookupTexture;
        }

        private static Texture2D CreatePointLightLookupTexture()
        {
            const int WIDTH = 256;
            const int HEIGHT = 256;

            var textureFormat = GraphicsFormat.R8G8B8A8_UNorm;
            if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.SetPixels))
                textureFormat = GraphicsFormat.R16G16B16A16_SFloat;
            else if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R32G32B32A32_SFloat, FormatUsage.SetPixels))
                textureFormat = GraphicsFormat.R32G32B32A32_SFloat;

            var texture = new Texture2D(WIDTH, HEIGHT, textureFormat, TextureCreationFlags.None);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var center = new Vector2(WIDTH / 2.0f, HEIGHT / 2.0f);

            for (var y = 0; y < HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    var pos = new Vector2(x, y);
                    var distance = Vector2.Distance(pos, center);
                    var relPos = pos - center;
                    var direction = center - pos;
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

                    var cosAngle = Vector2.Dot(Vector2.down, relPos.normalized);
                    var angle = Mathf.Acos(cosAngle) / Mathf.PI; // 0-1

                    var green = Mathf.Clamp(1 - angle, 0.0f, 1.0f);
                    var blue = direction.x;
                    var alpha = direction.y;

                    var color = new Color(red, green, blue, alpha);

                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }

        //        private static Texture2D s_FalloffLookupTexture;
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
