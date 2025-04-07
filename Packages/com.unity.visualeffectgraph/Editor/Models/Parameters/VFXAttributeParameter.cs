using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using UnityEditor.VFX.Block;
using UnityEngine;
using UnityEngine.VFX;
using Debug = UnityEngine.Debug;

namespace UnityEditor.VFX
{
    // This derived variant is used to provide a documentation link based on the attribute name
    class GetAttributeVariant : Variant
    {
        private string attribute;
        public GetAttributeVariant(string name, string category)
        : base("Get".AppendLiteral(ObjectNames.NicifyVariableName(name)),
            $"Attribute/{category}",
            typeof(VFXAttributeParameter),
            new[] { new KeyValuePair<string, object>("attribute", name) })
        {
            this.attribute = char.ToUpper(name[0]) + name.Substring(1);
        }
        public override string GetDocumentationLink()
        {
            return string.Format(base.GetDocumentationLink(), this.attribute);
        }
    }

    class AttributeProvider : IVFXModelStringProvider
    {
        public string[] GetAvailableString(VFXModel model)
        {
            return model.GetGraph().attributesManager.GetAllNamesOrCombination(true, false, true, false).ToArray();
        }
    }

    class ReadWritableAttributeProvider : IVFXModelStringProvider
    {
        public string[] GetAvailableString(VFXModel model)
        {
            return model.GetGraph().attributesManager.GetAllNamesOrCombination(true, false, false, false).ToArray();
        }
    }

    class AttributeVariant : VariantProvider
    {
        public override IEnumerable<Variant> GetVariants()
        {
            var groups = VFXAttributesManager
                .GetBuiltInAttributesOrCombination(true, false, true, false)
                .GroupBy(x => x.category);

            foreach (var group in groups)
            {
                foreach (var attribute in group)
                {
                    yield return new GetAttributeVariant(attribute.name, attribute.category);
                }
            }
        }
    }
    
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
    internal class VFXParameterHelpURLAttribute : VFXHelpURLAttribute
    {
        public VFXParameterHelpURLAttribute(string pageName, string pageHash = "")
            : base(pageName, pageHash)
        {
        }
        public override string URL
        {
            get
            {
                if (Selection.activeObject is not VFXAttributeParameter parameter) 
                    return base.URL;
                
                var parameterAttribute = parameter.attribute;
                var formattedString = char.ToUpper(parameterAttribute[0]) + parameterAttribute.Substring(1);
                return string.Format(base.URL, formattedString);
            }
        }
    }

    [VFXParameterHelpURL("Operator-GetAttribute{0}")]
    [VFXInfo(category = "Attribute", variantProvider = typeof(AttributeVariant))]
    class VFXAttributeParameter : VFXOperator, IVFXAttributeUsage
    {
        [VFXSetting(VFXSettingAttribute.VisibleFlags.InInspector), StringProvider(typeof(AttributeProvider)), Tooltip("Specifies which attribute to use.")]
        public string attribute = VFXAttributesManager.GetBuiltInNamesOrCombination(true, true, true, false).First();

        [VFXSetting, Tooltip("Specifies which version of the parameter to use. It can return the current value, or the source value derived from a GPU event or a spawn attribute.")]
        public VFXAttributeLocation location = VFXAttributeLocation.Current;

        [VFXSetting, Regex("[^x-zX-Z]", 3), Tooltip("Sets the axes and the order in which they are derived. The input can be only the letters x, y, and z, in any combination, up to a length of 3 (i.e. xyz).")]
        public string mask = "xyz";

        protected override IEnumerable<string> filteredOutSettings
        {
            get
            {
                if (string.IsNullOrEmpty(attribute))
                {
                    yield break;
                }
                foreach (string setting in base.filteredOutSettings)
                    yield return setting;
                if (currentAttribute.variadic == VFXVariadic.False)
                    yield return "mask";
            }
        }

        public IEnumerable<VFXAttribute> usedAttributes
        {
            get { yield return currentAttribute; }
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (string.IsNullOrEmpty(attribute))
                {
                    yield break;
                }

                var vfxAttribute = currentAttribute;

                var tooltip = !string.IsNullOrEmpty(vfxAttribute.description) ? new TooltipAttribute(vfxAttribute.description) : null;
                var attr = tooltip != null ? new VFXPropertyAttributes(tooltip) : new VFXPropertyAttributes();
                if (vfxAttribute.variadic == VFXVariadic.True)
                {
                    Type slotType = null;
                    switch (mask.Length)
                    {
                        case 1: slotType = typeof(float); break;
                        case 2: slotType = typeof(Vector2); break;
                        case 3: slotType = typeof(Vector3); break;
                        case 4: slotType = typeof(Vector4); break;
                        default: break;
                    }

                    if (slotType != null)
                        yield return new VFXPropertyWithValue(new VFXProperty(slotType, vfxAttribute.name, attr));
                }
                else
                {
                    yield return new VFXPropertyWithValue(new VFXProperty(VFXExpression.TypeToType(vfxAttribute.type), ObjectNames.NicifyVariableName(vfxAttribute.name), attr));
                }
            }
        }

        public override string name
        {
            get
            {
                string result = "Get".AppendLiteral(attribute).AppendLabel(location.ToString());

                if (GetGraph() is {} graph && graph.attributesManager.TryFind(attribute, out var attrib))
                {
                    if (attrib.variadic == VFXVariadic.True)
                        result += "." + mask;
                }

                return result;
            }
        }

        public override void Sanitize(int version)
        {
            var graph = GetGraph();
            if (graph != null)
            {
                if (!graph.attributesManager.Exist(attribute))
                {
                    Debug.LogWarningFormat("Attribute parameter was removed because attribute {0} does not exist",
                        attribute);
                    RemoveModel(this, false);
                    return; // Dont sanitize further, model was removed
                }

                VFXBlockUtility.SanitizeAttribute(graph, ref attribute, ref mask, version);
            }
            else
            {
                throw new InvalidOperationException("Graph is null during Sanitize");
            }

            base.Sanitize(version);
        }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var vfxAttribute = currentAttribute;
            if (vfxAttribute.variadic == VFXVariadic.True)
            {
                var attributes = new VFXAttribute[] { VFXAttributesManager.FindBuiltInOnly(vfxAttribute.name + "X"), VFXAttributesManager.FindBuiltInOnly(vfxAttribute.name + "Y"), VFXAttributesManager.FindBuiltInOnly(vfxAttribute.name + "Z") };
                var expressions = attributes.Select(a => new VFXAttributeExpression(a, location)).ToArray();

                var componentStack = new Stack<VFXExpression>();
                int outputSize = mask.Length;
                for (int iComponent = 0; iComponent < outputSize; iComponent++)
                {
                    char componentChar = char.ToLower(mask[iComponent]);
                    int currentComponent = Math.Min(componentChar - 'x', 2);
                    componentStack.Push(expressions[currentComponent]);
                }

                VFXExpression finalExpression = null;
                if (componentStack.Count == 1)
                {
                    finalExpression = componentStack.Pop();
                }
                else
                {
                    finalExpression = new VFXExpressionCombine(componentStack.Reverse().ToArray());
                }
                return new[] { finalExpression };
            }
            else
            {
                var expression = new VFXAttributeExpression(vfxAttribute, location);
                return new VFXExpression[] { expression };
            }
        }
        public VFXAttribute currentAttribute
        {
            get
            {
                if (GetGraph() is { } graph)
                {
                    if (graph.attributesManager.TryFind(attribute, out var vfxAttribute))
                    {
                        return vfxAttribute;
                    }
                }
                else // Happens when the node is not yet added to the graph, but should be ok as soon as it's added (see OnAdded)
                {
                    var attr = VFXAttributesManager.FindBuiltInOnly(attribute);
                    if (string.Compare(attribute, attr.name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return attr;
                    }
                }

                // Temporary attribute
                return new VFXAttribute(attribute, VFXValueType.Float, null);
            }
        }

        public void Rename(string oldName, string newName)
        {
            if (GetGraph() is {} graph && graph.attributesManager.IsCustom(newName))
            {
                attribute = newName;
                SyncSlots(VFXSlot.Direction.kOutput, true);
            }
        }

        internal sealed override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);

            if (!CustomAttributeUtility.IsShaderCompilableName(attribute))
            {
                report.RegisterError("InvalidCustomAttributeName", VFXErrorType.Error, $"Custom attribute name '{attribute}' is not valid.\n\t- The name must not contain spaces or any special character\n\t- The name must not start with a digit character", this);
            }
        }

        protected override void OnAdded()
        {
            base.OnAdded();
            SyncCustomAttributeIfNeeded();
        }

        private void SyncCustomAttributeIfNeeded()
        {
            var graph = GetGraph();
            if (graph != null && graph.attributesManager.IsCustom(attribute))
            {
                Invalidate(InvalidationCause.kUIChangedTransient);
                SyncSlots(VFXSlot.Direction.kOutput, true);
            }
            else if (graph != null && !string.IsNullOrEmpty(attribute) && !graph.attributesManager.TryFind(attribute, out _))
            {
                graph.TryAddCustomAttribute(attribute, VFXValueType.Float, string.Empty, false, out _);
                graph.SetCustomAttributeDirty();
                Invalidate(InvalidationCause.kUIChangedTransient);
            }
        }
    }
}
