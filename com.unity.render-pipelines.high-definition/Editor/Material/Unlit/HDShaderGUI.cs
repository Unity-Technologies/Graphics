using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // HDShaderGUI is here to provide a support for setup material keyword and pass function. It will allow the GUI
    // to setup the material properties needed for rendering when switching shaders on a material. For the GUI part
    // of the material you must use Material UI Blocks, examples of doing so can be found in the classes UnlitGUI,
    // LitGUI or LayeredLitGUI.

    abstract class HDShaderGUI : ShaderGUI
    {
        // The following set of functions are call by the ShaderGraph
        // It will allow to display our common parameters + setup keyword correctly for them
        protected abstract void SetupMaterialKeywordsAndPassInternal(Material material);

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            // When switching shader, the custom RenderQueue is reset due to shader assignment
            // To keep the correct render queue we need to save it here, do the change and re-assign it
            int currentRenderQueue = material.renderQueue;
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            material.renderQueue = currentRenderQueue;

            SetupMaterialKeywordsAndPassInternal(material);
        }
    }
}
