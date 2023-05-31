using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition;
using System.Reflection;

public class HDRPSettingSetter
{
    public static void SetCustomPasses( bool state )
    {
        var assetsGUIDs = AssetDatabase.FindAssets("t:HDRenderPipelineAsset");

        var needToSet = new List<HDRenderPipelineAsset>();

        foreach( var guid in assetsGUIDs )
        {
            var hdrpAsset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if ( hdrpAsset == null )
                continue;
            var settings = hdrpAsset.currentPlatformRenderPipelineSettings;

            if (settings.supportCustomPass != state )
                needToSet.Add( hdrpAsset );
        }

        if (needToSet.Count > 0)
        {
            if ( EditorUtility.DisplayDialog("Change HDRP Assets settings", $"Settings need to be changed for {needToSet.Count} asset(s).", "Ok, let's do it !", "No thanks, I'll do it myself") )
            {
                var rpSettingsField = typeof(HDRenderPipelineAsset).GetField("m_RenderPipelineSettings", BindingFlags.NonPublic | BindingFlags.Instance );

                foreach( var hdrpAsset in needToSet )
                {
                    var settings = hdrpAsset.currentPlatformRenderPipelineSettings;
                    settings.supportCustomPass = state;

                    rpSettingsField.SetValue( hdrpAsset, settings );
                    EditorUtility.SetDirty( hdrpAsset );
                    AssetDatabase.SaveAssetIfDirty(hdrpAsset);
                }
            }
        }
    }
}
