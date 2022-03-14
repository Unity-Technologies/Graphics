using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for UIs based on a model, i.e. graph elements but also ports and blackboard elements
    /// </summary>
    public interface IModelUI
    {
        /// <summary>
        /// The model that backs the UI.
        /// </summary>
        IGraphElementModel Model { get; }

        /// <summary>
        /// The view that owns this object.
        /// </summary>
        IModelView View { get; }

        /// <summary>
        /// The UI creation context.
        /// </summary>
        IUIContext Context { get; }

        /// <summary>
        /// Adds the instance to a view.
        /// </summary>
        /// <param name="view">The view to add the element to.</param>
        void AddToView(IModelView view);

        /// <summary>
        /// Removes the instance from the view.
        /// </summary>
        void RemoveFromView();

        /// <summary>
        /// Initializes the instance.
        /// </summary>
        /// <param name="model">The model that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        void Setup(IGraphElementModel model, IModelView view, IUIContext context);

        /// <summary>
        /// Instantiates and initializes the VisualElements that makes the UI.
        /// </summary>
        void BuildUI();

        /// <summary>
        /// Updates the UI using data from the model.
        /// </summary>
        void UpdateFromModel();

        /// <summary>
        /// Helper method that calls <see cref="Setup"/>, <see cref="BuildUI"/> and <see cref="UpdateFromModel"/>.
        /// </summary>
        /// <param name="model">The model that backs the instance.</param>
        /// <param name="view">The view to which the instance should be added.</param>
        /// <param name="context">The UI creation context.</param>
        void SetupBuildAndUpdate(IGraphElementModel model, IModelView view, IUIContext context);

        /// <summary>
        /// Tells whether the UI has some backward dependencies that got changed.
        /// </summary>
        /// <remarks>Used to know if the UI should be rebuilt.</remarks>
        /// <returns>True if some backward dependencies has changed, false otherwise.</returns>
        bool HasBackwardsDependenciesChanged();

        /// <summary>
        /// Tells whether the UI has some forward dependencies that got changed.
        /// </summary>
        /// <remarks>Used to know if the UI should be rebuilt.</remarks>
        /// <returns>True if some forward dependencies has changed, false otherwise.</returns>
        bool HasForwardsDependenciesChanged();

        /// <summary>
        /// Tells whether the UI has some dependencies that got changed.
        /// </summary>
        /// <remarks>Used to know if the UI should be rebuilt.</remarks>
        /// <returns>True if some dependencies has changed, false otherwise.</returns>
        bool HasModelDependenciesChanged();

        /// <summary>
        /// Adds graph elements to the backward dependencies list. A backward dependency is
        /// a graph element that causes this model UI to be updated whenever it is updated.
        /// </summary>
        void AddBackwardDependencies();

        /// <summary>
        /// Adds graph elements to the forward dependencies list. A forward dependency is
        /// a graph element that should be updated whenever this model UI is updated.
        /// </summary>
        void AddForwardDependencies();

        /// <summary>
        /// Adds graph elements to the model dependencies list. A model dependency is
        /// a graph element model that causes this model UI to be updated whenever it is updated.
        /// </summary>
        void AddModelDependencies();

        /// <summary>
        /// Tries to paste in the selected ModelUI
        /// </summary>
        /// <param name="operation">The paste operation type.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="delta">The delta to add to node positions.</param>
        /// <param name="copyPasteData">The pasted data.</param>
        /// <returns>true if the paste is handled, false if the paste must be handled by another node or the graph</returns>
        bool PasteIn(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData);
    }
}
