using System;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph
{
    enum SwizzleOutputSize
    {
        Matrix4x4,
        Matrix3x3,
        Matrix2x2,
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

        public event Action<string> OnSizeChange;

        [SerializeField]
        string index_Row0;

        [SerializeField]
        string index_Row1;

        [SerializeField]
        string index_Row2;

        [SerializeField]
        string index_Row3;
        
        [TextControl("00010203", 0, "", " m", "   m", "   m", "   m")]
        public string row0
        {
            get { return index_Row0; }
            set { SetRow(ref index_Row0, value);  }
        }

        [TextControl("10111213", 1, "", " m", "   m", "   m", "   m")]
        public string row1
        {
            get { return index_Row1; }
            set { SetRow(ref index_Row1, value);  }
        }

        [TextControl("20212223", 2, "", " m", "   m", "   m", "   m")]
        public string row2
        {
            get { return index_Row2; }
            set { SetRow(ref index_Row2, value);  }
        }

        [TextControl("30313233", 3, "", " m", "   m", "   m", "   m")]
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
                case SwizzleOutputSize.Matrix4x4:
                    AddSlot(new Matrix4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
                    break;
                case SwizzleOutputSize.Matrix3x3:
                    AddSlot(new Matrix3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
                    break;
                case SwizzleOutputSize.Matrix2x2:
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
                default:
                    AddSlot(new Matrix4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
                    break;
            }
            RemoveSlotsNameNotMatching(new int[] { InputSlotId, OutputSlotId });
        }

        int[] getIndex(float input)
        {
            var row = (int)input;
            float temp_col = (float)(input - Math.Truncate(input)) * 10;

            int[] index = { row, (int)temp_col };
            //Debug.Log("row:"+index[0]+ ", col:"+ temp_col);
            return index;
        }

        string mapComp(int n)
        {
            switch (n)
            {
                default:
                    return "r";
                case 1:
                    return "g";
                case 2:
                    return "b";
                case 3:
                    return "a";
            }
        }


        Vector4  StringToVec4 (string value)
        {
            char[] value_char = value.ToCharArray();


            if (value.Length == 8)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] < '0' || value[i] > '3')
                    {
                        value_char[i] = '0';
                        AreIndiciesValid = false;
                        ValidateNode();
                    }
                }

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
                return new Vector4(x, y, z, w);
            }
            else
            {
                AreIndiciesValid = false;
                ValidateNode();
            }
            return new Vector4(0, 0, 0, 0);
        }

        private bool AreIndiciesValid = true;
        public override void ValidateNode()
        {
            base.ValidateNode();

            if (!AreIndiciesValid)
            {
                owner.AddValidationError(objectId, "Invalid index!", ShaderCompilerMessageSeverity.Error);
            }
        }
        //1. get input matrix and demension
        //2. get swizzle output row count
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

            //get output row count
            int outputRowCount = 4;
            switch (m_OutputSize)
            {
                default:
                    outputRowCount = 4;
                    break;
                case SwizzleOutputSize.Matrix3x3:
                    outputRowCount = 3;
                    break;
                case SwizzleOutputSize.Matrix2x2:
                    outputRowCount = 2;
                    break;
                case SwizzleOutputSize.Vector4:
                    outputRowCount = 4;
                    break;
                case SwizzleOutputSize.Vector3:
                    outputRowCount = 3;
                    break;
                case SwizzleOutputSize.Vector2:
                    outputRowCount = 2;
                    break;
                case SwizzleOutputSize.Vector1:
                    outputRowCount = 1;
                    break;
            }

            var inputIndices = new Matrix4x4();
            //get input indices
            inputIndices.SetRow(0, StringToVec4(index_Row0));
            inputIndices.SetRow(1, StringToVec4(index_Row1));
            inputIndices.SetRow(2, StringToVec4(index_Row2));
            inputIndices.SetRow(3, StringToVec4(index_Row3));
            Debug.Log(index_Row0);

            //set all unused indices to zero
            for (int r = 0; r < 4; r++)
            {
                Vector4 indecies_row = inputIndices.GetRow(r);

                for (int c = 0; c <4; c++)
                {
                    if (IsOutputMatrix(m_OutputSize))
                    {
                        if (c >= outputRowCount)
                        {
                            indecies_row[c] = 0;
                        }
                    }
                    else
                    {
                        if (c > 0)
                        {
                            indecies_row[c] = 0;
                        }
                    }
                }

                inputIndices.SetRow(r, indecies_row);
                if (r >= outputRowCount)
                {
                    inputIndices.SetRow(r, new Vector4(0, 0, 0, 0));
                }
            }

            AreIndiciesValid = true;
            //Check indices sizes
            for (int r = 0; r< 4; r++)
            {
                Vector4 indices_row = inputIndices.GetRow(r);
                for(int c = 0; c<4; c++)
                {
                    float cell = inputIndices.GetRow(r)[c];
                    int[] cell_lst = getIndex(cell);
                    if (cell_lst[0] > concreteRowCount-1 || cell_lst[1] > concreteRowCount - 1)
                    {
                        indices_row[c] = 0;
                        AreIndiciesValid = false;
                        ValidateNode();
                    }

                }

                inputIndices.SetRow(r, indices_row);
                AreIndiciesValid = true;
            }

            //build shader string
            string real_outputValue = "";
            for (var r = 0; r < outputRowCount; r++)
            {
                string outputValue = "";
                Vector4 indecies = inputIndices.GetRow(r);

                if (r != 0)
                    outputValue += ",";

                //If output is a matrix
                if (IsOutputMatrix(m_OutputSize))
                {
                    for (int c = 0; c < outputRowCount; c++)
                    {
                        if (useIndentity == true)
                        {
                            if (c != 0)
                            {
                                outputValue += ",";
                            }
                            outputValue += Matrix4x4.identity.GetRow(getIndex(indecies[c])[0])[getIndex(indecies[c])[1]];

                        }
                        else
                        {
                            if (c != 0)
                            {
                                outputValue += ",";
                            }
                            outputValue += string.Format("{0}[{1}].{2}", inputValue, getIndex(indecies[c])[0], mapComp(getIndex(indecies[c])[1]));
                        }
                    }
                }
                else
                {
                    //If output is a vector
                    if (useIndentity == true)
                    {

                        outputValue += Matrix4x4.identity.GetRow(getIndex(indecies[0])[0])[getIndex(indecies[0])[1]];

                    }
                    else
                    {

                        outputValue += string.Format("{0}[{1}].{2}", inputValue, getIndex(indecies[0])[0], mapComp(getIndex(indecies[0])[1]));
                    }
                }

                real_outputValue += outputValue;

            }

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
                        //Debug.Log("inputSlot:" + inputSlot + ", outputConcreteType: "+ outputConcreteType);
                        continue;
                    }
                }

                // and now dynamic matrices
                //input matrix type
                var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicMatrixInputSlotsToCompare.Values);
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

                    var this_outputConcreteType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType;

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
