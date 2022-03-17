using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Class used to hold copy paste data.
    /// </summary>
    [Serializable]
    public class CopyPasteData
    {
        [SerializeReference]
        internal List<INodeModel> nodes;

        [SerializeReference]
        internal List<IEdgeModel> edges;

        [Serializable]
        internal struct VariableDeclaration
        {
            [SerializeReference]
            public IVariableDeclarationModel model;

            [SerializeField]
            public SerializableGUID groupGUID;

            [SerializeField]
            public int groupIndex;

            [SerializeField]
            public int indexInGroup;
        }

        [Serializable]
        internal struct GroupPath
        {
            [SerializeField]
            public SerializableGUID originalGUID;

            [SerializeField]
            public string[] path;

            [SerializeField]
            public bool expanded;
        }

        [SerializeField]
        List<GroupPath> variableGroupPaths;

        [SerializeField]
        internal List<VariableDeclaration> variableDeclarations;

        [SerializeReference]
        internal List<IVariableDeclarationModel> implicitVariableDeclarations;

        [SerializeField]
        internal Vector2 topLeftNodePosition;

        [SerializeReference]
        internal List<IStickyNoteModel> stickyNotes;

        [SerializeReference]
        internal List<IPlacematModel> placemats;

        internal bool IsEmpty() => (!nodes.Any() && !edges.Any() &&
            !variableDeclarations.Any() && !stickyNotes.Any() && !placemats.Any() && !variableGroupPaths.Any());

        internal bool HasVariableContent()
        {
            return variableDeclarations.Any() || variableGroupPaths.Any();
        }

        internal static CopyPasteData GatherCopiedElementsData(BlackboardViewStateComponent bbState, IReadOnlyCollection<IModel> graphElementModels)
        {
            var originalNodes = graphElementModels.OfType<INodeModel>().ToList();

            List<IVariableDeclarationModel> variableDeclarationsToCopy = graphElementModels
                .OfType<IVariableDeclarationModel>()
                .ToList();

            List<IStickyNoteModel> stickyNotesToCopy = graphElementModels
                .OfType<IStickyNoteModel>()
                .ToList();

            List<IPlacematModel> placematsToCopy = graphElementModels
                .OfType<IPlacematModel>()
                .ToList();

            List<IEdgeModel> edgesToCopy = graphElementModels
                .OfType<IEdgeModel>()
                .ToList();

            var implicitVariableDeclarations = originalNodes.OfType<IVariableNodeModel>()
                .Select(t => t.VariableDeclarationModel).Except(variableDeclarationsToCopy).ToList();

            Vector2 topLeftNodePosition = Vector2.positiveInfinity;
            foreach (var n in originalNodes.Where(t => t.IsMovable()))
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.Position);
            }

            foreach (var n in stickyNotesToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }

            foreach (var n in placematsToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }

            if (topLeftNodePosition == Vector2.positiveInfinity)
            {
                topLeftNodePosition = Vector2.zero;
            }

            var originalGroups = graphElementModels.OfType<IGroupModel>().ToList();

            var groups = new List<IGroupModel>();

            originalGroups.Sort(GroupItemOrderComparer.Default);

            // Make sure all groups inside other selected groups are ignored.
            foreach (var group in originalGroups)
            {
                var current = group.ParentGroup;
                bool found = false;
                while (current != null)
                {
                    if (groups.Contains(current))
                    {
                        found = true;
                        break;
                    }

                    current = current.ParentGroup;
                }

                if (!found)
                    groups.Add(group);
            }


            for (int i = 0; i < groups.Count; ++i)
            {
                RecursiveAddGroups(ref i, groups);
            }

            // here groups contains an order list of all copied groups with their entire subgroup hierarchy

            var groupIndices = new Dictionary<IGroupModel, int>();
            int cpt = 0;
            var groupPaths = new List<GroupPath>();
            var groupPath = new List<String>();

            foreach (var group in groups)
            {
                groupPath.Clear();
                var current = group.ParentGroup;
                groupPath.Add(group.Title);
                while (current != null)
                {
                    if (!groups.Contains(current))
                        break;
                    groupPath.Insert(0, current.Title);
                    current = current.ParentGroup;
                }

                groupPath.Insert(0, group.GetSection().Title);

                groupPaths.Add(new GroupPath() { originalGUID = group.Guid, path = groupPath.ToArray(), expanded = bbState?.GetGroupExpanded(group) ?? true });
                groupIndices[group] = cpt++;
            }

            var declarations = new List<VariableDeclaration>(variableDeclarationsToCopy.Count);

            //First add all variables outside of copied groups
            foreach (var variable in variableDeclarationsToCopy)
            {
                IGroupModel group = variable.ParentGroup;

                if (groupIndices.ContainsKey(group))
                    continue;

                declarations.Add(new VariableDeclaration { model = variable, groupGUID = group.Guid, groupIndex = -1, indexInGroup = -1 });
            }

            var inGroupDeclarations = new List<VariableDeclaration>();
            if (groups.Any())
            {
                var graphModel = groups.First().GraphModel;

                foreach (var variable in graphModel.VariableDeclarations)
                {
                    if (groupIndices.TryGetValue(variable.ParentGroup, out int groupIndex))
                    {
                        int indexInGroup = variable.ParentGroup.Items.IndexOfInternal(variable);
                        inGroupDeclarations.Add(new VariableDeclaration { model = variable, groupGUID = variable.ParentGroup.Guid, groupIndex = groupIndex, indexInGroup = indexInGroup });
                    }
                }

                inGroupDeclarations.Sort((a, b) => a.indexInGroup.CompareTo(b.indexInGroup)); // make the variable go in the right order so that the inserts to indexInGroup are always valid.
            }

            declarations.AddRange(inGroupDeclarations);

            CopyPasteData copyPasteData = new CopyPasteData
            {
                topLeftNodePosition = topLeftNodePosition,
                nodes = originalNodes,
                edges = edgesToCopy,
                variableDeclarations = declarations,
                implicitVariableDeclarations = implicitVariableDeclarations,
                variableGroupPaths = groupPaths,
                stickyNotes = stickyNotesToCopy,
                placemats = placematsToCopy
            };

            return copyPasteData;
        }

        static void RecursiveAddGroups(ref int i, List<IGroupModel> groups)
        {
            var group = groups[i];

            foreach (var childGroup in group.Items.OfType<IGroupModel>())
            {
                groups.Insert(++i, childGroup);
                RecursiveAddGroups(ref i, groups);
            }
        }

        static void RecurseAddMapping(Dictionary<string, IGraphElementModel> elementMapping, IGraphElementModel originalElement, IGraphElementModel newElement)
        {
            elementMapping[originalElement.Guid.ToString()] = newElement;

            if (newElement is IGraphElementContainer container)
                foreach (var subElement in ((IGraphElementContainer)originalElement).GraphElementModels.Zip(
                             container.GraphElementModels, (a, b) => new { originalElement = a, newElement = b }))
                {
                    RecurseAddMapping(elementMapping, subElement.originalElement, subElement.newElement);
                }
        }

        internal static void PasteSerializedData(PasteOperation operation, Vector2 delta,
            GraphModelStateComponent.StateUpdater graphViewUpdater,
            BlackboardViewStateComponent.StateUpdater bbUpdater,
            SelectionStateComponent.StateUpdater selectionStateUpdater,
            CopyPasteData copyPasteData, IGraphModel graphModel, IGroupModel selectedGroup)
        {
            var elementMapping = new Dictionary<string, IGraphElementModel>();

            var declarationMapping = new Dictionary<string, IVariableDeclarationModel>();

            List<IGroupModel> createdGroups = new List<IGroupModel>();

            if (copyPasteData.variableGroupPaths != null)
            {
                for (int i = 0; i < copyPasteData.variableGroupPaths.Count; ++i)
                {
                    var groupPath = copyPasteData.variableGroupPaths[i];
                    var newGroup = graphModel.CreateGroup(groupPath.path.Last());
                    if (groupPath.path.Length == 2)
                    {
                        if (operation == PasteOperation.Duplicate)
                        {
                            graphModel.TryGetModelFromGuid(groupPath.originalGUID, out IGroupModel originalGroup);
                            if (originalGroup != null) // If we duplicate try to put the new group next to the duplicated one.
                            {
                                var parentGroup = originalGroup.ParentGroup;
                                graphViewUpdater.MarkChanged(parentGroup.InsertItem(newGroup, parentGroup.Items.IndexOfInternal(originalGroup) + 1));
                            }
                            else
                            {
                                var parentGroup = selectedGroup ?? graphModel.GetSectionModel(groupPath.path[0]) ?? graphModel.SectionModels.First();
                                graphViewUpdater.MarkChanged(parentGroup.InsertItem(newGroup));
                            }
                        }
                        else
                        {
                            var parentGroup = selectedGroup ?? graphModel.GetSectionModel(groupPath.path[0]) ?? graphModel.SectionModels.First();
                            graphViewUpdater.MarkChanged(parentGroup.InsertItem(newGroup));
                            bbUpdater?.SetGroupModelExpanded(parentGroup, true);
                        }
                    }
                    else
                    {
                        int j = copyPasteData.variableGroupPaths.FindLastIndex(i - 1, t => t.path.Length == groupPath.path.Length - 1); // our parent group is always the first item above us that have one less path element.
                        var parentGroup = createdGroups[j];
                        graphViewUpdater.MarkChanged(parentGroup.InsertItem(newGroup));
                    }

                    graphViewUpdater.MarkNew(newGroup);
                    bbUpdater?.SetGroupModelExpanded(newGroup, groupPath.expanded);
                    createdGroups.Add(newGroup);
                    selectionStateUpdater.SelectElement(newGroup, true);
                }
            }

            if (copyPasteData.variableDeclarations.Any())
            {
                List<VariableDeclaration> variableDeclarationModels =
                    copyPasteData.variableDeclarations.ToList();
                List<IVariableDeclarationModel> duplicatedModels = new List<IVariableDeclarationModel>();

                foreach (var source in variableDeclarationModels)
                {
                    if (!graphModel.Stencil.CanPasteVariable(source.model, graphModel))
                        break;
                    duplicatedModels.Add(graphModel.DuplicateGraphVariableDeclaration(source.model));
                    if (source.groupIndex >= 0) // if we have a valid groupIndex, it means we are in a duplicated group
                    {
                        createdGroups[source.groupIndex].InsertItem(duplicatedModels.Last(), source.indexInGroup);
                    }
                    else if (operation == PasteOperation.Duplicate && graphModel.TryGetModelFromGuid(source.groupGUID, out IGroupModel group)) // If we duplicate in the same graph, put the new variable in the same group, after the original.
                    {
                        group.InsertItem(duplicatedModels.Last());
                    }
                    else
                    {
                        selectedGroup?.InsertItem(duplicatedModels.Last());
                    }

                    declarationMapping[source.model.Guid.ToString()] = duplicatedModels.Last();
                }

                var duplicatedParents = new HashSet<IGroupModel>(duplicatedModels.Select(t => t.ParentGroup));

                graphViewUpdater.MarkChanged(duplicatedParents, ChangeHint.Grouping);
                foreach (var duplicatedParent in duplicatedParents)
                {
                    var current = duplicatedParent;
                    while (current != null)
                    {
                        bbUpdater?.SetGroupModelExpanded(current, true);
                        current = current.ParentGroup;
                    }
                }
                graphViewUpdater.MarkNew(duplicatedModels);
                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            if (copyPasteData.implicitVariableDeclarations.Any())
            {
                List<IVariableDeclarationModel> variableDeclarationModels =
                    copyPasteData.implicitVariableDeclarations.ToList();
                List<IVariableDeclarationModel> duplicatedModels = new List<IVariableDeclarationModel>();

                foreach (var source in variableDeclarationModels)
                {
                    if (!graphModel.TryGetModelFromGuid(source.Guid, out IVariableDeclarationModel variable))
                    {
                        if (graphModel.Stencil.CanPasteVariable(source, graphModel))
                        {
                            duplicatedModels.Add(graphModel.DuplicateGraphVariableDeclaration(source, true));
                            declarationMapping[source.Guid.ToString()] = duplicatedModels.Last();
                        }
                    }
                    else
                    {
                        declarationMapping[source.Guid.ToString()] = variable;
                    }
                }

                graphViewUpdater.MarkChanged(duplicatedModels.Select(t => t.ParentGroup),
                    ChangeHint.Grouping);
                graphViewUpdater.MarkNew(duplicatedModels);
                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            foreach (var originalModel in copyPasteData.nodes)
            {
                if (!graphModel.Stencil.CanPasteNode(originalModel, graphModel))
                    continue;
                if (originalModel.NeedsContainer())
                    continue;

                IVariableDeclarationModel declarationModel = null;
                var variableNode = originalModel as IVariableNodeModel;
                if (variableNode != null)
                {
                    if (!declarationMapping.TryGetValue(variableNode.VariableDeclarationModel.Guid.ToString(), out declarationModel))
                        continue;
                }

                var pastedNode = graphModel.DuplicateNode(originalModel, delta);

                if (variableNode != null)
                {
                    ((IVariableNodeModel)pastedNode).VariableDeclarationModel = declarationModel;
                }

                graphViewUpdater?.MarkNew(pastedNode);
                selectionStateUpdater?.SelectElements(new[] { pastedNode }, true);
                RecurseAddMapping(elementMapping, originalModel, pastedNode);
            }

            foreach (var edge in copyPasteData.edges.Where(edge => graphModel.GetEdgesForPort(edge.FromPort).Any() && graphModel.GetEdgesForPort(edge.ToPort).Any()))
            {
                elementMapping.TryGetValue(edge.ToPort.NodeModel.Guid.ToString(), out var newInput);
                elementMapping.TryGetValue(edge.FromPort.NodeModel.Guid.ToString(), out var newOutput);

                var copiedEdge = graphModel.DuplicateEdge(edge, newInput as INodeModel, newOutput as INodeModel);
                if (copiedEdge != null)
                {
                    elementMapping.Add(edge.Guid.ToString(), copiedEdge);
                    graphViewUpdater?.MarkNew(copiedEdge);
                    selectionStateUpdater?.SelectElements(new[] { copiedEdge }, true);
                }
            }

            foreach (var stickyNote in copyPasteData.stickyNotes)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position + delta, stickyNote.PositionAndSize.size);
                var pastedStickyNote = graphModel.CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                graphViewUpdater?.MarkNew(pastedStickyNote);
                selectionStateUpdater?.SelectElements(new[] { pastedStickyNote }, true);
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<IPlacematModel> pastedPlacemats = new List<IPlacematModel>();

            // Keep placemats relative order
            foreach (var placemat in copyPasteData.placemats)
            {
                var newPosition = new Rect(placemat.PositionAndSize.position + delta, placemat.PositionAndSize.size);
                var newTitle = "Copy of " + placemat.Title;
                var pastedPlacemat = graphModel.CreatePlacemat(newPosition);
                pastedPlacemat.Title = newTitle;
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElements = placemat.HiddenElements;
                graphViewUpdater?.MarkNew(pastedPlacemat);
                selectionStateUpdater?.SelectElements(new[] { pastedPlacemat }, true);
                pastedPlacemats.Add(pastedPlacemat);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            // Update hidden content to new node ids.
            foreach (var pastedPlacemat in pastedPlacemats)
            {
                if (pastedPlacemat.Collapsed)
                {
                    List<IGraphElementModel> pastedHiddenContent = new List<IGraphElementModel>();
                    foreach (var elementGuid in pastedPlacemat.HiddenElements.Select(t => t.Guid.ToString()))
                    {
                        if (elementMapping.TryGetValue(elementGuid, out var pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement);
                        }
                    }

                    pastedPlacemat.HiddenElements = pastedHiddenContent;
                }
            }
        }
    }
}
