using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering.UIGen
{
    /// <summary>
    /// Defines where files should be generated
    /// </summary>
    public struct GenerationTargetLocations
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
