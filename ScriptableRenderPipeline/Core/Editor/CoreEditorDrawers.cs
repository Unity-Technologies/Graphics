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
        public delegate SerializedProperty SerializedPropertyGetter(TUIState s, TData p, Editor o);


        public static IDrawer Action(params ActionDrawer[] drawers)
        {
            return new ActionDrawerInternal(drawers);
        }

        public static IDrawer FadeGroup(FloatGetter fadeGetter, params IDrawer[] groupDrawers)
        {
            return new FadeGroupsDrawerInternal(fadeGetter, groupDrawers);
        }

        public static IDrawer FoldoutGroup(string title, SerializedPropertyGetter root, params IDrawer[] bodies)
        {
            return new FoldoutDrawerInternal(title, root, bodies);
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

            public FadeGroupsDrawerInternal(FloatGetter getter, params IDrawer[] groupDrawers)
            {
                this.groupDrawers = groupDrawers;
                this.getter = getter;
            }

            void IDrawer.Draw(TUIState s, TData p, Editor owner)
            {
                for (var i = 0; i < groupDrawers.Length; ++i)
                {
                    if (EditorGUILayout.BeginFadeGroup(getter(s, p, owner, i)))
                    {
                        ++EditorGUI.indentLevel;
                        groupDrawers[i].Draw(s, p, owner);
                        --EditorGUI.indentLevel;
                    }
                    EditorGUILayout.EndFadeGroup();
                }
            }
        }

        class FoldoutDrawerInternal : IDrawer
        {
            IDrawer[] bodies;
            SerializedPropertyGetter root;
            string title;

            public FoldoutDrawerInternal(string title, SerializedPropertyGetter root, params IDrawer[] bodies)
            {
                this.title = title;
                this.root = root;
                this.bodies = bodies;
            }

            public void Draw(TUIState s, TData p, Editor owner)
            {
                var r = root(s, p, owner);
                CoreEditorUtils.DrawSplitter();
                r.isExpanded = CoreEditorUtils.DrawHeaderFoldout(title, r.isExpanded);
                if (r.isExpanded)
                {
                    ++EditorGUI.indentLevel;
                    for (var i = 0; i < bodies.Length; i++)
                        bodies[i].Draw(s, p, owner);
                    --EditorGUI.indentLevel;
                }
            }
        }
    }
}
