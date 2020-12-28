using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;
using RenderQueueType = UnityEngine.Rendering.HighDefinition.HDRenderQueue.RenderQueueType;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    abstract class SubTargetPropertyBlock : VisualElement
    {
        // Null/Empty means no title
        protected virtual string title => null;
        
        protected TargetPropertyGUIContext context;
        protected Action onChange;
        protected Action<String> registerUndo;
        protected SystemData systemData;
        protected BuiltinData builtinData;
        protected LightingData lightingData;

        internal void Initialize(TargetPropertyGUIContext context, Action onChange, Action<String> registerUndo,
            SystemData systemData, BuiltinData builtinData, LightingData lightingData)
        {
            this.context = context;
            this.onChange = onChange;
            this.registerUndo = registerUndo;
            this.systemData = systemData;
            this.builtinData = builtinData;
            this.lightingData = lightingData;
        }

        // Utility function to create UIElement fields:
        protected void AddProperty<Data>(string displayName, Func<Data> getter, Action<Data> setter, int indentLevel = 0)
            => AddProperty<Data>(new GUIContent(displayName), getter, setter, indentLevel);

        protected void AddProperty<Data>(GUIContent displayName, Func<Data> getter, Action<Data> setter, int indentLevel = 0)
        {
            // Create UIElement from type:
            BaseField<Data> elem = null;
            BaseField<Enum> elemEnum = null;

            switch (getter())
            {
                case bool b: elem = new Toggle { value = b, tooltip = displayName.tooltip } as BaseField<Data>; break;
                case int i: elem = new IntegerField { value = i, tooltip = displayName.tooltip } as BaseField<Data>; break;
                case float f: elem = new FloatField { value = f, tooltip = displayName.tooltip } as BaseField<Data>; break;
                case SurfaceType e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case RenderQueueType e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case BlendMode e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case CompareFunction e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case TransparentCullMode e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case DoubleSidedMode e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case NormalDropOffSpace e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case HDLitData.MaterialType e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case DistortionMode e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case ScreenSpaceRefraction.RefractionModel e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case SpecularOcclusionMode e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case FabricData.MaterialType e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case EyeData.MaterialType e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case StackLit.BaseParametrization e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case StackLit.DualSpecularLobeParametrization e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                case OpaqueCullMode e: elemEnum = new EnumField(e) { value = e, tooltip = displayName.tooltip } as BaseField<Enum>; break;
                default: throw new Exception($"Can't create UI field for type {getter().GetType()}, please add it if it's relevant. If you can't consider using TargetPropertyGUIContext.AddProperty instead.");
            }

            if (elem != null)
            {
                context.AddProperty<Data>(displayName.text, indentLevel, elem, (evt) => {
                    if (Equals(getter(), evt.newValue))
                        return;

                    registerUndo(displayName.text);
                    setter(evt.newValue);
                    onChange();
                });
            }
            else
            {
                context.AddProperty<Enum>(displayName.text, indentLevel, elemEnum, (evt) => {
                    if (Equals(getter(), evt.newValue))
                        return;

                    registerUndo(displayName.text);
                    setter((Data)(object)evt.newValue);
                    onChange();
                });
            }
        }

        protected void AddFoldout(string text, Func<bool> getter, Action<bool> setter)
            => AddFoldout(new GUIContent(text), getter, setter);

        protected void AddFoldout(GUIContent content, Func<bool> getter, Action<bool> setter)
        {
            var foldout = new Foldout() {
                value = getter(),
                text = content.text,
                tooltip = content.tooltip
            };

            foldout.RegisterValueChangedCallback((evt) => {
                setter(evt.newValue);
                onChange();
            });

            // Apply padding:
            foldout.style.paddingLeft = context.globalIndentLevel * 15;

            context.Add(foldout);
        }

        protected void AddHelpBox(string message, MessageType type)
        {
            // We don't use UIElement HelpBox because it's width is not dynamic.
            int indentLevel = context.globalIndentLevel;
            var imgui = new IMGUIContainer(() => 
            {
                float indentPadding = indentLevel * 15;
                var rect = EditorGUILayout.GetControlRect(false, 42);
                rect.x += indentPadding;
                rect.width -= indentPadding;
                EditorGUI.HelpBox(rect, message, type);
            });

            context.Add(imgui);
        }

        public void CreatePropertyGUIWithHeader()
        {
            if (!String.IsNullOrEmpty(title))
            {
                int index = foldoutIndex;
                AddFoldout(title,
                    () => (systemData.inspectorFoldoutMask & (1 << index)) != 0,
                    (value) =>
                    {
                        systemData.inspectorFoldoutMask &= ~(1 << index); // Clear
                        systemData.inspectorFoldoutMask |= (value ? 1 : 0) << index; // Set
                    }
                );
                context.globalIndentLevel++;
                if ((systemData.inspectorFoldoutMask & (1 << index)) != 0)
                    CreatePropertyGUI();
                context.globalIndentLevel--;
            }
            else
                CreatePropertyGUI();
        }

        protected abstract void CreatePropertyGUI();

        /// <summary>Warning: this property must have a different value for each property block type!</summary>
        protected abstract int foldoutIndex { get; }
    }
}
