using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Material upgrader and relevant utilities for SpeedTree 9.
    /// </summary>
    public class SpeedTree9MaterialUpgrader : MaterialUpgrader
    {
        /// <summary>
        /// Postprocesses materials while you are importing a SpeedTree 9 asset. Call from OnPostprocessSpeedTree in a MaterialPostprocessor.
        /// </summary>
        /// <param name="speedtree">The GameObject Unity creates from this imported SpeedTree.</param>
        /// <param name="finalizer">Render pipeline-specific material finalizer.</param>
        protected static void PostprocessSpeedTree9Materials(GameObject speedtree, MaterialFinalizer finalizer = null)
        {
            LODGroup lg = speedtree.GetComponent<LODGroup>();
            LOD[] lods = lg.GetLODs();
            for (int l = 0; l < lods.Length; l++)
            {
                LOD lod = lods[l];
                foreach (Renderer r in lod.renderers)
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m == null)
                            continue;

                        if (finalizer != null)
                            finalizer(m);
                    }
                }
            }
        }
    }
}
