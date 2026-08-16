using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.Universal
{
    // One bit per pushable field of GlobalShaderVariablesOnly3D, see GlobalShaderVariablesBaseDirty.
    [Flags]
    internal enum GlobalShaderVariablesOnly3DDirty : uint
    {
        None = 0u,
        _URPDummy3D = 1u << 0,

        // Keep this referencing the last bit declared above when adding a variable.
        All = (_URPDummy3D << 1) - 1u,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GlobalShaderVariablesOnly3D
    {
        internal Vector4 _URPDummy3D;
    }

    internal static class GlobalShaderVariablesOnly3DExtensions
    {
        internal static void SetGlobals(this in GlobalShaderVariablesOnly3D vars, IBaseCommandBuffer cmd, GlobalShaderVariablesOnly3DDirty dirty)
        {

        }
    }
}