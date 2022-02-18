using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;

namespace UnityEditor.Rendering.HighDefinition
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
        bool m_Initialized = false;

        Material[] m_Materials;

        /// <summary>
        /// Parent of the ui block list, in case of nesting (Layered Lit material)
        /// </summary>
        public MaterialUIBlockList parent;

        /// <summary>
        /// List of materials currently selected in the inspector
        /// </summary>
        public Material[] materials => m_Materials;

        /// <summary>
        /// Construct a sub ui block list by passing the parent ui block list (useful for layered UI where ui blocks are nested)
        /// </summary>
        /// <param name="parent"></param>
        public MaterialUIBlockList(MaterialUIBlockList parent) => this.parent = parent;

        /// <summary>
        /// Construct a ui block list
        /// </summary>
        public MaterialUIBlockList() : this(null) { }

        /// <summary>
        /// Render the list of ui blocks
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
                try
                {
                    // We load material properties at each frame because materials can be animated and to make undo/redo works
                    uiBlock.UpdateMaterialProperties(properties);
                    uiBlock.OnGUI();
                }
                // Never catch ExitGUIException as they are used to handle color picker and object pickers.
                catch (ExitGUIException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        /// <summary>
        /// Initialize the ui blocks
        /// <remarks>This function is called automatically by MaterialUIBlockList.OnGUI so you only need this when you want to render the UI Blocks in a custom order</remarks>
        /// </summary>
        /// <param name="materialEditor">Material editor instance.</param>
        /// <param name="properties">The list of properties in the inspected material(s).</param>
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
        public T FetchUIBlock<T>() where T : MaterialUIBlock
        {
            return this.FirstOrDefault(uiBlock => uiBlock is T) as T;
        }
    }
}
