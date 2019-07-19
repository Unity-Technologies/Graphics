using System;

namespace UnityEditor.Rendering.HighDefinition
{
    [Flags]
    public enum InstancingOption
    {
        None = 0,
        AssumeUniformScaling    = 1 << 0,
        NoMatrices              = 1 << 1,
        NoLODFade               = 1 << 2,
        NoLightProbe            = 1 << 3,
        NoLightmap              = 1 << 4,
        RenderingLayer          = 1 << 5
    }

    public struct InstancingSettings
    {
        public bool Enabled;
        public InstancingOption Options;
        public string ProceduralFuncName;

        public static readonly InstancingSettings Default = new InstancingSettings()
        {
            Enabled = true,
            Options = InstancingOption.None,
            ProceduralFuncName = null
        };
    }
}
