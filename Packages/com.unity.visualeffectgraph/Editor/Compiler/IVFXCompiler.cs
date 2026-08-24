using UnityEngine.VFX;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    struct VFXCompileOutput
    {
        public bool success;
        public HashSet<GUID> sourceDependencies;
        public VisualEffectAssetDesc assetDesc;
        public VFXExpressionCompiledData compiledData;
    }

    interface IVFXCompiler
    {
        VFXCompileOutput Compile(VFXGraph graph, VFXCompilationMode compilationMode, bool enableShaderDebugSymbols);

        // Compiles only what is needed to push updated values into the value sheet. Returns null on failure.
        VFXExpressionCompiledData CompileExpressionsOnly(VFXGraph graph, VFXCompilationMode compilationMode);
    }
}
