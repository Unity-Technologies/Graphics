using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    abstract class AutoPlacementHelper
    {
        protected GraphView m_GraphView;

        HashSet<IModel> m_SelectedElementModels = new HashSet<IModel>();
        HashSet<IModel> m_LeftOverElementModels;

        internal Dictionary<INodeModel, HashSet<INodeModel>> NodeDependencies { get; } = new Dictionary<INodeModel, HashSet<INodeModel>>(); // All parent nodes and their children nodes
        Dictionary<INodeModel, INodeModel> m_DependenciesToMerge = new Dictionary<INodeModel, INodeModel>();  // All parent nodes that will merge into another parent node

        protected void SendPlacementCommand(List<IModel> updatedModels, List<Vector2> updatedDeltas)
        {
            var models = updatedModels.OfType<IMovable>();
            m_GraphView.Dispatch(new AutoPlaceElementsCommand(updatedDeltas, models.ToList()));
        }

        protected Dictionary<IModel, Vector2> GetElementDeltaResults()
        {
            GetSelectedElementModels();

            // Elements will be moved by a delta depending on their bounding rect
            List<Tuple<Rect, List<IModel>>> boundingRects = GetBoundingRects();

            return GetDeltas(boundingRects);
        }

        protected abstract void UpdateReferencePosition(ref float referencePosition, Rect currentElementRect);

        protected abstract Vector2 GetDelta(Rect elementPosition, float referencePosition);

        protected abstract float GetStartingPosition(List<Tuple<Rect, List<IModel>>> boundingRects);

        void GetSelectedElementModels()
        {
            m_SelectedElementModels.Clear();
            m_SelectedElementModels.UnionWith(m_GraphView.GetSelection().Where(element => !(element is IEdgeModel) && element.IsMovable()));
            m_LeftOverElementModels = new HashSet<IModel>(m_SelectedElementModels);
        }

        List<Tuple<Rect, List<IModel>>> GetBoundingRects()
        {
            List<Tuple<Rect, List<IModel>>> boundingRects = new List<Tuple<Rect, List<IModel>>>();

            GetPlacematsBoundingRects(ref boundingRects);
            GetLeftOversBoundingRects(ref boundingRects);

            return boundingRects;
        }

        void GetPlacematsBoundingRects(ref List<Tuple<Rect, List<IModel>>> boundingRects)
        {
            List<IPlacematModel> selectedPlacemats = m_SelectedElementModels.OfType<IPlacematModel>().ToList();
            foreach (var placemat in selectedPlacemats.Where(placemat => m_LeftOverElementModels.Contains(placemat)))
            {
                Placemat placematUI = placemat.GetView<Placemat>(m_GraphView);
                if (placematUI != null)
                {
                    var boundingRect = GetPlacematBoundingRect(ref boundingRects, placematUI, selectedPlacemats);
                    boundingRects.Add(new Tuple<Rect, List<IModel>>(boundingRect.Key, boundingRect.Value));
                }
            }
        }

        void GetLeftOversBoundingRects(ref List<Tuple<Rect, List<IModel>>> boundingRects)
        {
            foreach (IModel element in m_LeftOverElementModels)
            {
                GraphElement elementUI = element.GetView<GraphElement>(m_GraphView);
                if (elementUI != null)
                {
                    boundingRects.Add(new Tuple<Rect, List<IModel>>(elementUI.layout, new List<IModel> { element }));
                }
            }
        }

        KeyValuePair<Rect, List<IModel>> GetPlacematBoundingRect(ref List<Tuple<Rect, List<IModel>>> boundingRects, Placemat placematUI, List<IPlacematModel> selectedPlacemats)
        {
            Rect boundingRect = placematUI.layout;
            List<IModel> elementsOnBoundingRect = new List<IModel>();
            List<Placemat> placematsOnBoundingRect = GetPlacematsOnBoundingRect(ref boundingRect, ref elementsOnBoundingRect, selectedPlacemats);

            // Adjust the bounding rect with elements overlapping any of the placemats on the bounding rect
            AdjustPlacematBoundingRect(ref boundingRect, ref elementsOnBoundingRect, placematsOnBoundingRect);

            foreach (var otherRect in boundingRects.ToList())
            {
                Rect otherBoundingRect = otherRect.Item1;
                List<IModel> otherBoundingRectElements = otherRect.Item2;
                if (otherBoundingRectElements.Any(element => IsOnPlacemats(element.GetView<GraphElement>(m_GraphView), placematsOnBoundingRect)))
                {
                    AdjustBoundingRect(ref boundingRect, otherBoundingRect);
                    elementsOnBoundingRect.AddRange(otherBoundingRectElements);
                    boundingRects.Remove(otherRect);
                }
            }

            return new KeyValuePair<Rect, List<IModel>>(boundingRect, elementsOnBoundingRect);
        }

        protected virtual List<Tuple<Rect, List<IModel>>> GetBoundingRectsList(List<Tuple<Rect, List<IModel>>> boundingRects)
        {
            return boundingRects;
        }

        Dictionary<IModel, Vector2> GetDeltas(List<Tuple<Rect, List<IModel>>> boundingRects)
        {
            List<Tuple<Rect, List<IModel>>> boundingRectsList = GetBoundingRectsList(boundingRects);

            float referencePosition = GetStartingPosition(boundingRectsList);

            Dictionary<IModel, Vector2> deltas = new Dictionary<IModel, Vector2>();

            foreach (var(boundingRect, elements) in boundingRectsList)
            {
                Vector2 delta = GetDelta(boundingRect, referencePosition);
                foreach (var element in elements.Where(element => !deltas.ContainsKey(element)))
                {
                    deltas[element] = delta;
                }
                UpdateReferencePosition(ref referencePosition, boundingRect);
            }

            return deltas;
        }

        List<Placemat> GetPlacematsOnBoundingRect(ref Rect boundingRect, ref List<IModel> elementsOnBoundingRect, List<IPlacematModel> selectedPlacemats)
        {
            List<Placemat> placematsOnBoundingRect = new List<Placemat>();

            foreach (IPlacematModel placemat in selectedPlacemats.Where(placemat => m_LeftOverElementModels.Contains(placemat)))
            {
                Placemat placematUI = placemat.GetView<Placemat>(m_GraphView);
                if (placematUI != null && placematUI.layout.Overlaps(boundingRect))
                {
                    AdjustBoundingRect(ref boundingRect, placematUI.layout);

                    placematsOnBoundingRect.Add(placematUI);
                    elementsOnBoundingRect.Add(placemat);
                    m_LeftOverElementModels.Remove(placemat);
                }
            }

            return placematsOnBoundingRect;
        }

        static readonly List<ModelView> k_AdjustPlacematBoundingRectAllUIs = new List<ModelView>();
        void AdjustPlacematBoundingRect(ref Rect boundingRect, ref List<IModel> elementsOnBoundingRect, List<Placemat> placematsOnBoundingRect)
        {
            m_GraphView.GraphModel.GraphElementModels
                .Where(e => e != null && !(e is IPlacematModel))
                .GetAllViewsInList(m_GraphView, null, k_AdjustPlacematBoundingRectAllUIs);
            foreach (var elementUI in k_AdjustPlacematBoundingRectAllUIs.OfType<GraphElement>())
            {
                if (IsOnPlacemats(elementUI, placematsOnBoundingRect))
                {
                    AdjustBoundingRect(ref boundingRect, elementUI.layout);
                    elementsOnBoundingRect.Add(elementUI.Model);
                    m_LeftOverElementModels.Remove(elementUI.Model);
                }
            }
            k_AdjustPlacematBoundingRectAllUIs.Clear();
        }

        static void AdjustBoundingRect(ref Rect boundingRect, Rect otherRect)
        {
            if (otherRect.yMin < boundingRect.yMin)
            {
                boundingRect.yMin = otherRect.yMin;
            }
            if (otherRect.xMin < boundingRect.xMin)
            {
                boundingRect.xMin = otherRect.xMin;
            }
            if (otherRect.yMax > boundingRect.yMax)
            {
                boundingRect.yMax = otherRect.yMax;
            }
            if (otherRect.xMax > boundingRect.xMax)
            {
                boundingRect.xMax = otherRect.xMax;
            }
        }

        static bool IsOnPlacemats(GraphElement element, List<Placemat> placemats)
        {
            return placemats.Any(placemat => !element.Equals(placemat) && element.layout.Overlaps(placemat.layout));
        }
    }
}
