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
                || typeHandle == TypeHandle.Int)
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

        public override void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menu, IRootView view, IGroupModel selectedGroup = null)
        {
            // Need to get types from registry here I guess
            base.PopulateBlackboardCreateMenu(sectionName, menu, view, selectedGroup);
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
