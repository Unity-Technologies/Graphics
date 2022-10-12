using System;
using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphStencil : Stencil
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

        public ShaderGraphStencil()
        {
        }

        public override BlackboardGraphModel CreateBlackboardGraphModel(GraphModel graphModel) =>
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

        public override IItemDatabaseProvider GetItemDatabaseProvider()
        {
            return new ShaderGraphSearcherDatabaseProvider(this);
        }

        public override ILibraryFilterProvider GetLibraryFilterProvider()
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
            GetGraphProcessorContainer().AddGraphProcessor(new ShaderGraphProcessor());
        }

        /// <summary>
        /// Returns true if a blackboard property with the given TypeHandle can be included in the property block for
        /// the current model. Use this to avoid exporting invalid properties like matrices.
        /// </summary>
        public bool IsExposable(TypeHandle typeHandle)
        {
            var descriptor = typeHandle.GetBackingDescriptor();
            switch (descriptor)
            {
                case ParametricTypeDescriptor {Height: GraphType.Height.One}:
                case TextureTypeDescriptor:
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateBlackboardCreateMenu(
            string sectionName,
            List<MenuItem> menu,
            RootView view,
            GroupModel selectedGroup = null)
        {
            // Only populate the Properties section for now. Will change in the future.
            if (sectionName != sections[0]) return;

            foreach (var type in ShaderGraphExampleTypes.BlackboardTypes)
            {
                var displayName = TypeMetadataResolver.Resolve(type)?.FriendlyName ?? type.Name;
                menu.Add(new MenuItem
                {
                    name = $"Create {displayName}",
                    action = () =>
                    {
                        var command = new CreateGraphVariableDeclarationCommand(displayName, true, type, typeof(GraphDataVariableDeclarationModel), selectedGroup ?? GraphModel.GetSectionModel(sectionName));
                        view.Dispatch(command);
                    }
                });
            }
        }

        public override bool CanPasteNode(AbstractNodeModel originalModel, GraphModel graph)
        {
            return originalModel is not GraphDataContextNodeModel;
        }

        public override bool CanPasteVariable(VariableDeclarationModel originalModel, GraphModel graph)
        {
            // TODO: (Sai) When we have built-in keywords, those do not allow for duplication
            return true;
        }
    }
}
