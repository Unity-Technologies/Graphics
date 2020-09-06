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

    [Serializable]
    class MatrixSwizzleRow
    {
        // stored as four columns
        public string c0, c1, c2, c3;

        public MatrixSwizzleRow(string c0, string c1, string c2, string c3)
        {
            this.c0 = c0;
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;
        }

        public void SetColumn(int colIndex, string value)
        {
            switch (colIndex)
            {
                case 0:
                    c0 = value;
                    break;
                case 1:
                    c1 = value;
                    break;
                case 2:
                    c2 = value;
                    break;
                case 3:
                    c3 = value;
                    break;
                default:
                    break;
            }
        }

        public string GetColumn(int colIndex)
        {
            switch (colIndex)
            {
                case 0:
                    return c0;
                case 1:
                    return c1;
                case 2:
                    return c2;
                case 3:
                    return c3;
                default:
                    return string.Empty;
            }
        }

        public Vector4 GetRows(ref bool indicesValid)
        {
            Vector4 result = Vector4.zero;
            for (int colIndex = 0; colIndex < 4; colIndex++)
            {
                string indexString = GetColumn(colIndex);

                char rowChar = '0';

                if (indexString.Length >= 1)
                    rowChar = indexString[0];
                else
                    indicesValid = false;

                if (rowChar < '0' || rowChar > '3')
                {
                    rowChar = '0';
                    indicesValid = false;
                }

                result[colIndex] = (float) Char.GetNumericValue(rowChar);
            }

            return result;
        }

        public Vector4 GetColumns(ref bool indicesValid)       // TODO: would be better to return two integers per column instead of using floats
        {
            Vector4 result = Vector4.zero;
            for (int colIndex = 0; colIndex < 4; colIndex++)
            {
                string indexString = GetColumn(colIndex);

                char colChar = '0';

                if (indexString.Length >= 2)
                    colChar = indexString[1];
                else
                    indicesValid = false;

                if (colChar < '0' || colChar > '3')
                {
                    colChar = '0';
                    indicesValid = false;
                }

                // convert the index into a float:  row.column      i.e.  row 3, column 2 == 3.2
                result[colIndex] = (float) Char.GetNumericValue(colChar);
            }

            return result;
        }
    }

    [Title("Math", "Matrix", "Matrix Swizzle")]
    class MatrixSwizzleNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";
     
        public const int InputSlotId = 0;
        public const int OutputSlotId = 1;

        private bool AreIndicesValid = true;        // TODO: need to make this not stateful...

        public event Action<SwizzleOutputSize> OnSizeChange;

        [SerializeField]
        MatrixSwizzleRow _row0 = new MatrixSwizzleRow("00", "01", "02", "03");

        [SerializeField]
        MatrixSwizzleRow _row1 = new MatrixSwizzleRow("10", "11", "12", "13");

        [SerializeField]
        MatrixSwizzleRow _row2 = new MatrixSwizzleRow("20", "21", "22", "23");

        [SerializeField]
        MatrixSwizzleRow _row3 = new MatrixSwizzleRow("30", "31", "32", "33");

        [TextControl(0, "", " m", "   m", "   m", "   m")]
        public MatrixSwizzleRow row0
        {
            get { return _row0; }
//            set { SetRow(ref _row0, value);  }
        }

        [TextControl(1, "", " m", "   m", "   m", "   m")]
        public MatrixSwizzleRow row1
        {
            get { return _row1; }
//            set { SetRow(ref _row1, value);  }
        }

        [TextControl(2, "", " m", "   m", "   m", "   m")]
        public MatrixSwizzleRow row2
        {
            get { return _row2; }
//            set { SetRow(ref _row2, value);  }
        }

        [TextControl(3, "", " m", "   m", "   m", "   m")]
        public MatrixSwizzleRow row3
        {
            get { return _row3; }
//            set { SetRow(ref _row3, value);  }
        }
/*
        void SetRow(ref MatrixSwizzleRow row, MatrixSwizzleRow value)
        {
            if (value == row)
                return;
            row = value;
            owner.ValidateGraph();
            Dirty(ModificationScope.Topological);
        }
*/
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
                OnSizeChange?.Invoke(m_OutputSize);
                return m_OutputSize; }
            set
            {
                if (m_OutputSize.Equals(value))
                    return;
                m_OutputSize = value;
                OnSizeChange?.Invoke(value);
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

/*
        int[] getIndex(float input)
        {
            var row = (int)input;
            float temp_col = (float)(input - Math.Truncate(input)) * 10;

            int[] index = { row, (int)Math.Round(temp_col) };
            //Debug.Log(input+", row:" + index[0] + ", col:"+ (int)Math.Round(temp_col));
            return index;
        }
*/

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


        public override void ValidateNode()
        {
            base.ValidateNode();

            if (!AreIndicesValid)
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

            //TODO: Change UI(2x2) to (4x4)
            int concreteRowCount = useIndentity ? 4 : numInputRows;

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

            Matrix4x4 inputRows = new Matrix4x4();
            Matrix4x4 inputCols = new Matrix4x4();

            //get input indices
            inputRows.SetRow(0, _row0.GetRows(ref AreIndicesValid));
            inputRows.SetRow(1, _row1.GetRows(ref AreIndicesValid));
            inputRows.SetRow(2, _row2.GetRows(ref AreIndicesValid));
            inputRows.SetRow(3, _row3.GetRows(ref AreIndicesValid));

            inputCols.SetRow(0, _row0.GetColumns(ref AreIndicesValid));
            inputCols.SetRow(1, _row1.GetColumns(ref AreIndicesValid));
            inputCols.SetRow(2, _row2.GetColumns(ref AreIndicesValid));
            inputCols.SetRow(3, _row3.GetColumns(ref AreIndicesValid));

/*          //set all unused indices to zero
            for (int r = 0; r < 4; r++)
            {
                Vector4 rows = inputRows.GetRow(r);
                Vector4 cols = inputCols.GetRow(r);

                for (int c = 0; c < 4; c++)
                {
                    if (IsOutputMatrix(m_OutputSize))
                    {
                        if (c >= outputRowCount)
                        {
                            rows[c] = 0;
                            cols[c] = 0;
                        }
                    }
                    else
                    {
                        if (c > 0)
                        {
                            rows[c] = 0;
                            cols[c] = 0;
                        }
                    }
                }

                inputIndices.SetRow(r, rows);
                if (r >= outputRowCount)
                {
                    inputIndices.SetRow(r, new Vector4(0, 0, 0, 0));
                }
            }
*/

            AreIndicesValid = true;

            // replace any out-of-bounds values with 0
            for (int r = 0; r < 4; r++)
            {
                Vector4 rows = inputRows.GetRow(r);
                Vector4 cols = inputCols.GetRow(r);

                for (int c = 0; c<4; c++)
                {
                    float row = inputRows[c];
                    float col = inputCols[c];

                    if (row > concreteRowCount-1 || col > concreteRowCount - 1)
                    {
                        rows[c] = 0;
                        cols[c] = 0;
                        AreIndicesValid = false;
                        ValidateNode();
                    }

                }

                inputRows.SetRow(r, rows);
                inputCols.SetRow(r, cols);
            }

            // build shader string
            string real_outputValue = "";
            for (var r = 0; r < outputRowCount; r++)
            {
                string outputValue = "";
                // Vector4 indices = inputIndices.GetRow(r);
                Vector4 rows = inputRows.GetRow(r);
                Vector4 cols = inputCols.GetRow(r);

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
                            outputValue += Matrix4x4.identity.GetRow((int) rows[c])[(int) cols[c]];

                        }
                        else
                        {
                            if (c != 0)
                            {
                                outputValue += ",";
                            }
                            outputValue += string.Format("{0}[{1}].{2}", inputValue, (int) rows[c], mapComp((int) cols[c]));
                        }
                    }
                }
                else
                {
                    //If output is a vector
                    if (useIndentity == true)
                    {
                        outputValue += Matrix4x4.identity.GetRow((int) rows[0])[(int) cols[0]];
                    }
                    else
                    {
                        outputValue += string.Format("{0}[{1}].{2}", inputValue, (int) rows[0], mapComp((int) cols[0]));
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
   
        private bool IsOutputMatrix (SwizzleOutputSize swizzleSize)
        {
            return (swizzleSize <= SwizzleOutputSize.Matrix2x2);
        }

        public void OnSwizzleChange()
        {
            owner.ValidateGraph();
            Dirty(ModificationScope.Topological);
        }
    }
}
