using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    ///  Visual element used to contain and display the rendering output of the graph as a whole
    /// </summary>
    class MainPreviewView : VisualElement
    {
        Image m_PreviewTextureImage;

        public Texture mainPreviewTexture
        {
            get => m_PreviewTextureImage.image;
            set
            {
                if (value != null)
                {
                    m_PreviewTextureImage.image = value;
                    this.MarkDirtyRepaint();
                }
            }
        }

        Vector2 m_PreviewScrollPosition = new();

        bool m_LockPreviewRotation;

        static Type s_ObjectSelector = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEditor.ObjectSelector");

        List<string> m_DoNotShowPrimitives = new (new [] { PrimitiveType.Plane.ToString() });

        ICommandTarget m_CommandDispatcher;

        Dictionary<string, Mesh> m_PreviewMeshIndex = new();

        ContextualMenuManipulator m_ContextualMenuManipulator;

        IPreviewUpdateListener m_PreviewUpdateListener;

        public MainPreviewView(ICommandTarget dispatcher)
        {
            m_CommandDispatcher = dispatcher;

            // Initialize the preview image
            m_PreviewTextureImage = CreatePreview(Texture2D.redTexture);
            Add(m_PreviewTextureImage);

            // Setup scroll manipulator for zoom in/out
            m_PreviewScrollPosition = new Vector2(0f, 0f);
            this.AddManipulator(new Scrollable(OnScroll));

            BuildPreviewMeshIndex();

            this.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        /// <summary>
        /// This sets the preview update listener this main preview visual element gets its data from
        /// </summary>
        /// <param name="previewUpdateListener"> The view-model for the main preview </param>
        public void SetTargetPreviewUpdateListener(IPreviewUpdateListener previewUpdateListener)
        {
            m_PreviewUpdateListener = previewUpdateListener;
        }

        public void HandlePreviewUpdates()
        {
            mainPreviewTexture = m_PreviewUpdateListener?.PreviewTexture;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            var owningOverlay = this.GetFirstAncestorWithName("unity-overlay");
            if (owningOverlay != null)
            {
                owningOverlay.UnregisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
                owningOverlay.RegisterCallback<GeometryChangedEvent>(OnGeometryChangedEvent);
            }
        }

        void BuildPreviewMeshIndex()
        {
            // Build preview mesh index
            foreach (var primitiveTypeName in Enum.GetNames(typeof(PrimitiveType)))
            {
                if (m_DoNotShowPrimitives.Contains(primitiveTypeName))
                    continue;

                Mesh primitiveMesh = Resources.GetBuiltinResource(typeof(Mesh), $"{primitiveTypeName}.fbx") as Mesh;
                var primitiveMeshKey  = primitiveTypeName == "Quad" ? "Sprite" : primitiveTypeName;
                m_PreviewMeshIndex.Add(primitiveMeshKey, primitiveMesh);
            }
        }

        Image CreatePreview(Texture inputTexture)
        {
            var previewImage = new Image{ name = "previewImage", image = inputTexture, scaleMode = ScaleMode.ScaleAndCrop };
            // Setup manipulator for mesh dragging and panning
            previewImage.AddManipulator(new Draggable(OnMouseDragPreviewMesh, true));
            // Setup context menu to change preview mesh
            m_ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
            previewImage.AddManipulator(m_ContextualMenuManipulator);
            return previewImage;
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            foreach (var meshName in m_PreviewMeshIndex.Keys)
            {
                evt.menu.AppendAction(meshName, e
                    => ChangePreviewMesh(m_PreviewMeshIndex[meshName]), DropdownMenuAction.AlwaysEnabled);
            }

            evt.menu.AppendAction("Custom Mesh", e => ChangeMeshCustom(), DropdownMenuAction.AlwaysEnabled);
        }

        void ChangePreviewMesh(Mesh newPreviewMesh)
        {
            m_LockPreviewRotation = newPreviewMesh.name == "Sprite" ? true : false;
            var changePreviewMeshCommand = new ChangePreviewMeshCommand(newPreviewMesh, m_LockPreviewRotation);
            m_CommandDispatcher.Dispatch(changePreviewMeshCommand);
        }

        static EditorWindow Get()
        {
            PropertyInfo propertyInfo = s_ObjectSelector.GetProperty("get", BindingFlags.Public | BindingFlags.Static);
            return propertyInfo?.GetValue(null, null) as EditorWindow;
        }

        void ChangeMeshCustom()
        {
            MethodInfo showMethod = s_ObjectSelector.GetMethod("Show", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.DefaultBinder, new[] { typeof(Object), typeof(Type), typeof(Object), typeof(bool), typeof(List<int>), typeof(Action<Object>), typeof(Action<Object>) }, new ParameterModifier[7]);
            showMethod?.Invoke(Get(), new object[] { null, typeof(Mesh), null, false, null, (Action<Object>)OnObjectSelectorClosed, (Action<Object>)OnObjectSelectionUpdated });
        }

        void OnObjectSelectorClosed(object currentMesh)
        {
            var newPreviewMesh = currentMesh as Mesh;
            var changePreviewMeshCommand = new ChangePreviewMeshCommand(newPreviewMesh, m_LockPreviewRotation);
            m_CommandDispatcher.Dispatch(changePreviewMeshCommand);
        }

        void OnObjectSelectionUpdated(object currentMesh)
        {
            var newPreviewMesh = currentMesh as Mesh;
            var changePreviewMeshCommand = new ChangePreviewMeshCommand(newPreviewMesh, m_LockPreviewRotation);
            m_CommandDispatcher.Dispatch(changePreviewMeshCommand);
        }

        void OnMouseDragPreviewMesh(Vector2 deltaMouse)
        {
            if (m_LockPreviewRotation) return;

            var previewWidth = this.style.width.value.value;
            var previewHeight = this.style.height.value.value;

            m_PreviewScrollPosition -= deltaMouse * (Event.current.shift ? 3f : 1f) / Mathf.Min(previewWidth, previewHeight) * 140f;
            m_PreviewScrollPosition.y = Mathf.Clamp(m_PreviewScrollPosition.y, -90f, 90f);
            Quaternion previewRotation = Quaternion.Euler(m_PreviewScrollPosition.y, 0, 0) * Quaternion.Euler(0, m_PreviewScrollPosition.x, 0);
            if (float.IsNaN(previewRotation.x) || float.IsNaN(previewRotation.y) || float.IsNaN(previewRotation.z) || float.IsNaN(previewRotation.w))
            {
                m_PreviewScrollPosition = Vector2.zero;
                previewRotation = Quaternion.identity;
            }
            var changePreviewRotationCommand = new ChangePreviewRotationCommand(previewRotation);
            m_CommandDispatcher.Dispatch(changePreviewRotationCommand);
        }

        void OnScroll(float scrollValue)
        {
            float rescaleAmount = -scrollValue * .03f;
            var changePreviewZoomCommand = new ChangePreviewZoomCommand(rescaleAmount);
            m_CommandDispatcher.Dispatch(changePreviewZoomCommand);
        }

        void OnGeometryChangedEvent(GeometryChangedEvent evt)
        {
            // When the overlay containing this view is hidden, our size becomes 0. No need to resize in this case,
            // because the preview won't be shown and can't have a size of 0 regardless.
            if (evt.newRect == Rect.zero ||
                float.IsInfinity(evt.newRect.width) ||
                float.IsInfinity(evt.newRect.height)||
                float.IsNaN(evt.newRect.width)||
                float.IsNaN(evt.newRect.height) )
                return;

            var targetWidth = new Length(evt.newRect.width, LengthUnit.Pixel);
            var targetHeight = new Length(evt.newRect.height, LengthUnit.Pixel);

            style.width = targetWidth;
            style.height = targetHeight;

            // TODO: Should prevent from issuing a bunch of wasteful renders until we reach our final size
            var changePreviewSizeCommand = new ChangePreviewSizeCommand(evt.newRect.size);
            m_CommandDispatcher.Dispatch(changePreviewSizeCommand);
        }
    }
}
