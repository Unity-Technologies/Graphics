using System;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
{
    public class GraphInspectorView : DataWatchContainer
    {
        [SerializeField]
        GraphInspectorPresenter m_Presenter;

        int m_PresenterHash;

        VisualElement m_Title;
        VisualElement m_ContentContainer;
        VisualElement m_MultipleSelectionsElement;
        VisualElement m_PropertiesContainer;

        TypeMapper m_TypeMapper;

        public GraphInspectorView()
        {
            AddStyleSheetPath("Styles/MaterialGraph");

            var headerContainer = new VisualElement { name = "header" };
            {
                m_Title = new VisualElement() { name = "title" };
                headerContainer.Add(m_Title);
            }
            Add(headerContainer);

            ReaddProps();

            m_ContentContainer = new VisualElement { name = "contentContainer" };
            Add(m_ContentContainer);

            m_TypeMapper = new TypeMapper(typeof(AbstractNodeEditorPresenter), typeof(AbstractNodeEditorView))
            {
                { typeof(StandardNodeEditorPresenter), typeof(StandardNodeEditorView) },
//                { typeof(SurfaceMasterNodeEditorPresenter), typeof(SurfaceMasterNodeEditorView) }
            };
        }

        private void ReaddProps()
        {
            if (m_PropertiesContainer != null)
                Remove(m_PropertiesContainer);

            m_PropertiesContainer = new VisualElement()
            {
                new IMGUIContainer(OnGuiHandler)
            };

            m_PropertiesContainer.style.flexDirection = StyleValue<FlexDirection>.Create(FlexDirection.Column);
            Add(m_PropertiesContainer);
        }

        private void OnGuiHandler()
        {
            if (m_Presenter == null)
                return;

            if (GUILayout.Button("Add Property"))
            {
                var gm = new GenericMenu();
                gm.AddItem(new GUIContent("Float"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new FloatShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Vector2"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new Vector2ShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Vector3"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new Vector3ShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Vector4"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new Vector4ShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Color"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new ColorShaderProperty());
                    ReaddProps();

                });
                gm.AddItem(new GUIContent("Texture"), false, () =>
                {
                    m_Presenter.graph.AddShaderProperty(new TextureShaderProperty());
                    ReaddProps();

                });
                gm.ShowAsContext();
            }

            EditorGUI.BeginChangeCheck();
            foreach (var property in m_Presenter.graph.properties.ToArray())
            {
                property.name = EditorGUILayout.DelayedTextField("Name", property.name);
                property.description = EditorGUILayout.DelayedTextField("Description", property.description);

                if (property is FloatShaderProperty)
                {
                    var fProp = property as FloatShaderProperty;
                    fProp.value = EditorGUILayout.FloatField("Value", fProp.value);
                }
                else if (property is Vector2ShaderProperty)
                {
                    var fProp = property as Vector2ShaderProperty;
                    fProp.value = EditorGUILayout.Vector2Field("Value", fProp.value);
                }
                else if (property is Vector3ShaderProperty)
                {
                    var fProp = property as Vector3ShaderProperty;
                    fProp.value = EditorGUILayout.Vector3Field("Value", fProp.value);
                }
                else if (property is Vector4ShaderProperty)
                {
                    var fProp = property as Vector4ShaderProperty;
                    fProp.value = EditorGUILayout.Vector4Field("Value", fProp.value);
                }
                else if (property is ColorShaderProperty)
                {
                    var fProp = property as ColorShaderProperty;
                    fProp.value = EditorGUILayout.ColorField("Value", fProp.value);
                }
                else if (property is TextureShaderProperty)
                {
                    var fProp = property as TextureShaderProperty;
                    fProp.value.texture = EditorGUILayout.MiniThumbnailObjectField(new GUIContent("Texture"), fProp.value.texture, typeof(Texture), null) as Texture;
                }

                if (GUILayout.Button("Remove"))
                {
                    m_Presenter.graph.RemoveShaderProperty(property.guid);
                }
                EditorGUILayout.Separator();
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var node in m_Presenter.graph.GetNodes<PropertyNode>())
                    node.onModified(node, ModificationScope.Node);
            }
        }

        public override void OnDataChanged()
        {
            if (presenter == null)
            {
                m_ContentContainer.Clear();
                m_PresenterHash = 0;
                return;
            }

            var presenterHash = 17;
            unchecked
            {
                presenterHash = presenterHash * 31 + (presenter.editor == null ? 79 : presenter.editor.GetHashCode());
                presenterHash = presenterHash * 31 + presenter.selectionCount;
            }

            m_Title.text = presenter.title;

            if (presenterHash != m_PresenterHash)
            {
                m_PresenterHash = presenterHash;
                m_ContentContainer.Clear();
                if (presenter.selectionCount > 1)
                {
                    var element = new VisualElement { name = "selectionCount", text = string.Format("{0} nodes selected.", presenter.selectionCount) };
                    m_ContentContainer.Add(element);
                }
                else if (presenter.editor != null)
                {
                    var view = (AbstractNodeEditorView)Activator.CreateInstance(m_TypeMapper.MapType(presenter.editor.GetType()));
                    view.presenter = presenter.editor;
                    m_ContentContainer.Add(view);
                }
            }

            Dirty(ChangeType.Repaint);
        }

        public GraphInspectorPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;
                RemoveWatch();
                m_Presenter = value;
                OnDataChanged();
                AddWatch();
            }
        }

        protected override Object[] toWatch
        {
            get { return new Object[] { m_Presenter }; }
        }
    }
}
