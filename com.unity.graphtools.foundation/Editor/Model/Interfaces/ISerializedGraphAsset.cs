using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for <see cref="IGraphAsset"/> than can be saved in a file.
    /// </summary>
    public interface ISerializedGraphAsset : IGraphAsset
    {
        /// <summary>
        /// The path of the file that contains the asset.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Creates a file to store the asset, if it does not already exists.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        void CreateFile(string path);

        /// <summary>
        /// Saves the asset to the file.
        /// </summary>
        void Save();

        /// <summary>
        /// Import the asset from the file, returning the imported asset as a result.
        /// </summary>
        /// <remarks>If the import can be done in place, the returned asset is this objet. Otherwise, a new asset object is returned.</remarks>
        /// <returns>The imported asset.</returns>
        ISerializedGraphAsset Import();
    }
}
