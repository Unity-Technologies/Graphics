using UnityEditor;
using UnityEngine;

namespace UnityEngine.Rendering.PostProcessing
{
    [InitializeOnLoad]
    internal static class DeprecationNotice
    {
        const string k_DialogOptOutKey = "PPv2.DeprecationNotice";
        const string k_SessionShownKey = "PPv2.DeprecationNotice.SessionShown";

        static DeprecationNotice()
        {
            // Check if user has permanently opted out
            if (EditorPrefs.GetBool(k_DialogOptOutKey, false))
                return;

            // Check if already shown this session
            if (SessionState.GetBool(k_SessionShownKey, false))
                return;

            // Mark as shown for this session
            SessionState.SetBool(k_SessionShownKey, true);

            // Use EditorApplication.update to show dialog after editor is fully initialized
            EditorApplication.update += ShowDeprecationDialog;
        }

        static void ShowDeprecationDialog()
        {
            // Remove the callback so it only runs once
            EditorApplication.update -= ShowDeprecationDialog;

            // Use Unity's built-in opt-out dialog with ForThisUser (permanent checkbox)
            // Clicking OK without checkbox = dismiss for this session only
            // Checking the checkbox = dismiss permanently
            EditorUtility.DisplayDialog(
                "Post Processing Stack v2 - Deprecated",
                "This package is deprecated and no longer actively developed.\n\n" +
                "For URP and HDRP projects, use the integrated post-processing via the Volume framework.\n\n" +
                "For Built-in Render Pipeline projects, consider migrating to URP.\n\n" +
                "See documentation for migration guidance:\n" +
                "https://docs.unity3d.com/Manual/PostProcessingOverview.html",
                "OK",
                DialogOptOutDecisionType.ForThisUser,
                k_DialogOptOutKey);
        }
    }
}
