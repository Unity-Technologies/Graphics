using System;
using UnityEditor.ShortcutManagement;
using UnityEngine;
// ReSharper disable RedundantArgumentDefaultValue

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An event sent by the Frame All shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutFrameAllEvent : ShortcutEventBase<ShortcutFrameAllEvent>
    {
        internal const string id = "Frame All";
        internal const KeyCode keyCode = KeyCode.A;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Frame Origin shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutFrameOriginEvent : ShortcutEventBase<ShortcutFrameOriginEvent>
    {
        internal const string id = "Frame Origin";
        internal const KeyCode keyCode = KeyCode.O;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Frame Previous shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutFramePreviousEvent : ShortcutEventBase<ShortcutFramePreviousEvent>
    {
        internal const string id = "Frame Previous";
        internal const KeyCode keyCode = KeyCode.LeftBracket;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Frame Next shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutFrameNextEvent : ShortcutEventBase<ShortcutFrameNextEvent>
    {
        internal const string id = "Frame Next";
        internal const KeyCode keyCode = KeyCode.RightBracket;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Delete shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutDeleteEvent : ShortcutEventBase<ShortcutDeleteEvent>
    {
        internal const string id = "Delete";
        internal const KeyCode keyCode = KeyCode.Backspace;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Display Smart Search shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutDisplaySmartSearchEvent : ShortcutEventBase<ShortcutDisplaySmartSearchEvent>
    {
        internal const string id = "Display Smart Search";
        internal const KeyCode keyCode = KeyCode.Space;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Convert Variable And Constant shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutConvertConstantAndVariableEvent : ShortcutEventBase<ShortcutConvertConstantAndVariableEvent>
    {
        internal const string id = "Convert Variable And Constant";
        internal const KeyCode keyCode = KeyCode.C;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Align Nodes shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutAlignNodesEvent : ShortcutEventBase<ShortcutAlignNodesEvent>
    {
        internal const string id = "Align Nodes";
        internal const KeyCode keyCode = KeyCode.I;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }

    /// <summary>
    /// An event sent by the Align Hierarchies shortcut.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutAlignNodeHierarchiesEvent : ShortcutEventBase<ShortcutAlignNodeHierarchiesEvent>
    {
        internal const string id = "Align Hierarchies";
        internal const KeyCode keyCode = KeyCode.I;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.Shift;
    }

    /// <summary>
    /// An event sent by the Create Sticky Note.
    /// </summary>
    [ToolShortcutEvent(null, id, keyCode, modifiers)]
    public class ShortcutCreateStickyNoteEvent : ShortcutEventBase<ShortcutCreateStickyNoteEvent>
    {
        internal const string id = "Create Sticky Note";
        internal const KeyCode keyCode = KeyCode.BackQuote;
        internal const ShortcutModifiers modifiers = ShortcutModifiers.None;
    }
}
