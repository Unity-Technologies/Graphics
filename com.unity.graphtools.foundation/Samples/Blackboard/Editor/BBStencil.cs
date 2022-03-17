using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public class BBStencil : Stencil
    {
        internal static readonly string[] sections = { "Input", "Output", "Variables", "Stuff" };

        public static string toolName = "Blackboard Sample";

        public static readonly string graphName = "Blackboard";

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is IVariableNodeModel;
        }

        public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return originalModel is BBDeclarationModel;
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new BBBlackboardGraphModel(graphAssetModel);
        }

        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return new InspectorModel(inspectedModel);
        }

        /// <inheritdoc />
        public override void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menuItems, IRootView view, IGroupModel selectedGroup = null)
        {
            if (sectionName == sections[(int)VariableType.Input])
            {
                menuItems.Add(new MenuItem{name = $"Create {sectionName}", action = ()=>
                    CreateVariable<BBInputVariableDeclarationModel>(sectionName, view, selectedGroup)
                });
            }

            if (sectionName == sections[(int)VariableType.Output])
            {
                menuItems.Add(new MenuItem{name = $"Create {sectionName}", action = ()=>
                    CreateVariable<BBOutputVariableDeclarationModel>(sectionName, view, selectedGroup)
                });
            }

            if (sectionName == sections[(int)VariableType.Variable])
            {
                menuItems.Add(new MenuItem{name = $"Create {sectionName}", action = ()=>
                    CreateVariable<BBVariableDeclarationModel>(sectionName, view, selectedGroup)
                });
            }

            if (sectionName == sections[(int)VariableType.Stuff])
            {
                menuItems.Add(new MenuItem{name = $"Create {sectionName}", action= ()=>
                    CreateVariable<BBStuffVariableDeclarationModel>(sectionName, view, selectedGroup)
                });
            }
        }

        public override IEnumerable<string> SectionNames =>
            GraphModel == null ? Enumerable.Empty<string>() : sections;

        /// <inheritdoc />
        public override string GetVariableSection(IVariableDeclarationModel variable)
        {
            if( ! (variable is BBDeclarationModel bbVariable))
                return sections[0];
            return sections[(int)bbVariable.Type];
        }

        void CreateVariable<T>(string sectionName, IRootView view,
            IGroupModel selectedGroup) where T : IVariableDeclarationModel
        {
            var section = GraphModel.GetSectionModel(sectionName);

            if (selectedGroup != null && !section.AcceptsDraggedModel(selectedGroup))
            {
                selectedGroup = null;
            }

            view.Dispatch(new CreateGraphVariableDeclarationCommand(sectionName, true, TypeHandle.Float,
                typeof(T), selectedGroup ?? section));
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        /// <inheritdoc />
        public override bool CanConvertVariable(IVariableDeclarationModel variable, string sectionName)
        {
            if (!(variable is BBDeclarationModel bbVariable))
                return false;
            switch (bbVariable.Type)
            {
                case VariableType.Input:
                    if( bbVariable.SomeValue > 10)
                        return sectionName == sections[(int)VariableType.Output] ||
                               sectionName == sections[(int)VariableType.Variable];
                    return false;
                case VariableType.Output:
                    return sectionName == sections[(int)VariableType.Input] ||
                           sectionName == sections[(int)VariableType.Variable];

            }
            return false;
        }

        public override IVariableDeclarationModel ConvertVariable(IVariableDeclarationModel variable, string sectionName)
        {
            if (!(variable is BBDeclarationModel))
                return null;

            if (sectionName == sections[(int)VariableType.Input])
                return GraphModel.CreateGraphVariableDeclaration(typeof(BBInputVariableDeclarationModel),
                    TypeHandle.Float, variable.GetVariableName(), variable.Modifiers, variable.IsExposed);
            if (sectionName == sections[(int)VariableType.Output])
                return GraphModel.CreateGraphVariableDeclaration(typeof(BBOutputVariableDeclarationModel),
                    TypeHandle.Float, variable.GetVariableName(), variable.Modifiers, variable.IsExposed);
            if (sectionName == sections[(int)VariableType.Variable])
                return GraphModel.CreateGraphVariableDeclaration(typeof(BBVariableDeclarationModel),
                    TypeHandle.Float, variable.GetVariableName(), variable.Modifiers, variable.IsExposed);

            return null;
        }
    }
}
