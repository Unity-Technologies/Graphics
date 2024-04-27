using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    [CustomEditor(typeof(CapsuleShadows))]
    public class CapsuleShadowsInspector : Editor
    {
        public VisualTreeAsset m_InspectorUXML;
        public VisualTreeAsset m_EntryUXML;

        private VisualElement inspector;
        private CapsuleShadows targetScript;
        private Label statsLabel;
        private Button generateButton;
        private Button clearAllButton;
        private VisualElement helpboxElement;
        private VisualElement occluderWarningElement;
        private Foldout generationFoldout;
        private Label capsulesLabel;
        private TreeView capsulesTreeview;
        private CapsuleModel currentCapsule;

        private Tool previousTool;

        private List<CapsuleModel> generatedCapsuleModels = new();
        private Dictionary<SkinnedMeshRenderer, List<CapsuleModel>> renderersToCapsules = new();

        //SceneUI Panel measurements
        private const float PANEL_WIDTH = 240.0f;
        private const float PANEL_HEIGHT = 160.0f;
        private const float PANEL_PADDING = 30.0f;
        private const float LISTVIEW_MAXHEIGHT = 300.0f;

        private readonly Color selectedColor = Color.red;
        private readonly Color defaultColor = Color.yellow;

        public override VisualElement CreateInspectorGUI()
        {
            targetScript = target as CapsuleShadows;
            inspector = new ();
            m_InspectorUXML.CloneTree(inspector);

            generateButton = inspector.Q<Button>("generateBT");
            clearAllButton = inspector.Q<Button>("clearAllBT");
            helpboxElement = inspector.Q<VisualElement>("helpboxVE");
            generationFoldout = inspector.Q<Foldout>("generationFO");
            capsulesLabel = inspector.Q<Label>("capsulesLB");
            capsulesTreeview = inspector.Q<TreeView>("capsulesTV");
            occluderWarningElement = inspector.Q<VisualElement>("occluderWarningbox");

            ObjectChangeEvents.changesPublished -= OnObjectChanged;
            ObjectChangeEvents.changesPublished += OnObjectChanged;
            generateButton.clicked -= GenerateCapsules;
            generateButton.clicked += GenerateCapsules;
            clearAllButton.clicked -= ClearAllCapsules;
            clearAllButton.clicked += ClearAllCapsules;

            InitializeTreeView();
            Refresh();
            SubscribeToEvents();

            previousTool = Tools.current;
            Tools.current = Tool.None;

            return inspector;
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            Tools.current = previousTool;
        }
        private void SubscribeToEvents()
        {
            SceneView.duringSceneGui += OnDuringSceneGui;
            Undo.undoRedoPerformed += OnUndoRedo;

        }

        private void UnsubscribeFromEvents()
        {
            SceneView.duringSceneGui -= OnDuringSceneGui;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnObjectChanged(ref ObjectChangeEventStream stream)
        {
            if (target == null)
                return;

            if (targetScript.enabled == false)
            {
                OnDisable();
                EditorApplication.QueuePlayerLoopUpdate();
            }

            Refresh();
        }

        private void OnUndoRedo()
        {
            targetScript.RefreshOccluders();
            Refresh();
            currentCapsule = null;
        }

        private void OnListSelectionChanged(IEnumerable<object> selectedObject)
        {
            currentCapsule = capsulesTreeview.GetItemDataForIndex<CapsuleModel>(capsulesTreeview.selectedIndex);
            SceneView.RepaintAll();
        }

        private void InitializeTreeView()
        {
            if (capsulesTreeview == null)
            {
                Debug.LogWarning("CapsulesTreeViewNull");
                return;
            }

            // Set TreeView.makeItem to initialize each node in the tree.
            capsulesTreeview.makeItem = () =>
            {
                VisualElement capsuleEntry = new ();
                m_EntryUXML.CloneTree(capsuleEntry);

                var entryLogic = new CapsuleShadowEntry();
                capsuleEntry.userData = entryLogic;
                entryLogic.SetVisualElement(capsuleEntry);
                return capsuleEntry;

            };

            // Set TreeView.bindItem to bind an initialized node to a data item.
            capsulesTreeview.bindItem = (VisualElement element, int index) =>
                {
                    (element.userData as CapsuleShadowEntry)?.SetData(capsulesTreeview.GetItemDataForIndex<CapsuleModel>(index),this);
                };

            capsulesTreeview.unbindItem = (element, index) =>
            {
                (element.userData as CapsuleShadowEntry)?.Dispose();
            };

            capsulesTreeview.fixedItemHeight = 40f;
            capsulesTreeview.Rebuild();

            capsulesTreeview.selectionChanged -= OnListSelectionChanged;
            capsulesTreeview.selectionChanged += OnListSelectionChanged;
        }

        List<TreeViewItemData<CapsuleModel>> GetTreeViewData
        {
            get
            {
                List<TreeViewItemData<CapsuleModel>> treeList = new List<TreeViewItemData<CapsuleModel>>();
                int addedLists = 0;
                foreach (var rendererCapsules in renderersToCapsules)
                {
                    AddNode(treeList, rendererCapsules.Value[0], 0 + addedLists);
                    addedLists = rendererCapsules.Key.bones.Length + 1;
                }
                return treeList;
            }
        }

        int AddNode(List<TreeViewItemData<CapsuleModel>> treeList, CapsuleModel node, int id)
        {
            List<TreeViewItemData<CapsuleModel>> children = null;
            int childID = id;
            if (node.m_SubItems != null && node.m_SubItems.Count > 0)
            {
                children = new List<TreeViewItemData<CapsuleModel>>();

                foreach (var child in node.m_SubItems)
                {
                    childID = AddNode(children, child, childID + 1);
                }
            }

            TreeViewItemData<CapsuleModel> itemData = new TreeViewItemData<CapsuleModel>(id, node, children);
            treeList.Add(itemData);
            return childID;
        }

        private void OnDuringSceneGui(SceneView view)
        {
            if (targetScript == null || targetScript.enabled == false)
            {
                return;
            }

            if(currentCapsule == null)
            {
                return;
            }

            //Unselect tools if we have a capsule selected
            Tools.current = Tool.None;

            CapsuleOccluder currentOccluder = currentCapsule.m_Occluder;
            if(currentOccluder == null || currentOccluder.enabled == false)
            {
                return;
            }

            Handles.matrix = currentOccluder.transform.localToWorldMatrix;
            Handles.zTest = CompareFunction.Always;

            Undo.RecordObject(currentOccluder, "transform changed");

            Vector3 scale = new Vector3(currentOccluder.m_Radius, currentOccluder.m_Radius, currentOccluder.m_Height);
            Handles.TransformHandle(ref currentOccluder.m_Center, ref currentOccluder.m_Rotation, ref scale);
            currentOccluder.m_Radius = Mathf.Abs(currentOccluder.m_Radius - scale.x) > Mathf.Abs(currentOccluder.m_Radius - scale.y) ? scale.x : scale.y;
            currentOccluder.m_Radius = currentOccluder.m_Radius < 0.0f ? 0.0f : currentOccluder.m_Radius;
            currentOccluder.m_Height = scale.z < 0.0f ? 0.0f : scale.z;
            Handles.BeginGUI();
            {
                var x = view.position.width - (PANEL_WIDTH + PANEL_PADDING);
                var y = view.position.height - (PANEL_HEIGHT + (PANEL_PADDING * 2.0f));

                GUILayout.BeginArea(new Rect(x,y, PANEL_WIDTH, PANEL_HEIGHT),EditorStyles.textArea);
                {
                    GUILayout.Label(currentOccluder.gameObject.name, EditorStyles.boldLabel);
                    currentOccluder.m_Center = EditorGUILayout.Vector3Field("Position", currentOccluder.m_Center);
                    var eulerAngles = EditorGUILayout.Vector3Field("Rotation", currentOccluder.m_Rotation.eulerAngles);
                    currentOccluder.m_Rotation = Quaternion.Euler(eulerAngles);

                    currentOccluder.m_Radius = EditorGUILayout.FloatField("Radius", currentOccluder.m_Radius);
                    currentOccluder.m_Height = EditorGUILayout.FloatField("Height", currentOccluder.m_Height);
                    if (currentOccluder.IsChanged())
                    {
                        if (GUILayout.Button("Reset Capsule"))
                        {
                            ResetCapsule(currentOccluder);
                        }
                    }
                }
                GUILayout.EndArea();
            }
            Handles.EndGUI();
        }

        private void OnSceneGUI()
        {
            if (targetScript == null || targetScript.enabled == false)
            {
                return;
            }

            // If there are no capsule occluders we can return because we dont need to draw anything in the scene view in this case
            if (targetScript.CapsuleOccluders == null || targetScript.CapsuleOccluders.Count == 0)
            {
                return;
            }

            //Otherwise loop over every capsule occluder component on the target and draw a capsule into the scene view to visualize it
            for (int index = 0; index < targetScript.CapsuleOccluders.Count; index++)
            {
                var capsule = targetScript.CapsuleOccluders[index];
                if (CapsuleOccluderManager.instance.IsOccluderIgnored(capsule))
                {
                    CapsuleShadowsUtils.DrawWireCapsule(capsule,Color.gray);
                }
                else
                {
                    CapsuleShadowsUtils.DrawWireCapsule(capsule, currentCapsule == null || capsule != currentCapsule.m_Occluder ? defaultColor : selectedColor);
                }
            }

            Event e = Event.current;
            // Add default control so that clicking in scene view does not deselect object
            if (e.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(0);
            }

            if (EditorWindow.mouseOverWindow is SceneView)
            {
                if (e.type == EventType.MouseUp && e.button == 0 && GUIUtility.hotControl == 0)
                {
                    SelectClosestCapsule(e.mousePosition);
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }

        void SelectClosestCapsule(Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);

            float closestCapsuleCentreDst = float.MaxValue;
            int closestCapsuleIndex = 0;
            CapsuleModel closestModel = currentCapsule;

            for (var i = 0; i < targetScript.CapsuleOccluders.Count; i++)
            {
                var capsule = targetScript.CapsuleOccluders[i];
                var ballpointA = capsule.CapsuleToWorld.MultiplyPoint3x4(Vector3.forward * (capsule.m_Height / 2.0f - capsule.m_Radius));
                var ballpointB = capsule.CapsuleToWorld.MultiplyPoint3x4(-Vector3.forward * (capsule.m_Height / 2.0f - capsule.m_Radius));

                bool intersects = CapsuleShadowsUtils.RayIntersectsCapsule(ray.origin, ray.direction, ballpointA, ballpointB,
                    capsule.m_Radius);

                if (intersects)
                {
                    var capsuleCentreDst = ((ballpointA + ballpointB) / 2.0f - ray.origin).sqrMagnitude;
                    if (capsuleCentreDst < closestCapsuleCentreDst)
                    {
                        closestCapsuleCentreDst = capsuleCentreDst;
                        closestCapsuleIndex = i;
                        closestModel = capsule.Model;
                    }
                }
            }
            if (currentCapsule == null || currentCapsule != closestModel)
            {
                currentCapsule = closestModel;
            }
        }

        // Refreshes the inspector UI for the CapsuleShadows component. Dis- and Enables the generation and clear buttons based on
        // whether or not the target is valid (has at least one SkinnedMeshRenderer component attached to itself or its children).
        private void Refresh()
        {
            inspector.SetEnabled(targetScript.enabled);
            generateButton.SetEnabled(targetScript.IsValid);
            generationFoldout.SetEnabled(targetScript.IsValid);
            clearAllButton.SetEnabled(targetScript.CapsuleOccluders != null &&
                                      targetScript.CapsuleOccluders.Count != 0);

            helpboxElement.style.display = targetScript.IsValid ? DisplayStyle.None : DisplayStyle.Flex;
            occluderWarningElement.style.display =
                CapsuleOccluderManager.instance.ContainsIgnoredOccluders(targetScript.CapsuleOccluders)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            capsulesLabel.text = $"Capsules ({targetScript.CapsuleOccluders?.Count ?? 0})";
            RefreshTreeView();
        }

        // Refreshes the list of Capsules that can be found for this GameObject.That means any CapsuleOccluder components that can be found on the object or its children
        private void RefreshTreeView()
        {
            targetScript.RefreshOccluders();
            if (targetScript.MeshRenderers == null || targetScript.MeshRenderers.Length == 0)
            {
                return;
            }

            renderersToCapsules = new();
            foreach (var meshRenderer in targetScript.MeshRenderers)
            {
                renderersToCapsules.Add(meshRenderer,CapsuleShadowsUtils.GetAllModels(meshRenderer));
            }
            capsulesTreeview.SetRootItems(GetTreeViewData);
            capsulesTreeview.Rebuild();
        }

        // Creates capsule components for every bone of every SkinnedMeshRenderer that can be found on the target. Automatically turns of
        // shadow casting for the the MeshRenderer.
        private void GenerateCapsules()
        {
            foreach (var meshRenderer in targetScript.MeshRenderers)
            {
                CapsuleShadowsUtils.GenerateCapsules(targetScript,meshRenderer);
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            targetScript.RefreshOccluders();
            Refresh();
        }

        // Deletes a single selected capsule component.
        public void DeleteCapsule(CapsuleOccluder capsule)
        {
            // Should never happen -- defensive coding!
            if (!targetScript.CapsuleOccluders.Contains(capsule))
            {
                Refresh();
                Debug.LogWarning("Can't find capsule you are tying to delete! Capsules have been refreshed - try again!");
                return;
            }

            Undo.DestroyObjectImmediate(capsule);
            targetScript.CapsuleOccluders.Remove(capsule);
            targetScript.RefreshOccluders();
            Refresh();
        }

        public void AddCapsule(CapsuleModel model)
        {
            CapsuleOccluder capsule = model.m_BoneTransform.gameObject.GetComponent<CapsuleOccluder>();
            if (capsule == null)
            {
                capsule = Undo.AddComponent<CapsuleOccluder>(model.m_BoneTransform.gameObject);
                capsule.SetModel(model);
                CapsuleShadowsUtils.GenerateSingleCapsule(targetScript,model.m_SkinnedMeshRenderer,capsule);
            }
            targetScript.RefreshOccluders();
            Refresh();
        }

        public void ResetCapsule(CapsuleOccluder capsule)
        {
            Undo.RecordObject(capsule, "reset capsule");
            capsule.ResetParams();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        // Deletes all capsule components from the gameObject and its children
        private void ClearAllCapsules()
        {
            if (targetScript.CapsuleOccluders == null || targetScript.CapsuleOccluders.Count == 0)
            {
                return;
            }

            for (int i = 0; i < targetScript.CapsuleOccluders.Count; i++)
            {
                var capsuleOccluder = targetScript.CapsuleOccluders[i];
                Undo.DestroyObjectImmediate(capsuleOccluder);
            }

            targetScript.CapsuleOccluders.Clear();
            targetScript.RefreshOccluders();
            Refresh();
        }
    }
}
