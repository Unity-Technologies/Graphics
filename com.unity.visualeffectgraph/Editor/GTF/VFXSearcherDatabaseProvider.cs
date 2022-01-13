using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;

namespace UnityEditor.VFX
{
    public class VFXSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        /// <inheritdoc />
        public VFXSearcherDatabaseProvider(Stencil stencil)
            : base(stencil)
        {
        }

        public override GraphElementSearcherDatabase InitialGraphElementDatabase(IGraphModel graphModel)
        {
            var db = new GraphElementSearcherDatabase(Stencil, graphModel)
                .AddNodesWithSearcherItemAttribute()
                //.AddConstant(typeof(Attitude))
                .AddStickyNote();

            return AddContexts(AddBlocks(AddInlineOperators(db)));
        }

        private GraphElementSearcherDatabase AddInlineOperators(GraphElementSearcherDatabase db)
        {
            db.Items.AddRange(LoadModels<VFXOperator>().Select(MakeOperatorItem));
            return db;

            SearcherItem MakeOperatorItem(VFXModelDescriptor<VFXOperator> descriptor)
            {
                return new GraphNodeModelSearcherItem(
                        descriptor.name,
                        null,
                        x => x.GraphModel.CreateNode(
                            typeof(VFXOperatorNode),
                            descriptor.name,
                            x.Position,
                            x.Guid,
                            y => (y as VFXOperatorNode).SetOperator(descriptor.CreateInstance()),
                            x.SpawnFlags))
                    { CategoryPath = descriptor.category };
            }
        }

        private GraphElementSearcherDatabase AddBlocks(GraphElementSearcherDatabase db)
        {
            db.Items.AddRange(LoadModels<VFXBlock>().Select(MakeBlockItem));
            return db;

            SearcherItem MakeBlockItem(VFXModelDescriptor<VFXBlock> descriptor)
            {
                return new GraphNodeModelSearcherItem(
                        descriptor.name,
                        null,
                        x => x.CreateBlock(
                            typeof(VFXBlockNode),
                            y => (y as VFXBlockNode).SetBlock(descriptor.CreateInstance()),
                            typeof(VFXContextNode)))
                    { CategoryPath = descriptor.category };
            }
        }

        private GraphElementSearcherDatabase AddContexts(GraphElementSearcherDatabase db)
        {
            db.Items.AddRange(LoadModels<VFXContext>().Select(MakeContextItem));
            return db;

            SearcherItem MakeContextItem(VFXModelDescriptor<VFXContext> descriptor)
            {
                return new GraphNodeModelSearcherItem(
                        descriptor.name,
                        null,
                        x => x.GraphModel.CreateNode(
                            typeof(VFXContextNode),
                            descriptor.name,
                            x.Position,
                            x.Guid,
                            y => (y as VFXContextNode).SetContext(descriptor.CreateInstance()),
                            x.SpawnFlags))
                    { CategoryPath = descriptor.category };
            }
        }

        private IEnumerable<VFXModelDescriptor<T>> LoadModels<T>() where T : VFXModel
        {
            var modelTypes = FindConcreteSubclasses(typeof(T), typeof(VFXInfoGTFAttribute));
            var modelDescs = new List<VFXModelDescriptor<T>>();
            var error = new StringBuilder();

            foreach (var modelType in modelTypes)
            {
                try
                {
                    T instance = (T)ScriptableObject.CreateInstance(modelType);

                    var modelDesc = new VFXModelDescriptor<T>(instance);
                    if (modelDesc.info.autoRegister)
                    {
                        if (modelDesc.info.variantProvider != null)
                        {
                            var provider = Activator.CreateInstance(modelDesc.info.variantProvider) as VariantProvider;
                            modelDescs.AddRange(provider.ComputeVariants().Select(variant => new VFXModelDescriptor<T>((T)ScriptableObject.CreateInstance(modelType), variant)));
                        }
                        else
                        {
                            modelDescs.Add(modelDesc);
                        }
                    }
                }
                catch (Exception e)
                {
                    error.AppendFormat("Error while loading model from type " + modelType + ": " + e);
                    error.AppendLine();
                }
            }

            if (error.Length != 0)
            {
                Debug.LogError(error);
            }

            return modelDescs.OrderBy(o => o.name).ToList();
        }

        private IEnumerable<Type> FindConcreteSubclasses(Type objectType = null, Type attributeType = null)
        {
            foreach (var domainAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                IEnumerable<Type> assemblyTypes = null;
                try
                {
                    assemblyTypes = domainAssembly
                        .GetTypes()
                        .Where(x => IsMatchingType(x, objectType, attributeType));
                }
                catch (Exception)
                {
                    if (VFXViewPreference.advancedLogs)
                        Debug.Log("Cannot access assembly: " + domainAssembly);
                    continue;
                }

                foreach (var assemblyType in assemblyTypes)
                {
                    yield return assemblyType;
                }
            }
        }

        private bool IsMatchingType(Type type, Type objectType, Type attributeType)
        {
            return (objectType == null || type.IsSubclassOf(objectType))
                   && !type.IsAbstract
                   && (attributeType == null || type.GetCustomAttributes(attributeType, false).Length == 1);
        }
    }
}
