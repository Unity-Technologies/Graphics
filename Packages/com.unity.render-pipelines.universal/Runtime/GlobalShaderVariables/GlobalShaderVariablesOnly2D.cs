using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    // One bit per pushable field of GlobalShaderVariablesOnly2D, see GlobalShaderVariablesBaseDirty.
    [Flags]
    internal enum GlobalShaderVariablesOnly2DDirty : uint
    {
        None = 0u,
        _URPDummy2D = 1u << 0,

        // Keep this referencing the last bit declared above when adding a variable.
        All = (_URPDummy2D << 1) - 1u,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GlobalShaderVariablesOnly2D
    {
        internal Vector4 _URPDummy2D;
    }

    internal static class GlobalShaderVariablesOnly2DExtensions
    {
        internal static void SetGlobals(this in GlobalShaderVariablesOnly2D vars, IBaseCommandBuffer cmd, GlobalShaderVariablesOnly2DDirty dirty)
        {

        }
    }
}