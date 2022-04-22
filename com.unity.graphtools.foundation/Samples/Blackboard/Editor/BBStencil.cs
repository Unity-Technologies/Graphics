using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Blackboard
{
    public enum CustomEnumEnum
    {
        Value1,
        Value2,
        Value3
    }

    public class BBStencil : Stencil
    {
        internal static readonly string[] sections = { "Input", "Output", "Variables", "Stuff" };

        public static readonly string graphName = "Blackboard Editor";

        static Dictionary<TypeHandle, Type> s_TypeToConstantTypeCache;

        static TypeHandle Color => TypeHandleHelpers.GenerateTypeHandle(typeof(Color));
        static TypeHandle LayerMask => TypeHandleHelpers.GenerateTypeHandle(typeof(LayerMask));
        static TypeHandle Rect => TypeHandleHelpers.GenerateTypeHandle(typeof(Rect));
        static TypeHandle AnimationCurve => TypeHandleHelpers.GenerateTypeHandle(typeof(AnimationCurve));
        static TypeHandle Bounds => TypeHandleHelpers.GenerateTypeHandle(typeof(Bounds));
        static TypeHandle Gradient => TypeHandleHelpers.GenerateTypeHandle(typeof(Gradient));
        static TypeHandle Vector2Int => TypeHandleHelpers.GenerateTypeHandle(typeof(Vector2Int));
        static TypeHandle Vector3Int => TypeHandleHelpers.GenerateTypeHandle(typeof(Vector3Int));
        static TypeHandle RectInt => TypeHandleHelpers.GenerateTypeHandle(typeof(RectInt));
        static TypeHandle BoundsInt => TypeHandleHelpers.GenerateTypeHandle(typeof(BoundsInt));
        static TypeHandle CustomEnum => TypeHandleHelpers.GenerateTypeHandle(typeof(CustomEnumEnum));

        /// <summary>
        /// All the types that have a default editor defined in
        /// <see cref="CustomizableModelPropertyField.CreateDefaultFieldForType"/>.
        /// </summary>
        public static readonly IReadOnlyList<(TypeHandle type, string name)> SupportedConstants =
            new List<(TypeHandle type, string name)>()
            {
                (TypeHandle.Bool, "Boolean"),
                (TypeHandle.Char, "Character"),
                (TypeHandle.Double, "Double"),
                (TypeHandle.Float, "Float"),
                (TypeHandle.Int, "Integer"),
                (TypeHandle.Long, "Long Integer"),
                (TypeHandle.Object, "Object"),
                (TypeHandle.GameObject, "GameObject"),
                (TypeHandle.String, "String"),
                (TypeHandle.Vector2, "Vector2"),
                (TypeHandle.Vector3, "Vector3"),
                (TypeHandle.Vector4, "Vector4"),
                (Color, "Color"),
                (LayerMask, "LayerMask"),
                (Rect, "Rect"),
                (AnimationCurve, "AnimationCurve"),
                (Bounds, "Bounds"),
                (Gradient, "Gradient"),
                (Vector2Int, "Vector2Int"),
                (Vector3Int, "Vector3Int"),
                (RectInt, "RectInt"),
                (BoundsInt, "BoundsInt"),
                (CustomEnum, "CustomEnum"),
            };

        public override Type GetConstantType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantTypeCache == null)
            {
                s_TypeToConstantTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { TypeHandle.Bool, typeof(BooleanConstant) },
                    { TypeHandle.Char, typeof(CharConstant) },
                    { TypeHandle.Double, typeof(DoubleConstant) },
                    { TypeHandle.Float, typeof(FloatConstant) },
                    { TypeHandle.Int, typeof(IntConstant) },
                    { TypeHandle.Long, typeof(LongConstant) },
                    { TypeHandle.Object, typeof(ObjectConstant) },
                    { TypeHandle.GameObject, typeof(GameObjectConstant) },
                    { TypeHandle.String, typeof(StringConstant) },
                    { TypeHandle.Vector2, typeof(Vector2Constant) },
                    { TypeHandle.Vector3, typeof(Vector3Constant) },
                    { TypeHandle.Vector4, typeof(Vector4Constant) },
                    { Color, typeof(ColorConstant) },
                    { LayerMask, typeof(LayerMaskConstant) },
                    { Rect, typeof(RectConstant) },
                    { AnimationCurve, typeof(AnimationCurveConstant) },
                    { Bounds, typeof(BoundsConstant) },
                    { Gradient, typeof(GradientConstant) },
                    { Vector2Int, typeof(Vector2IntConstant) },
                    { Vector3Int, typeof(Vector3IntConstant) },
                    { RectInt, typeof(RectIntConstant) },
                    { BoundsInt, typeof(BoundsIntConstant) },
                };
            }

            if (s_TypeToConstantTypeCache.TryGetValue(typeHandle, out var result))
                return result;

            Type t = typeHandle.Resolve();
            if (t.IsEnum || t == typeof(Enum))
                return typeof(EnumConstant);

            return null;
        }

        /// <inheritdoc />
        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new BBSearcherProvider(this);
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is IVariableNodeModel;
        }

        public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return originalModel is BBDeclarationModel;
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel)
        {
            return new BBBlackboardGraphModel { GraphModel = graphModel };
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
