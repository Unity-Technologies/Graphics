using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMaterialNode : SerializableNode, IGenerateProperties
    {
       private static readonly Mesh[] s_Meshes = {null, null, null, null};
        
        [SerializeField]
        private DrawMode m_DrawMode = DrawMode.Full;

        protected PreviewMode m_GeneratedShaderMode = PreviewMode.Preview2D;

        [NonSerialized]
        private bool m_HasError;

        [SerializeField]
        private string m_LastShader; 

        [NonSerialized]
        private Material m_PreviewMaterial;

        [NonSerialized]
        private Shader m_PreviewShader;

        public AbstractMaterialGraph materialGraphOwner
        {
            get
            {
                return owner as AbstractMaterialGraph;;
            }
        }

        public string precision
        {
            get { return "half"; }
        }

        public string[] m_PrecisionNames = { "half" };
       
        // Nodes that want to have a preview area can override this and return true
        public virtual bool hasPreview
        {
            get { return false; }
        }

        public virtual PreviewMode previewMode
        {
            get { return PreviewMode.Preview2D; }
        }

        public DrawMode drawMode
        {
            get { return m_DrawMode; }
            set { m_DrawMode = value; }
        }

        protected virtual bool generateDefaultInputs
        {
            get { return true; }
        }

        public Material previewMaterial
        {
            get
            {
                if (m_PreviewMaterial == null)
                {
                    m_PreviewMaterial = new Material(m_PreviewShader) {hideFlags = HideFlags.HideInHierarchy};
                    m_PreviewMaterial.hideFlags = HideFlags.HideInHierarchy;
                }
                return m_PreviewMaterial;
            }
        }

        public bool hasError
        {
            get
            {
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

        public IEnumerable<MaterialSlot> materialSlots
        {
            get { return slots.OfType<MaterialSlot>(); }
        }

        public IEnumerable<MaterialSlot> materialInputSlots
        {
            get { return inputSlots.OfType<MaterialSlot>(); }
        }

        public IEnumerable<MaterialSlot> materialOuputSlots
        {
            get { return outputSlots.OfType<MaterialSlot>(); }
        }

        protected AbstractMaterialNode(IGraph theOwner) : base(theOwner)
        { }
        
        public virtual void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {}

        public virtual void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType)
        {
            if (!generateDefaultInputs)
                return;

            if (!generationMode.IsPreview())
                return;

            foreach (var inputSlot in materialInputSlots)
            {
                var edges = owner.GetEdges(GetSlotReference(inputSlot.name));
                if (edges.Any())
                    continue;

                inputSlot.GeneratePropertyUsages(visitor, generationMode, inputSlot.concreteValueType, this);
            }
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
            GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GUI.DrawTexture(rect, preview, ScaleMode.StretchToFill, false);
            GL.sRGBWrite = false;
        }

        protected string GetSlotValue(MaterialSlot inputSlot, GenerationMode generationMode)
        {
            var edges = owner.GetEdges(GetSlotReference(inputSlot.name)).ToArray();

            if (edges.Length > 0)
            {
                var fromSocketRef = edges[0].outputSlot;
                var fromNode = materialGraphOwner.GetMaterialNodeFromGuid(fromSocketRef.nodeGuid);
                if (fromNode == null)
                    return string.Empty;
               
                var slot = fromNode.FindOutputSlot(fromSocketRef.slotName) as MaterialSlot;
                if (slot == null)
                    return string.Empty;

                return ShaderGenerator.AdaptNodeOutput(this, slot, generationMode, inputSlot.concreteValueType);
            }

            return inputSlot.GetDefaultValue(generationMode, inputSlot.concreteValueType, this);
        }

        public MaterialSlot FindMaterialInputSlot(string name)
        {
            var slot = FindInputSlot(name);
            if (slot == null)
                return null;

            if (slot is MaterialSlot)
                return slot as MaterialSlot;

            Debug.LogErrorFormat("Input Slot: {0} exists but is not of type {1}", name, typeof(MaterialSlot));
            return null;
        }

        public MaterialSlot FindMaterialOutputSlot(string name)
        {
            var slot = FindOutputSlot(name);
            if (slot == null)
                return null;

            if (slot is MaterialSlot)
                return slot as MaterialSlot;

            Debug.LogErrorFormat("Output Slot: {0} exists but is not of type {1}", name, typeof(MaterialSlot));
            return null;
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

        private static bool ImplicitConversionExists(ConcreteSlotValueType from, ConcreteSlotValueType to)
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
            var isInError = false;

            // all children nodes needs to be updated first
            // so do that here
            foreach (var inputSlot in inputSlots)
            {
                var edges = owner.GetEdges(GetSlotReference(inputSlot.name));
                foreach (var edge in edges)
                {
                    var fromSocketRef = edge.outputSlot;
                    var outputNode = materialGraphOwner.GetMaterialNodeFromGuid(fromSocketRef.nodeGuid);
                    if (outputNode == null)
                        continue;
                    
                    outputNode.ValidateNode();
                    if (outputNode.hasError)
                        isInError = true;
                }
            }
            
            var dynamicInputSlotsToCompare = new Dictionary<MaterialSlot, ConcreteSlotValueType>();
            var skippedDynamicSlots = new List<MaterialSlot>();

            // iterate the input slots
            foreach (var inputSlot in materialInputSlots)
            {
                var inputType = inputSlot.valueType;
                // if there is a connection
                var edges = owner.GetEdges(GetSlotReference(inputSlot.name)).ToList();
                if (!edges.Any())
                {
                    if (inputType != SlotValueType.Dynamic)
                        inputSlot.concreteValueType = ToConcreteType(inputType);
                    else
                        skippedDynamicSlots.Add(inputSlot);
                    continue;
                }

                // get the output details
                var outputSlotRef = edges[0].outputSlot;
                var outputNode = materialGraphOwner.GetMaterialNodeFromGuid(outputSlotRef.nodeGuid);
                if (outputNode == null)
                    continue;

                var outputSlot = outputNode.FindOutputSlot(outputSlotRef.slotName) as MaterialSlot;
                if (outputSlot == null)
                    continue;
         
                var outputConcreteType = outputSlot.concreteValueType;

                // if we have a standard connection... just check the types work!
                if (inputType != SlotValueType.Dynamic)
                {
                    var inputConcreteType = ToConcreteType(inputType);
                    inputSlot.concreteValueType = FindCommonChannelType(outputConcreteType, inputConcreteType);
                    continue;
                }

                // dynamic input... depends on output from other node.
                // we need to compare ALL dynamic inputs to make sure they
                // are compatable.
                dynamicInputSlotsToCompare.Add(inputSlot, outputConcreteType);
            }

            // we can now figure out the dynamic slotType
            // from here set all the 
            var dynamicType = ConvertDynamicInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
            foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                dynamicKvP.Key.concreteValueType= dynamicType;
            foreach (var skippedSlot in skippedDynamicSlots)
                skippedSlot.concreteValueType = dynamicType;

            var inputError = materialInputSlots.Any(x => x.concreteValueType == ConcreteSlotValueType.Error);

            // configure the output slots now
            // their slotType will either be the default output slotType
            // or the above dynanic slotType for dynamic nodes
            // or error if there is an input error
            foreach (var outputSlot in materialOuputSlots)
            {
                if (inputError)
                {
                    outputSlot.concreteValueType = ConcreteSlotValueType.Error;
                    continue;
                }

                if (outputSlot.valueType == SlotValueType.Dynamic)
                {
                    outputSlot.concreteValueType = dynamicType;
                    continue;
                }
                outputSlot.concreteValueType = ToConcreteType(outputSlot.valueType);
            }

            isInError |= inputError;
            isInError |= materialOuputSlots.Any(x => x.concreteValueType == ConcreteSlotValueType.Error);
            isInError |= CalculateNodeHasError();
            hasError = isInError;

            if (!hasError)
            {
                previewShaderNeedsUpdate = true;
            }
        }

        public bool previewShaderNeedsUpdate { get; set; }

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

        public virtual bool DrawSlotDefaultInput(Rect rect, MaterialSlot inputSlot)
        {
            var inputSlotType = inputSlot.concreteValueType;
            return inputSlot.OnGUI(rect, inputSlotType);
        }

        public void ExecuteRepaint()
        {
            if (onNeedsRepaint != null)
                onNeedsRepaint();
        }
        
        protected virtual bool UpdatePreviewShader()
        {
            if (hasError)
                return false;

            var resultShader = ShaderGenerator.GeneratePreviewShader(this, out m_GeneratedShaderMode);
            return InternalUpdatePreviewShader(resultShader);
        }

        private static bool ShaderHasError(Shader shader)
        {
            var hasErrorsCall = typeof(ShaderUtil).GetMethod("GetShaderErrorCount", BindingFlags.Static | BindingFlags.NonPublic);
            var result = hasErrorsCall.Invoke(null, new object[] {shader});
            return (int) result != 0;
        }

        protected bool InternalUpdatePreviewShader(string resultShader)
        {
            Debug.Log("RecreateShaderAndMaterial : " + name + "_" + guid.ToString().Replace("-","_") + "\n" + resultShader);

            // workaround for some internal shader compiler weirdness
            // if we are in error we sometimes to not properly clean 
            // clean out the error flags and will stay in error, even
            // if we are now valid
            if (m_PreviewShader && ShaderHasError(m_PreviewShader))
            {
                Object.DestroyImmediate(m_PreviewShader, true);
                Object.DestroyImmediate(m_PreviewMaterial, true);
                m_PreviewShader = null;
                m_PreviewMaterial = null;
            }

            if (m_PreviewShader == null)
            {
                m_PreviewShader = ShaderUtil.CreateShaderAsset(resultShader);
                m_PreviewShader.hideFlags = HideFlags.HideInHierarchy;
                m_LastShader = resultShader;
            }
            else
            {
                if (string.CompareOrdinal(resultShader, m_LastShader) != 0)
                {
                    ShaderUtil.UpdateShaderAsset(m_PreviewShader, resultShader);
                    m_LastShader = resultShader;
                }
            }

            return !ShaderHasError(m_PreviewShader);
        }

        /// <summary>
        ///     RenderPreview gets called in OnPreviewGUI. Nodes can override
        ///     RenderPreview and do their own rendering to the render texture
        /// </summary>
        public Texture RenderPreview(Rect targetSize)
        {
            if (hasError)
                return null;

            if (previewShaderNeedsUpdate)
            {
                UpdatePreviewShader();
                previewShaderNeedsUpdate = false;
            }

            UpdatePreviewProperties();

            if (s_Meshes[0] == null)
            {
                var handleGo = (GameObject) EditorGUIUtility.LoadRequired("Previews/PreviewMaterials.fbx");
                // @TODO: temp workaround to make it not render in the scene
                handleGo.SetActive(false);
                foreach (Transform t in handleGo.transform)
                {
                    switch (t.name)
                    {
                        case "sphere":
                            s_Meshes[0] = ((MeshFilter) t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        case "cube":
                            s_Meshes[1] = ((MeshFilter) t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        case "cylinder":
                            s_Meshes[2] = ((MeshFilter) t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        case "torus":
                            s_Meshes[3] = ((MeshFilter) t.GetComponent("MeshFilter")).sharedMesh;
                            break;
                        default:
                            Debug.Log("Something is wrong, weird object found: " + t.name);
                            break;
                    }
                }
            }

            var previewUtil = materialGraphOwner.previewUtility;
            previewUtil.BeginPreview(targetSize, GUIStyle.none);

            if (m_GeneratedShaderMode == PreviewMode.Preview3D)
            {
                previewUtil.m_Camera.transform.position = -Vector3.forward * 5;
                previewUtil.m_Camera.transform.rotation = Quaternion.identity;
                EditorUtility.SetCameraAnimateMaterialsTime(previewUtil.m_Camera, Time.realtimeSinceStartup);
                var amb = new Color(.2f, .2f, .2f, 0);
                previewUtil.m_Light[0].intensity = 1.0f;
                previewUtil.m_Light[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
                previewUtil.m_Light[1].intensity = 1.0f;

                InternalEditorUtility.SetCustomLighting(previewUtil.m_Light, amb);
                previewUtil.DrawMesh(s_Meshes[0], Vector3.zero, Quaternion.Euler(-20, 0, 0) * Quaternion.Euler(0, 0, 0), previewMaterial, 0);
                var oldFog = RenderSettings.fog;
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

        protected virtual void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            var validSlots = materialInputSlots.ToArray();

            for (var index = 0; index < validSlots.Length; index++)
            {
                var s = validSlots[index];
                var edges = owner.GetEdges(GetSlotReference(s.name));
                if (edges.Any())
                    continue;

                var pp = new PreviewProperty
                {
                    m_Name = s.GetInputName(this),
                    m_PropType = PropertyType.Vector4,
                    m_Vector4 = s.currentValue
                };
                properties.Add(pp);
            }
        }

        public static void UpdateMaterialProperties(AbstractMaterialNode target, Material material)
        {
            var childNodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(childNodes, target);

            var pList = ListPool<PreviewProperty>.Get();
            for (var index = 0; index < childNodes.Count; index++)
            {
                var node = childNodes[index] as AbstractMaterialNode;
                if (node == null)
                    continue;

                node.CollectPreviewMaterialProperties(pList);
            }

            foreach (var prop in pList)
                SetPreviewMaterialProperty(prop, material);

            ListPool<INode>.Release(childNodes);
            ListPool<PreviewProperty>.Release(pList);
        }

        public void UpdatePreviewProperties()
        {
            if (!hasPreview)
                return;

            UpdateMaterialProperties(this, previewMaterial);
        }

        public virtual string GetOutputVariableNameForSlot(MaterialSlot s, GenerationMode generationMode)
        {
            if (s.isInputSlot) Debug.LogError("Attempting to use input MaterialSlot (" + s + ") for output!");
            if (!materialSlots.Contains(s)) Debug.LogError("Attempting to use MaterialSlot (" + s + ") for output on a node that does not have this MaterialSlot!");

            return GetOutputVariableNameForNode() + "_" + s.name;
        }

        public virtual string GetOutputVariableNameForNode()
        {
            return name + "_" + guid.ToString().Replace("-", "_");
        }
        
        public sealed override void AddSlot(ISlot slot)
        {
            if (!(slot is MaterialSlot))
            {
                Debug.LogWarningFormat("Trying to add slot {0} to Material node {1}, but it is not a {2}", slot, this, typeof(MaterialSlot));
                return;
            }

            base.AddSlot(slot);

            var addingSlot = (MaterialSlot) slot;
            var foundSlot = (MaterialSlot)slots.FirstOrDefault(x => x.name == slot.name);
            
            // if the default and current are the same, change the current
            // to the new default.
            if (addingSlot.defaultValue == foundSlot.currentValue)
                foundSlot.currentValue = addingSlot.defaultValue;

            foundSlot.defaultValue = addingSlot.defaultValue;
            foundSlot.valueType = addingSlot.valueType;
        }
    }
}
