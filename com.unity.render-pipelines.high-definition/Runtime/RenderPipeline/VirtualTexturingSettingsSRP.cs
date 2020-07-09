using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    internal sealed class VirtualTexturingSettingsSRP
    {
        public int streamingCpuCacheSizeInMegaBytes = 256;
        public List<GPUCacheSettingSRP> streamingGpuCacheSettings = new List<GPUCacheSettingSRP>() { new GPUCacheSettingSRP() { format = Experimental.Rendering.GraphicsFormat.None, sizeInMegaBytes = 128 } };
    }

    [Serializable]
    internal struct GPUCacheSettingSRP
    {
        /// <summary>
        ///   <para>Format of the cache these settings are applied to.</para>
        /// </summary>
        public GraphicsFormat format;
        /// <summary>
        ///   <para>Size in MegaBytes of the cache created with these settings.</para>
        /// </summary>
        public uint sizeInMegaBytes;
    }
}
