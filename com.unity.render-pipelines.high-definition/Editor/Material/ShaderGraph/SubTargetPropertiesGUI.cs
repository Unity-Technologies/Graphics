using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    class SubTargetPropertiesGUI : VisualElement
    {
        TargetPropertyGUIContext context;
        Action onChange;
        Action<String> registerUndo;
        SystemData systemData;
        BuiltinData builtinData;
        LightingData lightingData;

        public List<SubTargetPropertyBlock> uiBlocks = new List<SubTargetPropertyBlock>();

        public SubTargetPropertiesGUI(TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo,
                                      SystemData systemData, BuiltinData builtinData, LightingData lightingData)
        {
            this.context = context;
            this.onChange = onChange;
            this.registerUndo = registerUndo;
            this.systemData = systemData;
            this.builtinData = builtinData;
            this.lightingData = lightingData;
        }

        public void AddPropertyBlock(SubTargetPropertyBlock block)
        {
            block.Initialize(context, onChange, registerUndo, systemData, builtinData, lightingData);
            block.CreatePropertyGUIWithHeader();
            Add(block);
        }
    }
}
