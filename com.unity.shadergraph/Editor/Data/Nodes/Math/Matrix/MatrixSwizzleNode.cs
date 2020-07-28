using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing.Util;
using JetBrains.Annotations;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEditor.PackageManager.Requests;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    // string[] swizzleTypeNames = new string[];
    enum SwizzleOutputSize
    {
        Matrix4,
        Matrix3,
        Matrix2,
        Vector4,
        Vector3,
        Vector2,
        Vector1
    }
    [Title("Math", "Matrix", "Matrix Swizzle")]
    class MatrixSwizzleNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";
      

        public const int InputSlotId = 0;
        public const int OutputSlotId = 1;
        //TODO: Do we want to use build in matrix constrcut function??

        public const int Output4x4SlotId = 2;
        public const int Output3x3SlotId = 3;
        public const int Output2x2SlotId = 4;


        [SerializeField]
        Vector4 index_Row0;

        [SerializeField]
        Vector4 index_Row1;

        [SerializeField]
        Vector4 index_Row2;

        [SerializeField]
        Vector4 index_Row3;

        [MultiFloatControl("", " m", "   m", "   m", "   m")]
        public Vector4 row0
        {
            get { return index_Row0; }
            set { SetRow(ref index_Row0, value); }
        }

        [MultiFloatControl("", " m", "   m", "   m", "   m")]
        public Vector4 row1
        {
            get { return index_Row1; }
            set { SetRow(ref index_Row1, value); }
        }

        [MultiFloatControl("", " m", "   m", "   m", "   m")]
        public Vector4 row2
        {
            get { return index_Row2; }
            set { SetRow(ref index_Row2, value); }
        }

        [MultiFloatControl("", " m", "   m", "   m", "   m")]
        public Vector4 row3
        {
            get { return index_Row3; }
            set { SetRow(ref index_Row3, value); }
        }

        void SetRow(ref Vector4 row, Vector4 value)
        {
            if (value == row)
                return;
            row = value;
            Dirty(ModificationScope.Node);
        }


        public MatrixSwizzleNode()
        {
            name = "Matrix Swizzle";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        SwizzleOutputSize m_OutputSize;

        [EnumControl("Output Size:")]
        SwizzleOutputSize outputSize
        {
            get { return m_OutputSize; }
            set
            {
                if (m_OutputSize.Equals(value))
                    return;
                m_OutputSize = value;
                Dirty(ModificationScope.Graph);
                UpdateNodeAfterDeserialization();
                //EvaluateDynamicMaterialSlots();
            }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicMatrixMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input));
            //AddSlot(new DynamicMatrixMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            switch (m_OutputSize)
            {
                case SwizzleOutputSize.Matrix4:
                    AddSlot(new Matrix4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
                    break;
                case SwizzleOutputSize.Matrix3:
                    AddSlot(new Matrix3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
                    break;
                case SwizzleOutputSize.Matrix2:
                    AddSlot(new Matrix2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
                    break;
                case SwizzleOutputSize.Vector4:
                    AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
                    break;
                case SwizzleOutputSize.Vector3:
                    AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
                    break;
                case SwizzleOutputSize.Vector2:
                    AddSlot(new Vector2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector2.zero));
                    break;
                case SwizzleOutputSize.Vector1:
                    AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
                    break;
            }
            RemoveSlotsNameNotMatching(new int[] { InputSlotId, OutputSlotId });

        }

        int[] getIndex(float input)
        {
            var row = (int)input;
            float temp_col = (float)(input - Math.Truncate(input)) * 10;
            int col = 0;

            if (temp_col > 2.5)
            {
                col = 3;
            }
            else if (temp_col > 1.5 && temp_col < 2.5)
            {
                col = 2;
            }
            else if (temp_col > 0.5 && temp_col < 1.5)
            {
                col = 1;
            }
            else
            {
                col = 0;
            }

            int[] index = { row, col };

            return index;

        }

        string mapComp(int n)
        {
            switch (n)
            {
                default:
                    return "x";
                case 1:
                    return "y";
                case 2:
                    return "z";
                case 3:
                    return "w";
            }
        }

        bool IsIndexSizeCorrect(Vector4 vec, int inputSize)
        {
            int[] x = getIndex(vec.x);
            int[] y = getIndex(vec.y);
            int[] z = getIndex(vec.z);
            int[] w = getIndex(vec.w);
            var list = new List<int>();
            list.AddRange(x);
            list.AddRange(y);
            list.AddRange(z);
            list.AddRange(w);

            inputSize -= 1;
            

            bool check = true;
            foreach (int index in list)
            {
                if ((inputSize - index)<0 )
                {

                    check = false;
                }
            }
            return check;

        }


        
        //1. get input matrix and demension
        //2. get swizzle output size
        //3. get index matrix (HACKYYYYYYY)
        //4. map output matirx/vec according to index matrix/vec
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {

            //Get input matrix and its demension
            var inputValue = GetSlotValue(InputSlotId, generationMode);

            var inputSlot = FindInputSlot<MaterialSlot>(InputSlotId);
            var numInputRows = 0;
            bool useIndentity = false;

            if (inputSlot != null)
            {
                numInputRows = SlotValueHelper.GetMatrixDimension(inputSlot.concreteValueType);
                if (numInputRows > 4)
                    numInputRows = 0;

                if (!owner.GetEdges(inputSlot.slotReference).Any())
                {
                    numInputRows = 0;
                    useIndentity = true;
                }
            }

            int concreteRowCount = useIndentity ? 2 : numInputRows;
            int outputRowCount = 0;

            //INDECIES VALIDATION
            //TODO: Should give what row/columns the problems are
            var inputIndecies = new Matrix4x4();

            //set all indecies that won't be used to zero
            switch (m_OutputSize)
            {
                default:
                    inputIndecies.SetRow(0, index_Row0);
                    inputIndecies.SetRow(1, index_Row1);
                    inputIndecies.SetRow(2, index_Row2);
                    inputIndecies.SetRow(3, index_Row3);
                    break;
                case SwizzleOutputSize.Matrix3:
                    inputIndecies.SetRow(0, new Vector4(index_Row0.x, index_Row0.y, index_Row0.z, 0));
                    inputIndecies.SetRow(1, new Vector4(index_Row1.x, index_Row1.y, index_Row1.z, 0));
                    inputIndecies.SetRow(2, new Vector4(index_Row2.x, index_Row2.y, index_Row2.z, 0));
                    inputIndecies.SetRow(3, new Vector4(0,0,0,0));
                    break;
                case SwizzleOutputSize.Matrix2:
                    inputIndecies.SetRow(0, new Vector4(index_Row0.x, index_Row0.y, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(index_Row1.x, index_Row1.y, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector4:
                    inputIndecies.SetRow(0, new Vector4(index_Row0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(index_Row1.x, 0 ,0, 0));
                    inputIndecies.SetRow(2, new Vector4(index_Row2.x, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(index_Row3.x, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector3:
                    inputIndecies.SetRow(0, new Vector4(index_Row0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(index_Row1.x, 0, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(index_Row2.x, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector2:
                    inputIndecies.SetRow(0, new Vector4(index_Row0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(index_Row1.x, 0, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector1:
                    inputIndecies.SetRow(0, new Vector4(index_Row0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
            }

            //Check indeceis sizes
            if (!(IsIndexSizeCorrect(inputIndecies.GetRow(0), concreteRowCount)&&
                IsIndexSizeCorrect(inputIndecies.GetRow(1), concreteRowCount)&&
                IsIndexSizeCorrect(inputIndecies.GetRow(2), concreteRowCount) && IsIndexSizeCorrect(inputIndecies.GetRow(3), concreteRowCount)) )
            {
                Debug.LogError("Indices need to be smaller than input size!");
                inputIndecies.SetRow(0, new Vector4(0,0,0,0));
                inputIndecies.SetRow(1, new Vector4(0, 0, 0, 0));
                inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
            }






            string real_outputValue = " ";
            for (var r = 0; r < 4; r++)
            {
                string outputValue = " ";


                Vector4 indecies = inputIndecies.GetRow(r);
                switch (m_OutputSize)
                        {

                            default:
                                outputRowCount = 4;

                                //get indicies for input matrix at current output matrix position
                                int input_x_R = getIndex(indecies.x)[0];
                                string input_x_C = mapComp(getIndex(indecies.x)[1]);


                                int input_y_R = getIndex(indecies.y)[0];
                                string input_y_C = mapComp(getIndex(indecies.y)[1]);
                                int input_z_R = getIndex(indecies.z)[0];
                                string input_z_C = mapComp(getIndex(indecies.z)[1]);
                                int input_w_R = getIndex(indecies.w)[0];
                                string input_w_C = mapComp(getIndex(indecies.w)[1]);

                                if (r != 0)
                                    outputValue += ",";

                                
                                outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R, input_x_C);
                                outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_y_R, input_y_C);
                                outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_z_R, input_z_C);
                                outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_w_R, input_w_C);


                                break;
                            case SwizzleOutputSize.Matrix3:

                                outputRowCount = 3;
                                if (r >= 3)
                                {

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);
                                    int input_y_R3 = getIndex(indecies.y)[0];
                                    string input_y_C3 = mapComp(getIndex(indecies.y)[1]);
                                    int input_z_R3 = getIndex(indecies.z)[0];
                                    string input_z_C3 = mapComp(getIndex(indecies.z)[1]);

                                    if (r != 0)
                                        outputValue += ",";


                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_y_R3, input_y_C3);
                                    outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_z_R3, input_z_C3);


                                    break;
                                }

                            case SwizzleOutputSize.Matrix2:
                                outputRowCount = 2;
                                if (r >= 2)
                                {

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);

                                    int input_y_R3 = getIndex(indecies.y)[0];
                                    string input_y_C3 = mapComp(getIndex(indecies.y)[1]);

                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                    outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_y_R3, input_y_C3);

                                    break;
                                }
                            case SwizzleOutputSize.Vector1:
                                outputRowCount = 1;
                                if (r >= 1)
                                {

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);

                                    if (r != 0)
                                        outputValue += ",";


                                    outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);

                                    break;
                                }
                            case SwizzleOutputSize.Vector2:
                                outputRowCount = 2;
                                if (r >= 2)
                                {


                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);
 

                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);

                                    break;
                                }
                            case SwizzleOutputSize.Vector3:
                                outputRowCount = 3;
                                if (r >= 3)
                                {


                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);


                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);

                                    break;
                                }
                            case SwizzleOutputSize.Vector4:
                                outputRowCount = 4;
                                if (r >= 4)
                                {


                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);


                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);

                                    break;
                                }
                }
                 




                real_outputValue += outputValue;

            }

           // Debug.Log("output: " + real_outputValue + outputRowCount);
            if (IsOutputMatrix(m_OutputSize))
            {
                sb.AppendLine(string.Format("$precision{2}x{2} {0} = $precision{2}x{2} ({1});", GetVariableNameForSlot(OutputSlotId), real_outputValue, outputRowCount));
            }
            else
            {
                sb.AppendLine(string.Format("$precision{2} {0} = $precision{2} ({1});", GetVariableNameForSlot(OutputSlotId), real_outputValue, outputRowCount));
            }
           
            




        }
        public override void EvaluateDynamicMaterialSlots()
        {
            var dynamicInputSlotsToCompare = DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicSlots = ListPool<DynamicVectorMaterialSlot>.Get();

            var dynamicMatrixInputSlotsToCompare = DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Get();
            var skippedDynamicMatrixSlots = ListPool<DynamicMatrixMaterialSlot>.Get();
            // iterate the input slots
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var inputSlot in tempSlots)
                {
                    inputSlot.hasError = false;

                    // if there is a connection
                    var edges = owner.GetEdges(inputSlot.slotReference).ToList();
                    if (!edges.Any())
                    {
                        if (inputSlot is DynamicVectorMaterialSlot)
                            skippedDynamicSlots.Add(inputSlot as DynamicVectorMaterialSlot);
                        if (inputSlot is DynamicMatrixMaterialSlot)
                            skippedDynamicMatrixSlots.Add(inputSlot as DynamicMatrixMaterialSlot);
                        continue;
                    }

                    // get the output details
                    var outputSlotRef = edges[0].outputSlot;
                    var outputNode = outputSlotRef.node;
                    
                    if (outputNode == null)
                        continue;

                    var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(outputSlotRef.slotId);
                    if (outputSlot == null)
                        continue;

                    if (outputSlot.hasError)
                    {
                        inputSlot.hasError = true;
                        continue;
                    }

                    var outputConcreteType = outputSlot.concreteValueType;
                    // dynamic input... depends on output from other node.
                    // we need to compare ALL dynamic inputs to make sure they
                    // are compatable.
                    if (inputSlot is DynamicVectorMaterialSlot)
                    {
                        dynamicInputSlotsToCompare.Add((DynamicVectorMaterialSlot)inputSlot, outputConcreteType);
                        continue;
                    }
                    else if (inputSlot is DynamicMatrixMaterialSlot)
                    {
                        dynamicMatrixInputSlotsToCompare.Add((DynamicMatrixMaterialSlot)inputSlot, outputConcreteType);
                        Debug.Log("inputSlot:" + inputSlot + ", outputConcreteType: "+ outputConcreteType);
                        continue;
                    }
                }

                // and now dynamic matrices
                //input matrix type
                var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicMatrixInputSlotsToCompare.Values);
               // Debug.Log("dynamicMatrixType: " + dynamicMatrixType);
                foreach (var dynamicKvP in dynamicMatrixInputSlotsToCompare)
                    dynamicKvP.Key.SetConcreteType(dynamicMatrixType);
                foreach (var skippedSlot in skippedDynamicMatrixSlots)
                    skippedSlot.SetConcreteType(dynamicMatrixType);

                // we can now figure out the dynamic slotType
                // from here set all the
                var dynamicType = SlotValueHelper.ConvertMatrixToVectorType(dynamicMatrixType);
                foreach (var dynamicKvP in dynamicInputSlotsToCompare)
                    dynamicKvP.Key.SetConcreteType(dynamicType);
                foreach (var skippedSlot in skippedDynamicSlots)
                    skippedSlot.SetConcreteType(dynamicType);

                tempSlots.Clear();
                GetInputSlots(tempSlots);
                bool inputError = tempSlots.Any(x => x.hasError);
                if (inputError)
                {
                    owner.AddConcretizationError(objectId, string.Format("Node {0} had input error", objectId));
                    hasError = true;
                }
                // configure the output slots now
                // their slotType will either be the default output slotType
                // or the above dynanic slotType for dynamic nodes
                // or error if there is an input error
                tempSlots.Clear();
                GetOutputSlots(tempSlots);
                foreach (var outputSlot in tempSlots)
                {
                    outputSlot.hasError = false;

                    if (inputError)
                    {
                        outputSlot.hasError = true;
                        continue;
                    }

                    //var this_ouputSlot = owner.FindInputSlot<MaterialSlot>(OutputSlotId);
                    var this_outputConcreteType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType;
                    //Debug.Log("this_outputConcreteType: " + this_outputConcreteType);
                    //var ouputType = ConvertDynamicMatrixInputTypeToConcrete(this_ouputSlot.concreteValueType);

                    if (outputSlot is DynamicVectorMaterialSlot)
                    {
                        (outputSlot as DynamicVectorMaterialSlot).SetConcreteType(this_outputConcreteType);
                        continue;
                    }
                    else if (outputSlot is DynamicMatrixMaterialSlot)
                    {
                        (outputSlot as DynamicMatrixMaterialSlot).SetConcreteType(this_outputConcreteType);
                        continue;
                    }
                }


                tempSlots.Clear();
                GetOutputSlots(tempSlots);
                if (tempSlots.Any(x => x.hasError))
                {
                    owner.AddConcretizationError(objectId, string.Format("Node {0} had output error", objectId));
                    hasError = true;
                }
            }

            CalculateNodeHasError();

            ListPool<DynamicVectorMaterialSlot>.Release(skippedDynamicSlots);
            DictionaryPool<DynamicVectorMaterialSlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);

            ListPool<DynamicMatrixMaterialSlot>.Release(skippedDynamicMatrixSlots);
            DictionaryPool<DynamicMatrixMaterialSlot, ConcreteSlotValueType>.Release(dynamicMatrixInputSlotsToCompare);

            //UpdateNodeAfterDeserialization();
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            //get input slot concrete value type 
            //inject shader properties based on incoming matrix type 

            //cleanup into an if statement to avoid duplicated code where possible 
            switch (m_OutputSize)
            {
                case SwizzleOutputSize.Matrix2:
                    properties.AddShaderProperty(new Vector2ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m0", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row0
                    });
                    properties.AddShaderProperty(new Vector2ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m1", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row1
                    });
                    break;
                case SwizzleOutputSize.Matrix3:
                    properties.AddShaderProperty(new Vector3ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m0", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row0
                    });
                    properties.AddShaderProperty(new Vector3ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m1", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row1
                    });
                    properties.AddShaderProperty(new Vector3ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m2", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row2
                    });
                    break;
                case SwizzleOutputSize.Matrix4:
                    properties.AddShaderProperty(new Vector4ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m0", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row0
                    });
                    properties.AddShaderProperty(new Vector4ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m1", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row1
                    });
                    properties.AddShaderProperty(new Vector4ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m2", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row2
                    });
                    properties.AddShaderProperty(new Vector4ShaderProperty()
                    {
                        overrideReferenceName = string.Format("_{0}_m3", GetVariableNameForNode()),
                        generatePropertyBlock = false,
                        value = index_Row3
                    });
                    break;
            }
        }

    

    private bool IsOutputMatrix (SwizzleOutputSize SwizzleSize)
        {
            string str = SwizzleSize.ToString();
            if (str.StartsWith("M"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
