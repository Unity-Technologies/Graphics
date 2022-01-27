using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering.UIGen
{
    public struct GenerationTarget
    {
        string assetLocation;
        string runtimeCodeLocation;
        string editorCodeLocation;

        public bool GetEditorPathFor(
            [DisallowNull] string relativePath,
            [NotNullWhen(true)] out string path,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }
}
