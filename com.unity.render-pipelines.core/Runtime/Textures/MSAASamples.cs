using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Number of MSAA samples.
    /// </summary>
    public enum MSAASamples
    {
        /// <summary>No MSAA.</summary>
        None = 1,
        /// <summary>MSAA 2X.</summary>
        MSAA2x = 2,
        /// <summary>MSAA 4X.</summary>
        MSAA4x = 4,
        /// <summary>MSAA 8X.</summary>
        MSAA8x = 8
    }

    public static class MSAASamplesExtensions
    {
        public static FormatUsage AsFormatUsage(this MSAASamples samples)
        {
            switch (samples)
            {
                case MSAASamples.MSAA2x:
                    return FormatUsage.MSAA2x;
                case MSAASamples.MSAA4x:
                    return FormatUsage.MSAA4x;
                case MSAASamples.MSAA8x:
                    return FormatUsage.MSAA8x;
                default:
                    return FormatUsage.Render;
            }
        }

        public static MSAASamples Validate(this MSAASamples samples)
        {
            if (samples == MSAASamples.None)
                return samples;

            GraphicsFormat ldrFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            MSAASamples[] probe = {MSAASamples.MSAA2x, MSAASamples.MSAA4x, MSAASamples.MSAA8x};

            int minSampleCount = 0;
            int maxSampleCount = 0;

            // Some GPUs might not support MSAA2x, so probe the supported range for clamping
            foreach (MSAASamples entry in probe)
            {
                bool supported = SystemInfo.IsFormatSupported(ldrFormat, entry.AsFormatUsage());
                if (!supported)
                    continue;
                if (minSampleCount == 0)
                    minSampleCount = maxSampleCount = (int)entry;
                maxSampleCount = (int)entry;
            }

            MSAASamples origSampleCount = samples;
            samples = (MSAASamples) Math.Min(Math.Max((int) samples, minSampleCount), maxSampleCount);

            if (origSampleCount != samples)
                Debug.LogWarning($"MSAASamples changed from {origSampleCount} into {samples} (Supported MSAA sampleCount range {minSampleCount}-{maxSampleCount})");
            return samples;
        }
    }
}
