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

        internal void ConfigureDescriptor(GraphicsFormat format = GraphicsFormat.None, int w = -1, int h = -1,
            int samples = -1, bool depthOnly = false)
        {
            width = w;
            height = h;
            sampleCount = samples;
            isLastPass = false;
            isDepthOnly = depthOnly;
            formats[0] = format;
        }

        internal void ConfigureDescriptor(GraphicsFormat format, bool depthOnly)
        {
            ConfigureDescriptor(format, -1, -1, -1, depthOnly);
        }

        internal void ConfigureDescriptor(GraphicsFormat[] format, int w = -1, int h = -1,
            int samples = -1, bool depthOnly = false)
        {
            width = w;
            height = h;
            sampleCount = samples;
            isLastPass = false;
            isDepthOnly = depthOnly;
            formats = format;
        }

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
