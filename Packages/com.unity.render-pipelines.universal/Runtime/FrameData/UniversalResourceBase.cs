using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Base class for URP texture data.
    /// </summary>
    public abstract class UniversalResourceDataBase : ContextItem
    {
        /// <summary>
        /// Options for the active color &amp; depth target texture.
        /// </summary>
        internal enum ActiveID
        {
            /// <summary>The camera buffer.</summary>
            Camera,

            /// <summary>The backbuffer.</summary>
            BackBuffer
        }

        internal bool isAccessible { get; set; }

        internal void InitFrame()
        {
            isAccessible = true;
        }

        internal void EndFrame()
        {
            isAccessible = false;
        }

        /// <summary>
        /// Updates the texture handle if the texture is accessible.
        /// </summary>
        /// <param name="handle">Handle to update.</param>
        /// <param name="newHandle">Handle of the new data.</param>
        protected void CheckAndSetTextureHandle(ref TextureHandle handle, TextureHandle newHandle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return;

            handle = newHandle;
        }

        /// <summary>
        /// Fetches the texture handle if the texture is accessible.
        /// </summary>
        /// <param name="handle">Handle to the texture you want to retrieve</param>
        /// <returns>Returns the handle if the texture is accessible and a null handle otherwise.</returns>
        protected TextureHandle CheckAndGetTextureHandle(ref TextureHandle handle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return TextureHandle.nullHandle;

            return handle;
        }

        /// <summary>
        /// Updates the texture handles if the texture is accessible. The current and new handles needs to be of the same size.
        /// </summary>
        /// <param name="handle">Handles to update.</param>
        /// <param name="newHandle">Handles of the new data.</param>
        protected void CheckAndSetTextureHandle(ref TextureHandle[] handle, TextureHandle[] newHandle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return;

            if (handle == null || handle.Length != newHandle.Length)
                handle = new TextureHandle[newHandle.Length];

            for (int i = 0; i < newHandle.Length; i++)
                handle[i] = newHandle[i];
        }

        /// <summary>
        /// Fetches the texture handles if the texture is accessible.
        /// </summary>
        /// <param name="handle">Handles to the texture you want to retrieve</param>
        /// <returns>Returns the handles if the texture is accessible and a null handle otherwise.</returns>
        protected TextureHandle[] CheckAndGetTextureHandle(ref TextureHandle[] handle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return new []{TextureHandle.nullHandle};

            return handle;
        }

        /// <summary>
        /// Check if the texture is accessible.
        /// </summary>
        /// <returns>Returns true if the texture is accessible and false otherwise.</returns>
        protected bool CheckAndWarnAboutAccessibility()
        {
            if (!isAccessible)
                Debug.LogError("Trying to access Universal Resources outside of the current frame setup.");

            return isAccessible;
        }
    }
}
