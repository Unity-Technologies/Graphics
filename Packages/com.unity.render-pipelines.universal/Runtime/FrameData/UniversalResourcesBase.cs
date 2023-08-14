using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    abstract class UniversalResourcesDataBase : ContextItem
    {
        /// <summary>
        /// Options for the active color & depth target texture.
        /// </summary>
        internal enum ActiveID
        {
            Camera,
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

        protected void CheckAndSetTextureHandle(ref TextureHandle handle, TextureHandle newHandle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return;

            handle = newHandle;
        }

        protected TextureHandle CheckAndGetTextureHandle(ref TextureHandle handle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return TextureHandle.nullHandle;

            return handle;
        }

        protected void CheckAndSetTextureHandle(ref TextureHandle[] handle, TextureHandle[] newHandle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return;

            if (handle == null || handle.Length != newHandle.Length)
                handle = new TextureHandle[newHandle.Length];

            for (int i = 0; i < newHandle.Length; i++)
                handle[i] = newHandle[i];
        }

        protected TextureHandle[] CheckAndGetTextureHandle(ref TextureHandle[] handle)
        {
            if (!CheckAndWarnAboutAccessibility())
                return new []{TextureHandle.nullHandle};

            return handle;
        }

        protected bool CheckAndWarnAboutAccessibility()
        {
            if (!isAccessible)
                Debug.LogError("Trying to access Universal Resources outside of the current frame setup.");

            return isAccessible;
        }
    }
}
