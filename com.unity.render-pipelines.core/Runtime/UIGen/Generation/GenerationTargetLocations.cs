using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;

namespace UnityEngine.Rendering.UIGen
{
    /// <summary>
    /// Defines where files should be generated
    /// </summary>
    public struct GenerationTargetLocations
    {
        [MustUseReturnValue]
        public static bool TryFrom(
            [DisallowNull] string assetLocation,
            [DisallowNull] string runtimeCodeLocation,
            [DisallowNull] string editorCodeLocation,
            out GenerationTargetLocations locations,
            [NotNullWhen(false)] out Exception error
        )
        {
            locations = new GenerationTargetLocations(assetLocation, runtimeCodeLocation, editorCodeLocation);
            error = default;
            return true;
        }

        public static GenerationTargetLocations From(
            [DisallowNull] string assetLocation,
            [DisallowNull] string runtimeCodeLocation,
            [DisallowNull] string editorCodeLocation
        )
        {
            if (!TryFrom(assetLocation, runtimeCodeLocation, editorCodeLocation, out var result, out var error))
                throw error;

            return result;
        }

        string m_AssetLocation;
        string m_RuntimeCodeLocation;
        string m_EditorCodeLocation;

        GenerationTargetLocations(
            [DisallowNull] string assetLocation,
            [DisallowNull] string runtimeCodeLocation,
            [DisallowNull] string editorCodeLocation)
        {
            m_AssetLocation = assetLocation;
            m_RuntimeCodeLocation = runtimeCodeLocation;
            m_EditorCodeLocation = editorCodeLocation;
        }

        public bool GetEditorPathFor(
            [DisallowNull] string relativePath,
            [NotNullWhen(true)] out string path,
            [NotNullWhen(false)] out Exception error
        ) => GetPathFor(m_EditorCodeLocation, relativePath, out path, out error);

        public bool GetRuntimeCodePathFor(
            [DisallowNull] string relativePath,
            [NotNullWhen(true)] out string path,
            [NotNullWhen(false)] out Exception error
        ) => GetPathFor(m_RuntimeCodeLocation, relativePath, out path, out error);

        public bool GetAssetPathFor(
            [DisallowNull] string relativePath,
            [NotNullWhen(true)] out string path,
            [NotNullWhen(false)] out Exception error
        ) => GetPathFor(m_AssetLocation, relativePath, out path, out error);

        static bool GetPathFor(
            [DisallowNull] string basePath,
            [DisallowNull] string relativePath,
            [NotNullWhen(true)] out string path,
            [NotNullWhen(false)] out Exception error
        )
        {
            path = Path.Combine(basePath, relativePath);
            error = default;
            return true;
        }
    }
}
