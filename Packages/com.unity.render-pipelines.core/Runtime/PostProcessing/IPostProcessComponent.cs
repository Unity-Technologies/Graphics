using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Implement this interface on every post process volumes
    /// </summary>
    public interface IPostProcessComponent
    {
        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        bool IsActive();

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        bool IsTileCompatible() => false;
    }
}
