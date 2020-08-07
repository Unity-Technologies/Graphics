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
using UnityEditor.Rendering;

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


        public event Action<string> OnSizeChange;
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";
      

        public const int InputSlotId = 0;
        public const int OutputSlotId = 1;

        public const int Output4x4SlotId = 2;
        public const int Output3x3SlotId = 3;
        public const int Output2x2SlotId = 4;



        [SerializeField]
        string index_Row0;

        [SerializeField]
        string index_Row1;

        [SerializeField]
        string index_Row2;

        [SerializeField]
        string index_Row3;

        

        [TextControl(0, "", " m", "   m", "   m", "   m")]
        public string row0
        {
            get { return index_Row0; }
            set { SetRow(ref index_Row0, value);  }
        }

        [TextControl(1, "", " m", "   m", "   m", "   m")]
        public string row1
        {
            get { return index_Row1; }
            set { SetRow(ref index_Row1, value);  }
        }

        [TextControl(2, "", " m", "   m", "   m", "   m")]
        public string row2
        {
            get { return index_Row2; }
            set { SetRow(ref index_Row2, value);  }
        }

        [TextControl(3, "", " m", "   m", "   m", "   m")]
        public string row3
        {
            get { return index_Row3; }
            set { SetRow(ref index_Row3, value);  }
        }

        void SetRow(ref string row, string value)
        {
            if (value == row)
                return;
            row = value;
            owner.ValidateGraph();
            //Debug.Log(value);
            //Dirty(ModificationScope.Node);
            Dirty(ModificationScope.Topological);
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
            get {
                OnSizeChange?.Invoke(m_OutputSize.ToString());
                return m_OutputSize; }
            set
            {
                if (m_OutputSize.Equals(value))
                    return;
                m_OutputSize = value;
                OnSizeChange?.Invoke(value.ToString());
                UpdateNodeAfterDeserialization();
                owner.ValidateGraph();
                Dirty(ModificationScope.Topological);
            }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicMatrixMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input));
            //add slot needs to add connected edges to update loop in handlegraphchanges
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

        Vector4  StringToVec4 (string value)
        {
            
            if (value.Length == 8)
            {
               // Debug.Log("input value: " + value);
                char[] value_char = value.ToCharArray();
                float x0 = (float)Char.GetNumericValue(value_char[0]);
                float x1 = (float)Char.GetNumericValue(value_char[1]);
                float y0 = (float)Char.GetNumericValue(value_char[2]);
                float y1 = (float)Char.GetNumericValue(value_char[3]);
                float z0 = (float)Char.GetNumericValue(value_char[4]);
                float z1 = (float)Char.GetNumericValue(value_char[5]);
                float w0 = (float)Char.GetNumericValue(value_char[6]);
                float w1 = (float)Char.GetNumericValue(value_char[7]);

                float x = x0 + x1 * 0.1f;
                float y = y0 + y1 * 0.1f;
                float z = z0 + z1 * 0.1f;
                float w = w0 + w1 * 0.1f;
               // Debug.Log("Vector4(x, y, z, w): " + new Vector4(x, y, z, w));
                return new Vector4(x, y, z, w);
                

            }
            return new Vector4(0, 0, 0, 0);
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


        private bool AreIndiciesValid = true;
        public override void ValidateNode()
        {
            base.ValidateNode();
            Debug.Log("AreIndiciesValid" + AreIndiciesValid);
            if (!AreIndiciesValid)
            {
                owner.AddValidationError(objectId, "Indices need to be smaller than input size!", ShaderCompilerMessageSeverity.Error);
            }
   

        }
        //1. get input matrix and demension
        //2. get swizzle output size
        //3. get index matrix 
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
            Vector4 r0 = StringToVec4(index_Row0);
            Vector4 r1 = StringToVec4(index_Row1);
            Vector4 r2 = StringToVec4(index_Row2);
            Vector4 r3 = StringToVec4(index_Row3);


            //INDECIES VALIDATION
            //TODO: Should give what row/columns the problems are?
            var inputIndecies = new Matrix4x4();

            //set all indices that won't be used to zero
            switch (m_OutputSize)
            {
                default:
                    inputIndecies.SetRow(0, r0);
                    inputIndecies.SetRow(1, r1);
                    inputIndecies.SetRow(2, r2);
                    inputIndecies.SetRow(3, r3);
                    break;
                case SwizzleOutputSize.Matrix3:
                    inputIndecies.SetRow(0, new Vector4(r0.x, r0.y, r0.z, 0));
                    inputIndecies.SetRow(1, new Vector4(r1.x, r1.y, r1.z, 0));
                    inputIndecies.SetRow(2, new Vector4(r2.x, r2.y, r2.z, 0));
                    inputIndecies.SetRow(3, new Vector4(0,0,0,0));
                    break;
                case SwizzleOutputSize.Matrix2:
                    inputIndecies.SetRow(0, new Vector4(r0.x, r0.y, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(r1.x, r1.y, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector4:
                    inputIndecies.SetRow(0, new Vector4(r0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(r1.x, 0 ,0, 0));
                    inputIndecies.SetRow(2, new Vector4(r2.x, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(r3.x, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector3:
                    inputIndecies.SetRow(0, new Vector4(r0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(r1.x, 0, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(r2.x, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector2:
                    inputIndecies.SetRow(0, new Vector4(r0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(r1.x, 0, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
                case SwizzleOutputSize.Vector1:
                    inputIndecies.SetRow(0, new Vector4(r0.x, 0, 0, 0));
                    inputIndecies.SetRow(1, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                    inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                    break;
            }

            //Check indices sizes
            Debug.Log("chekign in code" );
            AreIndiciesValid = true;
            if (!(IsIndexSizeCorrect(inputIndecies.GetRow(0), concreteRowCount)&&
                IsIndexSizeCorrect(inputIndecies.GetRow(1), concreteRowCount)&&
                IsIndexSizeCorrect(inputIndecies.GetRow(2), concreteRowCount) && IsIndexSizeCorrect(inputIndecies.GetRow(3), concreteRowCount)) )
            {
                AreIndiciesValid = false;
                ValidateNode();
                inputIndecies.SetRow(0, new Vector4(0,0,0,0));
                inputIndecies.SetRow(1, new Vector4(0, 0, 0, 0));
                inputIndecies.SetRow(2, new Vector4(0, 0, 0, 0));
                inputIndecies.SetRow(3, new Vector4(0, 0, 0, 0));
                AreIndiciesValid = true;
                //ValidateNode();
            }






            string real_outputValue = " ";
            for (var r = 0; r < 4; r++)
            {
                string outputValue = " ";


                Vector4 indecies = inputIndecies.GetRow(r);
                //Debug.Log("row: " + r + "- " + indecies);
                switch (m_OutputSize)
                        {

                            default:
                                outputRowCount = 4;

                                //get indices for input matrix at current output matrix position
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
                                
                                if (useIndentity == true)
                                {
                                    outputValue += Matrix4x4.identity.GetRow(input_x_R)[getIndex(indecies.x)[1]]+",";
                                    outputValue += Matrix4x4.identity.GetRow(input_y_R)[getIndex(indecies.y)[1]] + ",";
                                    outputValue += Matrix4x4.identity.GetRow(input_z_R)[getIndex(indecies.z)[1]] + ",";
                                    outputValue += Matrix4x4.identity.GetRow(input_w_R)[getIndex(indecies.w)[1]];
                                }
                                else
                                {
                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R, input_x_C);
                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_y_R, input_y_C);
                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_z_R, input_z_C);
                                    outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_w_R, input_w_C);
                                }



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

                                    if (useIndentity == true)
                                    {
                                        outputValue += Matrix4x4.identity.GetRow(getIndex(indecies.x)[0])[getIndex(indecies.x)[1]] + ",";
                                        outputValue += Matrix4x4.identity.GetRow(getIndex(indecies.y)[0])[getIndex(indecies.y)[1]] + ",";
                                        outputValue += Matrix4x4.identity.GetRow(getIndex(indecies.z)[0])[getIndex(indecies.z)[1]];

                                    }
                                    else
                                    {
                                        outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                        outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_y_R3, input_y_C3);
                                        outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_z_R3, input_z_C3);
                                    }


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
                                    if (useIndentity == true)
                                    {
                                        outputValue += Matrix4x4.identity.GetRow(getIndex(indecies.x)[0])[getIndex(indecies.x)[1]] + ",";
                                        outputValue += Matrix4x4.identity.GetRow(getIndex(indecies.y)[0])[getIndex(indecies.y)[1]] ;


                                    }
                                    else
                                    {
                                        outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                        outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_y_R3, input_y_C3);
                                    }


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

                                    if (useIndentity == true)
                                    {
                                        outputValue += Matrix4x4.identity.GetRow(input_x_R3)[getIndex(indecies.x)[1]];

                                    }
                                    else
                                    {
                                        outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);

                                    }
 

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
                                    if (useIndentity == true)
                                    {
                                        outputValue += Matrix4x4.identity.GetRow(input_x_R3)[getIndex(indecies.x)[1]];

                                    }
                                    else
                                    {
                                        outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);

                                    }
                           

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
                                    if (useIndentity == true)
                                    {
                                        outputValue += Matrix4x4.identity.GetRow(input_x_R3)[getIndex(indecies.x)[1]];

                                    }
                                    else
                                    {
                                        outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);

                                    }
                            

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
                                    if (useIndentity == true)
                                    {
                                        outputValue += Matrix4x4.identity.GetRow(input_x_R3)[getIndex(indecies.x)[1]] ;

                                    }
                                    else
                                    {
                                        outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_x_R3, input_x_C3);
                                    }

                           

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
