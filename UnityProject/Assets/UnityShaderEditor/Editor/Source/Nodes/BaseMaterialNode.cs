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
        public float m_Float;
    }

    public enum PreviewMode
    {
        Preview2D,
        Preview3D
    }

    [Serializable]
    class SlotDefaultValueKVP
    {
        [SerializeField]
        public string slotName;
        [SerializeField]
        public SlotValue value;

        public SlotDefaultValueKVP(string slotName, SlotValue value)
        {
            this.slotName = slotName;
            this.value = value;
        }
    } 
    
    public struct MaterialGraphSlot
    {
        public Slot slot;
        public SlotValueType valueType;

        public MaterialGraphSlot(Slot slot, SlotValueType valueType)
        {
            this.slot = slot;
            this.valueType = valueType;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} - {2}", slot.name, slot.isInputSlot, valueType);
        }
    }

    public abstract class BaseMaterialNode : Node, IGenerateProperties
    {
        #region Fields
        private const int kPreviewWidth = 64;
        private const int kPreviewHeight = 64;

        [NonSerialized]
        private Material m_Material;

        [SerializeField]
        private List<SlotDefaultValueKVP> m_SlotDefaultValues;
        #endregion

        #region Properties
        internal PixelGraph pixelGraph { get { return graph as PixelGraph; } }
        public bool generated { get; set; }

        // Nodes that want to have a preview area can override this and return true
        public virtual bool hasPreview { get { return false; } }
        public virtual PreviewMode previewMode { get { return PreviewMode.Preview2D; } }

        public bool isSelected { get; set; }

        // lookup custom slot properties
        public void SetSlotDefaultValue(string slotName, SlotValue defaultValue)
        {
            m_SlotDefaultValues.RemoveAll(x => x.slotName == slotName);

            if (defaultValue == null)
                return;

            Debug.LogFormat("Configuring Default: {0} on {1}.{2}", defaultValue, this, slotName);
            m_SlotDefaultValues.Add(new SlotDefaultValueKVP(slotName, defaultValue));
        }
        
        public string precision
        {
            get { return "half"; }
        }

        public string[] m_PrecisionNames = {"half"};

        public SlotValue GetSlotDefaultValue(string slotName)
        {
            var found = m_SlotDefaultValues.FirstOrDefault(x => x.slotName == slotName);
            return found != null ? found.value : null;
        }

        private static Shader s_DefaultPreviewShader;

        protected static Shader defaultPreviewShader
        {
            get
            {
                if (s_DefaultPreviewShader == null)
                    s_DefaultPreviewShader = Shader.Find("Diffuse");

                return s_DefaultPreviewShader;
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

        public virtual void OnCreate()
        {
            hideFlags = HideFlags.HideInHierarchy;
        }

        public virtual void OnEnable()
        {
            if (m_SlotDefaultValues == null)
            {
                m_SlotDefaultValues = new List<SlotDefaultValueKVP>();
            }
        }

        public virtual float GetNodeUIHeight(float width)
        {
            return 0;
        }

        public virtual bool NodeUI(Rect drawArea)
        {
            return false;
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
                case PropertyType.Float:
                    mat.SetFloat(previewProperty.m_Name, previewProperty.m_Float);
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
        public virtual Vector4 GetNewSlotDefaultValue(SlotValueType type)
        {
            return Vector4.one;
        }

        [Obsolete ("This call is not supported for Material Graph. Use: AddSlot(MaterialGraphSlot mgSlot)", true)]
        public new void AddSlot(Slot slot)
        {
            throw new NotSupportedException("Material graph requires the use of: AddSlot(MaterialGraphSlot mgSlot)");
        }
        
        public void AddSlot(MaterialGraphSlot mgSlot)
        {
            if (mgSlot.slot == null)
                return;

            Debug.Log(mgSlot);

            var slot = mgSlot.slot;

            if (this[slot.name] == null)
            {
                base.AddSlot(slot);
            }

            var slotValue = GetSlotDefaultValue(slot.name);
            if (slotValue == null || !slotValue.IsValid())
                SetSlotDefaultValue(slot.name, new SlotValue(this, slot.name, GetNewSlotDefaultValue(mgSlot.valueType)));

            // slots are not serialzied but the default values are
            // because of this we need to see if the default has
            // already been set
            // if it has... do nothing.
            MaterialWindow.DebugMaterialGraph("Node ID: " + GetInstanceID());
            MaterialWindow.DebugMaterialGraph("Node Name: " + GetOutputVariableNameForNode());

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
            var slotName = string.Format(format, index);
            if (slotsToCheck.All(x => x.name != slotName))
                return slotName;
            index = 0;
            do
            {
                slotName = string.Format(format, index++);
            }
            while (slotsToCheck.Any(x => x.name == slotName));

            return slotName;
        }

        #endregion

        public virtual void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {}

        public virtual void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            foreach (var inputSlot in inputSlots)
            {
                if (inputSlot.edges.Count > 0)
                    continue;

                Debug.LogFormat("On {0} and trying to genereate for {1}", this, inputSlot);

                var defaultForSlot = GetSlotDefaultValue(inputSlot.name);
                if (defaultForSlot != null)
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
                Debug.LogFormat("Searching for {0} on {1}", inputSlot.name, this);
                inputValue = defaultValue.GetDefaultValue(generationMode);
            }
            return inputValue;
        }

        protected void RemoveSlotsNameNotMatching(string[] slotNames)
        {
            var invalidSlots = slots.Select(x => x.name).Except(slotNames);

            foreach (var invalidSlot in invalidSlots.ToList())
            {
                Debug.LogWarningFormat("Removing Invalid Slot: {0}", invalidSlot);
                RemoveSlot(this[invalidSlot]);
            }

            var invalidSlotDefaults = m_SlotDefaultValues.Select(x => x.slotName).Except(slotNames);
            foreach (var invalidSlot in invalidSlotDefaults.ToList())
            {
                Debug.LogWarningFormat("Removing Invalid Slot Default: {0}", invalidSlot);
                m_SlotDefaultValues.RemoveAll(x => x.slotName == invalidSlot);
            }
        }
    }
}
