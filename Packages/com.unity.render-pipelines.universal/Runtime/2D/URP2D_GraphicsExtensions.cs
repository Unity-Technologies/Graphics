using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class <c>URP2D_GraphicsExtensions</c> provides additional functions to extend built in graphics classes in URP2D
    /// </summary>
    public static class URP2D_GraphicsExtensions
    {
        /// <summary>
        /// Gets the SpriteMaskInteraction state for MeshRenderer
        /// </summary>
        /// <param name="meshRenderer"> The <see cref="MeshRenderer"/> instance to query.</param>
        /// <returns>Returns the SpriteMaskInteraction</returns>
        public static SpriteMaskInteraction GetSpriteMaskInteraction(this MeshRenderer meshRenderer) { return meshRenderer.Internal_GetSpriteMaskInteraction(); }

        /// <summary>
        /// Gets the SpriteMaskInteraction state for SkinnedMeshRenderer
        /// </summary>
        /// <param name="skinnedMeshRenderer"> The <see cref="SkinnedMeshRenderer"/> instance to query.</param>
        /// <returns>Returns the SpriteMaskInteraction</returns>
        public static SpriteMaskInteraction GetSpriteMaskInteraction(this SkinnedMeshRenderer skinnedMeshRenderer) { return skinnedMeshRenderer.Internal_GetSpriteMaskInteraction(); }

        /// <summary>
        /// Sets the SpriteMaskInteraction state for SkinnedMeshRenderer
        /// </summary>
        /// <param name="meshRenderer"> The <see cref="MeshRenderer"/> instance to modify.</param>
        /// <param name="maskInteraction"> The mask interaction state to set.</param>
        public static void SetSpriteMaskInteraction(this MeshRenderer meshRenderer, SpriteMaskInteraction maskInteraction) { meshRenderer.Internal_SetSpriteMaskInteraction(maskInteraction); }

        /// <summary>
        /// Sets the SpriteMaskInteraction state for SkinnedMeshRenderer
        /// </summary>
        /// <param name="skinnedMeshRenderer"> The <see cref="SkinnedMeshRenderer"/> instance to modify.</param>
        /// <param name="maskInteraction"> The mask interaction state to set.</param>
        public static void SetSpriteMaskInteraction(this SkinnedMeshRenderer skinnedMeshRenderer, SpriteMaskInteraction maskInteraction) { skinnedMeshRenderer.Internal_SetSpriteMaskInteraction(maskInteraction); }
    }
}
