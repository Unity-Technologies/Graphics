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

    abstract class HDShaderGUI : ShaderGUI
    {
        protected bool m_FirstFrame = true;

        // The following set of functions are call by the ShaderGraph
        // It will allow to display our common parameters + setup keyword correctly for them
        protected abstract void SetupMaterialKeywordsAndPassInternal(Material material);

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            ResetMaterialCustomRenderQueue(material);

            SetupMaterialKeywordsAndPassInternal(material);
        }

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

        protected abstract void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props);

        protected static void ResetMaterialCustomRenderQueue(Material material)
        {
            HDRenderQueue.RenderQueueType targetQueueType;
            switch (material.GetSurfaceType())
            {
                case SurfaceType.Opaque:
                    targetQueueType = HDRenderQueue.GetOpaqueEquivalent(HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue));
                    break;
                case SurfaceType.Transparent:
                    targetQueueType = HDRenderQueue.GetTransparentEquivalent(HDRenderQueue.GetTypeByRenderQueueValue(material.renderQueue));
                    break;
                default:
                    throw new ArgumentException("Unknown SurfaceType");
            }

            // Decal doesn't have properties to compute the render queue 
            if (material.HasProperty(kTransparentSortPriority) && material.HasProperty(kAlphaCutoffEnabled))
            {
                float sortingPriority = material.GetFloat(kTransparentSortPriority);
                bool alphaTest = material.GetFloat(kAlphaCutoffEnabled) > 0.5f;
                material.renderQueue = HDRenderQueue.ChangeType(targetQueueType, (int)sortingPriority, alphaTest);
            }
        }

        readonly static string[] floatPropertiesToSynchronize = {
            "_UseShadowThreshold", kReceivesSSR, kUseSplitLighting
        };

        protected static void SynchronizeShaderGraphProperties(Material material)
        {
            var defaultProperties = new Material(material.shader);
            foreach (var floatToSync in floatPropertiesToSynchronize)
                if (material.HasProperty(floatToSync))
                    material.SetFloat(floatToSync, defaultProperties.GetFloat(floatToSync));
            defaultProperties = null;
        }
    }
}
