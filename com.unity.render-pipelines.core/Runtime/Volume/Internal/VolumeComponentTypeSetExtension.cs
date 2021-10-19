using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Implement this class to extract more data about volume components.
    ///
    /// The purpose is to extract immutable data from the volume component set.
    /// </summary>
    abstract class VolumeComponentTypeSetExtension
    {
    }

    interface IVolumeComponentTypeSetExtensionFactory<T>
        where T : VolumeComponentTypeSetExtension
    {
        [return: NotNull] T Create([DisallowNull] VolumeComponentTypeSet volumeComponentTypeSet);
    }
}
