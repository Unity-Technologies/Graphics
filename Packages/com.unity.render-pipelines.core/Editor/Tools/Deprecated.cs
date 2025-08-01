using System.Collections.Generic;
using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public partial class MaterialUpgrader
    {
        /// <summary>
        /// Material Upgrader dialog text.
        /// </summary>
        [Obsolete("DialogText has been deprecated. #from(6000.3)")]
        public static class DialogText
        {
            /// <summary>Material Upgrader title.</summary>
            public static readonly string title = "Material Upgrader";
            /// <summary>Material Upgrader proceed.</summary>
            public static readonly string proceed = "Proceed";
            /// <summary>Material Upgrader Ok.</summary>
            public static readonly string ok = "OK";
            /// <summary>Material Upgrader cancel.</summary>
            public static readonly string cancel = "Cancel";
            /// <summary>Material Upgrader no selection message.</summary>
            public static readonly string noSelectionMessage = "You must select at least one material.";
            /// <summary>Material Upgrader project backup message.</summary>
            public static readonly string projectBackMessage = "Make sure to have a project backup before proceeding.";
        }

        /// <summary>
        /// Checking if project folder contains any materials that are not using built-in shaders.
        /// </summary>
        /// <param name="upgraders">List if MaterialUpgraders</param>
        /// <returns>Returns true if at least one material uses a non-built-in shader (ignores Hidden, HDRP and Shader Graph Shaders)</returns>
        [Obsolete("Please directly use ProjectContainsNonAutomaticUpgradePath now. #from(6000.3)")]
        public static bool ProjectFolderContainsNonBuiltinMaterials(List<MaterialUpgrader> upgraders)
        {
            string[] pathsWhiteList = new[]
            {
                "Hidden/",
                "HDRP/",
                "Shader Graphs/"
            };

            foreach (var material in AssetDatabaseHelper.FindAssets<Material>(".mat"))
            {
                if (material.shader.name.ContainsAny(pathsWhiteList))
                    continue;

                if (!IsMaterialUpgradable(upgraders, material))
                    return true;
            }

            return false;
        }
    }
}
