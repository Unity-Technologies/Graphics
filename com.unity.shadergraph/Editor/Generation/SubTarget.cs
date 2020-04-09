using System;
using System.Collections.Generic;
using System.Reflection;
using Data.Interfaces;
using Drawing.Inspector;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable, GenerationAPI] // TODO: Public
    internal abstract class SubTarget : IInspectable
    {
        internal abstract Type targetType { get; }
        internal Target target { get; set; }
        public string displayName { get; set; }
        public object GetObjectToInspect()
        {
            return this;
        }

        public PropertyInfo[] GetPropertyInfo()
        {
            return this.GetType().GetProperties();
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            //Currently not implemented
        }

        public abstract bool IsActive();
        public abstract void Setup(ref TargetSetupContext context);
        public abstract void GetFields(ref TargetFieldContext context);
        public abstract void GetActiveBlocks(ref TargetActiveBlockContext context);
        public abstract void GetPropertiesGUI(ref TargetPropertyGUIContext context, Action onChange);

        public virtual void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode) { }
        public virtual void ProcessPreviewMaterial(Material material) { }
    }

    [GenerationAPI] // TODO: Public
    internal abstract class SubTarget<T> : SubTarget where T : Target
    {
        internal override Type targetType => typeof(T);

        public new T target
        {
            get => base.target as T;
            set => base.target = value;
        }
    }
}
