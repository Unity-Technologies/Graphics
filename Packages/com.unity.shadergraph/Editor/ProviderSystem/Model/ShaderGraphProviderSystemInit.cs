using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Forwards ProviderLibrary reload notifications to any open MaterialGraphEditWindow so
    // graphs displaying the affected providers refresh their state.
    [InitializeOnLoad]
    static class ShaderGraphProviderSystemInit
    {
        static ShaderGraphProviderSystemInit()
        {
            ProviderLibrary.OnProvidersReloaded += OnProvidersReloaded;
        }

        static void OnProvidersReloaded(IReadOnlyList<GUID> modifiedAssetIDs)
        {
            if (modifiedAssetIDs == null || modifiedAssetIDs.Count == 0) return;

            var editors = Resources.FindObjectsOfTypeAll<MaterialGraphEditWindow>();
            foreach (var editor in editors)
                editor.NotifyDependencyUpdated(modifiedAssetIDs);
        }
    }
}
