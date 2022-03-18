using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The kind of paste.
    /// </summary>
    public enum PasteOperation
    {
        /// <summary>
        /// The paste is part of a duplicate operation.
        /// </summary>
        Duplicate,

        /// <summary>
        /// Paste the current clipboard content.
        /// </summary>
        Paste
    }

    /// <summary>
    /// Class that provides standard copy paste operations on a <see cref="SelectionStateComponent"/>.
    /// </summary>
    public abstract class ViewSelection
    {
        static IReadOnlyList<IGraphElementModel> s_EmptyList = new List<IGraphElementModel>();

        protected const string k_SerializedDataMimeType = "application/vnd.unity.graphview.elements";

        protected readonly RootView m_View;
        protected readonly GraphModelStateComponent m_GraphModelState;
        protected readonly SelectionStateComponent m_SelectionState;

        // For tests only
        protected virtual bool UseInternalClipboard => false;
        string m_Clipboard = string.Empty;

        // Internal access for tests.
        internal string Clipboard
        {
            get => UseInternalClipboard ? m_Clipboard : EditorGUIUtility.systemCopyBuffer;

            set
            {
                if (UseInternalClipboard)
                {
                    m_Clipboard = value;
                }
                else
                {
                    EditorGUIUtility.systemCopyBuffer = value;
                }
            }
        }

        /// <summary>
        /// All the models that can be selected in this view.
        /// </summary>
        public abstract IEnumerable<IGraphElementModel> SelectableModels {get;}

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewSelection"/> class.
        /// </summary>
        /// <param name="view">The view used to dispatch commands.</param>
        /// <param name="graphModelState">The graph model state.</param>
        /// <param name="selectionState">The selection state.</param>
        public ViewSelection(RootView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState)
        {
            m_View = view;
            m_GraphModelState = graphModelState;
            m_SelectionState = selectionState;
        }

        /// <summary>
        /// Makes the <see cref="ViewSelection"/> start processing copy paste commands.
        /// </summary>
        public void AttachToView()
        {
            m_View.RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            m_View.RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        }

        /// <summary>
        /// Makes the <see cref="ViewSelection"/> stop processing copy paste commands.
        /// </summary>
        public void DetachFromView()
        {
            m_View.UnregisterCallback<ValidateCommandEvent>(OnValidateCommand);
            m_View.UnregisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
        }

        /// <summary>
        /// Handles the <see cref="ValidateCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (m_View.panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == GraphViewStaticBridge.EventCommandNames.Copy && CanCopySelection)
                || (evt.commandName == GraphViewStaticBridge.EventCommandNames.Paste && CanPaste)
                || (evt.commandName == GraphViewStaticBridge.EventCommandNames.Duplicate && CanDuplicateSelection)
                || (evt.commandName == GraphViewStaticBridge.EventCommandNames.Cut && CanCutSelection)
                || evt.commandName == GraphViewStaticBridge.EventCommandNames.SelectAll
                || evt.commandName == GraphViewStaticBridge.EventCommandNames.DeselectAll
                || evt.commandName == GraphViewStaticBridge.EventCommandNames.InvertSelection
                || ((evt.commandName == GraphViewStaticBridge.EventCommandNames.Delete || evt.commandName == GraphViewStaticBridge.EventCommandNames.SoftDelete) && CanDeleteSelection))
            {
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Handles the <see cref="ExecuteCommandEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (m_View.panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Copy)
            {
                CopySelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Paste)
            {
                Paste();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Duplicate)
            {
                DuplicateSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Cut)
            {
                CutSelection();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Delete)
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection()) { UndoString = "Delete" });
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.SoftDelete)
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection()) { UndoString = "Delete" });
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.SelectAll)
            {
                m_View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, SelectableModels.ToList()));
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.DeselectAll)
            {
                m_View.Dispatch(new ClearSelectionCommand());
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.InvertSelection)
            {
                m_View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Toggle, SelectableModels.ToList()));
                evt.StopPropagation();
            }
        }

        /// <summary>
        /// Gets the selected models.
        /// </summary>
        /// <returns>The selected models.</returns>
        public IReadOnlyList<IGraphElementModel> GetSelection()
        {
            return m_SelectionState?.GetSelection(m_GraphModelState.GraphModel) ?? s_EmptyList;
        }

        /// <summary>
        /// Returns true if the selection can be copied.
        /// </summary>
        protected virtual bool CanCopySelection => GetSelection().Any(ge => ge.IsCopiable());

        /// <summary>
        /// Returns true if the selection can be cut (copied and deleted).
        /// </summary>
        protected virtual bool CanCutSelection => GetSelection().Any(ge => ge.IsCopiable() && ge.IsDeletable());

        /// <summary>
        /// Returns true if the clipboard content can be pasted.
        /// </summary>
        protected virtual bool CanPaste => CanPasteSerializedData(Clipboard);

        /// <summary>
        /// Returns true if the selection can be duplicated.
        /// </summary>
        protected virtual bool CanDuplicateSelection => CanCopySelection;

        /// <summary>
        /// Returns true if the selection can be deleted.
        /// </summary>
        protected virtual bool CanDeleteSelection => GetSelection().Any(ge => ge.IsDeletable());

        /// <summary>
        /// Serializes the selection and related elements to the clipboard.
        /// </summary>
        protected virtual void CopySelection()
        {
            var elementsToCopySet = CollectCopyableGraphElements(GetSelection());
            var copyPasteData = BuildCopyPasteData(elementsToCopySet);
            var serializedData = SerializedPasteData(copyPasteData);
            if (!string.IsNullOrEmpty(serializedData))
            {
                Clipboard = k_SerializedDataMimeType + " " + serializedData;
            }
        }

        /// <summary>
        /// Serializes the selection and related elements to the clipboard, then deletes the selection.
        /// </summary>
        protected virtual void CutSelection()
        {
            CopySelection();
            m_View.Dispatch(new DeleteElementsCommand(GetSelection()) { UndoString = "Cut" });
        }

        /// <summary>
        /// Pastes the clipboard content into the graph.
        /// </summary>
        protected virtual void Paste()
        {
            var data = Clipboard;
            if (data.StartsWith(k_SerializedDataMimeType))
            {
                data = data.Substring(k_SerializedDataMimeType.Length + 1);
            }

            UnserializeAndPaste(PasteOperation.Paste, "Paste", data);
        }

        /// <summary>
        /// Duplicates the selection and related elements.
        /// </summary>
        protected virtual void DuplicateSelection()
        {
            var elementsToCopySet = CollectCopyableGraphElements(GetSelection());
            var copyPasteData = BuildCopyPasteData(elementsToCopySet);
            var serializedData = SerializedPasteData(copyPasteData);
            UnserializeAndPaste(PasteOperation.Duplicate, "Duplicate", serializedData);
        }

        /// <summary>
        /// Builds the set of elements to be copied from an initial set of elements.
        /// </summary>
        /// <param name="elements">The initial set of elements, usually the selection.</param>
        /// <returns>A set of elements to be copied, usually <paramref name="elements"/> plus related elements.</returns>
        protected virtual HashSet<IGraphElementModel> CollectCopyableGraphElements(IEnumerable<IGraphElementModel> elements)
        {
            var elementsToCopySet = new HashSet<IGraphElementModel>();
            FilterElements(elements, elementsToCopySet, IsCopiable);
            return elementsToCopySet;
        }

        /// <summary>
        /// Creates a <see cref="CopyPasteData"/> from a set of elements to copy. This data will eventually be
        /// serialized and saved to the clipboard.
        /// </summary>
        /// <param name="elementsToCopySet">The set of elements to copy.</param>
        /// <returns>The newly created <see cref="CopyPasteData"/>.</returns>.
        protected abstract CopyPasteData BuildCopyPasteData(HashSet<IGraphElementModel> elementsToCopySet);

        /// <summary>
        /// Serializes the <paramref name="copyPasteData"/>.
        /// </summary>
        /// <param name="copyPasteData">The data to serialize.</param>
        /// <returns>The serialized data.</returns>
        protected virtual string SerializedPasteData(CopyPasteData copyPasteData)
        {
            return JsonUtility.ToJson(copyPasteData, true);
        }

        /// <summary>
        /// Gets the offset at which data should be pasted.
        /// </summary>
        /// <remarks>
        /// Often, pasted nodes should not be pasted at their original position so they
        /// do not hide the original nodes. This method gives the offset to apply on the pasted nodes.
        /// </remarks>
        /// <param name="data">The data to paste.</param>
        /// <returns>The offset to apply to the pasted elements.</returns>
        protected virtual Vector2 GetPasteDelta(CopyPasteData data)
        {
            return Vector2.zero;
        }

        /// <summary>
        /// Paste the content of data into the graph.
        /// </summary>
        /// <param name="operation">The kind of operation.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="data">The serialized data.</param>
        protected virtual void UnserializeAndPaste(PasteOperation operation, string operationName, string data)
        {
            var copyPaste = JsonUtility.FromJson<CopyPasteData>(data);
            var delta = GetPasteDelta(copyPaste);
            var selection = GetSelection();
            foreach (var selected in selection.Reverse())
            {
                var ui = selected.GetView(m_View);
                if (ui != null && ui.HandlePasteOperation(operation, operationName, delta, copyPaste))
                    return;
            }

            m_View.Dispatch(new PasteSerializedDataCommand(operation, operationName, delta, copyPaste));
        }

        /// <summary>
        /// Builds a set of unique, non null elements that satisfies the <paramref name="conditionFunc"/>.
        /// </summary>
        /// <param name="elements">The source elements.</param>
        /// <param name="collectedElementSet">The set of elements that satisfies the <paramref name="conditionFunc"/>.</param>
        /// <param name="conditionFunc">The filter to apply.</param>
        protected static void FilterElements(IEnumerable<IGraphElementModel> elements, HashSet<IGraphElementModel> collectedElementSet, Func<IGraphElementModel, bool> conditionFunc)
        {
            foreach (var element in elements.Where(e => e != null && conditionFunc(e)))
            {
                collectedElementSet.Add(element);
            }
        }

        /// <summary>
        /// Returns true if the model is not null and the model is copiable.
        /// </summary>
        /// <param name="model">The model to check.</param>
        /// <returns>True if the model is not null and the model is copiable</returns>
        protected static bool IsCopiable(IGraphElementModel model)
        {
            return model?.IsCopiable() ?? false;
        }

        /// <summary>
        /// Returns true if the data can be pasted into <see cref="m_View"/>.
        /// </summary>
        /// <param name="data">The data to check.</param>
        /// <returns>True if the data can be pasted.</returns>
        protected virtual bool CanPasteSerializedData(string data)
        {
            return data.StartsWith(k_SerializedDataMimeType);
        }

        /// <summary>
        /// Adds items related to the selection to the contextual menu.
        /// </summary>
        /// <param name="evt">The contextual menu event.</param>
        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Cut", _ => { CutSelection(); },
                CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy", _ => { CopySelection(); },
                CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Paste", _ => { Paste(); },
                CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Duplicate", _ => { DuplicateSelection(); },
                CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Delete", _ =>
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection().ToList()));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Select All", _ =>
            {
                m_View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, SelectableModels.ToList()));
            }, _ => DropdownMenuAction.Status.Normal);
        }
    }
}
