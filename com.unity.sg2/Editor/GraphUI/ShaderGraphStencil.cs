using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphStencil : Stencil
    {
        public const string Name = "ShaderGraph";

        public const string DefaultGraphAssetName = "NewShaderGraph";
        public const string GraphExtension = "sg2";

        public const string DefaultSubGraphAssetName = "NewShaderSubGraph";
        public const string SubGraphExtension = "sg2subgraph";

        public string ToolName =>
            Name;

        // TODO: (Sai) When subgraphs come in, add support for dropdown section
        internal static readonly string[] sections = {"Properties", "Keywords"};

        public override IEnumerable<string> SectionNames => sections;

        ShaderGraphModel shaderGraphModel => GraphModel as ShaderGraphModel;

        public ShaderGraphStencil()
        {
        }

        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel) =>
            new SGBlackboardGraphModel(graphModel);

        // See ShaderGraphExampleTypes.GetGraphType for more details
        public override Type GetConstantType(TypeHandle typeHandle)
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

            if (typeHandle == ShaderGraphExampleTypes.Texture2DArrayTypeHandle
                || typeHandle == ShaderGraphExampleTypes.Texture2DTypeHandle
                || typeHandle == ShaderGraphExampleTypes.CubemapTypeHandle
                || typeHandle == ShaderGraphExampleTypes.Texture3DTypeHandle)
            {
                return typeof(TextureTypeConstant);
            }
            if (typeHandle == ShaderGraphExampleTypes.SamplerStateTypeHandle)
                return typeof(SamplerStateTypeConstant);

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

        internal ShaderGraphRegistry GetRegistry()
        {
            return ShaderGraphRegistry.Instance;
        }

        internal NodeUIDescriptor GetUIHints(RegistryKey nodeKey, NodeHandler node)
        {
            return ShaderGraphRegistry.Instance.GetNodeUIDescriptor(nodeKey, node);
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
            ShaderGraphExampleTypes.Matrix2,
            ShaderGraphExampleTypes.Matrix3,
            ShaderGraphExampleTypes.Matrix4,
            // ShaderGraphExampleTypes.GradientTypeHandle,  TODO: Awaiting GradientType support
        };

        public override void PopulateBlackboardCreateMenu(
            string sectionName,
            List<MenuItem> menu,
            IRootView view,
            IGroupModel selectedGroup = null)
        {
            // Only populate the Properties section for now. Will change in the future.
            if (sectionName != sections[0]) return;

            foreach (var type in k_SupportedBlackboardTypes)
            {
                var displayName = TypeMetadataResolver.Resolve(type)?.FriendlyName ?? type.Name;
                menu.Add(new MenuItem
                {
                    name = $"Create {displayName}",
                    action = () =>
                    {
                        var command = new CreateGraphVariableDeclarationCommand(displayName, true, type, typeof(GraphDataVariableDeclarationModel), selectedGroup ?? GraphModel.GetSectionModel(sectionName));
                        command.InitializationCallback = InitVariableDeclarationModel;
                        view.Dispatch(command);
                    }
                });
            }

            void InitVariableDeclarationModel(IVariableDeclarationModel model, IConstant constant)
            {
                if (model is not GraphDataVariableDeclarationModel graphDataVar) return;

                // Use this variables' generated guid to bind it to an underlying element in the graph data.
                var registry = ((ShaderGraphStencil)shaderGraphModel.Stencil).GetRegistry();
                var graphHandler = shaderGraphModel.GraphHandler;

                // If the guid starts with a number, it will produce an invalid identifier in HLSL.
                var variableDeclarationName = "_" + graphDataVar.Guid;
                var contextName = Registry.ResolveKey<PropertyContext>().Name;

                var propertyContext = graphHandler.GetNode(contextName);
                Debug.Assert(propertyContext != null, "Material property context was missing from graph when initializing a variable declaration");

                var entry = new ContextEntry
                {
                    fieldName = variableDeclarationName,
                    height = ShaderGraphExampleTypes.GetGraphTypeHeight(model.DataType),
                    length = ShaderGraphExampleTypes.GetGraphTypeLength(model.DataType),
                    primitive = ShaderGraphExampleTypes.GetGraphTypePrimitive(model.DataType),
                    precision = GraphType.Precision.Any,
                    initialValue = Matrix4x4.zero,
                };

                ContextBuilder.AddReferableEntry(propertyContext, entry, registry.Registry, ContextEntryEnumTags.PropertyBlockUsage.Included, displayName: variableDeclarationName);
                graphHandler.ReconcretizeNode(propertyContext.ID.FullPath, registry.Registry);

                graphDataVar.contextNodeName = contextName;
                graphDataVar.graphDataName = variableDeclarationName;
            }
        }

        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is not GraphDataContextNodeModel;
        }

        public override bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            throw new NotImplementedException();
        }

        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return new InspectorModel(inspectedModel);
        }
    }
}
