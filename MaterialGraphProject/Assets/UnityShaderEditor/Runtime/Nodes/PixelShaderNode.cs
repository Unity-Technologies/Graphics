using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    [Title("Pixel Shader Node")]
    public class PixelShaderNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        [SerializeField]
        public string m_LightFunctionClassName;
         
        private string lightFunctionClassName
        {
            get { return m_LightFunctionClassName; }
            set { m_LightFunctionClassName = value; }
        }

        private static List<BaseLightFunction> s_LightFunctions;

        public PixelShaderNode()
        {
            name = "PixelMaster";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            GetLightFunction().DoSlotsForConfiguration(this);
        }

        protected override bool generateDefaultInputs { get { return false; } }

       // public override bool canDeleteNode { get { return false; } }

        public static List<BaseLightFunction> GetLightFunctions()
        {
            if (s_LightFunctions == null)
            {
                s_LightFunctions = new List<BaseLightFunction>();

                foreach (Type type in Assembly.GetAssembly(typeof(BaseLightFunction)).GetTypes())
                {
                    if (type.IsClass && !type.IsAbstract && (type.IsSubclassOf(typeof(BaseLightFunction))))
                    {
                        var func = Activator.CreateInstance(type) as BaseLightFunction;
                        s_LightFunctions.Add(func);
                    }
                }
            }
            return s_LightFunctions;
        }

        public BaseLightFunction GetLightFunction()
        {
            var lightFunctions = GetLightFunctions();
            var lightFunction = lightFunctions.FirstOrDefault(x => x.GetType().ToString() == lightFunctionClassName);

            if (lightFunction == null && lightFunctions.Count > 0)
                lightFunction = lightFunctions[0];

            return lightFunction;
        }

        public virtual void GenerateLightFunction(ShaderGenerator visitor)
        {
            var lightFunction = GetLightFunction();
            lightFunction.GenerateLightFunctionName(visitor);
            lightFunction.GenerateLightFunctionBody(visitor);
        }

        public void GenerateSurfaceOutput(ShaderGenerator visitor)
        {
            var lightFunction = GetLightFunction();
            lightFunction.GenerateSurfaceOutputStructureName(visitor);
        }
        
        public void GenerateNodeCode(ShaderGenerator shaderBody, GenerationMode generationMode)
        {
            var lightFunction = GetLightFunction();
            var firstPassSlotId = lightFunction.GetFirstPassSlotId();
            // do the normal slot first so that it can be used later in the shader :)
            var firstPassSlot = FindInputSlot<MaterialSlot>(firstPassSlotId);
            var nodes = ListPool<INode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, firstPassSlotId, NodeUtils.IncludeSelf.Exclude);

            for (int index = 0; index < nodes.Count; index++)
            {
                var node = nodes[index];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

            foreach (var edge in owner.GetEdges(firstPassSlot.slotReference))
            {
                var outputRef = edge.outputSlot;
                var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                if (fromNode == null)
                    continue;

                shaderBody.AddShaderChunk("o." + firstPassSlot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
            }

            // track the last index of nodes... they have already been processed :)
            int pass2StartIndex = nodes.Count;

            //Get the rest of the nodes for all the other slots
            NodeUtils.DepthFirstCollectNodesFromNode(nodes, this, null, NodeUtils.IncludeSelf.Exclude);
            for (var i = pass2StartIndex; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, generationMode);
            }

           ListPool<INode>.Release(nodes);

            foreach (var slot in GetInputSlots<MaterialSlot>())
            {
                if (slot == firstPassSlot)
                    continue;
                
                foreach (var edge in owner.GetEdges(slot.slotReference))
                {
                    var outputRef = edge.outputSlot;
                    var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(outputRef.nodeGuid);
                    if (fromNode == null)
                        continue;
                    
                    shaderBody.AddShaderChunk("o." + slot.shaderOutputName + " = " + fromNode.GetVariableNameForSlot(outputRef.slotId) + ";", true);
                }
            }
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return GetVariableNameForNode();
        }

   /*     public override float GetNodeUIHeight(float width)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override GUIModificationType NodeUI(Rect drawArea)
        {
            var lightFunctions = GetLightFunctions();
            var lightFunction = GetLightFunction();

            int lightFuncIndex = 0;
            if (lightFunction != null)
                lightFuncIndex = lightFunctions.IndexOf(lightFunction);

            EditorGUI.BeginChangeCheck();
            lightFuncIndex = EditorGUI.Popup(new Rect(drawArea.x, drawArea.y, drawArea.width, EditorGUIUtility.singleLineHeight), lightFuncIndex, lightFunctions.Select(x => x.GetLightFunctionName()).ToArray(), EditorStyles.popup);
            lightFunctionClassName = lightFunctions[lightFuncIndex].GetType().ToString();
            if (EditorGUI.EndChangeCheck())
            {
                var function = GetLightFunction();
                function.DoSlotsForConfiguration(this);
                owner.ValidateGraph();
                return GUIModificationType.ModelChanged;
            }
            return GUIModificationType.None;
        }*/

        public override IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return new List<ISlot>();
        }

        public override bool hasPreview
        {
            get { return true; }
        }


        /*
        protected override bool UpdatePreviewShader()
        {
            if (hasError)
                return false;

            var shaderName = "Hidden/PreviewShader/" + name + "_" + guid.ToString().Replace("-","_");
            List<PropertyGenerator.TextureInfo> defaultTextures;
            //TODO: Fix me
            var resultShader = string.Empty;//ShaderGenerator.GenerateSurfaceShader(materialGraphOwner.owner, shaderName, true, out defaultTextures);
            m_GeneratedShaderMode = PreviewMode.Preview3D;
            hasError = !InternalUpdatePreviewShader(resultShader);
            return true;
        }*/
    }
}
