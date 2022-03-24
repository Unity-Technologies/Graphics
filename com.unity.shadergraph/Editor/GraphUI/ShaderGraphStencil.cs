using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphStencil : Stencil
    {
        public const string Name = "ShaderGraph";

        public const string DefaultAssetName = "NewShaderGraph";

        public const string Extension = "sg2";

        public string ToolName => Name;

        Dictionary<RegistryKey, Dictionary<string, float>> m_NodeUIHints;

        // TODO: (Sai) When subgraphs come in, add support for dropdown section
        internal static readonly string[] sections = { "Properties", "Keywords" };

        public override IEnumerable<string> SectionNames => sections;

        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

        public ShaderGraphStencil()
        {
            m_NodeUIHints = new Dictionary<RegistryKey, Dictionary<string, float>>();
        }

        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel) => new SGBlackboardGraphModel(graphAssetModel);


        // See ShaderGraphExampleTypes.GetGraphType for more details
        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            if (typeHandle == TypeHandle.Vector2
                || typeHandle == TypeHandle.Vector3
                || typeHandle == TypeHandle.Vector4
                || typeHandle == TypeHandle.Float
                || typeHandle == TypeHandle.Bool
                || typeHandle == TypeHandle.Int
                || typeHandle == ShaderGraphExampleTypes.Color
                || typeHandle == ShaderGraphExampleTypes.Matrix4
                || typeHandle == ShaderGraphExampleTypes.Matrix3
                || typeHandle == ShaderGraphExampleTypes.Matrix2)
            {
                return typeof(GraphTypeConstant);
            }

            if (typeHandle == ShaderGraphExampleTypes.GradientTypeHandle)
            {
                return typeof(GradientTypeConstant);
            }

            // There is no inline editor for this port type, so there is no need for CLDS access.
            return typeof(AnyConstant);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return new ShaderGraphSearcherDatabaseProvider(this);
        }

        public override ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return new ShaderGraphSearcherFilterProvider();
        }

        private Registry.Registry RegistryInstance = null;

        public Registry.Registry GetRegistry()
        {
            if (RegistryInstance == null)
            {
                m_NodeUIHints.Clear();

                void ReadUIInfo(RegistryKey key, Type type)
                {
                    const string uiHintsGetterName = "get_UIHints";

                    var getUiHints = type.GetMethod(uiHintsGetterName);
                    if (getUiHints != null)
                    {
                        m_NodeUIHints[key] = (Dictionary<string, float>)getUiHints.Invoke(null, null);
                    }

                    // TODO: Get and use UI strings
                }

                RegistryInstance = Registry.Default.DefaultRegistry.CreateDefaultRegistry(afterNodeRegistered: ReadUIInfo);
            }

            return RegistryInstance;
        }

        public IReadOnlyDictionary<string, float> GetUIHints(RegistryKey nodeKey)
        {
            return m_NodeUIHints.GetValueOrDefault(nodeKey, new Dictionary<string, float>());
        }

        protected override void CreateGraphProcessors()
        {
            if (!AllowMultipleDataOutputInstances)
                GetGraphProcessorContainer().AddGraphProcessor(new ShaderGraphProcessor());
        }

        static readonly TypeHandle[] k_SupportedBlackboardTypes = {
            TypeHandle.Int,
            TypeHandle.Float,
            TypeHandle.Bool,
            TypeHandle.Vector2,
            TypeHandle.Vector3,
            TypeHandle.Vector4,
            ShaderGraphExampleTypes.Color,
            ShaderGraphExampleTypes.Matrix4,
            ShaderGraphExampleTypes.Matrix3,
            ShaderGraphExampleTypes.Matrix2,
            ShaderGraphExampleTypes.GradientTypeHandle,
        };

        public override void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menuItems, IRootView view, IGroupModel selectedGroup = null)
        {
            // Only populate the Properties section for now. Will change in the future.
            if (sectionName != sections[0]) return;

            foreach (var type in k_SupportedBlackboardTypes)
            {
                menuItems.Add(new MenuItem
                {
                    // TODO (Joe): Use friendlier names -- this uses the actual type names so "float" becomes "Single"
                    name = $"Create {type.Name}",
                    action = () =>
                    {
                        Debug.Log($"Create {type.Name}");
                        view.Dispatch(new CreateGraphVariableDeclarationCommand("variable", true, type, selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
                    }
                });
            }
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            throw new NotImplementedException();
        }

        public override bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            throw new NotImplementedException();
        }

        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return null;
            //throw new NotImplementedException();
        }
    }
}
