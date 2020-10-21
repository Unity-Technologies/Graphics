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
    internal abstract class HDShaderGUI : ShaderGUI
    {
        internal protected bool m_FirstFrame = true;

        // The following set of functions are call by the ShaderGraph
        // It will allow to display our common parameters + setup keyword correctly for them

        protected abstract void SetupMaterialKeywordsAndPassInternal(Material material);

        /// <summary>
        /// Unity calls this function when you assign a new shader to the material.
        /// </summary>
        /// <param name="material">The current material.</param>
        /// <param name="oldShader">The shader the material currently uses.</param>
        /// <param name="newShader">The new shader to assign to the material.</param>
        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            SetupMaterialKeywordsAndPassInternal(material);
        }

        /// <summary>
        /// Sets up the keywords and passes for the material. You must call this function after you change a property on a material to ensure it's validity.
        /// </summary>
        /// <param name="changed">GUI.changed is the usual value for this parameter. If this value is false, the function just exits.</param>
        /// <param name="materials">The materials to perform the setup on.</param>
        protected void ApplyKeywordsAndPassesIfNeeded(bool changed, Material[] materials)
        {
            // !!! HACK !!!
            // When a user creates a new Material from the contextual menu, the material is created from the editor code and the appropriate shader is applied to it.
            // This means that we never setup keywords and passes for a newly created material. The material is then in an invalid state.
            // To work around this, as the material is automatically selected when created, we force an update of the keyword at the first "frame" of the editor.

            // Apply material keywords and pass:
            if (changed || m_FirstFrame)
            {
                m_FirstFrame = false;

                foreach (var material in materials)
                    SetupMaterialKeywordsAndPassInternal(material);
            }
        }

        /// <summary>
        /// Unity calls this function when it displays the GUI. This method is sealed so you cannot override it. To implement your custom GUI, use OnMaterialGUI instead.
        /// </summary>
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
        /// Implement your custom GUI in this function. To display a UI similar to HDRP shaders, use a MaterialUIBlock.
        /// </summary>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="props">The list of properties the material has.</param>
        protected abstract void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props);

        readonly static string[] floatPropertiesToSynchronize = {
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
                if (material.HasProperty(floatToSync))
                    material.SetFloat(floatToSync, defaultProperties.GetFloat(floatToSync));

            CoreUtils.Destroy(defaultProperties);
            defaultProperties = null;
        }
    }
}
