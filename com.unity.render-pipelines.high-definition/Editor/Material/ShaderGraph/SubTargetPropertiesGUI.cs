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
        List<string> lockedProperties;

        public List<SubTargetPropertyBlock> uiBlocks = new List<SubTargetPropertyBlock>();

        public SubTargetPropertiesGUI(TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo,
                                      SystemData systemData, BuiltinData builtinData, LightingData lightingData,
                                      List<string> lockedProperties)
        {
            this.context = context;
            this.onChange = onChange;
            this.registerUndo = registerUndo;
            this.systemData = systemData;
            this.builtinData = builtinData;
            this.lightingData = lightingData;
            this.lockedProperties = lockedProperties;
        }

        public void AddPropertyBlock(SubTargetPropertyBlock block)
        {
            block.Initialize(context, onChange, registerUndo, systemData, builtinData, lightingData, lockedProperties);
            block.CreatePropertyGUIWithHeader();
            Add(block);
        }
    }
}
