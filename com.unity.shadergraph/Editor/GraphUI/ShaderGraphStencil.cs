using System;
using System.Collections.Generic;
using System.Linq;
using com.unity.shadergraph.defs;
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

        public string ToolName => Name;

        Dictionary<RegistryKey, Dictionary<string, float>> m_NodeUIParams;

        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel) => new SGBlackboardGraphModel(graphAssetModel);

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            if (typeHandle == TypeHandle.Vector2
                || typeHandle == TypeHandle.Vector3
                || typeHandle == TypeHandle.Vector4
                || typeHandle == TypeHandle.Float)
            {
                return typeof(GraphTypeConstant);
            }

            return typeHandle == ShaderGraphExampleTypes.DayOfWeek
                ? typeof(DayOfWeekConstant)
                : TypeToConstantMapper.GetConstantNodeType(typeHandle);
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
                RegistryInstance = new Registry.Registry();
                RegistryInstance.Register<Registry.Types.GraphType>();
                RegistryInstance.Register<Registry.Types.GraphTypeAssignment>();
                //RegistryInstance.Register<Registry.Types.AddNode>();

                // Register nodes from FunctionDescriptors in IStandardNode classes.
                var interfaceType = typeof(IStandardNode);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => interfaceType.IsAssignableFrom(p));

                var getFunctionDescriptor = $"get_{nameof(IStandardNode.FunctionDescriptor)}";
                var getUIHints = $"get_{nameof(IStandardNode.UIHints)}";
                m_NodeUIParams = new Dictionary<RegistryKey, Dictionary<string, float>>();

                foreach (var t in types)
                {
                    var fdMethod = t.GetMethod(getFunctionDescriptor);
                    if (t != interfaceType && fdMethod != null)
                    {
                        var fd = (FunctionDescriptor)fdMethod.Invoke(null, null);
                        var key = RegistryInstance.Register(fd);

                        var uiHintsMethod = t.GetMethod(getUIHints);
                        if (uiHintsMethod != null)
                        {
                            m_NodeUIParams[key] = (Dictionary<string, float>)uiHintsMethod.Invoke(null, null);
                        }
                    }
                }
            }
            return RegistryInstance;
        }

        public bool TryGetUIHints(RegistryKey nodeKey, out IReadOnlyDictionary<string, float> uiHints)
        {
            var hasHints = m_NodeUIParams.TryGetValue(nodeKey, out var result);
            uiHints = result;
            return hasHints;
        }

        public override IGraphProcessor CreateGraphProcessor()
        {
            return new ShaderGraphProcessor();
        }


        public override void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, IModelView view, IGraphModel graphModel, IGroupModel selectedGroup = null)
        {
            base.PopulateBlackboardCreateMenu(sectionName, menu, view, graphModel, selectedGroup);
        }
    }
}
