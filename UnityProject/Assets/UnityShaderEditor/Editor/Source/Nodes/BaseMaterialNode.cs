using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    public enum PropertyType
    {
        Color,
        Texture2D,
        Float,
        Vector2,
        Vector3,
        Vector4
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

    public enum DrawMode
    {
        Full,
        Collapsed
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

        [SerializeField]
        private DrawMode m_DrawMode = DrawMode.Full;

        public delegate void NeedsRepaint();
        public NeedsRepaint onNeedsRepaint;
        public void ExecuteRepaint()
        {
            if (onNeedsRepaint != null)
                onNeedsRepaint();
        } 

        private readonly Dictionary<string, SlotValueType> m_SlotValueTypes = new Dictionary<string, SlotValueType>();
        private readonly Dictionary<string, ConcreteSlotValueType> m_ConcreteInputSlotValueTypes = new Dictionary<string, ConcreteSlotValueType>();
        private readonly Dictionary<string, ConcreteSlotValueType> m_ConcreteOutputSlotValueTypes = new Dictionary<string, ConcreteSlotValueType>();
        #endregion

        #region Properties
        internal PixelGraph pixelGraph { get { return graph as PixelGraph; } }
        public bool generated { get; set; }

        // Nodes that want to have a preview area can override this and return true
        public virtual bool hasPreview { get { return false; } }
        public virtual PreviewMode previewMode { get { return PreviewMode.Preview2D; } }

        public DrawMode drawMode
        {
            get { return m_DrawMode; }
            set { m_DrawMode = value; }
        }

        public bool isSelected { get; set; }

        // lookup custom slot properties
        public void SetSlotDefaultValue(string slotName, SlotValue defaultValue)
        {
            m_SlotDefaultValues.RemoveAll(x => x.slotName == slotName);

            if (defaultValue == null)
                return;

            m_SlotDefaultValues.Add(new SlotDefaultValueKVP(slotName, defaultValue));
        }

        protected void SetSlotDefaultValueType(string slot, SlotValueType slotType)
        {
            if (string.IsNullOrEmpty(slot))
                return;

            m_SlotValueTypes.Add(slot, slotType);
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
                ValidateNode();
                if (m_Material == null)
                {
                    m_Material = new Material(defaultPreviewShader) {hideFlags = HideFlags.DontSave};
                    UpdatePreviewMaterial();
                }
                return m_Material;
            }
        }

        protected PreviewMode m_GeneratedShaderMode = PreviewMode.Preview2D;
        
        protected virtual int previewWidth
        {
            get { return kPreviewWidth; }
        }
        
        protected virtual int previewHeight
        {
            get { return kPreviewHeight; }
        }

        [NonSerialized]
        private bool m_NodeNeedsValidation= true;
        public bool nodeNeedsValidation
        {
            get { return m_NodeNeedsValidation; }
        }

        public void InvalidateNode()
        {
            m_NodeNeedsValidation = true;
        }

        [NonSerialized]
        private bool m_HasError;
        public bool hasError
        {
            get
            {
                if (nodeNeedsValidation)
                    ValidateNode();
                return m_HasError;
            }
            protected set
            {
                if (m_HasError != value)
                {
                    m_HasError = value;
                    ExecuteRepaint();
                }
            }
        }

        public Dictionary<string, ConcreteSlotValueType> concreteInputSlotValueTypes
        {
            get
            {
                if (nodeNeedsValidation)
                    ValidateNode(); 
                return m_ConcreteInputSlotValueTypes;
            }
        }

        public Dictionary<string, ConcreteSlotValueType> concreteOutputSlotValueTypes
        {
            get
            {
                if (nodeNeedsValidation)
                    ValidateNode();
                return m_ConcreteOutputSlotValueTypes;
            }
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
        public List<BaseMaterialNode> CollectChildNodesByExecutionOrder(List<BaseMaterialNode> nodeList, Slot slotToUse = null, bool includeSelf = true)
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

        protected virtual bool UpdatePreviewMaterial()
        {
            if (hasError)
                return false;

            var resultShader = ShaderGenerator.GeneratePreviewShader(this, out m_GeneratedShaderMode);
            return InternalUpdatePreviewShader(resultShader);
        }

        protected bool InternalUpdatePreviewShader(string resultShader)
        {
            MaterialWindow.DebugMaterialGraph("RecreateShaderAndMaterial : " + name + "_" + GetInstanceID() + "\n" + resultShader);
            if (previewMaterial.shader != defaultPreviewShader)
                DestroyImmediate(previewMaterial.shader, true);
            previewMaterial.shader = ShaderUtil.CreateShaderAsset(resultShader);
            previewMaterial.shader.hideFlags = HideFlags.DontSave;

            var hasErrorsCall = typeof(ShaderUtil).GetMethod("GetShaderErrorCount", BindingFlags.Static | BindingFlags.NonPublic);
            var result = hasErrorsCall.Invoke(null, new object[] {previewMaterial.shader});
            return (int)result == 0;
        }

        private static Mesh[] s_Meshes = { null, null, null, null };
        
        /// <summary>
        /// RenderPreview gets called in OnPreviewGUI. Nodes can override
        /// RenderPreview and do their own rendering to the render texture
        /// </summary>
        public Texture RenderPreview(Rect targetSize)
        {
            UpdatePreviewProperties();

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
                case PropertyType.Vector2:
                    mat.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    break;
                case PropertyType.Vector3:
                    mat.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    break;
                case PropertyType.Vector4:
                    mat.SetVector(previewProperty.m_Name, previewProperty.m_Vector4);
                    break;
                case PropertyType.Float:
                    mat.SetFloat(previewProperty.m_Name, previewProperty.m_Float);
                    break;
            }
        }
        
        protected virtual void CollectPreviewMaterialProperties (List<PreviewProperty> properties)
        {
            var validSlots = ListPool<Slot>.Get();
            GetValidInputSlots(validSlots);

            for (int index = 0; index < validSlots.Count; index++)
            {
                var s = validSlots[index];
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
                properties.Add(pp);
            }

            ListPool<Slot>.Release(validSlots);
        }

        public static void UpdateMaterialProperties(BaseMaterialNode target, Material material)
        {
            var childNodes = ListPool<BaseMaterialNode>.Get();
            target.CollectChildNodesByExecutionOrder(childNodes);

            var pList = ListPool<PreviewProperty>.Get();
            for (int index = 0; index < childNodes.Count; index++)
            {
                var node = childNodes[index];
                node.CollectPreviewMaterialProperties(pList);
            }

            foreach (var prop in pList)
                SetPreviewMaterialProperty(prop, material);

            ListPool<BaseMaterialNode>.Release(childNodes);
            ListPool<PreviewProperty>.Release(pList);
        }

        public void UpdatePreviewProperties()
        {
            if (!hasPreview)
                return;

            UpdateMaterialProperties(this, previewMaterial);
        }

        #endregion

        #region Slots

        public virtual void GetValidInputSlots(List<Slot> slotsToFill)
        {
            for (int i = 0; i < slots.Count; ++i)
            {
                var slot = slots[i];
                if (slot != null && slot.isInputSlot)
                    slotsToFill.Add(slot);
            }
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
            
            var slot = mgSlot.slot;

            if (this[slot.name] == null)
            {
                base.AddSlot(slot);
            }

            var slotValue = GetSlotDefaultValue(slot.name);
            if (slotValue == null || !slotValue.IsValid())
                SetSlotDefaultValue(slot.name, new SlotValue(this, slot.name, GetNewSlotDefaultValue(mgSlot.valueType)));

            SetSlotDefaultValueType(slot.name, mgSlot.valueType);
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

        public virtual void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            if (!generationMode.IsPreview())
                return;

            foreach (var inputSlot in inputSlots)
            {
                if (inputSlot.edges.Count > 0)
                    continue;
                
                var defaultForSlot = GetSlotDefaultValue(inputSlot.name);
                if (defaultForSlot != null)
                    defaultForSlot.GeneratePropertyUsages(visitor, generationMode, concreteInputSlotValueTypes[inputSlot.name]);
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
                inputValue = ShaderGenerator.AdaptNodeOutput(inputSlot.edges[0].fromSlot, generationMode, concreteInputSlotValueTypes[inputSlot.name]);
            }
            else
            {
                var defaultValue = GetSlotDefaultValue(inputSlot.name);
                inputValue = defaultValue.GetDefaultValue(generationMode, concreteInputSlotValueTypes[inputSlot.name]);
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
        
        public ConcreteSlotValueType GetConcreteOutputSlotValueType(Slot slot)
        {
            return concreteOutputSlotValueTypes[slot.name];
        }
        public ConcreteSlotValueType GetConcreteInputSlotValueType(Slot slot)
        {
            return concreteInputSlotValueTypes[slot.name];
        }

        private ConcreteSlotValueType FindCommonChannelType(ConcreteSlotValueType @from, ConcreteSlotValueType to)
        {
            if (ImplicitConversionExists(@from, to))
                return to;

            return ConcreteSlotValueType.Error;
        }

        private static ConcreteSlotValueType ToConcreteType(SlotValueType svt)
        {
            switch (svt)
            {
                case SlotValueType.Vector1:
                    return ConcreteSlotValueType.Vector1;
                case SlotValueType.Vector2:
                    return ConcreteSlotValueType.Vector2;
                case SlotValueType.Vector3:
                    return ConcreteSlotValueType.Vector3;
                case SlotValueType.Vector4:
                    return ConcreteSlotValueType.Vector4;
            }
            return ConcreteSlotValueType.Error;
        }

        private static bool ImplicitConversionExists (ConcreteSlotValueType from, ConcreteSlotValueType to)
        {
            return from >= to || from == ConcreteSlotValueType.Vector1;
        }

        protected virtual ConcreteSlotValueType ConvertDynamicInputTypeToConcrete(IEnumerable<ConcreteSlotValueType> inputTypes)
        {
            var concreteSlotValueTypes = inputTypes as IList<ConcreteSlotValueType> ?? inputTypes.ToList();
            if (concreteSlotValueTypes.Any(x => x == ConcreteSlotValueType.Error))
                return ConcreteSlotValueType.Error;

            var inputTypesDistinct = concreteSlotValueTypes.Distinct().ToList();
            switch (inputTypesDistinct.Count)
            {
                case 0:
                    return ConcreteSlotValueType.Vector1;
                case 1:
                    return inputTypesDistinct.FirstOrDefault();
                default:
                    // find the 'minumum' channel width excluding 1 as it can promote
                    inputTypesDistinct.RemoveAll(x => x == ConcreteSlotValueType.Vector1);
                    var ordered = inputTypesDistinct.OrderBy(x => x);
                    if (ordered.Any())
                        return ordered.FirstOrDefault();
                    break;
            }
            return ConcreteSlotValueType.Error;
        }
        
        public void ValidateNode()
        {
            if (!nodeNeedsValidation)
                return;

            bool isInError = false;

            // all children nodes needs to be updated first
            // so do that here
            foreach (var inputSlot in inputSlots)
            {
                foreach (var edge in inputSlot.edges)
                {
                    var outputSlot = edge.fromSlot;
                    var outputNode = (BaseMaterialNode) outputSlot.node;
                    outputNode.ValidateNode();
                    if (outputNode.hasError)
                        isInError |= true;
                }
            }

            m_ConcreteInputSlotValueTypes.Clear(); 
            m_ConcreteOutputSlotValueTypes.Clear();

            var dynamicInputSlotsToCompare = new Dictionary<string, ConcreteSlotValueType>();
            var skippedDynamicSlots = new List<Slot>();
            
            // iterate the input slots
            foreach (var inputSlot in inputSlots)
            {
                var inputType = m_SlotValueTypes[inputSlot.name];
                // if there is a connection
                if (inputSlot.edges.Count == 0)
                {
                    if (inputType != SlotValueType.Dynamic)
                        m_ConcreteInputSlotValueTypes.Add(inputSlot.name, ToConcreteType(inputType));
                    else
                        skippedDynamicSlots.Add(inputSlot);
                    continue;
                }

                // get the output details
                var outputSlot = inputSlot.edges[0].fromSlot;
                var outputNode = (BaseMaterialNode) outputSlot.node;
                var outputConcreteType = outputNode.GetConcreteOutputSlotValueType(outputSlot);

                // if we have a standard connection... just check the types work!
                if (inputType != SlotValueType.Dynamic)
                {
                    var inputConcreteType = ToConcreteType(inputType);
                    m_ConcreteInputSlotValueTypes.Add(inputSlot.name, FindCommonChannelType (outputConcreteType, inputConcreteType));
                    continue;
                }

                // dynamic input... depends on output from other node.
                // we need to compare ALL dynamic inputs to make sure they
                // are compatable.
                dynamicInputSlotsToCompare.Add(inputSlot.name, outputConcreteType);
            }

            // we can now figure out the dynamic type
            // from here set all the 
            var dynamicType = ConvertDynamicInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
            foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                m_ConcreteInputSlotValueTypes.Add(dynamicKvP.Key, dynamicType);
            foreach (var skippedSlot in skippedDynamicSlots)
                m_ConcreteInputSlotValueTypes.Add(skippedSlot.name, dynamicType);

            bool inputError = m_ConcreteInputSlotValueTypes.Any(x => x.Value == ConcreteSlotValueType.Error);

            // configure the output slots now
            // their type will either be the default output type
            // or the above dynanic type for dynamic nodes
            // or error if there is an input erro
            foreach (var outputSlot in outputSlots)
            {
                if (inputError)
                {
                    m_ConcreteOutputSlotValueTypes.Add(outputSlot.name, ConcreteSlotValueType.Error);
                    continue;
                }

                if (m_SlotValueTypes[outputSlot.name] == SlotValueType.Dynamic)
                {
                    m_ConcreteOutputSlotValueTypes.Add(outputSlot.name, dynamicType);
                    continue;
                }

                m_ConcreteOutputSlotValueTypes.Add(outputSlot.name, ToConcreteType(m_SlotValueTypes[outputSlot.name]));
            }
            
            isInError |= inputError;
            isInError |= m_ConcreteOutputSlotValueTypes.Values.Any(x => x == ConcreteSlotValueType.Error);
            isInError |= CalculateNodeHasError();
            m_NodeNeedsValidation = false;
            hasError = isInError;

            if (!hasError)
            {
                bool valid = UpdatePreviewMaterial();
                if (!valid)
                    hasError = true;
            }
        }

        //True if error
        protected virtual bool CalculateNodeHasError()
        {
            return false;
        }

        public static string ConvertConcreteSlotValueTypeToString(ConcreteSlotValueType slotValue)
        {
            switch (slotValue)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Empty;
                case ConcreteSlotValueType.Vector2:
                    return "2";
                case ConcreteSlotValueType.Vector3:
                    return "3";
                case ConcreteSlotValueType.Vector4:
                    return "4";
                default:
                    return "Error";
            }
        }

        public virtual bool OnGUI()
        {
            GUILayout.Label("Slot Defaults", EditorStyles.boldLabel);
            bool modified = false;
            foreach (var slot in slots.Where(x => x.isInputSlot && x.edges.Count == 0))
                modified |= DoSlotUI(this, slot);

            return modified;
        }

        public static bool DoSlotUI(BaseMaterialNode node, Slot slot)
        {
            GUILayout.BeginHorizontal(/*EditorStyles.inspectorBig*/);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Slot " + slot.title, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            return DoMaterialSlotUIBody(node, slot);
        }

        private static bool DoMaterialSlotUIBody(BaseMaterialNode node, Slot slot)
        {
            SlotValue value = node.GetSlotDefaultValue(slot.name);
            if (value == null)
                return false;

            var def = node.GetSlotDefaultValue(slot.name);
            return def.OnGUI();
        }
    }

}
