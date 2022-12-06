using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class ObjectPropertyRM : PropertyRM<UnityObject>
    {
        static readonly Dictionary<Type, TextureDimension> s_TypeToDimensionMap = new()
        {
            { typeof(Texture2D), TextureDimension.Tex2D },
            { typeof(Texture3D), TextureDimension.Tex3D },
            { typeof(Cubemap), TextureDimension.Cube },
        };

        readonly TextField m_TextField;
        readonly Image m_ValueIcon;
        readonly TextureDimension m_textureDimension;

        public ObjectPropertyRM(IPropertyRMProvider controller, float labelWidth) : base(controller, labelWidth)
        {
            styleSheets.Add(VFXView.LoadStyleSheet("ObjectPropertyRM"));

            m_TextField = new TextField { name = "PickLabel", isReadOnly = true };
            var button = new Button { name = "PickButton" };
            var icon = new VisualElement { name = "PickIcon" };
            m_ValueIcon = new Image { name = "ValueIcon" };

            button.clicked += OnPickObject;
            button.Add(icon);
            m_TextField.Add(m_ValueIcon);
            m_TextField.Add(button);
            Add(m_TextField);

            m_TextField.RegisterCallback<ClickEvent>(OnClickToShow);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdate);
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragPerformEvent>(OnDragPerformed);

            // This allow to detect when a resource is deleted or restored
            RegisterCallback<AttachToPanelEvent>((evt) =>
            {
                EditorApplication.projectChanged += OnProjectOrHierarchyChanged;
                EditorApplication.hierarchyChanged += OnProjectOrHierarchyChanged;
            });
            RegisterCallback<DetachFromPanelEvent>((evt) =>
            {
                EditorApplication.projectChanged -= OnProjectOrHierarchyChanged;
                EditorApplication.hierarchyChanged -= OnProjectOrHierarchyChanged;
            });


            if (!s_TypeToDimensionMap.TryGetValue(m_Provider.portType, out m_textureDimension))
            {
                m_textureDimension = TextureDimension.Unknown;
            }
        }

        public override float GetPreferredControlWidth() => 120;

        public override void UpdateGUI(bool force) { }

        public override void SetValue(object obj) // object setvalue should accept null
        {
            try
            {
                m_Value = (UnityObject)obj;
                if (m_Provider.portType == typeof(ShaderGraphVfxAsset))
                {
                    //m_ValueIcon.image = AssetPreview.GetMiniTypeThumbnail(typeof(Shader));
                    m_ValueIcon.image = Resources.Load<Texture2D>("Icons/sg_graph_icon");
                }
                else
                {
                    m_ValueIcon.image = obj != null
                        ? AssetPreview.GetMiniTypeThumbnail(m_Value)
                        : AssetPreview.GetMiniTypeThumbnail(m_Provider.portType);
                }

                CheckForMissingReference();
            }
            catch (Exception)
            {
                Debug.Log($"Error Trying to convert {obj?.GetType().Name ?? "null"} to Object");
            }

            UpdateGUI(!ReferenceEquals(m_Value, obj));
        }

        public override bool showsEverything => true;

        protected override void UpdateEnabled() => SetEnabled(propertyEnabled);

        protected override void UpdateIndeterminate() => visible = !indeterminate;

        void OnPickObject() => CustomObjectPicker.Pick(m_Provider.portType, m_textureDimension, SelectHandler);

        void SelectHandler(UnityObject obj, bool isCanceled)
        {
            if (!isCanceled)
            {
                SetValue(obj);
                NotifyValueChanged();
            }
        }

        void OnClickToShow(ClickEvent evt)
        {
            EditorGUI.PingObjectOrShowPreviewOnClick(m_Value, Rect.zero);
        }

        bool CanDrag()
        {
            if (DragAndDrop.objectReferences.Length == 1)
            {
                var type = DragAndDrop.objectReferences[0].GetType();
                if (m_Provider.portType.IsAssignableFrom(type))
                {
                    return true;
                }

                if (m_textureDimension != TextureDimension.Unknown && DragAndDrop.objectReferences[0] is Texture texture)
                {
                    return texture.dimension == m_textureDimension;
                }
            }

            return false;
        }

        void OnDragEnter(DragEnterEvent evt)
        {
            DragAndDrop.visualMode = CanDrag() ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        void OnDragUpdate(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = CanDrag() ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        void OnDragPerformed(DragPerformEvent evt)
        {
            var dragObject = DragAndDrop.objectReferences.First();
            SelectHandler(dragObject, false);
        }

        void OnProjectOrHierarchyChanged()
        {
            CheckForMissingReference();
        }

        void CheckForMissingReference()
        {
            if (m_Value == null)
            {
                m_TextField.value = !object.ReferenceEquals(m_Value, null) && m_Value.GetInstanceID() != 0
                    ? $"Missing ({m_Provider.portType.Name})"
                    : $"None ({m_Provider.portType.Name})";
            }
            else
            {
                m_TextField.value = m_Value.name;
            }
        }
    }
}
