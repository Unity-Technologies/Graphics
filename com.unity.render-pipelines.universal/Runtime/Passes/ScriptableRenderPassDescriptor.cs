using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal sealed class ScriptableRenderPassDescriptor
    {
        internal int width;
        internal int height;
        internal int sampleCount;
        internal GraphicsFormat[] formats;
        internal bool isLastPass;
        internal bool isDepthOnly;

        internal ScriptableRenderPassDescriptor(GraphicsFormat format, int w, int h, int samples, bool depthOnly)
        {
            width = w;
            height = h;
            sampleCount = samples;
            isLastPass = false;
            isDepthOnly = depthOnly;
            formats = new GraphicsFormat[]
            {
                format, GraphicsFormat.None, GraphicsFormat.None,
                GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None, GraphicsFormat.None
            };
        }

        internal ScriptableRenderPassDescriptor(GraphicsFormat format)
            : this(format, -1, -1, -1, false)
        {}

        internal ScriptableRenderPassDescriptor(GraphicsFormat format, bool depthOnly)
            : this(format, -1, -1, -1, depthOnly)
        {}
        internal ScriptableRenderPassDescriptor(GraphicsFormat[] format, int w, int h, int samples, bool depthOnly)
        {
            width = w;
            height = h;
            sampleCount = samples;
            isLastPass = false;
            isDepthOnly = depthOnly;
            formats = format;
        }

        internal ScriptableRenderPassDescriptor(GraphicsFormat[] format)
            : this(format, -1, -1, -1, false)
        {}

    }
}
