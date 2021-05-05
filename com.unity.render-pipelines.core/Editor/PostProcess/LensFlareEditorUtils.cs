using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    static class LensFlareEditorUtils
    {
        static class Icons
        {
            const string k_IconFolder = @"Packages/com.unity.render-pipelines.core/Editor/PostProcess/LensFlareResource/";
            public static readonly Texture2D circle = CoreEditorUtils.LoadIcon(k_IconFolder, "CircleFlareThumbnail", forceLowRes: false);
            public static readonly Texture2D polygon = CoreEditorUtils.LoadIcon(k_IconFolder, "PolygonFlareThumbnail", forceLowRes: false);
            public static readonly Texture2D generic = CoreEditorUtils.LoadIcon(k_IconFolder, "Flare128", forceLowRes: false);
        }
    }
}
