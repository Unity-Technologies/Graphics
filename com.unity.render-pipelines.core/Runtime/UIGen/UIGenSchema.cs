using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using Microsoft.CodeAnalysis.CSharp;

namespace UnityEngine.Rendering.UIGen
{
    // TODO: Make immutable
    class UIGenSchema
    {
        [DefaultValue(null)]
        public HashSet<Type> types = new();
        public List<UIGenSchemaFeature> features;
        public List<UIGenSchemaCategorizedProperty> properties;
    }

    abstract class UIGenSchemaFeature
    {
        [DefaultValue(null)]
        public HashSet<Type> validFor = new();
    }

    sealed class UIGenSchemaFeature<TParameter> : UIGenSchemaFeature
    {

    }

    abstract class UIGenSchemaFeatureParameters
    {
        public Type feature;
    }

    sealed class UIGenSchemaFeatureParameters<TParameter> : UIGenSchemaFeatureParameters
    {
        public TParameter parameters;
    }

    class UIGenSchemaProperty
    {
        [Flags]
        public enum Options
        {
            None = 0,
            Runtime = 1 << 0,
            Editor = 1 << 1,
            EditorForceUpdate = 1 << 2,
        }

        public Type type;
        public string name;
        public string tooltip;
        public List<UIGenSchemaFeatureParameters> features;
        public Options options;
        public List<UIGenSchemaProperty> children;
    }

    class UIGenSchemaCategory
    {
        public string primary;
        [DefaultValue("")]
        public string secondary;
    }

    class UIGenSchemaCategorizedProperty
    {
        public UIGenSchemaProperty property;
        public UIGenSchemaCategory category;
    }
}

namespace UnityEngine.Rendering.UIGen
{
    public class BindableView
    {
        public XmlDocument uxml;
        public CSharpSyntaxTree bindingCode;
    }

    class DebugMenuUIGenerator
    {
        public bool GenerateBindableView(
            [DisallowNull] UIGenSchema schema,
            [NotNullWhen(true)] out BindableView result,
            [NotNullWhen(false)] out Exception error
        )
        {

        }
    }
}
