using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

// Include material common properties names
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // HDShaderGUI is here to provide a support for setup material keyword and pass function. It will allow the GUI
    // to setup the material properties needed for rendering when switching shaders on a material. For the GUI part
    // of the material you must use Material UI Blocks, examples of doing so can be found in the classes UnlitGUI,
    // LitGUI or LayeredLitGUI.

    /// <summary>
    /// Use this class to build your custom Shader GUI for HDRP.
    /// You can use a class that inherits from HDShaderGUI in the Shader Graph Custom EditorGUI field.
    /// </summary>
    public abstract class HDShaderGUI : ShaderGUI
    {
        /// <summary>
        /// Sets up the keywords and passes for the material you pass in as a parameter.
        /// </summary>
        /// <param name="material">Target material.</param>
        [Obsolete("SetupMaterialKeywordsAndPass has been renamed ValidateMaterial", false)]
        protected virtual void SetupMaterialKeywordsAndPass(Material material)
        {
            ValidateMaterial(material);
        }

        /// <summary>
        /// Unity calls this function when it displays the GUI. This method is sealed so you cannot override it. To implement your custom GUI, use OnMaterialGUI instead.
        /// </summary>
        /// <param name="materialEditor">Material editor instance.</param>
        /// <param name="props">The list of properties in the inspected material(s).</param>
        public sealed override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline))
            {
                EditorGUILayout.HelpBox("Editing HDRP materials is only supported when an HDRP asset assigned in the graphic settings", MessageType.Warning);
            }
            else
            {
                OnMaterialGUI(materialEditor, props);
            }
        }

        /// <summary>
        /// Implement your custom GUI in this function. To display a UI similar to HDRP shaders, use a MaterialUIBlockList.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties in the inspected material(s).</param>
        protected abstract void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props);

        readonly static string[] floatPropertiesToSynchronize =
        {
            kUseSplitLighting,
        };

        /// <summary>
        /// Synchronize a set of properties that Unity requires for Shader Graph materials to work correctly. This function is for Shader Graph only.
        /// </summary>
        /// <param name="material">The target material.</param>
        protected static void SynchronizeShaderGraphProperties(Material material)
        {
            var defaultProperties = new Material(material.shader);
            foreach (var floatToSync in floatPropertiesToSynchronize)
                if (material.HasProperty(floatToSync) && defaultProperties.HasProperty(floatToSync))
                    material.SetFloat(floatToSync, defaultProperties.GetFloat(floatToSync));

            CoreUtils.Destroy(defaultProperties);
            defaultProperties = null;
        }
    }
}
