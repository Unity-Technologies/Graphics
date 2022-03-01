using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Class used to hold copy paste data.
    /// </summary>
    [Serializable]
    public class CopyPasteData
    {
        internal static CopyPasteData s_LastCopiedData;

        internal List<INodeModel> nodes;
        internal List<IEdgeModel> edges;
        internal List<IVariableDeclarationModel> variableDeclarations;
        internal Vector2 topLeftNodePosition;
        internal List<IStickyNoteModel> stickyNotes;
        internal List<IPlacematModel> placemats;

        internal string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        internal bool IsEmpty() => (!nodes.Any() && !edges.Any() &&
            !variableDeclarations.Any() && !stickyNotes.Any() && !placemats.Any());

        internal static CopyPasteData GatherCopiedElementsData(IReadOnlyCollection<IGraphElementModel> graphElementModels)
        {
            var originalNodes = graphElementModels.OfType<INodeModel>().Where(t => !t.NeedsContainer()).ToList();

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

            CopyPasteData copyPasteData = new CopyPasteData
            {
                topLeftNodePosition = topLeftNodePosition,
                nodes = originalNodes,
                edges = edgesToCopy,
                variableDeclarations = variableDeclarationsToCopy,
                stickyNotes = stickyNotesToCopy,
                placemats = placematsToCopy
            };

            return copyPasteData;
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

        internal static void PasteSerializedData(IGraphModel graph, Vector2 delta,
            GraphViewStateComponent.StateUpdater graphViewUpdater,
            SelectionStateComponent.StateUpdater selectionStateUpdater,
            CopyPasteData copyPasteData)
        {
            var elementMapping = new Dictionary<string, IGraphElementModel>();

            if (copyPasteData.variableDeclarations.Any())
            {
                List<IVariableDeclarationModel> variableDeclarationModels =
                    copyPasteData.variableDeclarations.ToList();
                List<IVariableDeclarationModel> duplicatedModels = new List<IVariableDeclarationModel>();

                foreach (var sourceModel in variableDeclarationModels)
                {
                    duplicatedModels.Add(graph.DuplicateGraphVariableDeclaration(sourceModel));
                }

                graphViewUpdater?.MarkNew(duplicatedModels);
                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            foreach (var originalModel in copyPasteData.nodes)
            {
                if (!graph.Stencil.CanPasteNode(originalModel, graph))
                    continue;
                if (originalModel.NeedsContainer())
                    continue;

                var pastedNode = graph.DuplicateNode(originalModel, delta);
                graphViewUpdater?.MarkNew(pastedNode);
                selectionStateUpdater?.SelectElements(new[] { pastedNode }, true);
                RecurseAddMapping(elementMapping, originalModel, pastedNode);
            }

            foreach (var edge in copyPasteData.edges.Where(edge => graph.GetEdgesForPort(edge.FromPort).Any() && graph.GetEdgesForPort(edge.ToPort).Any()))
            {
                elementMapping.TryGetValue(edge.ToPort.NodeModel.Guid.ToString(), out var newInput);
                elementMapping.TryGetValue(edge.FromPort.NodeModel.Guid.ToString(), out var newOutput);

                var copiedEdge = graph.DuplicateEdge(edge, newInput as INodeModel, newOutput as INodeModel);
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
                var pastedStickyNote = graph.CreateStickyNote(newPosition);
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
                var pastedPlacemat = graph.CreatePlacemat(newPosition);
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
