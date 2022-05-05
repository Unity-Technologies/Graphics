using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class MainPreviewView : VisualElement
    {
        Image m_PreviewTextureImage;

        public Texture mainPreviewTexture
        {
            get => m_PreviewTextureImage.image;
            set
            {
                if(value != null)
                    m_PreviewTextureImage.image = value;
            }
        }

        Vector2 m_PreviewScrollPosition;

        static Type s_ObjectSelector = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypesOrNothing()).FirstOrDefault(t => t.FullName == "UnityEditor.ObjectSelector");

        List<string> m_DoNotShowPrimitives = new (new [] { PrimitiveType.Plane.ToString() });

        Dispatcher m_CommandDispatcher;

        Dictionary<string, Mesh> m_PreviewMeshIndex = new();

        ContextualMenuManipulator m_ContextualMenuManipulator;

        Mesh m_PreviousMesh;

        // TODO: (Sai) Remove from here? And make commands affect this in the asset model?
        // View and model are a bit tightly coupled here
        MainPreviewData m_MainPreviewData;

        public MainPreviewView(Dispatcher dispatcher)
        {
            m_CommandDispatcher = dispatcher;

            // Initialize the preview image
            m_PreviewTextureImage = CreatePreview(Texture2D.redTexture);
            Add(m_PreviewTextureImage);

            // Setup scroll manipulator for zoom in/out
            m_PreviewScrollPosition = new Vector2(0f, 0f);
            this.AddManipulator(new Scrollable(OnScroll));

            BuildPreviewMeshIndex();
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

        public void Initialize(MainPreviewData mainPreviewData)
        {
            m_MainPreviewData = mainPreviewData;
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
            var changePreviewMeshCommand = new ChangePreviewMeshCommand(newPreviewMesh);
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
            m_PreviousMesh = m_MainPreviewData.serializedMesh.mesh;
            showMethod?.Invoke(Get(), new object[] { null, typeof(Mesh), null, false, null, (Action<Object>)OnObjectSelectorClosed, (Action<Object>)OnObjectSelectionUpdated });
        }

        void OnObjectSelectorClosed(object currentMesh)
        {
            var newPreviewMesh = currentMesh as Mesh;
            var changePreviewMeshCommand = new ChangePreviewMeshCommand(newPreviewMesh);
            m_CommandDispatcher.Dispatch(changePreviewMeshCommand);
        }

        void OnObjectSelectionUpdated(object currentMesh)
        {
            var newPreviewMesh = currentMesh as Mesh;
            var changePreviewMeshCommand = new ChangePreviewMeshCommand(newPreviewMesh);
            m_CommandDispatcher.Dispatch(changePreviewMeshCommand);
        }

        void OnMouseDragPreviewMesh(Vector2 obj)
        {
            throw new System.NotImplementedException();
        }

        void OnScroll(float scrollValue)
        {
            throw new System.NotImplementedException();
        }

    }
}
