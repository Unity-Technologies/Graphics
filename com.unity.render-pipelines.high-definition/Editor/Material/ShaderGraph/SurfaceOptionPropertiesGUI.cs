using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class SurfaceOptionPropertiesGUI
    {
        public delegate void Olol<T>(T data, ref object field);
        
        static TargetPropertyGUIContext    ctx;
        static Action                      change;
        static Action<String>              undo;

        /// <summary>Standard function to create the UI for a property</summary>
        static void AddProperty<Data, FieldType>(string displayName, ref Data field, BaseField<FieldType> elem)
        {
            // ctx.AddProperty(displayName, 0, elem, (evt) => {
            //     if (Equals(field, evt.newValue))
            //         return;

            //     undo(displayName);
            //     field = evt.newValue;
            //     change();
            // });
        }

        public static void AddProperties(SystemData systemData, ref TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo)
        {
            ctx = context;
            undo = registerUndo;
            change = onChange;
            // Alpha Test
            // TODO: AlphaTest is in SystemData but Alpha to Mask is in BuiltinData?
            // AddProperty("Alpha Clipping", systemData, nameof(systemData.alphaTest));

            // AddProperty("Alpha Clipping", () => systemData.alphaTest, (newValue) => systemData.alphaTest);

            // Misc
            context.AddProperty("Double-Sided Mode", 0, new EnumField(DoubleSidedMode.Disabled) { value = systemData.doubleSidedMode }, (evt) =>
            {
                if (Equals(systemData.doubleSidedMode, evt.newValue))
                    return;

                registerUndo("Double-Sided Mode");
                systemData.doubleSidedMode = (DoubleSidedMode)evt.newValue;
                onChange();
            });
        }
    }
}