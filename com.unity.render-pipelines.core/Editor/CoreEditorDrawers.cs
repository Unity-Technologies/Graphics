using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>display options added to the Foldout</summary>
    [Flags]
    public enum FoldoutOption
    {
        /// <summary>No Option</summary>
        None = 0,
        /// <summary>Foldout will be indented</summary>
        Indent = 1 << 0,
        /// <summary>Foldout will be boxed</summary>
        Boxed = 1 << 2,
        /// <summary>Foldout will be inside another foldout</summary>
        SubFoldout = 1 << 3,
        /// <summary>Remove the space at end of the foldout</summary>
        NoSpaceAtEnd = 1 << 4
    }

    /// <summary>display options added to the Group</summary>
    [Flags]
    public enum GroupOption
    {
        /// <summary>No Option</summary>
        None = 0,
        /// <summary>Group will be indented</summary>
        Indent = 1 << 0
    }

    /// <summary>
    /// Utility class to draw inspectors
    /// </summary>
    /// <typeparam name="TData">Type of class containing data needed to draw inspector</typeparam>
    public static class CoreEditorDrawer<TData>
    {
        /// <summary> Abstraction that have the Draw hability </summary>
        public interface IDrawer
        {
            /// <summary>
            /// The draw function
            /// </summary>
            /// <param name="serializedProperty">The SerializedProperty to draw</param>
            /// <param name="owner">The editor handling this draw call</param>
            void Draw(TData serializedProperty, Editor owner);
        }

        /// <summary>Delegate that must say if this is enabled for drawing</summary>
        /// <param name="data">The data used</param>
        /// <param name="owner">The editor rendering</param>
        /// <returns>True if this should be drawn</returns>
        public delegate bool Enabler(TData data, Editor owner);

        /// <summary>Delegate is called when the foldout state is switched</summary>
        /// <param name="data">The data used</param>
        /// <param name="owner">The editor rendering</param>
        public delegate void SwitchEnabler(TData data, Editor owner);
        
        /// <summary>Delegate that must be used to select sub object for data for drawing</summary>
        /// <typeparam name="T2Data">The type of the sub object used for data</typeparam>
        /// <param name="data">The data used</param>
        /// <param name="owner">The editor rendering</param>
        /// <returns>Embeded object that will be used as data source for later draw in this Select</returns>
        public delegate T2Data DataSelect<T2Data>(TData data, Editor owner);

        /// <summary>Delegate type alternative to IDrawer</summary>
        /// <param name="data">The data used</param>
        /// <param name="owner">The editor rendering</param>
        public delegate void ActionDrawer(TData data, Editor owner);

        /// <summary> Equivalent to EditorGUILayout.Space that can be put in a drawer group </summary>
        public static readonly IDrawer space = Group((data, owner) => EditorGUILayout.Space());

        /// <summary> Use it when IDrawer required but no operation should be done </summary>
        public static readonly IDrawer noop = Group((data, owner) => { });

        /// <summary>
        /// Conditioned drawer that will only be drawn if its enabler function is null or return true
        /// </summary>
        /// <param name="enabler">Enable the drawing if null or return true</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Conditional(Enabler enabler, params IDrawer[] contentDrawers)
        {
            return new ConditionalDrawerInternal(enabler, contentDrawers.Draw);
        }

        /// <summary>
        /// Conditioned drawer that will only be drawn if its enabler function is null or return true
        /// </summary>
        /// <param name="enabler">Enable the drawing if null or return true</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Conditional(Enabler enabler, params ActionDrawer[] contentDrawers)
        {
            return new ConditionalDrawerInternal(enabler, contentDrawers);
        }

        class ConditionalDrawerInternal : IDrawer
        {
            ActionDrawer[] actionDrawers { get; set; }
            Enabler m_Enabler;

            public ConditionalDrawerInternal(Enabler enabler = null, params ActionDrawer[] actionDrawers)
            {
                this.actionDrawers = actionDrawers;
                m_Enabler = enabler;
            }

            void IDrawer.Draw(TData data, Editor owner)
            {
                if (m_Enabler != null && !m_Enabler(data, owner))
                    return;

                for (var i = 0; i < actionDrawers.Length; i++)
                    actionDrawers[i](data, owner);
            }
        }

        /// <summary>
        /// Conditioned drawer that will draw something depending of the return of the switch
        /// </summary>
        /// <param name="switch">Chose witch drawing to use</param>
        /// <param name="drawIfTrue">This will be draw if the <see cref="switch"/> is true</param>
        /// <param name="drawIfFalse">This will be draw if the <see cref="switch"/> is false</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer TernaryConditional(Enabler @switch, IDrawer drawIfTrue, IDrawer drawIfFalse)
            => new TernaryConditionalDrawerInternal(@switch, drawIfTrue.Draw, drawIfFalse.Draw);

        /// <summary>
        /// Conditioned drawer that will draw something depending of the return of the switch
        /// </summary>
        /// <param name="switch">Chose witch drawing to use</param>
        /// <param name="drawIfTrue">This will be draw if the <see cref="switch"/> is true</param>
        /// <param name="drawIfFalse">This will be draw if the <see cref="switch"/> is false</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer TernaryConditional(Enabler @switch, ActionDrawer drawIfTrue, ActionDrawer drawIfFalse)
            => new TernaryConditionalDrawerInternal(@switch, drawIfTrue, drawIfFalse);

        class TernaryConditionalDrawerInternal : IDrawer
        {
            ActionDrawer drawIfTrue;
            ActionDrawer drawIfFalse;
            Enabler m_Switch;

            public TernaryConditionalDrawerInternal(Enabler @switch, ActionDrawer drawIfTrue, ActionDrawer drawIfFalse)
            {
                this.drawIfTrue = drawIfTrue;
                this.drawIfFalse = drawIfFalse;
                m_Switch = @switch;
            }

            void IDrawer.Draw(TData data, Editor owner)
            {
                if (m_Switch != null && !m_Switch(data, owner))
                    drawIfFalse?.Invoke(data, owner);
                else
                    drawIfTrue?.Invoke(data, owner);
            }
        }

        /// <summary>
        /// Group of drawing function for inspector.
        /// They will be drawn one after the other.
        /// </summary>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(params IDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(-1f, GroupOption.None, contentDrawers.Draw);
        }

        /// <summary>
        /// Group of drawing function for inspector.
        /// They will be drawn one after the other.
        /// </summary>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(params ActionDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(-1f, GroupOption.None, contentDrawers);
        }

        /// <summary> Group of drawing function for inspector with a set width for labels </summary>
        /// <param name="labelWidth">Width used for all labels in the group</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(float labelWidth, params IDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(labelWidth, GroupOption.None, contentDrawers.Draw);
        }

        /// <summary> Group of drawing function for inspector with a set width for labels </summary>
        /// <param name="labelWidth">Width used for all labels in the group</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(float labelWidth, params ActionDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(labelWidth, GroupOption.None, contentDrawers);
        }

        /// <summary>
        /// Group of drawing function for inspector.
        /// They will be drawn one after the other.
        /// </summary>
        /// <param name="options">Allow to add indentation on this group</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(GroupOption options, params IDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(-1f, options, contentDrawers.Draw);
        }

        /// <summary>
        /// Group of drawing function for inspector.
        /// They will be drawn one after the other.
        /// </summary>
        /// <param name="options">Allow to add indentation on this group</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(GroupOption options, params ActionDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(-1f, options, contentDrawers);
        }

        /// <summary> Group of drawing function for inspector with a set width for labels </summary>
        /// <param name="labelWidth">Width used for all labels in the group</param>
        /// <param name="options">Allow to add indentation on this group</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(float labelWidth, GroupOption options, params IDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(labelWidth, options, contentDrawers.Draw);
        }

        /// <summary> Group of drawing function for inspector with a set width for labels </summary>
        /// <param name="labelWidth">Width used for all labels in the group</param>
        /// <param name="options">Allow to add indentation on this group</param>
        /// <param name="contentDrawers">The content of the group</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Group(float labelWidth, GroupOption options, params ActionDrawer[] contentDrawers)
        {
            return new GroupDrawerInternal(labelWidth, options, contentDrawers);
        }

        class GroupDrawerInternal : IDrawer
        {
            ActionDrawer[] actionDrawers { get; set; }
            float m_LabelWidth;
            bool isIndented;

            public GroupDrawerInternal(float labelWidth = -1f, GroupOption options = GroupOption.None, params ActionDrawer[] actionDrawers)
            {
                this.actionDrawers = actionDrawers;
                m_LabelWidth = labelWidth;
                isIndented = (options & GroupOption.Indent) != 0;
            }

            void IDrawer.Draw(TData data, Editor owner)
            {
                if (isIndented)
                    ++EditorGUI.indentLevel;
                var currentLabelWidth = EditorGUIUtility.labelWidth;
                if (m_LabelWidth >= 0f)
                {
                    EditorGUIUtility.labelWidth = m_LabelWidth;
                }
                for (var i = 0; i < actionDrawers.Length; i++)
                    actionDrawers[i](data, owner);
                if (m_LabelWidth >= 0f)
                {
                    EditorGUIUtility.labelWidth = currentLabelWidth;
                }
                if (isIndented)
                    --EditorGUI.indentLevel;
            }
        }

        /// <summary> Create an IDrawer based on an other data container </summary>
        /// <typeparam name="T2Data">Type of selected object containing in the given data containing data needed to draw inspector</typeparam>
        /// <param name="dataSelect">The data new source for the inner drawers</param>
        /// <param name="otherDrawers">Inner drawers drawed with given data sources</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Select<T2Data>(
            DataSelect<T2Data> dataSelect,
            params CoreEditorDrawer<T2Data>.IDrawer[] otherDrawers)
        {
            return new SelectDrawerInternal<T2Data>(dataSelect, otherDrawers.Draw);
        }

        /// <summary> Create an IDrawer based on an other data container </summary>
        /// <typeparam name="T2Data">Type of selected object containing in the given data containing data needed to draw inspector</typeparam>
        /// <param name="dataSelect">The data new source for the inner drawers</param>
        /// <param name="otherDrawers">Inner drawers drawed with given data sources</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer Select<T2Data>(
            DataSelect<T2Data> dataSelect,
            params CoreEditorDrawer<T2Data>.ActionDrawer[] otherDrawers)
        {
            return new SelectDrawerInternal<T2Data>(dataSelect, otherDrawers);
        }

        class SelectDrawerInternal<T2Data> : IDrawer
        {
            DataSelect<T2Data> m_DataSelect;
            CoreEditorDrawer<T2Data>.ActionDrawer[] m_SourceDrawers;

            public SelectDrawerInternal(DataSelect<T2Data> dataSelect,
                params CoreEditorDrawer<T2Data>.ActionDrawer[] otherDrawers)
            {
                m_SourceDrawers = otherDrawers;
                m_DataSelect = dataSelect;
            }

            void IDrawer.Draw(TData data, Editor o)
            {
                var p2 = m_DataSelect(data, o);
                for (var i = 0; i < m_SourceDrawers.Length; i++)
                    m_SourceDrawers[i](p2, o);
            }
        }

        /// <summary>
        /// Create an IDrawer foldout header using an ExpandedState.
        /// The default option is Indent in this version.
        /// </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(string title, TEnum mask, ExpandedState<TEnum, TState> state, params IDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(title, mask, state, contentDrawers.Draw);
        }

        /// <summary>
        /// Create an IDrawer foldout header using an ExpandedState.
        /// The default option is Indent in this version.
        /// </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(string title, TEnum mask, ExpandedState<TEnum, TState> state, params ActionDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(EditorGUIUtility.TrTextContent(title), mask, state, contentDrawers);
        }
        
        /// <summary> Create an IDrawer foldout header using an ExpandedState </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="options">Drawing options</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(string title, TEnum mask, ExpandedState<TEnum, TState> state, FoldoutOption options, params IDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(title, mask, state, options, contentDrawers.Draw);
        }

        /// <summary> Create an IDrawer foldout header using an ExpandedState </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="options">Drawing options</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(string title, TEnum mask, ExpandedState<TEnum, TState> state, FoldoutOption options, params ActionDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(EditorGUIUtility.TrTextContent(title), mask, state, options, contentDrawers);
        }

        /// <summary>
        /// Create an IDrawer foldout header using an ExpandedState.
        /// The default option is Indent in this version.
        /// </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(GUIContent title, TEnum mask, ExpandedState<TEnum, TState> state, params IDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(title, mask, state, contentDrawers.Draw);
        }

        /// <summary>
        /// Create an IDrawer foldout header using an ExpandedState.
        /// The default option is Indent in this version.
        /// </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(GUIContent title, TEnum mask, ExpandedState<TEnum, TState> state, params ActionDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(title, mask, state, FoldoutOption.Indent, contentDrawers);
        }

        /// <summary> Create an IDrawer foldout header using an ExpandedState </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="options">Drawing options</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(GUIContent title, TEnum mask, ExpandedState<TEnum, TState> state, FoldoutOption options, params IDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(title, mask, state, options, contentDrawers.Draw);
        }

        /// <summary> Create an IDrawer foldout header using an ExpandedState </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="title">Title wanted for this foldout header</param>
        /// <param name="mask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="state">The ExpandedState describing the component</param>
        /// <param name="options">Drawing options</param>
        /// <param name="contentDrawers">The content of the foldout header</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer FoldoutGroup<TEnum, TState>(GUIContent title, TEnum mask, ExpandedState<TEnum, TState> state, FoldoutOption options, params ActionDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(title, mask, state, options, null, null, contentDrawers);
        }

        // This one is private as we do not want to have unhandled advanced switch. Change it if necessary.
        static IDrawer FoldoutGroup<TEnum, TState>(GUIContent title, TEnum mask, ExpandedState<TEnum, TState> state, FoldoutOption options, Enabler isAdvanced, SwitchEnabler switchAdvanced, params ActionDrawer[] contentDrawers)
            where TEnum : struct, IConvertible
        {
            return Group((data, owner) =>
            {
                bool isBoxed = (options & FoldoutOption.Boxed) != 0;
                bool isIndented = (options & FoldoutOption.Indent) != 0;
                bool isSubFoldout = (options & FoldoutOption.SubFoldout) != 0;
                bool noSpaceAtEnd = (options & FoldoutOption.NoSpaceAtEnd) != 0;
                bool expended = state[mask];
                bool newExpended = expended;
                if (isSubFoldout)
                {
                    newExpended = CoreEditorUtils.DrawSubHeaderFoldout(title, expended, isBoxed,
                        isAdvanced == null ? (Func<bool>)null : () => isAdvanced(data, owner),
                        switchAdvanced == null ? (Action)null : () => switchAdvanced(data, owner));
                }
                else
                {
                    CoreEditorUtils.DrawSplitter(isBoxed);
                    newExpended = CoreEditorUtils.DrawHeaderFoldout(title, expended, isBoxed,
                        isAdvanced == null ? (Func<bool>)null : () => isAdvanced(data, owner),
                        switchAdvanced == null ? (Action)null : () => switchAdvanced(data, owner));
                }
                if (newExpended ^ expended)
                    state[mask] = newExpended;
                if (newExpended)
                {
                    if (isIndented)
                        ++EditorGUI.indentLevel;
                    for (var i = 0; i < contentDrawers.Length; i++)
                        contentDrawers[i](data, owner);
                    if (isIndented)
                        --EditorGUI.indentLevel;
                    if (!noSpaceAtEnd)
                        EditorGUILayout.Space();
                }
            });
        }

        /// <summary> Helper to draw a foldout with an advanced switch on it. </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="foldoutTitle">Title wanted for this foldout header</param>
        /// <param name="foldoutMask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="foldoutState">The ExpandedState describing the component</param>
        /// <param name="isAdvanced"> Delegate allowing to check if advanced mode is active. </param>
        /// <param name="switchAdvanced"> Delegate to know what to do when advance is switched. </param>
        /// <param name="normalContent"> The content of the foldout header always visible if expended. </param>
        /// <param name="advancedContent"> The content of the foldout header only visible if advanced mode is active and if foldout is expended. </param>
        /// <param name="options">Drawing options</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer AdvancedFoldoutGroup<TEnum, TState>(GUIContent foldoutTitle, TEnum foldoutMask, ExpandedState<TEnum, TState> foldoutState, Enabler isAdvanced, SwitchEnabler switchAdvanced, IDrawer normalContent, IDrawer advancedContent, FoldoutOption options = FoldoutOption.Indent)
            where TEnum : struct, IConvertible
        {
            return AdvancedFoldoutGroup(foldoutTitle, foldoutMask, foldoutState, isAdvanced, switchAdvanced, normalContent.Draw, advancedContent.Draw, options);
        }

        /// <summary> Helper to draw a foldout with an advanced switch on it. </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="foldoutTitle">Title wanted for this foldout header</param>
        /// <param name="foldoutMask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="foldoutState">The ExpandedState describing the component</param>
        /// <param name="isAdvanced"> Delegate allowing to check if advanced mode is active. </param>
        /// <param name="switchAdvanced"> Delegate to know what to do when advance is switched. </param>
        /// <param name="normalContent"> The content of the foldout header always visible if expended. </param>
        /// <param name="advancedContent"> The content of the foldout header only visible if advanced mode is active and if foldout is expended. </param>
        /// <param name="options">Drawing options</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer AdvancedFoldoutGroup<TEnum, TState>(GUIContent foldoutTitle, TEnum foldoutMask, ExpandedState<TEnum, TState> foldoutState, Enabler isAdvanced, SwitchEnabler switchAdvanced, ActionDrawer normalContent, IDrawer advancedContent, FoldoutOption options = FoldoutOption.Indent)
            where TEnum : struct, IConvertible
        {
            return AdvancedFoldoutGroup(foldoutTitle, foldoutMask, foldoutState, isAdvanced, switchAdvanced, normalContent, advancedContent.Draw, options);
        }

        /// <summary> Helper to draw a foldout with an advanced switch on it. </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="foldoutTitle">Title wanted for this foldout header</param>
        /// <param name="foldoutMask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="foldoutState">The ExpandedState describing the component</param>
        /// <param name="isAdvanced"> Delegate allowing to check if advanced mode is active. </param>
        /// <param name="switchAdvanced"> Delegate to know what to do when advance is switched. </param>
        /// <param name="normalContent"> The content of the foldout header always visible if expended. </param>
        /// <param name="advancedContent"> The content of the foldout header only visible if advanced mode is active and if foldout is expended. </param>
        /// <param name="options">Drawing options</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer AdvancedFoldoutGroup<TEnum, TState>(GUIContent foldoutTitle, TEnum foldoutMask, ExpandedState<TEnum, TState> foldoutState, Enabler isAdvanced, SwitchEnabler switchAdvanced, IDrawer normalContent, ActionDrawer advancedContent, FoldoutOption options = FoldoutOption.Indent)
            where TEnum : struct, IConvertible
        {
            return AdvancedFoldoutGroup(foldoutTitle, foldoutMask, foldoutState, isAdvanced, switchAdvanced, normalContent.Draw, advancedContent, options);
        }

        /// <summary> Helper to draw a foldout with an advanced switch on it. </summary>
        /// <typeparam name="TEnum">Type of the mask used</typeparam>
        /// <typeparam name="TState">Type of the persistent state</typeparam>
        /// <param name="foldoutTitle">Title wanted for this foldout header</param>
        /// <param name="foldoutMask">Bit mask (enum) used to define the boolean saving the state in ExpandedState</param>
        /// <param name="foldoutState">The ExpandedState describing the component</param>
        /// <param name="isAdvanced"> Delegate allowing to check if advanced mode is active. </param>
        /// <param name="switchAdvanced"> Delegate to know what to do when advance is switched. </param>
        /// <param name="normalContent"> The content of the foldout header always visible if expended. </param>
        /// <param name="advancedContent"> The content of the foldout header only visible if advanced mode is active and if foldout is expended. </param>
        /// <param name="options">Drawing options</param>
        /// <returns>A IDrawer object</returns>
        public static IDrawer AdvancedFoldoutGroup<TEnum, TState>(GUIContent foldoutTitle, TEnum foldoutMask, ExpandedState<TEnum, TState> foldoutState, Enabler isAdvanced, SwitchEnabler switchAdvanced, ActionDrawer normalContent, ActionDrawer advancedContent, FoldoutOption options = FoldoutOption.Indent)
            where TEnum : struct, IConvertible
        {
            return FoldoutGroup(foldoutTitle, foldoutMask, foldoutState, options, isAdvanced, switchAdvanced,
                normalContent,
                Conditional((serialized, owner) => isAdvanced(serialized, owner) && foldoutState[foldoutMask], advancedContent).Draw
                );
        }
    }

    /// <summary>CoreEditorDrawer extensions</summary>
    public static class CoreEditorDrawersExtensions
    {
        /// <summary> Concatenate a collection of IDrawer as a unique IDrawer </summary>
        /// <typeparam name="TData">Type of class containing data needed to draw inspector</typeparam>
        /// <param name="drawers">A collection of IDrawers</param>
        /// <param name="data">The data source for the inner drawers</param>
        /// <param name="owner">The editor drawing</param>
        public static void Draw<TData>(this IEnumerable<CoreEditorDrawer<TData>.IDrawer> drawers, TData data, Editor owner)
        {
            foreach (var drawer in drawers)
                drawer.Draw(data, owner);
        }
    }
}
