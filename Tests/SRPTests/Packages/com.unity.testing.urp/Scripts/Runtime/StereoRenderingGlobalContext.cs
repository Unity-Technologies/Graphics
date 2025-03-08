using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools.Graphics;

namespace Unity.Rendering.Universal.Tests
{
    [Flags]
    public enum StereoRenderingContext
    {
        None = 0,
        StereoRenderingEnabled = 1,
        StereoRenderingDisabled = 2,
    }

    public class StereoRenderingGlobalContext : IGlobalContextProvider
    {
        public int Context =>
            IsStereoRenderingActive()
                ? (int)StereoRenderingContext.StereoRenderingEnabled
                : (int)StereoRenderingContext.StereoRenderingDisabled;

        static bool IsStereoRenderingActive()
        {
            return XRGraphicsAutomatedTests.enabled;
        }

        public void ActivateContext(StereoRenderingContext context)
        {
            if (context == StereoRenderingContext.None)
                return;

            XRGraphicsAutomatedTests.enabled = context == StereoRenderingContext.StereoRenderingEnabled;
        }
    }
}
