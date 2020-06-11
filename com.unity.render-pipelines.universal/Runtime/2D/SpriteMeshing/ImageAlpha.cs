using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public struct ImageAlpha
    {
        public int width;
        public int height;
        public NativeArray<short> imageAlpha;

        public ImageAlpha(int w, int h)
        {
            width = w;
            height = h;
            imageAlpha = new NativeArray<short>(width * height, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        }

        public void Dispose()
        {
            imageAlpha.Dispose();
        }

        public bool InsideImageBounds(int x, int y)
        {
            return (x >= 0) && (y >= 0) && (x < width) && (y < height);
        }

        public short GetImageAlpha(int x, int y)
        {
            if (InsideImageBounds(x, y))
                return imageAlpha[x + y * width];
            else
                return -1;
        }

        public OutlineTypes.AlphaType GetImageAlphaType(short alpha, short alphaCutoff)
        {
            if (alpha == -1)
                return OutlineTypes.AlphaType.NA;
            else if (alpha < alphaCutoff)
                return OutlineTypes.AlphaType.Transparent;
            else if (alpha >= 255)
                return OutlineTypes.AlphaType.Opaque;
            else
                return OutlineTypes.AlphaType.Translucent;
        }

        public OutlineTypes.AlphaType GetImageAlphaType(int x, int y, short alphaCutoff)
        {
            short alpha = GetImageAlpha(x, y);
            return GetImageAlphaType(alpha, alphaCutoff);
        }


        public void Copy(Texture2D texture, int4 tileRect)
        {
            Color32[] colors = texture.GetPixels32();

            int textureWidth = texture.width;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int colorsIndex = (x + tileRect.x) + (y + tileRect.y) * textureWidth;
                    int imageAlphaIndex = x + y * width;
                    imageAlpha[imageAlphaIndex] = colors[colorsIndex].a;
                }
            }
        }

    }
}
