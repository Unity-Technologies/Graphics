using System;
using System.Linq;
using com.unity.shadergraph.defs;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
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

        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel) => new SGBlackboardGraphModel(graphAssetModel);

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
                RegistryInstance = Registry.Default.DefaultRegistry.CreateDefaultRegistry();
            }
            return RegistryInstance;
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
