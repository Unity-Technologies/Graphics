using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphStencil : Stencil
    {
        public const string Name = "ShaderGraph";

        public override string ToolName => Name;

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
                RegistryInstance.Register<Registry.Types.AddNode>();
            }
            return RegistryInstance;
        }

        public override IGraphProcessor CreateGraphProcessor()
        {
            return new ShaderGraphProcessor();
        }
        public override void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            base.PopulateBlackboardCreateMenu(sectionName, menu, commandDispatcher);

            menu.AddItem(new GUIContent("Create Float4"), false, () =>
            {
                const string newItemName = "variable";
                var finalName = newItemName;
                var i = 0;
                // ReSharper disable once AccessToModifiedClosure
                while (commandDispatcher.State.WindowState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                    finalName = newItemName + i++;

                commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, TypeHandle.Vector4));
            });
        }
    }
}
