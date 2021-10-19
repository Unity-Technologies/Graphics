using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Implement this interface to extract more data about volume components
    /// </summary>
    internal interface IVolumeComponentSetExtension
    {
        void Initialize([DisallowNull] VolumeComponentSet volumeComponentSet);
    }
}
