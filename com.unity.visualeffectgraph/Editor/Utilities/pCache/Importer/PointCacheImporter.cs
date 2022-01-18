using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.VFX;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif


namespace UnityEditor.Experimental.VFX.Utility
{
    [ScriptedImporter(2, "pcache")]
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

                var outProperties = new Dictionary<string, OutProperty>();
                var surfaces = new List<(Texture2D texture, VFXValueType type)>();

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
                    var outputType = VFXValueType.None;
                    var size = Array.IndexOf(kvp.Value.indices, kvp.Value.indices.Max()) + 1;

                    switch (kvp.Value.PropertyType)
                    {
                        case "uchar":
                            switch (size)
                            {
                                case 1:
                                    surfaceFormat = GraphicsFormat.R8_SRGB;
                                    outputType = VFXValueType.Float;
                                    break;
                                case 2:
                                    surfaceFormat = GraphicsFormat.R8G8_SRGB;
                                    outputType = VFXValueType.Float2;
                                    break;
                                case 3:
                                    //R8G8B8 not supported on most platform (with Texture.Sample)
                                    surfaceFormat = GraphicsFormat.R8G8B8A8_SRGB;
                                    outputType = VFXValueType.Float3;
                                    break;
                                default:
                                    surfaceFormat = GraphicsFormat.R8G8B8A8_SRGB;
                                    outputType = VFXValueType.Float4;
                                    break;
                            }
                            break;
                        case "float":
                            switch (size)
                            {
                                case 1:
                                    surfaceFormat = GraphicsFormat.R16_SFloat;
                                    outputType = VFXValueType.Float;
                                    break;
                                case 2:
                                    surfaceFormat = GraphicsFormat.R16G16_SFloat;
                                    outputType = VFXValueType.Float2;
                                    break;
                                case 3:
                                    //RGB_Half not supported on most platform
                                    surfaceFormat = GraphicsFormat.R16G16B16A16_SFloat;
                                    outputType = VFXValueType.Float3;
                                    break;
                                default:
                                    surfaceFormat = GraphicsFormat.R16G16B16A16_SFloat;
                                    outputType = VFXValueType.Float4;
                                    break;
                            }
                            break;
                        case "int":
                            switch (size)
                            {
                                case 1:
                                    //R32_SInt isn't available on Texture2D (store in float and using asint later)
                                    surfaceFormat = GraphicsFormat.R32_SFloat;
                                    outputType = VFXValueType.Int32;
                                    break;
                                default:
                                    throw new NotImplementedException("Vector int isn't supported");
                            }
                            break;
                        default:
                            throw new NotImplementedException("Types other than uchar/int/float are not supported yet");
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
                    surface.Apply();
                    surfaces.Add((surface, outputType));
                }

                cache.PointCount = pcache.elementCount;
                cache.surfaces = surfaces.Select(o => o.texture).ToArray();
                cache.types = surfaces.Select(o => o.type).ToArray();
                foreach (var surface in surfaces)
                {
                    ctx.AddObjectToAsset(surface.texture.name, surface.texture);
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
