using System.Collections.Generic;
using UnityEditor.AnimatedValues;

namespace UnityEditor.Experimental.Rendering
{
    public static class CoreEditorDrawer<TUIState, TData>
    {
        public interface IDrawer
        {
            void Draw(TUIState s, TData p, Editor owner);
        }

        public delegate void ActionDrawer(TUIState s, TData p, Editor owner);
        public delegate float FloatGetter(TUIState s, TData p, Editor owner, int i);
        public delegate AnimBool AnimBoolGetter(TUIState s, TData p, Editor owner);

        public static readonly IDrawer space = Action((state, data, owner) => EditorGUILayout.Space());
        public static readonly IDrawer noop = Action((state, data, owner) => { });

        public static IDrawer Action(params ActionDrawer[] drawers)
        {
            return new ActionDrawerInternal(drawers);
        }

        public static IDrawer FadeGroup(FloatGetter fadeGetter, bool indent, params IDrawer[] groupDrawers)
        {
            return new FadeGroupsDrawerInternal(fadeGetter, indent, groupDrawers);
        }

        public static IDrawer FoldoutGroup(string title, AnimBoolGetter root, bool indent, params IDrawer[] bodies)
        {
            return new FoldoutDrawerInternal(title, root, indent, bodies);
        }

        class ActionDrawerInternal : IDrawer
        {
            ActionDrawer[] actionDrawers { get; set; }
            public ActionDrawerInternal(params ActionDrawer[] actionDrawers)
            {
                this.actionDrawers = actionDrawers;
            }

            void IDrawer.Draw(TUIState s, TData p, Editor owner)
            {
                for (var i = 0; i < actionDrawers.Length; i++)
                    actionDrawers[i](s, p, owner);
            }
        }

        class FadeGroupsDrawerInternal : IDrawer
        {
            IDrawer[] groupDrawers;
            FloatGetter getter;
            bool indent;

            public FadeGroupsDrawerInternal(FloatGetter getter, bool indent, params IDrawer[] groupDrawers)
            {
                this.groupDrawers = groupDrawers;
                this.getter = getter;
                this.indent = indent;
            }

            void IDrawer.Draw(TUIState s, TData p, Editor owner)
            {
                for (var i = 0; i < groupDrawers.Length; ++i)
                {
                    if (EditorGUILayout.BeginFadeGroup(getter(s, p, owner, i)))
                    {
                        if (indent)
                            ++EditorGUI.indentLevel;
                        groupDrawers[i].Draw(s, p, owner);
                        if (indent)
                            --EditorGUI.indentLevel;
                    }
                    EditorGUILayout.EndFadeGroup();
                }
            }
        }

        class FoldoutDrawerInternal : IDrawer
        {
            IDrawer[] bodies;
            AnimBoolGetter isExpanded;
            string title;
            bool indent;

            public FoldoutDrawerInternal(string title, AnimBoolGetter isExpanded, bool indent, params IDrawer[] bodies)
            {
                this.title = title;
                this.isExpanded = isExpanded;
                this.bodies = bodies;
                this.indent = indent;
            }

            public void Draw(TUIState s, TData p, Editor owner)
            {
                var r = isExpanded(s, p, owner);
                CoreEditorUtils.DrawSplitter();
                r.target = CoreEditorUtils.DrawHeaderFoldout(title, r.target);
                if (EditorGUILayout.BeginFadeGroup(r.faded))
                {
                    if (indent)
                        ++EditorGUI.indentLevel;
                    for (var i = 0; i < bodies.Length; i++)
                        bodies[i].Draw(s, p, owner);
                    if (indent)
                        --EditorGUI.indentLevel;
                }
                EditorGUILayout.EndFadeGroup();
            }
        }
    }

    public static class CoreEditorDrawersExtensions
    {
        public static void Draw<TUIState, TData>(this IEnumerable<CoreEditorDrawer<TUIState, TData>.IDrawer> drawers, TUIState s, TData p, Editor o)
        {
            foreach (var drawer in drawers)
                drawer.Draw(s, p, o);
        }
    }
}
