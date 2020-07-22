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

        [EnumControl("")]
        SwizzleOutputSize outputSize
        {
            get { return m_OutputSize; }
            set
            {
                if (m_OutputSize.Equals(value))
                    return;
                m_OutputSize = value;
                Dirty(ModificationScope.Graph);
            }
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicMatrixMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input));
            AddSlot(new DynamicMatrixMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            //AddSlot(new DynamicValueMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Matrix4x4.zero));
            //AddSlot(new DynamicValueMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Matrix4x4.zero));
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


        //TODO:
        //1. get input matrix and demension
        //2. get swizzle output size
        //3. get index matrix (HACKYYYYYYY)
        //4. map output matirx/vec according to index matrix/vec
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //Debug.LogError("swizzle update!" + m_OutputSize);
            //Debug.Log((int)index_Row0.x + " ,"+((index_Row0.x - Math.Truncate(index_Row0.x))*10));
            //Debug.Log(getIndex(index_Row1.z)[0]+" "+ getIndex(index_Row1.z)[1]);

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

            //TODO: fix upscaling(3x3 -> 4x4)
            var inputIndecies = new Matrix4x4();
            inputIndecies.SetRow(0, index_Row0);
            inputIndecies.SetRow(1, index_Row1);
            inputIndecies.SetRow(2, index_Row2);
            inputIndecies.SetRow(3, index_Row3);




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
                                //Vector4 indecies = inputIndecies.GetRow(r);
                        int input_x_R = getIndex(indecies.x)[0];
                                string input_x_C = mapComp(getIndex(indecies.x)[1]);
                                //Debug.Log(input_x_R + "  , " + input_x_C);
                                //Debug.LogError(getIndex(indecies.w)[1]);

                                int input_y_R = getIndex(indecies.y)[0];
                                string input_y_C = mapComp(getIndex(indecies.y)[1]);
                                int input_z_R = getIndex(indecies.z)[0];
                                string input_z_C = mapComp(getIndex(indecies.z)[1]);
                                int input_w_R = getIndex(indecies.w)[0];
                                string input_w_C = mapComp(getIndex(indecies.w)[1]);

                                //Debug.LogError(r+": "+input_x_R+" "+ input_y_R + " " + input_z_R + " " + input_w_R);
                                //Debug.LogError(input_w_R + " " + input_w_C);
                                if (r != 0)
                                    outputValue += ",";

                                //TODO: needs to check wether input indecies >=3 (validation)
                                outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R, input_x_C);
                                outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_y_R, input_y_C);
                                outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_z_R, input_z_C);
                                outputValue += string.Format("_{0}_m{1}.{2}", inputValue, input_w_R, input_w_C);


                                break;
                            case SwizzleOutputSize.Matrix3:
                                outputRowCount = 3;
                                if (r >= 3)
                                {

                                    for (int c = 0; c < 4; c++)
                                    {

                                        outputValue += ", ";
                                        outputValue += Matrix4x4.identity.GetRow(r)[c];
                                    }

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
                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_z_R3, input_z_C3);
                                    outputValue += Matrix4x4.identity.GetRow(3)[3];
                                    break;
                                }

                            case SwizzleOutputSize.Matrix2:
                                outputRowCount = 2;
                                if (r >= 2)
                                {


                                    for (int c = 0; c < 4; c++)
                                    {
                                
                                            outputValue += ", ";
                                        outputValue += Matrix4x4.identity.GetRow(r)[c];
                                    }

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);
                                    //Debug.Log(input_x_R + "  , " + input_x_C);
                                    //Debug.LogError(getIndex(indecies.w)[1]);

                                    int input_y_R3 = getIndex(indecies.y)[0];
                                    string input_y_C3 = mapComp(getIndex(indecies.y)[1]);
                                    //int input_z_R3 = getIndex(indecies.z)[0];
                                    //string input_z_C3 = mapComp(getIndex(indecies.z)[1]);
                                    //int input_w_R3 = getIndex(indecies.w)[0];
                                    //string input_w_C3 = mapComp(getIndex(indecies.w)[1]);

                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_y_R3, input_y_C3);
                                    outputValue += Matrix4x4.identity.GetRow(r)[2]+",";
                                    outputValue += Matrix4x4.identity.GetRow(r)[3];
                                    break;
                                }
                            case SwizzleOutputSize.Vector1:
                                outputRowCount = 1;
                                if (r >= 1)
                                {


                                    for (int c = 0; c < 4; c++)
                                    {

                                        outputValue += ", ";
                                        outputValue += Matrix4x4.identity.GetRow(r)[c];
                                    }

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);
                                    //Debug.Log(input_x_R + "  , " + input_x_C);
                                    //Debug.LogError(getIndex(indecies.w)[1]);

                                    //int input_y_R3 = getIndex(indecies.y)[0];
                                    //string input_y_C3 = mapComp(getIndex(indecies.y)[1]);
                                    //int input_z_R3 = getIndex(indecies.z)[0];
                                    //string input_z_C3 = mapComp(getIndex(indecies.z)[1]);
                                    //int input_w_R3 = getIndex(indecies.w)[0];
                                    //string input_w_C3 = mapComp(getIndex(indecies.w)[1]);

                                    if (r != 0)
                                        outputValue += ",";


                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                    outputValue += Matrix4x4.identity.GetRow(r)[1] + ","; ;
                                    outputValue += Matrix4x4.identity.GetRow(r)[2] + ",";
                                    outputValue += Matrix4x4.identity.GetRow(r)[3];
                                    break;
                                }
                            case SwizzleOutputSize.Vector2:
                                outputRowCount = 1;
                                if (r >= 2)
                                {


                                    for (int c = 0; c < 4; c++)
                                    {

                                        outputValue += ", ";
                                        outputValue += Matrix4x4.identity.GetRow(r)[c];
                                    }

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);
                                    //Debug.Log(input_x_R + "  , " + input_x_C);
                                    //Debug.LogError(getIndex(indecies.w)[1]);

                                    //int input_y_R3 = getIndex(indecies.y)[0];
                                    //string input_y_C3 = mapComp(getIndex(indecies.y)[1]);
                                    //int input_z_R3 = getIndex(indecies.z)[0];
                                    //string input_z_C3 = mapComp(getIndex(indecies.z)[1]);
                                    //int input_w_R3 = getIndex(indecies.w)[0];
                                    //string input_w_C3 = mapComp(getIndex(indecies.w)[1]);

                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                    outputValue += Matrix4x4.identity.GetRow(r)[1] + ","; ;
                                    outputValue += Matrix4x4.identity.GetRow(r)[2] + ",";
                                    outputValue += Matrix4x4.identity.GetRow(r)[3];
                                    break;
                                }
                            case SwizzleOutputSize.Vector3:
                                outputRowCount = 1;
                                if (r >= 3)
                                {


                                    for (int c = 0; c < 4; c++)
                                    {

                                        outputValue += ", ";
                                        outputValue += Matrix4x4.identity.GetRow(r)[c];
                                    }

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);
                                    //Debug.Log(input_x_R + "  , " + input_x_C);
                                    //Debug.LogError(getIndex(indecies.w)[1]);

                                    //int input_y_R3 = getIndex(indecies.y)[0];
                                    //string input_y_C3 = mapComp(getIndex(indecies.y)[1]);
                                    //int input_z_R3 = getIndex(indecies.z)[0];
                                    //string input_z_C3 = mapComp(getIndex(indecies.z)[1]);
                                    //int input_w_R3 = getIndex(indecies.w)[0];
                                    //string input_w_C3 = mapComp(getIndex(indecies.w)[1]);

                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                    outputValue += Matrix4x4.identity.GetRow(r)[1] + ","; ;
                                    outputValue += Matrix4x4.identity.GetRow(r)[2] + ",";
                                    outputValue += Matrix4x4.identity.GetRow(r)[3];
                                    break;
                                }
                            case SwizzleOutputSize.Vector4:
                                outputRowCount = 1;
                                if (r >= 4)
                                {


                                    for (int c = 0; c < 4; c++)
                                    {

                                        outputValue += ", ";
                                        outputValue += Matrix4x4.identity.GetRow(r)[c];
                                    }

                                    break;
                                }
                                else
                                {
                                    int input_x_R3 = getIndex(indecies.x)[0];
                                    string input_x_C3 = mapComp(getIndex(indecies.x)[1]);
                                    //Debug.Log(input_x_R + "  , " + input_x_C);
                                    //Debug.LogError(getIndex(indecies.w)[1]);

                                    //int input_y_R3 = getIndex(indecies.y)[0];
                                    //string input_y_C3 = mapComp(getIndex(indecies.y)[1]);
                                    //int input_z_R3 = getIndex(indecies.z)[0];
                                    //string input_z_C3 = mapComp(getIndex(indecies.z)[1]);
                                    //int input_w_R3 = getIndex(indecies.w)[0];
                                    //string input_w_C3 = mapComp(getIndex(indecies.w)[1]);

                                    if (r != 0)
                                        outputValue += ",";

                                    outputValue += string.Format("_{0}_m{1}.{2},", inputValue, input_x_R3, input_x_C3);
                                    outputValue += Matrix4x4.identity.GetRow(r)[1] + ","; ;
                                    outputValue += Matrix4x4.identity.GetRow(r)[2] + ",";
                                    outputValue += Matrix4x4.identity.GetRow(r)[3];
                                    break;
                                }
                }
                 




                real_outputValue += outputValue;

            }
            //Debug.Log("matrixSwizzle: " + GetVariableNameForSlot(OutputSlotId) + ", input: " + inputValue);
            Debug.Log("output: " + real_outputValue + outputRowCount);
            //sb.AppendLine(string.Format("$precision4x4 {0} = {1};", GetVariableNameForSlot(OutputSlotId), inputValue));
            sb.AppendLine(string.Format("$precision{2}x{2} {0} = $precision{2}x{2} ({1});", GetVariableNameForSlot(OutputSlotId), real_outputValue, 4));





        }
        //TODO: set up dynamic output slot?
        //public override void EvaluateDynamicMaterialSlots()
        //{
        //    var dynamicInputSlotsToCompare = DictionaryPool<DynamicValueMaterialSlot, ConcreteSlotValueType>.Get();
        //    var skippedDynamicSlots = ListPool<DynamicValueMaterialSlot>.Get();

        //    // iterate the input slots
        //    using (var tempSlots = PooledList<MaterialSlot>.Get())
        //    {
        //        GetInputSlots(tempSlots);
        //        foreach (var inputSlot in tempSlots)
        //        {
        //            inputSlot.hasError = false;

        //            // if there is a connection
        //            var edges = owner.GetEdges(inputSlot.slotReference).ToList();
        //            if (!edges.Any())
        //            {
        //                if (inputSlot is DynamicValueMaterialSlot)
        //                    skippedDynamicSlots.Add(inputSlot as DynamicValueMaterialSlot);
        //                continue;
        //            }

        //            // get the output details
        //            var outputSlotRef = edges[0].outputSlot;
        //            var outputNode = outputSlotRef.node;
        //            if (outputNode == null)
        //                continue;

        //            var outputSlot = outputNode.FindOutputSlot<MaterialSlot>(outputSlotRef.slotId);
        //            if (outputSlot == null)
        //                continue;

        //            if (outputSlot.hasError)
        //            {
        //                inputSlot.hasError = true;
        //                continue;
        //            }

        //            var outputConcreteType = outputSlot.concreteValueType;
        //            // dynamic input... depends on output from other node.
        //            // we need to compare ALL dynamic inputs to make sure they
        //            // are compatable.
        //            if (inputSlot is DynamicValueMaterialSlot)
        //            {
        //                dynamicInputSlotsToCompare.Add((DynamicValueMaterialSlot)inputSlot, outputConcreteType);
        //                continue;
        //            }
        //        }
        //        string output_type = GetOutputType(m_OutputSize);
        //        Debug.LogError("output size" + output_type);

        //        switch (output_type)
        //        {
        //            // As per dynamic matrix
        //            default:
        //                var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
        //                foreach (var dynamicKvP in dynamicInputSlotsToCompare)
        //                    dynamicKvP.Key.SetConcreteType(dynamicMatrixType);
        //                foreach (var skippedSlot in skippedDynamicSlots)
        //                    skippedSlot.SetConcreteType(dynamicMatrixType);
        //                break;

        //            // As per dynamic vector
        //            case "vector":
        //                var dynamicVectorType = ConvertDynamicVectorInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
        //                foreach (var dynamicKvP in dynamicInputSlotsToCompare)
        //                    dynamicKvP.Key.SetConcreteType(dynamicVectorType);
        //                foreach (var skippedSlot in skippedDynamicSlots)
        //                    skippedSlot.SetConcreteType(dynamicVectorType);
        //                break;
        //        }

        //        tempSlots.Clear();
        //        GetInputSlots(tempSlots);
        //        bool inputError = tempSlots.Any(x => x.hasError);
        //        if (inputError)
        //        {
        //            owner.AddConcretizationError(objectId, string.Format("Node {0} had input error", objectId));
        //            hasError = true;
        //        }
        //        // configure the output slots now
        //        // their slotType will either be the default output slotType
        //        // or the above dynanic slotType for dynamic nodes
        //        // or error if there is an input error
        //        tempSlots.Clear();
        //        GetOutputSlots(tempSlots);



        //        foreach (var outputSlot in tempSlots)
        //        {
        //            outputSlot.hasError = false;

        //            if (inputError)
        //            {
        //                outputSlot.hasError = true;
        //                continue;
        //            }

        //            if (outputSlot is DynamicValueMaterialSlot)
        //            {
        //                // Apply similar logic to output slot
        //                switch (output_type)
        //                {
        //                    // As per dynamic matrix
        //                    default:
        //                        var dynamicMatrixType = ConvertDynamicMatrixInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
        //                        (outputSlot as DynamicValueMaterialSlot).SetConcreteType(dynamicMatrixType);
        //                        break;

        //                    // As per dynamic vector
        //                    case "vector":
        //                        var dynamicVectorType = ConvertDynamicVectorInputTypeToConcrete(dynamicInputSlotsToCompare.Values);
        //                        (outputSlot as DynamicValueMaterialSlot).SetConcreteType(dynamicVectorType);
        //                        break;
        //                }
        //                continue;
        //            }
        //        }


        //        tempSlots.Clear();
        //        GetOutputSlots(tempSlots);
        //        if (tempSlots.Any(x => x.hasError))
        //        {
        //            owner.AddConcretizationError(objectId, string.Format("Node {0} had output error", objectId));
        //            hasError = true;
        //        }
        //    }

        //    CalculateNodeHasError();
        //    ListPool<DynamicValueMaterialSlot>.Release(skippedDynamicSlots);
        //    DictionaryPool<DynamicValueMaterialSlot, ConcreteSlotValueType>.Release(dynamicInputSlotsToCompare);
        //}
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
                        continue;
                    }
                }

                // and now dynamic matrices
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

                    if (outputSlot is DynamicVectorMaterialSlot)
                    {
                        (outputSlot as DynamicVectorMaterialSlot).SetConcreteType(dynamicType);
                        continue;
                    }
                    else if (outputSlot is DynamicMatrixMaterialSlot)
                    {
                        (outputSlot as DynamicMatrixMaterialSlot).SetConcreteType(dynamicMatrixType);
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
        private string GetOutputType (SwizzleOutputSize SwizzleSize)
        {
            string str = SwizzleSize.ToString();
            if (str.StartsWith("M"))
            {
                return "matrix";
            }
            else
            {
                return "vector";
            }
        }

    }
}
