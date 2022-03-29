using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphStencil : Stencil
    {
        public const string Name = "ShaderGraph";
        public const string DefaultAssetName = "NewShaderGraph";
        public const string Extension = "sg2";
        Dictionary<RegistryKey, Dictionary<string, float>> m_NodeUIHints;
        private Registry RegistryInstance = null;
        private NodeUIInfo NodeUIInfo = null;

        public string ToolName =>
            Name;

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

        public Registry GetRegistry()
        {
            if (RegistryInstance == null)
            {
                m_NodeUIHints.Clear();

                void ReadUIInfo(RegistryKey key, Type type)
                {
                    // TODO Remove the code that uses UIHints
                    const string uiHintsGetterName = "get_UIHints";
                    var getUiHints = type.GetMethod(uiHintsGetterName);
                    if (getUiHints != null)
                    {
                        m_NodeUIHints[key] = (Dictionary<string, float>)getUiHints.Invoke(null, null);
                    }

                    const string nodeUIDescriptorGetterName = "get_NodeUIDescriptor";
                    var getNodeUIDescriptor = type.GetMethod(nodeUIDescriptorGetterName);
                    if (getNodeUIDescriptor != null)
                    {
                        NodeUIInfo[key] = (NodeUIDescriptor)getNodeUIDescriptor.Invoke(null, null);
                    }
                }
                RegistryInstance = ShaderGraphRegistryBuilder.CreateDefaultRegistry(afterNodeRegistered: ReadUIInfo);
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
        }
    }
}
