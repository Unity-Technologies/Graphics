namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
    unsafe struct ShaderVariablesSubsurfaceScattering
    {
        // Use float4 to avoid any packing issue between compute and pixel shaders
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(Vector4))]
        public fixed float _ThicknessRemaps[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4]; // Remap: X = start, Y = end - start
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(Vector4))]
        public fixed float _ShapeParamsAndMaxScatterDists[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4]; // RGB = S = 1 / D, A = d = RgbMax(D)
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(Vector4))]
        public fixed float _TransmissionTintsAndFresnel0[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4];  // RGB = 1/4 * color, A = fresnel0
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(Vector4))]
        public fixed float _WorldScalesAndFilterRadii[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4]; // X = meters per world unit, Y = filter radius (in mm), Z = 1/X, W = 1/Y
        [HLSLArray(DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT, typeof(float))]
        public fixed uint _DiffusionProfileHashTable[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT]; // TODO: constant

        // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
        // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
        public uint   _EnableSubsurfaceScattering; // Globally toggles subsurface and transmission scattering on/off
        public float  _TexturingModeFlags;         // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
        public float  _TransmissionFlags;          // 1 bit/profile; 0 = regular, 1 = thin
        public uint   _DiffusionProfileCount;
    }
}
