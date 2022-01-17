using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif


namespace UnityEditor.Experimental.VFX.Utility
{
    [ScriptedImporter(1, "pcache")]
    class PointCacheImporter : ScriptedImporter
    {
        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        class OutProperty
        {
            public string Name;
            public string PropertyType;
            public int[] indices;
        }

        public static void GetHeader(Stream s, out long byteLength, out List<string> lines)
        {
            byteLength = 0;
            bool found_end_header = false;
            lines = new List<string>();

            s.Seek(0, SeekOrigin.Begin);
            BinaryReader sr = new BinaryReader(s);

            do
            {
                StringBuilder sb = new StringBuilder();
                bool newline = false;
                do
                {
                    char c = sr.ReadChar();
                    byteLength++;
                    if ((c == '\n' || c == '\r') && sb.Length > 0) newline = true;
                    else sb.Append(c);
                }
                while (!newline);

                string line = sb.ToString();
                lines.Add(line);
                if (line == "end_header") found_end_header = true;
            }
            while (!found_end_header);
        }

        private const int kMaxChannelCount = 4;
        private const int kChannelError = -1;

        public override void OnImportAsset(AssetImportContext ctx)
        {
            try
            {
                PCache pcache = PCache.FromFile(ctx.assetPath);

                PointCacheAsset cache = ScriptableObject.CreateInstance<PointCacheAsset>();
                cache.name = "PointCache";
                ctx.AddObjectToAsset("PointCache", cache);
                ctx.SetMainObject(cache);

                Dictionary<string, OutProperty> outProperties = new Dictionary<string, OutProperty>();
                Dictionary<OutProperty, Texture2D> surfaces = new Dictionary<OutProperty, Texture2D>();

                for (int bucketIndex = 0; bucketIndex < pcache.properties.Count; ++bucketIndex)
                {
                    var prop = pcache.properties[bucketIndex];
                    OutProperty p_out;
                    if (outProperties.ContainsKey(prop.ComponentName))
                    {
                        p_out = outProperties[prop.ComponentName];
                        if (prop.Type != p_out.PropertyType)
                            throw new InvalidOperationException("Unexpected pCache format");
                    }
                    else
                    {
                        p_out = new OutProperty()
                        {
                            Name = prop.ComponentName,
                            PropertyType = prop.Type,
                            indices = Enumerable.Repeat(kChannelError, kMaxChannelCount).ToArray()
                        };
                        outProperties.Add(prop.ComponentName, p_out);
                    }
                    p_out.indices[prop.ComponentIndex] = bucketIndex;
                }


                int width, height;
                FindBestSize(pcache.elementCount, out width, out height);

                // Output Surface Creation
                foreach (var kvp in outProperties)
                {
                    //Initialize Texture
                    var surfaceFormat = GraphicsFormat.None;
                    var lastIndex = Array.LastIndexOf(kvp.Value.indices, kChannelError);
                    var size = lastIndex == -1 ? kMaxChannelCount : lastIndex;
                    switch (kvp.Value.PropertyType)
                    {
                        case "uchar":
                            if (size == 1) surfaceFormat = GraphicsFormat.R8_SRGB;
                            else if (size == 2) surfaceFormat = GraphicsFormat.R8G8_SRGB;
                            else if (size == 3) surfaceFormat = GraphicsFormat.R8G8B8_SRGB;
                            else surfaceFormat = GraphicsFormat.R8G8B8A8_SRGB;
                            break;
                        case "float":
                            if (size == 1) surfaceFormat = GraphicsFormat.R16_SFloat;
                            else if (size == 2) surfaceFormat = GraphicsFormat.R16G16_SFloat;
                            else if (size == 3) surfaceFormat = GraphicsFormat.R16G16B16A16_SFloat; //RGB_Half not supported on most platform
                            else surfaceFormat = GraphicsFormat.R16G16B16A16_SFloat;
                            break;
                        case "int":
                            if (size == 1) surfaceFormat = GraphicsFormat.R32_SInt;
                            else if (size == 2) surfaceFormat = GraphicsFormat.R32G32_SInt;
                            else if (size == 3) surfaceFormat = GraphicsFormat.R32G32B32_SInt;
                            else surfaceFormat = GraphicsFormat.R32G32B32A32_SInt;
                            break;
                        default: throw new NotImplementedException("Types other than uchar/int/float are not supported yet");
                    }

                    var surface = new Texture2D(width, height, surfaceFormat, TextureCreationFlags.DontInitializePixels);
                    surface.name = kvp.Key;

                    var actualSize = GraphicsFormatUtility.GetComponentCount(surfaceFormat);
                    var actualPixelCount = width * height;
                    //Filling Texture (TODOPAUL: Factorize)
                    if (kvp.Value.PropertyType == "uchar")
                    {
                        var data = new byte[actualPixelCount * actualSize];
                        for (var point = 0; point < pcache.elementCount; ++point)
                        {
                            for (var channel = 0; channel < kMaxChannelCount; ++channel)
                            {
                                if (kvp.Value.indices[channel] != kChannelError)
                                    data[point * actualSize + channel] = (byte)pcache.buckets[kvp.Value.indices[channel]][point];
                            }
                        }
                        surface.SetPixelData(data, 0);
                    }
                    else if (kvp.Value.PropertyType == "float")
                    {
                        var data = new ushort[actualPixelCount * actualSize];
                        for (var point = 0; point < pcache.elementCount; ++point)
                        {
                            for (var channel = 0; channel < kMaxChannelCount; ++channel)
                            {
                                if (kvp.Value.indices[channel] != kChannelError)
                                    data[point * actualSize + channel] = Mathf.FloatToHalf((float)pcache.buckets[kvp.Value.indices[channel]][point]);
                            }
                        }
                        surface.SetPixelData(data, 0);
                    }
                    else if (kvp.Value.PropertyType == "int")
                    {
                        var data = new int[actualPixelCount * actualSize];
                        for (var point = 0; point < pcache.elementCount; ++point)
                        {
                            for (var channel = 0; channel < kMaxChannelCount; ++channel)
                            {
                                if (kvp.Value.indices[channel] != kChannelError)
                                    data[point * actualSize + channel] = (int)pcache.buckets[kvp.Value.indices[channel]][point];
                            }
                        }
                        surface.SetPixelData(data, 0);
                    }
                    surfaces.Add(kvp.Value, surface);
                }

                cache.PointCount = pcache.elementCount;
                cache.surfaces = new Texture2D[surfaces.Count];

                int k = 0;
                foreach (var kvp in surfaces)
                {
                    kvp.Value.Apply();
                    ctx.AddObjectToAsset(kvp.Key.Name, kvp.Value);
                    cache.surfaces[k] = kvp.Value;
                    k++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void FindBestSize(int count, out int width, out int height)
        {
            float r = Mathf.Sqrt(count);
            width = (int)Mathf.Ceil(r);
            height = width;
        }
    }
}
