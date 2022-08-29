namespace UnityEngine.Rendering.HighDefinition
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
        bool IsActive() => true;

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <param name="hdCamera">The current camera that renders your post processing effect.</param>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        bool IsActive(HDCamera hdCamera) => IsActive();
    }
}
