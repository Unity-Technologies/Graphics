using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model.Stencils", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    class ClassStencil : Stencil
    {
        ISearcherFilterProvider m_SearcherFilterProvider;
        List<ITypeMetadata> m_AssembliesTypes;

        static readonly string[] k_BlackListedNamespaces =
        {
            "aot",
            "collabproxy",
            "icsharpcode",
            "jetbrains",
            "microsoft",
            "mono",
            "packages.visualscripting",
            "treeeditor",
            "unityeditorinternal",
            "unityengineinternal",
            "visualscripting"
        };

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public override void PreProcessGraph(IGraphModel graphModel)
        {
            new PortInitializationTraversal().VisitGraph(graphModel);
        }

        public override ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return m_SearcherFilterProvider ??= new ClassSearcherFilterProvider(this);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new ClassSearcherDatabaseProvider(this);
        }

        public override List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            var types = AssemblyCache.CachedAssemblies.SelectMany(AssemblyExtensions.GetTypesSafe).ToList();
            m_AssembliesTypes = TaskUtilityInternal.RunTasks<Type, ITypeMetadata>(types, (type, cb) =>
            {
                if (IsValidType(type))
                    cb.Add(TypeHandleHelpers.GenerateTypeHandle(type).GetMetadata(this));
            }).ToList();
            m_AssembliesTypes.Sort((x, y) => string.CompareOrdinal(
                x.TypeHandle.Identification,
                y.TypeHandle.Identification)
            );

            return m_AssembliesTypes;
        }

        static bool IsValidType(Type type)
        {
            return !type.IsAbstract
                && !type.IsInterface
                && type.IsVisible
                && !Attribute.IsDefined(type, typeof(ObsoleteAttribute))
                && !k_BlackListedNamespaces.Any(b => type.Namespace != null && type.Namespace.ToLower().StartsWith(b)
                && !Attribute.IsDefined(type, typeof(ObsoleteAttribute)));
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new BlackboardGraphModel(graphAssetModel);
        }

        /// <inheritdoc />
        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return new InspectorModel(inspectedModel);
        }

        /// <inheritdoc />
        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return true;
        }

        public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return true;
        }
    }
}
