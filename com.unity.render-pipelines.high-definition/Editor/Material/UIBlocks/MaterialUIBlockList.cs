using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System.Linq;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    // A Material can be authored from the shader graph or by hand. When written by hand we need to provide an inspector.
    // Such a Material will share some properties between it various variant (shader graph variant or hand authored variant).
    // To create a such GUI, we provide Material UI Blocks, a modular API to create custom Material UI that allow
    // you to reuse HDRP pre-defined blocks and access support header toggles automatically.
    // Examples of such material UIs can be found in the classes UnlitGUI, LitGUI or LayeredLitGUI.

    /// <summary>
    /// Wrapper to handle Material UI Blocks, it will handle initialization of the blocks when drawing the GUI.
    /// </summary>
    public class MaterialUIBlockList : List<MaterialUIBlock>
    {
        [System.NonSerialized]
        bool        m_Initialized = false;

        Material[]  m_Materials;

        public Material[] materials => m_Materials;

        /// <summary>
        /// Render the list of ui blocks added contained in the materials property
        /// </summary>
        /// <param name="materialEditor"></param>
        /// <param name="properties"></param>
        public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            Initialize(materialEditor, properties);
            foreach (var uiBlock in this)
            {
                // We load material properties at each frame because materials can be animated and to make undo/redo works
                uiBlock.UpdateMaterialProperties(properties);
                uiBlock.OnGUI();
            }
        }

        /// <summary>
        /// Initialize the ui blocks, can be called at every frame, a guard is prevents more that one initialization
        /// <remarks>This function is called automatically by MaterialUIBlockList.OnGUI so you only need this when you want to render the UI Blocks in a custom order</remarks>
        /// </summary>
        public void Initialize(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (!m_Initialized)
            {
                foreach (var uiBlock in this)
                    uiBlock.Initialize(materialEditor, properties, this);

                m_Materials = materialEditor.targets.Select(target => target as Material).ToArray();
                m_Initialized = true;
            }
        }

        /// <summary>
        /// Fetch the first ui block of type T in the current list of material blocks
        /// </summary>
        /// <typeparam name="T">MaterialUIBlock type</typeparam>
        /// <returns></returns>
        public T FetchUIBlock< T >() where T : MaterialUIBlock
        {
            return this.FirstOrDefault(uiBlock => uiBlock is T) as T;
        }
    }
}