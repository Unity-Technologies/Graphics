using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Interface that is meant to be implemented by any entity that wants to communicate with the preview manager
    /// </summary>
    public interface IPreviewUpdateListener
    {
        void HandlePreviewTextureUpdated(Texture newPreviewTexture);

        void RequestPreviewUpdate(string listenerID);
    }
}
