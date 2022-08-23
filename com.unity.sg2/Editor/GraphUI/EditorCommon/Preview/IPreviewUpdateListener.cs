﻿using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Interface that should be implemented by any entity that wants to act as a view model for preview data
    /// </summary>
    public interface IPreviewUpdateListener
    {
        void HandlePreviewTextureUpdated(Texture newPreviewTexture);

        void HandlePreviewShaderErrors(ShaderMessage[] shaderMessages);

        Texture PreviewTexture { get; }

        int CurrentVersion { get; }

        string ListenerID { get; }
    }
}
