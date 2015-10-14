using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class TitleAttribute : Attribute
    {
        public string m_Title;
        public TitleAttribute(string title) { m_Title = title; }
    }

    public enum Precision
    {
        Default = 0, // half
        Full = 1,
        Fixed = 2,
    }

    public class PreviewProperty
    {
        public string m_Name;
        public PropertyType m_PropType;

        public Color m_Color;
        public Texture2D m_Texture;
        public Vector4 m_Vector4;
    }

    public enum PreviewMode
    {
        Preview2D,
        Preview3D
    }

    public abstract class BaseMaterialNode : Node, IGenerateProperties
    {
        #region Fields
        private const int kPreviewWidth = 64;
        private const int kPreviewHeight = 64;

        [NonSerialized]
        private Material m_Material;

        [SerializeField]
        private List<SlotDefaultValue> m_SlotDefaultValues;
        #endregion

        #region Properties
        internal PixelGraph pixelGraph { get { return graph as PixelGraph; } }
        public bool generated { get; set; }

        // Nodes that want to have a preview area can override this and return true
        public virtual bool hasPreview { get { return false; } }
        public virtual PreviewMode previewMode { get { return PreviewMode.Preview2D; } }

        public bool isSelected { get; set; }

        // lookup custom slot properties
        public void SetSlotDefaultValue(string slotName, SlotDefaultValue defaultValue)
        {
            var existingValue = m_SlotDefaultValues.FirstOrDefault(x => x.slotName == slotName);

            if (existingValue != null)
                m_SlotDefaultValues.Remove(existingValue);

            if (defaultValue == null)
                return;

            m_SlotDefaultValues.Add(defaultValue);
        }

        public string precision
        {
            get { return "half"; }
        }

        public string[] m_PrecisionNames = {"half"};

        public SlotDefaultValue GetSlotDefaultValue(string slotName)
        {
            return m_SlotDefaultValues.FirstOrDefault(x => x.slotName == slotName);
        }

        private static Shader m_DefaultPreviewShader;

        protected static Shader defaultPreviewShader
        {
            get
            {
                if (m_DefaultPreviewShader == null)
                    m_DefaultPreviewShader = Shader.Find("Diffuse");

                return m_DefaultPreviewShader;
            }
        }

        public Material previewMaterial
        {
            get
            {
                if (m_Material == null)
                {
                    m_Material = new Material(defaultPreviewShader) {hideFlags = HideFlags.DontSave};
                    UpdatePreviewMaterial();
                }

                return m_Material;
            }
        }

        protected PreviewMode m_GeneratedShaderMode = PreviewMode.Preview2D;

        private bool needsUpdate
        {
            get { return true; }
        }

        protected virtual int previewWidth
        {
            get { return kPreviewWidth; }
        }
        
        protected virtual int previewHeight
        {
            get { return kPreviewHeight; }
        }

        #endregion

        public virtual void Init()
        {
            hideFlags = HideFlags.HideInHierarchy;
        }

        public virtual void OnEnable()
        {
            if (m_SlotDefaultValues == null)
            {
                m_SlotDefaultValues = new List<SlotDefaultValue>();
            }
        }

        public override void NodeUI(GraphGUI host)
        {
            base.NodeUI(host);
            if (hasPreview)
                OnPreviewGUI();
        }

        protected virtual void OnPreviewGUI()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;

            GUILayout.BeginHorizontal(GUILayout.MinWidth(previewWidth + 10), GUILayout.MinWidth(previewHeight + 10));
            GUILayout.FlexibleSpace();
            var rect = GUILayoutUtility.GetRect(previewWidth, previewHeight, GUILayout.ExpandWidth(false));
            var preview = RenderPreview(rect);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GL.sRGBWrite = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GUI.DrawTexture(rect, preview, ScaleMode.StretchToFill, false);
            GL.sRGBWrite = false;
        }

        #region Nodes
        // CollectDependentNodes looks at the current node and calculates
        // which nodes further up the tree (parents) would be effected if this node was changed
        // it also includes itself in this list
        public IEnumerable<BaseMaterialNode> CollectDependentNodes()
        {
            var nodeList = new List<BaseMaterialNode>();
            NodeUtils.CollectDependentNodes(nodeList, this);
            return nodeList;
        }

        // CollectDependentNodes looks at the current node and calculates
        // which child nodes it depends on for it's calculation.
        // Results are returned depth first so by processing each node in
        // order you can generate a valid code block.
        public IEnumerable<BaseMaterialNode> CollectChildNodesByExecutionOrder(Slot slotToUse = null, bool includeSelf = true)
        {
            var nodeList = new List<BaseMaterialNode>();
            CollectChildNodesByExecutionOrder(nodeList, slotToUse, includeSelf);
            return nodeList;
        }

        public IEnumerable<BaseMaterialNode> CollectChildNodesByExecutionOrder(List<BaseMaterialNode> nodeList, Slot slotToUse = null, bool includeSelf = true)
        {
            if (slotToUse != null && !slots.Contains(slotToUse))
            {
                Debug.LogError("Attempting to collect nodes by execution order with an invalid slot on: " + name);
                return nodeList;
            }

            NodeUtils.CollectChildNodesByExecutionOrder(nodeList, this, slotToUse);

            if (!includeSelf)
                nodeList.Remove(this);

            return nodeList;
        }

        #endregion

        #region Previews
        public virtual bool UpdatePreviewMaterial()
        {
            var resultShader = ShaderGenerator.GeneratePreviewShader(this, out m_GeneratedShaderMode);
            InternalUpdatePreviewShader(resultShader);
            return true;
        }

        protected void InternalUpdatePreviewShader(string resultShader)
        {
            MaterialWindow.DebugMaterialGraph("RecreateShaderAndMaterial : " + name + "_" + GetInstanceID());
            MaterialWindow.DebugMaterialGraph(resultShader);
            if (previewMaterial.shader != defaultPreviewShader)
                DestroyImmediate(previewMaterial.shader, true);
            previewMaterial.shader = ShaderUtil.CreateShaderAsset(resultShader);
            UpdatePreviewProperties();
            previewMaterial.shader.hideFlags = HideFlags.DontSave;
        }

        // this function looks at all the nodes that have a
        // dependency on this node. They will then have their
        // preview regenerated.
        public void RegeneratePreviewShaders()
        {
            CollectDependentNodes()
            .Where(x => x.hasPreview)
            .All(s => s.UpdatePreviewMaterial());
        }

        private static Mesh[] s_Meshes = { null, null, null, null };


        /// <summary>
        /// RenderPreview gets called in OnPreviewGUI. Nodes can override
        /// RenderPreview and do their own rendering to the render texture
        /// </summary>
        public Texture RenderPreview(Rect targetSize)
        {
            if (s_Meshes[0] == null)
            {
                GameObject handleGo = (GameObject)EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
                // @TODO: temp workaround to make it not render in the scene
                handleGo.SetActive(false);
                foreach (Transform t in handleGo.transform)
                {
                    switch (t.name)
                    {
                        case "sphere":
                            s_Meshes[0] = ((MeshFilter)t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        case "cube":
                            s_Meshes[1] = ((MeshFilter)t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        case "cylinder":
                            s_Meshes[2] = ((MeshFilter)t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        case "torus":
                            s_Meshes[3] = ((MeshFilter)t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        default:
                            Debug.Log("Something is wrong, weird object found: " + t.name);
                            break;
                    }
                }
            }

            var bmg = (graph as BaseMaterialGraph);
            if (bmg == null)
                return null;

            var previewUtil = bmg.previewUtility;
            previewUtil.BeginPreview(targetSize, GUIStyle.none);

            if (m_GeneratedShaderMode == PreviewMode.Preview3D)
            {
                previewUtil.m_Camera.transform.position = -Vector3.forward * 5;
                previewUtil.m_Camera.transform.rotation = Quaternion.identity;
                EditorUtility.SetCameraAnimateMaterialsTime(previewUtil.m_Camera, Time.realtimeSinceStartup);
                var amb = new Color(.2f, .2f, .2f, 0);
                previewUtil.m_Light[0].intensity = 1.0f;
                previewUtil.m_Light[0].transform.rotation = Quaternion.Euler (50f, 50f, 0);
                previewUtil.m_Light[1].intensity = 1.0f;

                InternalEditorUtility.SetCustomLighting(previewUtil.m_Light, amb);
                previewUtil.DrawMesh(s_Meshes[0], Vector3.zero, Quaternion.Euler(-20, 0, 0) * Quaternion.Euler(0, 0, 0), previewMaterial, 0);
                bool oldFog = RenderSettings.fog;
                Unsupported.SetRenderSettingsUseFogNoDirty(false);
                previewUtil.m_Camera.Render();
                Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
                InternalEditorUtility.RemoveCustomLighting();
            }
            else
            {
               EditorUtility.UpdateGlobalShaderProperties(Time.realtimeSinceStartup);
               Graphics.Blit(null, previewMaterial);
            }
            return previewUtil.EndPreview();
        }

        private static void SetPreviewMaterialProperty(PreviewProperty previewProperty, Material mat)
        {
            switch (previewProperty.m_PropType)
            {
                case PropertyType.Texture2D:
                    mat.SetTexture(previewProperty.m_Name, previewProperty.m_Texture);
                    break;
                case PropertyType.Color:
                    mat.SetColor(previewProperty.m_Name, previewProperty.m_Color);
                    break;
                case PropertyType.Vector4:
                    mat.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    break;
            }
        }

        public void ForwardPreviewMaterialPropertyUpdate()
        {
            var dependentNodes = CollectDependentNodes();

            foreach (var node in dependentNodes)
            {
                if (node.hasPreview)
                    node.UpdatePreviewProperties();
            }
        }

        protected virtual void CollectPreviewMaterialProperties (List<PreviewProperty> properties)
        {
            foreach (var s in inputSlots)
            {
                if (s.edges.Count > 0)
                    continue;

                var defaultInput = GetSlotDefaultValue(s.name);
                if (defaultInput == null)
                    continue;

                var pp = new PreviewProperty
                {
                    m_Name = defaultInput.inputName,
                    m_PropType = PropertyType.Vector4,
                    m_Vector4 = defaultInput.defaultValue
                };
                properties.Add (pp);
            }
        }

        public void UpdatePreviewProperties()
        {
            var childrenNodes = CollectChildNodesByExecutionOrder();
            var pList = new List<PreviewProperty>();
            foreach (var node in childrenNodes)
                node.CollectPreviewMaterialProperties(pList);

            foreach (var prop in pList)
                SetPreviewMaterialProperty (prop, previewMaterial);
        }

        #endregion

        #region Slots

        public virtual IEnumerable<Slot> GetValidInputSlots()
        {
            return inputSlots;
        }

        public virtual string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            if (s.isInputSlot) Debug.LogError("Attempting to use input slot (" + s + ") for output!");
            if (!slots.Contains(s)) Debug.LogError("Attempting to use slot (" + s + ") for output on a node that does not have this slot!");

            return GetOutputVariableNameForNode() + "_" + s.name;
        }

        public virtual string GetOutputVariableNameForNode()
        {
            return name + "_" + Math.Abs(GetInstanceID());
        }

        public virtual Vector4 GetNewSlotDefaultValue()
        {
            return Vector4.one;
        }

        public new void AddSlot(Slot slot)
        {
            AddSlot(slot, GetNewSlotDefaultValue());
        }

        public void AddSlot(Slot slot, Vector4 defaultValue)
        {
            if (this[slot.name] != null)
                return;

            base.AddSlot(slot);

            // slots are not serialzied but the default values are
            // because of this we need to see if the default has
            // already been set
            // if it has... do nothing.
            MaterialWindow.DebugMaterialGraph("Node ID: " + GetInstanceID());
            MaterialWindow.DebugMaterialGraph("Node Name: " + GetOutputVariableNameForNode());
            Debug.Log("Slot name" + (slot != null ? slot.name : "Null"));

            SetSlotDefaultValue(slot.name, new SlotDefaultValue(defaultValue, this, slot.name, true));

            var slotthing = GetSlotDefaultValue(slot.name);
            MaterialWindow.DebugMaterialGraph("Slot Thing: " + slotthing.inputName);
        }

        public override void RemoveSlot(Slot slot)
        {
            SetSlotDefaultValue(slot.name, null);
            base.RemoveSlot(slot);
        }

        public string GenerateSlotName(SlotType type)
        {
            var slotsToCheck = type == SlotType.InputSlot ? inputSlots.ToArray() : outputSlots.ToArray();
            string format = type == SlotType.InputSlot ? "I{0:00}" : "O{0:00}";
            int index = slotsToCheck.Length;
            var name = string.Format(format, index);
            if (slotsToCheck.All(x => x.name != name))
                return name;
            index = 0;
            do
            {
                name = string.Format(format, index++);
            }
            while (slotsToCheck.Any(x => x.name == name));

            return name;
        }

        #endregion

        public virtual void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            foreach (var inputSlot in inputSlots)
            {
                if (inputSlot.edges.Count > 0)
                    continue;

                var defaultForSlot = GetSlotDefaultValue(inputSlot.name);
                defaultForSlot.GeneratePropertyBlock(visitor, generationMode);
            }
        }

        public virtual void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            foreach (var inputSlot in inputSlots)
            {
                if (inputSlot.edges.Count > 0)
                    continue;

                var defaultForSlot = GetSlotDefaultValue(inputSlot.name);
                defaultForSlot.GeneratePropertyUsages(visitor, generationMode);
            }
        }

        public Slot FindInputSlot(string name)
        {
            var slot = inputSlots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogError("Input slot: " + name + " could be found on node " + GetOutputVariableNameForNode());
            return slot;
        }

        public Slot FindOutputSlot(string name)
        {
            var slot = outputSlots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogError("Output slot: " + name + " could be found on node " + GetOutputVariableNameForNode());
            return slot;
        }

        protected string GetSlotValue(Slot inputSlot, GenerationMode generationMode)
        {
            bool pointInputConnected = inputSlot.edges.Count > 0;
            string inputValue;
            if (pointInputConnected)
            {
                var dataProvider = inputSlot.edges[0].fromSlot.node as BaseMaterialNode;
                inputValue = dataProvider.GetOutputVariableNameForSlot(inputSlot.edges[0].fromSlot, generationMode);
            }
            else
            {
                var defaultValue = GetSlotDefaultValue(inputSlot.name);
                inputValue = defaultValue.GetDefaultValue(generationMode);
            }
            return inputValue;
        }
    }
}
