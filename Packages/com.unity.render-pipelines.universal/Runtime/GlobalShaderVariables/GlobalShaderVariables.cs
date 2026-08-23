using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Constant buffer containing most URP 3D/2D global vanilla shader variables.
    /// Enable persistent constant buffer mode for better performance.
    /// If persistent, its inner fields cannot be set through classic uniform APIs (cmd.SetGlobalX(), Shader.SetGlobalX(), mpb.SetFloat(), ...).
    /// </summary>
    /// <remark>
    /// !!WARNING - PARAMETERS DECLARATION GUIDELINES!!
    /// 1. Add a field to GlobalShaderVariablesBase when both URP 2D and 3D consume it, otherwise to GlobalShaderVariablesOnly2D or GlobalShaderVariablesOnly3D.
    ///    - Each of these three structs must independently follow the rules below, they are laid out back to back here.
    ///
    /// 2. No bool, char or enum. These types have a size of 1 byte in C# but 4 bytes in an HLSL constant buffer. Use int/uint instead.
    ///
    /// 3. No double/half. Only consider int, uint, float, Vector4, Vector4Int, Matrix4x4. All data will be aligned on Vector4/float4 size, arrays elements included.
    ///    - Shader side structure will be padded for anything not aligned to Vector4. Add padding accordingly.
    ///    - Base element size for array should be 4 components of 4 bytes (Vector4 or Vector4Int) otherwise the array will be interlaced with padding on shader side.
    ///
    /// 4. No Vector3. HLSL/SPIR-V/Metal pack float3 differently.
    ///    - Metal: float3 has a size of 16 bytes and is aligned to 16 bytes (similar to float4).
    ///    - Vulkan (std140): float3 has a size of 12 bytes and is aligned to 16 bytes.
    ///    - HLSL: float3 has a size of 12 bytes and has no fixed alignment (aside from the 16-byte boundary).
    ///
    /// 5. Try to keep data grouped by access and rendering system as much as possible.
    ///   - Don't move a float parameter away from where it belongs for filling a hole. Add padding in this case.
    ///
    /// 6. Total size of each sub-struct must be a multiple of 16 bytes (Vector4), add trailing padding when needed.
    ///
    /// We pack all the URP global shader variables mainly for performance reasons in order to enable a persistent constant buffer owned by URP:
    /// - Filling loose uniforms through cmd.SetGlobalFloat() and similar APIs means rebuilding dynamically a constant buffer with dynamic layout (aside from GLES/GL) PER draw call and PER SRP batch.
    /// - Filling a persistent CB with a fixed layout only once on C# side reduces noticeably the CPU overhead of passing data to GPU.
    ///
    /// Besides, we need to keep the number of different global constant buffers low. Some Android mobile devices can't afford more than 12 CBs per shader stage. Hence the reason of storing so many variables in a single buffer.
    /// For specific shader passes with low CB count (see ConstantBufferBudgetTests.cs for testing your shader), you can also pack your local variables into your own local CB.
    ///
    /// After any field change:
    /// - update ShaderLibrary/GlobalShaderVariables.hlsl
    /// - run unit tests in GlobalShaderVariablesLayoutTests.cs
    /// </remark>
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlobalShaderVariables
    {
        internal GlobalShaderVariablesBase varsBase;
        internal GlobalShaderVariablesOnly3D vars3D;
        internal GlobalShaderVariablesOnly2D vars2D;
    }

    [Flags]
    internal enum GlobalShaderVariablesGroup
    {
        None = 0,
        Base = 1 << 0,
        Only3D = 1 << 1,
        Only2D = 1 << 2,
        All = Base | Only3D | Only2D,
    }

    // Tracks which fields of GlobalShaderVariables were modified since the last push to avoid useless SetGlobal() calls.
    internal struct GlobalShaderVariablesDirty
    {
        public GlobalShaderVariablesBaseDirty varsBase;
        public GlobalShaderVariablesOnly3DDirty vars3D;
        public GlobalShaderVariablesOnly2DDirty vars2D;

        public static readonly GlobalShaderVariablesDirty all = new()
        {
            varsBase = GlobalShaderVariablesBaseDirty.All,
            vars3D = GlobalShaderVariablesOnly3DDirty.All,
            vars2D = GlobalShaderVariablesOnly2DDirty.All,
        };

        public bool isDirty => varsBase != GlobalShaderVariablesBaseDirty.None
            || vars3D != GlobalShaderVariablesOnly3DDirty.None
            || vars2D != GlobalShaderVariablesOnly2DDirty.None;

        public void Add(in GlobalShaderVariablesDirty other)
        {
            varsBase |= other.varsBase;
            vars3D |= other.vars3D;
            vars2D |= other.vars2D;
        }

        public void Clear()
        {
            varsBase = GlobalShaderVariablesBaseDirty.None;
            vars3D = GlobalShaderVariablesOnly3DDirty.None;
            vars2D = GlobalShaderVariablesOnly2DDirty.None;
        }
    }

    internal static class GlobalShaderVariablesExtensions
    {
        /// <summary>
        /// Sets all shader variables as global uniforms using the provided CommandBuffer.
        /// This is a fallback when persistent constant buffer mode is disabled.
        /// Extension with `in` receiver so callers can invoke it without copying the struct.
        /// </summary>
        internal static void SetGlobals(this in GlobalShaderVariables vars, IBaseCommandBuffer cmd, in GlobalShaderVariablesDirty dirty, GlobalShaderVariablesGroup groups = GlobalShaderVariablesGroup.All)
        {
            if ((groups & GlobalShaderVariablesGroup.Base) != 0)
                vars.varsBase.SetGlobals(cmd, dirty.varsBase);

            if ((groups & GlobalShaderVariablesGroup.Only3D) != 0)
                vars.vars3D.SetGlobals(cmd, dirty.vars3D);

            if ((groups & GlobalShaderVariablesGroup.Only2D) != 0)
                vars.vars2D.SetGlobals(cmd, dirty.vars2D);
        }
    }
}