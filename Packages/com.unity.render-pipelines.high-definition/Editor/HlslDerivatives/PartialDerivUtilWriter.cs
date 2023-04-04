using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{

    // This class manages writing partial derivatives helper funcs.
    // 1. It maintains a list of which functions need to be generated.
    // 2. When needed, it writes a list of only the function it uses. Since there
    //    the number of permuations grows exponentially (like swizzle) we need to keep
    //    the output trimmed otherwise the file size will explode.
    // 3. This class also manages writing the shader code to covert to/from the analytic
    //    and finite versions.
    class PartialDerivUtilWriter
    {

        internal static readonly int kBinaryFuncNum = (int)BinaryFunc.Num;
        internal static readonly int kSingleFuncNum = (int)SingleFunc.Num;

        internal static readonly int kFunc1Num = (int)Func1.Num;
        internal static readonly int kFunc2Num = (int)Func2.Num;
        internal static readonly int kFunc3Num = (int)Func3.Num;

        internal static readonly int kTexSampleTypeNum = (int)TexSampleType.Num;

        internal static readonly int kUvChanNum = (int)8; // is 8 correct?
        internal static readonly int kColorChanNum = (int)8; // is 8 correct?


        internal bool[] MakeBoolVecAndInitFalse(int num)
        {
            bool[] ret = new bool[num];
            System.Array.Fill(ret, false);
            return ret;
        }

        static internal string GetPrecLower(int prec)
        {
            return (prec == 1) ? "half" : "float";
        }

        static internal string GetPrecUpper(int prec)
        {
            return (prec == 1) ? "Half" : "Float";
        }


        internal PartialDerivUtilWriter()
        {

            m_extractFromApd = new BoolDim2[2];
            m_mergeToApd = new bool[2][];
            m_selectApd = new bool[2][];
            m_makeStructFromFpd = new bool[2][];
            m_makeStructFromFpdFinite = new bool[2][];
            m_splatStructFromScalar = new bool[2][];
            m_extractIndexApd = new bool[2][];
            m_insertIndexApd = new bool[2][];

            m_texSample2d = new bool[2][];
            m_texSample2dArray = new bool[2][];
            m_texSampleCube = new bool[2][];
            m_texSampleCubeArray = new bool[2][];
            m_texSample3d = new bool[2][];

            m_makeStructDirect = new bool[2][];

            m_mulMatVecApd = new bool[2][];
            m_mulVecMatApd = new bool[2][];

            m_binaryFunc = new BoolDim2[2];
            m_singleFunc = new BoolDim2[2];

            m_fetchUv = new BoolDim2[2];
            m_fetchColor = new BoolDim2[2];

            m_func1 = new BoolDim2[2];
            m_func2 = new BoolDim2[2];
            m_func3 = new BoolDim2[2];

            // 6-dimentional, but array total size is only 4096
            // type, swizzle length, 0, 1, 2, 3,
            m_swizzles = new BoolDim6[2];
            m_swizzlesAssign = new BoolDim6[2];



            for (int prec = 0; prec < 2; prec++)
            {
                m_extractFromApd[prec] = new BoolDim2(4, 4);
                m_mergeToApd[prec] = MakeBoolVecAndInitFalse(4);

                m_selectApd[prec] = MakeBoolVecAndInitFalse(4);

                m_makeStructFromFpd[prec] = MakeBoolVecAndInitFalse(4);
                m_makeStructFromFpdFinite[prec] = MakeBoolVecAndInitFalse(4);
                m_splatStructFromScalar[prec] = MakeBoolVecAndInitFalse(4);
                m_extractIndexApd[prec] = MakeBoolVecAndInitFalse(4);
                m_insertIndexApd[prec] = MakeBoolVecAndInitFalse(4);

                m_makeStructDirect[prec] = MakeBoolVecAndInitFalse(4);

                m_mulMatVecApd[prec] = MakeBoolVecAndInitFalse(4);
                m_mulVecMatApd[prec] = MakeBoolVecAndInitFalse(4);

                m_texSample2d[prec] = MakeBoolVecAndInitFalse(kTexSampleTypeNum);
                m_texSample2dArray[prec] = MakeBoolVecAndInitFalse(kTexSampleTypeNum);
                m_texSampleCube[prec] = MakeBoolVecAndInitFalse(kTexSampleTypeNum);
                m_texSampleCubeArray[prec] = MakeBoolVecAndInitFalse(kTexSampleTypeNum);
                m_texSample3d[prec] = MakeBoolVecAndInitFalse(kTexSampleTypeNum);

                m_binaryFunc[prec] = new BoolDim2(kBinaryFuncNum, 4);
                m_singleFunc[prec] = new BoolDim2(kSingleFuncNum, 4);

                m_fetchUv[prec] = new BoolDim2(kUvChanNum, 2);
                m_fetchColor[prec] = new BoolDim2(kColorChanNum, 2);

                m_func1[prec] = new BoolDim2(kFunc1Num, 4);
                m_func2[prec] = new BoolDim2(kFunc2Num, 4);
                m_func3[prec] = new BoolDim2(kFunc3Num, 4);

                // 6-dimentional, but array total size is only 4096
                // type, swizzle length, 0, 1, 2, 3,
                m_swizzles[prec] = new BoolDim6(4, 4, 4, 4, 4, 4);
                m_swizzlesAssign[prec] = new BoolDim6(4, 4, 4, 4, 4, 4);

            }



            // src is Apd, dst is Apd, src is float, dst is float, src len, dst len
            m_implicitCast = new BoolDim6(2, 2, 2, 2, 4, 4);
        }

        static bool IsEitherOfStatus(ApdStatus lhs, ApdStatus rhs, ApdStatus status)
        {
            return (lhs == status) || (rhs == status);
        }

        static string[] GetSuffixUpper()
        {
            string[] ret = new string[4] { "X", "Y", "Z", "W" };
            return ret;
        }

        static string[] GetSuffixLower()
        {
            string[] ret = new string[4] { "x", "y", "z", "w" };
            return ret;
        }

        static string[] GetSuffix(bool isUpper)
        {
            string[] ret = isUpper ? GetSuffixUpper() : GetSuffixLower();
            return ret;
        }

        static string GetSwizzleName(int numChar, int i0, int i1, int i2, int i3, bool caps)
        {
            string[] suffixVec = GetSuffix(caps);

            int[] list = new int[4]{ i0, i1, i2, i3 };

            HlslUtil.ParserAssert(numChar > 0);

            string ret = "";
            for (int i = 0; i < numChar; i++)
            {
                int coord = list[i];
                ret += suffixVec[coord];
            }

            return ret;
        }




        // rules:
        // 1. If either is unknown or invalid, result is invalid.
        // 2. If both not needed, result is not needed. But if just one is not needed then the result is invalid.
        // 3. If both are zero, result is zero.
        // 4. If both are valid, or one is valid and the other is zero, then the result is valid.
        // 5. That should be all the cases.
        static internal ApdStatus MergeStatus(ApdStatus lhs, ApdStatus rhs)
        {
            if (IsEitherOfStatus(lhs,rhs,ApdStatus.Invalid))
            {
                return ApdStatus.Invalid;
            }

            if (IsEitherOfStatus(lhs,rhs,ApdStatus.Unknown))
            {
                return ApdStatus.Invalid;
            }

            if (IsEitherOfStatus(lhs,rhs,ApdStatus.NotNeeded))
            {
                if (lhs == ApdStatus.NotNeeded && rhs == ApdStatus.NotNeeded)
                {
                    return ApdStatus.NotNeeded;
                }
                else
                {
                    return ApdStatus.Invalid;
                }
            }

            // only options left are zero or valid
            if (lhs == ApdStatus.Zero && rhs == ApdStatus.Zero)
            {
                return ApdStatus.Zero;
            }

            if (IsEitherOfStatus(lhs,rhs,ApdStatus.Valid))
            {
                HlslUtil.ParserAssert(lhs == ApdStatus.Valid || lhs == ApdStatus.Zero);
                HlslUtil.ParserAssert(rhs == ApdStatus.Valid || rhs == ApdStatus.Zero);
                return ApdStatus.Valid;
            }

            HlslUtil.ParserAssert(false);
            return ApdStatus.Invalid;
        }

        internal static string GetDeclName(int index, ApdStatus apdStatus, ConcretePrecision precision)
        {
            string ret = "";
            if (apdStatus == ApdStatus.Valid)
            {
                ret = GetApdStructName(index,precision);
            }
            else
            {
                ret = GetApdBaseName(index, precision);
            }
            return ret;
        }


        internal static string GetApdStructName(int index, ConcretePrecision prec)
        {
            switch (index)
            {
                case 0:
                    return (prec == ConcretePrecision.Single) ? "FloatApd" : "HalfApd";
                case 1:
                    return (prec == ConcretePrecision.Single) ? "FloatApd2" : "HalfApd2";
                case 2:
                    return (prec == ConcretePrecision.Single) ? "FloatApd3" : "HalfApd3";
                case 3:
                    return (prec == ConcretePrecision.Single) ? "FloatApd4" : "HalfApd4";
            }
            HlslUtil.ParserAssert(false);
            return "Invalid";
        }

        internal static string GetPrecisionName(ConcretePrecision precision)
        {
            string ret;
            switch (precision)
            {
                case ConcretePrecision.Half:
                    ret = "half";
                    break;
                case ConcretePrecision.Single:
                    ret = "float";
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    ret = "<invalid>";
                    break;
            }
            return ret;
        }

        static bool IsVectorType(ConcreteSlotValueType type)
        {
            bool ret = false;

            switch (type)
            {
                case ConcreteSlotValueType.Vector1:
                case ConcreteSlotValueType.Vector2:
                case ConcreteSlotValueType.Vector3:
                case ConcreteSlotValueType.Vector4:
                    ret = true;
                    break;
                default:
                    ret = false;
                    break;
            }
            return ret;

        }


        static ConcreteSlotValueType ParamTypeFromIndex(int index)
        {
            ConcreteSlotValueType ret = ConcreteSlotValueType.Vector1;

            switch (index)
            {
                case 0:
                    ret = ConcreteSlotValueType.Vector1;
                    break;
                case 1:
                    ret = ConcreteSlotValueType.Vector2;
                    break;
                case 2:
                    ret = ConcreteSlotValueType.Vector3;
                    break;
                case 3:
                    ret = ConcreteSlotValueType.Vector4;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }
            return ret;
        }

        static int IndexFromParamType(ConcreteSlotValueType type)
        {
            int ret = -1;

            switch (type)
            {
                case ConcreteSlotValueType.Vector1:
                    ret = 0;
                    break;
                case ConcreteSlotValueType.Vector2:
                    ret = 1;
                    break;
                case ConcreteSlotValueType.Vector3:
                    ret = 2;
                    break;
                case ConcreteSlotValueType.Vector4:
                    ret = 3;
                    break;
                default:
                    ret = -1;
                    break;
            }
            return ret;
        }


        static string GetApdBaseName(int index, ConcretePrecision precision)
        {
            string precName = GetPrecisionName(precision);
            switch (index)
            {
                case 0:
                    return precName;
                case 1:
                    return precName + "2";
                case 2:
                    return precName + "3";
                case 3:
                    return precName + "4";
            }
            HlslUtil.ParserAssert(false);
            return "(invalid)";
        }

        static string GetApdMatrixName(int index, ConcretePrecision precision)
        {
            string precName = GetPrecisionName(precision);
            switch (index)
            {
                case 0:
                    return precName;
                case 1:
                    return precName + "2x2";
                case 2:
                    return precName + "3x3";
                case 3:
                    return precName + "4x4";
            }
            HlslUtil.ParserAssert(false);
            return "(invalid)";
        }

        private static string GetImplicitCastFuncName(int dstApdVal, int srcApdVal, int dstPrecisionVal, int srcPrecisionVal, int dstChanVal, int srcChanVal)
        {
            HlslUtil.ParserAssert(srcChanVal >= 0);
            HlslUtil.ParserAssert(dstChanVal >= 0);

            HlslUtil.ParserAssert(dstApdVal < 2);
            HlslUtil.ParserAssert(srcApdVal < 2);
            HlslUtil.ParserAssert(dstPrecisionVal < 2);
            HlslUtil.ParserAssert(srcPrecisionVal < 2);
            HlslUtil.ParserAssert(dstChanVal < 4);
            HlslUtil.ParserAssert(srcChanVal < 4);

            string dstApdName = (dstApdVal == 0) ? "Fpd" : "Apd";
            string srcApdName = (srcApdVal == 0) ? "Fpd" : "Apd";
            string dstPrecName = dstPrecisionVal == 0 ? "Half" : "Float";
            string srcPrecName = srcPrecisionVal == 0 ? "Half" : "Float";
            string dstChanName = (dstChanVal + 1).ToString();
            string srcChanName = (srcChanVal + 1).ToString();

            string ret = "ImplicitCast" + dstPrecName + dstApdName + dstChanName + "From" + srcPrecName + srcApdName + srcChanName;

            return ret;
        }

        private static void WriteImplicitCastVariationHelper(ShaderStringBuilder builder, int dstApd, int srcApd, int dstPrecision, int srcPrecision, int dstChan, int srcChan)
        {
            string srcStructName = GetApdStructName(srcChan,(ConcretePrecision)srcPrecision);
            string dstStructName = GetApdStructName(dstChan,(ConcretePrecision)dstPrecision);

            ConcretePrecision srcPrec = srcPrecision == 0 ? ConcretePrecision.Half : ConcretePrecision.Single;
            ConcretePrecision dstPrec = dstPrecision == 0 ? ConcretePrecision.Half : ConcretePrecision.Single;

            string srcBaseName = GetApdBaseName(srcChan, srcPrec);
            string dstBaseName = GetApdBaseName(dstChan, dstPrec);

            string funcName = GetImplicitCastFuncName(dstApd, srcApd, dstPrecision, srcPrecision, dstChan, srcChan);

            ApdStatus srcApdStatus = (srcApd == 0) ? ApdStatus.Zero : ApdStatus.Valid;
            ApdStatus dstApdStatus = (dstApd == 0) ? ApdStatus.Zero : ApdStatus.Valid;
            string srcDeclName = GetDeclName(srcChan, srcApdStatus, srcPrec);
            string dstDeclName = GetDeclName(dstChan, dstApdStatus, dstPrec);


            ConcreteSlotValueType srcType = ParamTypeFromIndex(srcChan);
            ConcreteSlotValueType dstType = ParamTypeFromIndex(dstChan);

            builder.AppendLine("{0} {1}({2} src)", dstDeclName, funcName, srcDeclName);
            builder.AppendLine("{");
            builder.IncreaseIndent();
            builder.AppendLine("{0} ret;", dstDeclName);

            string valSuffix = (srcApd == 1) ? ".m_val" : "";

            string valLine = ConvertBaseVariable(dstType, dstPrec, srcType, srcPrec, "src" + valSuffix);

            if (dstApdStatus == ApdStatus.Valid)
            {
                string ddxLine;
                string ddyLine;
                if (srcApdStatus == ApdStatus.Valid)
                {
                    ddxLine = ConvertBaseVariable(dstType, dstPrec, srcType, srcPrec, "src.m_ddx");
                    ddyLine = ConvertBaseVariable(dstType, dstPrec, srcType, srcPrec, "src.m_ddy");
                }
                else
                {
                    ddxLine = "0.0";
                    ddyLine = "0.0";
                }

                builder.AppendLine("ret.m_val = {0};", valLine);
                builder.AppendLine("ret.m_ddx = {0};", ddxLine);
                builder.AppendLine("ret.m_ddy = {0};", ddyLine);
            }
            else
            {
                builder.AppendLine("ret = {0};", valLine);
            }
            builder.AppendLine("return ret;");
            builder.DecreaseIndent();
            builder.AppendLine("}");
            builder.AppendLine("");

        }



        internal void ForceGenerateAllDefinitionsAndFuncs()
        {
            for (int prec = 0; prec < 2; prec++)
            {

                for (int i = 0; i < 4; i++)
                {
                    for (int funcIter = 0; funcIter < kBinaryFuncNum; funcIter++)
                    {
                        m_binaryFunc[prec].Set(funcIter, i, true);
                    }

                    for (int funcIter = 0; funcIter < kSingleFuncNum; funcIter++)
                    {
                        m_singleFunc[prec].Set(funcIter, i, true);
                    }

                    for (int funcIter = 0; funcIter < kFunc1Num; funcIter++)
                    {
                        m_func1[prec].Set(funcIter, i, true);
                    }

                    for (int funcIter = 0; funcIter < kFunc2Num; funcIter++)
                    {
                        if (funcIter == (int)Func2.Dot)
                        {
                            m_func2[prec].Set(funcIter, i, true);
                        }
                    }

                    m_func2[prec].Set((int)Func2.Cross, 2, true);

                    for (int funcIter = 0; funcIter < kFunc3Num; funcIter++)
                    {
                        m_func3[prec].Set(funcIter, i, true);
                    }
                }

                for (int i = 0; i < 4; i++)
                {

                    m_makeStructFromFpd[prec][i] = true;
                    m_makeStructFromFpdFinite[prec][i] = true;
                    m_makeStructDirect[prec][i] = true;

                    m_mulMatVecApd[prec][i] = true;
                    m_mulVecMatApd[prec][i] = true;

                    m_splatStructFromScalar[prec][i] = true;
                    m_extractIndexApd[prec][i] = true;
                    m_insertIndexApd[prec][i] = true;

                    if (i >= 1)
                    {
                        m_mergeToApd[prec][i] = true;
                    }

                    m_selectApd[prec][i] = true;

                    for (int funcIter = 0; funcIter < kTexSampleTypeNum; funcIter++)
                    {
                        m_texSample2d[prec][funcIter] = true;
                        m_texSample2dArray[prec][funcIter] = true;
                        m_texSampleCube[prec][funcIter] = true;
                        m_texSampleCubeArray[prec][funcIter] = true;
                        m_texSample3d[prec][funcIter] = true;
                    }



                    for (int j = 0; j < 4; j++)
                    {
                        if (j <= i)
                        {
                            m_extractFromApd[prec].Set(i, j, true);
                        }
                    }
                }

                // generate one random swizzle from each combination of vector size and swizzle length
                for (int type = 0; type < 4; type++)
                {
                    for (int swizzleLen = 0; swizzleLen < 4; swizzleLen++)
                    {
                        int[] randomVals = new int[4] { 72, 173, 34, 91 };

                        int[] vals = new int[4];
                        for (int i = 0; i < 4; i++)
                        {
                            // Pick a random value between [0,type]. It's just for debugging,
                            // so arbitrarily choosing the 4 seeds is fine. Multiply swizzleLen and
                            // type to add a little variety.
                            vals[i] = (randomVals[i] + 7 * swizzleLen + 13 * type) % (type + 1);
                        }

                        m_swizzles[prec].Set(type, swizzleLen, vals[0], vals[1], vals[2], vals[3], true);
                        m_swizzlesAssign[prec].Set(type, swizzleLen, vals[0], vals[1], vals[2], vals[3], true);
                    }
                }
            }

            for (int dstApd = 0; dstApd < 2; dstApd++)
            {
                for (int srcApd = 0; srcApd < 2; srcApd++)
                {
                    for (int dstPrecision = 0; dstPrecision < 2; dstPrecision++)
                    {
                        for (int srcPrecision = 0; srcPrecision < 2; srcPrecision++)
                        {
                            for (int dstChan = 0; dstChan < 4; dstChan++)
                            {
                                for (int srcChan = 0; srcChan < 4; srcChan++)
                                {
                                    m_implicitCast.Set(dstApd, srcApd, dstPrecision, srcPrecision, dstChan, srcChan, true);
                                }
                            }
                        }
                    }
                }
            }

            for (int prec = 0; prec < 2; prec++)
            {

                for (int i = 0; i < kUvChanNum; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        m_fetchUv[prec].Set(i, j, true);
                    }
                }

                for (int i = 0; i < kColorChanNum; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        m_fetchColor[prec].Set(i, j, true);
                    }
                }
            }


        }

        internal void GenerateDefinitionsAndFuncs(ShaderStringBuilder builder)
        {
            builder.AppendLine("");

            for (int prec = 0; prec < 2; prec++)
            {
                string precUpper = GetPrecUpper(prec);
                string precLower = GetPrecLower(prec);

                // generate base structs
                for (int i = 0; i < 4; i++)
                {
                    string structName = GetApdStructName(i,(ConcretePrecision)prec);
                    string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                    builder.AppendLine("struct {0}", structName);
                    builder.AppendLine("{");
                    builder.IncreaseIndent();
                    builder.AppendLine("{0} m_val;", baseName);
                    builder.AppendLine("{0} m_ddx;", baseName);
                    builder.AppendLine("{0} m_ddy;", baseName);
                    builder.DecreaseIndent();
                    builder.AppendLine("};");
                    builder.AppendLine("");
                }

                // note that apd funcs only use floats (not halfs)
                for (int i = 0; i < 4; i++)
                {
                    string structName = GetApdStructName(i,(ConcretePrecision)prec);
                    string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                    string boolName = "bool" + (i + 1).ToString();

                    builder.AppendLine("{0} SelectHelper({1} cond, {0} lhs, {0} rhs)", baseName, boolName);
                    builder.AppendLine("{");
                    builder.IncreaseIndent();

                    if (i == 0)
                    {
                        builder.AppendLine("return cond ? lhs : rhs;");
                    }
                    else
                    {
                        builder.AppendLine("{0} ret = 0.0f;", baseName);

                        for (int j = 0; j <= i; j++)
                        {
                            string suffix = GetSuffixLower()[j];
                            builder.AppendLine("ret.{0} = cond.{0} ? lhs.{0} : rhs.{0};", suffix);
                        }

                        builder.AppendLine("return ret;");
                    }

                    builder.DecreaseIndent();

                    builder.AppendLine("};");
                    builder.AppendLine("");
                }


                for (int i = 0; i < 4; i++)
                {
                    if (m_makeStructFromFpd[prec][i])
                    {
                        string structName = GetApdStructName(i,(ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        builder.AppendLine("{0} Make{0}({1} src)",structName,baseName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", structName);
                        builder.AppendLine("ret.m_val = src;");
                        builder.AppendLine("ret.m_ddx = 0.0f;");
                        builder.AppendLine("ret.m_ddy = 0.0f;");
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (m_makeStructFromFpdFinite[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        builder.AppendLine("{0} Make{0}Finite({1} src)", structName, baseName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", structName);
                        builder.AppendLine("ret.m_val = src;");
                        builder.AppendLine("ret.m_ddx = ddx(src);");
                        builder.AppendLine("ret.m_ddy = ddy(src);");
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (m_makeStructDirect[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        builder.AppendLine("{0} Make{0}Direct({1} val, {1} derivX, {1} derivY)", structName, baseName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", structName);
                        builder.AppendLine("ret.m_val = val;");
                        builder.AppendLine("ret.m_ddx = derivX;");
                        builder.AppendLine("ret.m_ddy = derivY;");
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                //m_mulMatVecApd[prec][i] = true;
                //m_mulVecMatApd[prec][i] = true;

                for (int i = 0; i < 4; i++)
                {
                    if (m_mulMatVecApd[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);
                        string matrixName = GetApdMatrixName(i, (ConcretePrecision)prec);

                        builder.AppendLine("{0} MulMatVec{0}({1} lhs, {0} rhs)", structName, matrixName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", structName);
                        builder.AppendLine("ret.m_val = mul(lhs,rhs.m_val);");
                        builder.AppendLine("ret.m_ddx = mul(lhs,rhs.m_ddx);");
                        builder.AppendLine("ret.m_ddy = mul(lhs,rhs.m_ddy);");
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (m_mulVecMatApd[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);
                        string matrixName = GetApdMatrixName(i, (ConcretePrecision)prec);

                        builder.AppendLine("{0} MulVecMat{0}({0} lhs, {1} rhs)", structName, matrixName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", structName);
                        builder.AppendLine("ret.m_val = mul(lhs.m_val,rhs);");
                        builder.AppendLine("ret.m_ddx = mul(lhs.m_ddx,rhs);");
                        builder.AppendLine("ret.m_ddy = mul(lhs.m_ddy,rhs);");
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }



                for (int i = 0; i < 4; i++)
                {
                    if (m_selectApd[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);
                        string precName = GetPrecisionName((ConcretePrecision)prec);

                        builder.AppendLine("{0} Make{0}Select({1} conditional, {2} lhs, {2} rhs)", structName, precName, structName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", structName);
                        builder.AppendLine("bool side = (conditional != 0);");
                        builder.AppendLine("ret.m_val = side ? lhs.m_val : rhs.m_val;");
                        builder.AppendLine("ret.m_ddx = side ? lhs.m_ddx : rhs.m_ddx;");
                        builder.AppendLine("ret.m_ddy = side ? lhs.m_ddy : rhs.m_ddy;");
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }

                }

                for (int i = 0; i < 4; i++)
                {
                    if (m_mergeToApd[prec][i])
                    {
                        HlslUtil.ParserAssert(i >= 0);

                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        string startLine = structName + " Merge" + structName + "(";
                        int numParam = i + 1;
                        for (int iter = 0; iter < numParam; iter++)
                        {
                            if (iter != 0)
                            {
                                startLine += ", ";
                            }
                            startLine += precUpper + "Apd param" + iter.ToString();
                        }
                        startLine += ")";

                        builder.AppendLine(startLine);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();

                        builder.AppendLine("{0} ret;", structName);

                        string dstVal = baseName + "(";
                        string dstDdx = baseName + "(";
                        string dstDdy = baseName + "(";
                        for (int iter = 0; iter < numParam; iter++)
                        {
                            if (iter != 0)
                            {
                                dstVal += ",";
                                dstDdx += ",";
                                dstDdy += ",";
                            }
                            string param = "param" + iter.ToString();
                            dstVal += param + ".m_val";
                            dstDdx += param + ".m_ddx";
                            dstDdy += param + ".m_ddy";
                        }
                        dstVal += ")";
                        dstDdx += ")";
                        dstDdy += ")";

                        builder.AppendLine("ret.m_val = {0};",dstVal);
                        builder.AppendLine("ret.m_ddx = {0};", dstDdx);
                        builder.AppendLine("ret.m_ddy = {0};", dstDdy);
                        builder.AppendLine("return ret;");

                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                for (int iterType = 0; iterType < 4; iterType++)
                {
                    for (int iterIndex = 0; iterIndex < 4; iterIndex++)
                    {
                        if (m_extractFromApd[prec].Get(iterType, iterIndex))
                        {
                            HlslUtil.ParserAssert(iterIndex <= iterType);

                            string structName = GetApdStructName(iterType, (ConcretePrecision)prec);
                            string baseName = GetApdBaseName(iterType, (ConcretePrecision)prec);

                            string suffix = GetSuffixLower()[iterIndex];

                            builder.AppendLine("{0}Apd Extract{1}_{2}({3} src)", precUpper, structName, iterIndex.ToString(), structName);
                            builder.AppendLine("{");
                            builder.IncreaseIndent();
                            builder.AppendLine("{0}Apd ret;", precUpper);
                            builder.AppendLine("ret.m_val = src.m_val.{0};", suffix);
                            builder.AppendLine("ret.m_ddx = src.m_ddx.{0};", suffix);
                            builder.AppendLine("ret.m_ddy = src.m_ddy.{0};", suffix);
                            builder.AppendLine("return ret;");
                            builder.DecreaseIndent();
                            builder.AppendLine("}");
                            builder.AppendLine("");
                        }
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (m_splatStructFromScalar[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        builder.AppendLine("{0} Splat{1}({2}Apd src)", structName, structName,precUpper);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", structName);
                        builder.AppendLine("ret.m_val = ({0})src.m_val;", baseName);
                        builder.AppendLine("ret.m_ddx = ({0})src.m_ddx;", baseName);
                        builder.AppendLine("ret.m_ddy = ({0})src.m_ddy;", baseName);
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (m_extractIndexApd[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string scalarName = GetApdStructName(0, (ConcretePrecision)prec);

                        // lhs is the vector to insert, index is where to pus it, and rhs is a one channel apd
                        builder.AppendLine("{1} Extract{0}(inout {0} lhs, int index)", structName, scalarName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("{0} ret;", scalarName);
                        builder.AppendLine("ret.m_val = lhs.m_val[index];");
                        builder.AppendLine("ret.m_ddx = lhs.m_ddx[index];");
                        builder.AppendLine("ret.m_ddy = lhs.m_ddy[index];");
                        builder.AppendLine("return ret;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (m_insertIndexApd[prec][i])
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string scalarName = GetApdStructName(0, (ConcretePrecision)prec);

                        // lhs is the vector to insert, index is where to pus it, and rhs is a one channel apd
                        builder.AppendLine("void Insert{0}(inout {0} lhs, int index, {1} rhs)", structName, scalarName);
                        builder.AppendLine("{");
                        builder.IncreaseIndent();
                        builder.AppendLine("lhs.m_val[index] = rhs.m_val;");
                        builder.AppendLine("lhs.m_ddx[index] = rhs.m_ddx;");
                        builder.AppendLine("lhs.m_ddy[index] = rhs.m_ddy;");
                        builder.DecreaseIndent();
                        builder.AppendLine("}");
                        builder.AppendLine("");
                    }
                }

                for (int type = 0; type < 4; type++)
                {
                    for (int len = 0; len < 4; len++)
                    {
                        int numChar = len + 1;
                        for (int baseI = 0; baseI < 4*4*4*4; baseI++)
                        {
                            int i0 = (baseI>>0)%4;
                            int i1 = (baseI>>2)%4;
                            int i2 = (baseI>>4)%4;
                            int i3 = (baseI>>6)%4;

                            if (m_swizzles[prec].Get(type,len,i0,i1,i2,i3))
                            {
                                HlslUtil.ParserAssert(i0 <= type);
                                HlslUtil.ParserAssert(i1 <= type);
                                HlslUtil.ParserAssert(i2 <= type);
                                HlslUtil.ParserAssert(i3 <= type);


                                string srcStructName = GetApdStructName(type, (ConcretePrecision)prec);
                                string dstStructName = GetApdStructName(len, (ConcretePrecision)prec);

                                string srcBaseName = GetApdBaseName(type, (ConcretePrecision)prec);
                                string dstBaseName = GetApdBaseName(len, (ConcretePrecision)prec);

                                string capsName = GetSwizzleName(numChar,i0,i1,i2,i3,true);
                                string lowerName = GetSwizzleName(numChar,i0,i1,i2,i3,false);

                                builder.AppendLine("{0} Swizzle{1}_{2}({1} src)",dstStructName,srcStructName,capsName);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("{0} ret;",dstStructName);
                                builder.AppendLine("ret.m_val = src.m_val.{0};",lowerName);
                                builder.AppendLine("ret.m_ddx = src.m_ddx.{0};",lowerName);
                                builder.AppendLine("ret.m_ddy = src.m_ddy.{0};",lowerName);
                                builder.AppendLine("return ret;");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                            }

                            if (m_swizzlesAssign[prec].Get(type, len, i0, i1, i2, i3))
                            {
                                HlslUtil.ParserAssert(i0 <= type);
                                HlslUtil.ParserAssert(i1 <= type);
                                HlslUtil.ParserAssert(i2 <= type);
                                HlslUtil.ParserAssert(i3 <= type);


                                string lhsStructName = GetApdStructName(type, (ConcretePrecision)prec);
                                string rhsStructName = GetApdStructName(len, (ConcretePrecision)prec);

                                string lhsBaseName = GetApdBaseName(type, (ConcretePrecision)prec);
                                string rhsBaseName = GetApdBaseName(len, (ConcretePrecision)prec);

                                string capsName = GetSwizzleName(numChar, i0, i1, i2, i3, true);
                                string lowerName = GetSwizzleName(numChar, i0, i1, i2, i3, false);

                                builder.AppendLine("{1} SwizzleAssign{0}_{2}(inout {0} lhs, {1} rhs)", lhsStructName, rhsStructName, capsName);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("lhs.m_val.{0} = rhs.m_val;", lowerName);
                                builder.AppendLine("lhs.m_ddx.{0} = rhs.m_ddx;", lowerName);
                                builder.AppendLine("lhs.m_ddy.{0} = rhs.m_ddy;", lowerName);
                                builder.AppendLine("{0} ret;", rhsStructName);
                                builder.AppendLine("ret.m_val = lhs.m_val.{0};", lowerName);
                                builder.AppendLine("ret.m_ddx = lhs.m_ddx.{0};", lowerName);
                                builder.AppendLine("ret.m_ddy = lhs.m_ddy.{0};", lowerName);
                                builder.AppendLine("return ret;");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                            }

                        }
                    }
                }

            }

            // generate APD version of GetTransformedUV()
            {
                builder.AppendLine("FloatApd2 GetTransformedUVApd(UnityTexture2D unityTex, FloatApd2 uv)");
                builder.AppendLine("{");
                builder.IncreaseIndent();
                {
                    builder.AppendLine("FloatApd2 ret;");
                    builder.AppendLine("ret.m_val = uv.m_val * unityTex.scaleTranslate.xy + unityTex.scaleTranslate.zw;");
                    builder.AppendLine("ret.m_ddx = uv.m_ddx * unityTex.scaleTranslate.xy;");
                    builder.AppendLine("ret.m_ddy = uv.m_ddy * unityTex.scaleTranslate.xy;");
                    builder.AppendLine("return ret;");
                }
                builder.DecreaseIndent();
                builder.AppendLine("}");
            }

            for (int dstApd = 0; dstApd < 2; dstApd++)
            {
                for (int srcApd = 0; srcApd < 2; srcApd++)
                {
                    for (int dstPrecision = 0; dstPrecision < 2; dstPrecision++)
                    {
                        for (int srcPrecision = 0; srcPrecision < 2; srcPrecision++)
                        {
                            for (int dstChan = 0; dstChan < 4; dstChan++)
                            {
                                for (int srcChan = 0; srcChan < 4; srcChan++)
                                {
                                    if (m_implicitCast.Get(dstApd, srcApd, dstPrecision, srcPrecision, dstChan, srcChan))
                                    {
                                        WriteImplicitCastVariationHelper(builder,dstApd, srcApd, dstPrecision, srcPrecision, dstChan, srcChan);
                                    }
                                }
                            }
                        }
                    }
                }
            }


            for (int prec = 0; prec < 2; prec++)
            {
                string precUpper = GetPrecUpper(prec);
                string precLower = GetPrecLower(prec);


                for (int funcIter = 0; funcIter < kSingleFuncNum; funcIter++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);
                        string boolName = "bool" + (i + 1).ToString();

                        if (m_singleFunc[prec].Get(funcIter, i))
                        {
                            switch ((SingleFunc)funcIter)
                            {
                                case SingleFunc.Saturate:
                                    builder.AppendLine("{0} SaturateApd({1} src)", structName, structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} zero = 0.0f;", baseName);
                                    builder.AppendLine("{0} one  = 1.0f;", baseName);
                                    builder.AppendLine("{0} cond = (zero <= src.m_val && src.m_val <= one);", boolName);
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = saturate(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = SelectHelper(cond,src.m_ddx,zero);");
                                    builder.AppendLine("ret.m_ddy = SelectHelper(cond,src.m_ddy,zero);");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();

                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Frac:
                                    builder.AppendLine("{0} FracApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = frac(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Abs:
                                    builder.AppendLine("{0} AbsApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("float scale = (src.m_val >= 0) ? 1.0f : -1.0f;");
                                    builder.AppendLine("ret.m_val = src.m_val * scale;");
                                    builder.AppendLine("ret.m_ddx = src.m_ddx * scale;");
                                    builder.AppendLine("ret.m_ddy = src.m_ddy * scale;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Negate:
                                    builder.AppendLine("{0} NegateApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = -src.m_val;");
                                    builder.AppendLine("ret.m_ddx = -src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = -src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Ceil:
                                    builder.AppendLine("{0} CeilApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = ceil(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = 0.0;");
                                    builder.AppendLine("ret.m_ddy = 0.0;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Floor:
                                    builder.AppendLine("{0} FloorApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = floor(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = 0.0f;");
                                    builder.AppendLine("ret.m_ddy = 0.0f;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Cos:
                                    builder.AppendLine("{0} CosApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = cos(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = -sin(src.m_val)*src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = -sin(src.m_val)*src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.CosH:
                                    builder.AppendLine("{0} CosHApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = cosh(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = sinh(src.m_ddx)*src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = sinh(src.m_ddy)*src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Sin:
                                    builder.AppendLine("{0} SinApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = sin(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = cos(src.m_val)*src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = cos(src.m_val)*src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.SinH:
                                    builder.AppendLine("{0} SinHApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = sinh(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = cosh(src.m_val)*src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = cosh(src.m_val)*src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Tan:
                                    builder.AppendLine("{0} TanApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("float scale = cos(src.m_val);", structName);
                                    builder.AppendLine("ret.m_val = tan(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = src.m_ddx/(scale * scale);");
                                    builder.AppendLine("ret.m_ddy = src.m_ddy/(scale * scale);");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.TanH:
                                    builder.AppendLine("{0} TanHApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("float scale = 1-tanh(src.m_val)*tanh(src.m_val);", structName);
                                    builder.AppendLine("ret.m_val = tanh(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = scale*src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = scale*src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Log:
                                    builder.AppendLine("{0} LogApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = log(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = src.m_ddx/src.m_val;");
                                    builder.AppendLine("ret.m_ddy = src.m_ddy/src.m_val;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Log2:
                                    builder.AppendLine("{0} Log2Apd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("float logScale = log(2.0f); // natural log of 2.0");
                                    builder.AppendLine("ret.m_val = log2(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = src.m_ddx/(src.m_val*logScale);");
                                    builder.AppendLine("ret.m_ddy = src.m_ddy/(src.m_val*logScale);");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Log10:
                                    builder.AppendLine("{0} Log10Apd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("float logScale = log(10.0f); // natural log of 10.0");
                                    builder.AppendLine("ret.m_val = log10(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = src.m_ddx/(src.m_val*logScale);");
                                    builder.AppendLine("ret.m_ddy = src.m_ddy/(src.m_val*logScale);");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Exp:
                                    builder.AppendLine("{0} ExpApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = exp(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = exp(src.m_val) * src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = exp(src.m_val) * src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Exp2:
                                    builder.AppendLine("{0} Exp2Apd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("float logScale = log(2.0);");
                                    builder.AppendLine("ret.m_val = exp2(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = exp(src.m_val) * src.m_ddx * logScale;");
                                    builder.AppendLine("ret.m_ddy = exp(src.m_val) * src.m_ddy * logScale;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Rcp:
                                    builder.AppendLine("{0} RcpApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = rcp(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = (-src.m_ddx) * ret.m_val * ret.m_val;");
                                    builder.AppendLine("ret.m_ddy = (-src.m_ddy) * ret.m_val * ret.m_val;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Sqrt:
                                    builder.AppendLine("{0} SqrtApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = sqrt(src.m_val);");
                                    builder.AppendLine("ret.m_ddx = rsqrt(src.m_val) * src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = rsqrt(src.m_val) * src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Rsqrt:
                                    HlslUtil.ParserAssert(m_singleFunc[prec].Get((int)SingleFunc.Sqrt, i));
                                    HlslUtil.ParserAssert(m_singleFunc[prec].Get((int)SingleFunc.Rcp, i));
                                    builder.AppendLine("{0} RsqrtApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret = RcpApd(SqrtApd(src));", structName);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case SingleFunc.Sqr:
                                    builder.AppendLine("{0} SqrApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = src.m_val * src.m_val;");
                                    builder.AppendLine("ret.m_ddx = 2.0f * (src.m_val) * src.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = 2.0f * (src.m_val) * src.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                // handle these funcs later
                                case SingleFunc.Normalize:
                                    break;

                                default:
                                    HlslUtil.ParserAssert(false);
                                    break;
                            }
                        }
                    }
                }


                for (int funcIter = 0; funcIter < kBinaryFuncNum; funcIter++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        if (m_binaryFunc[prec].Get(funcIter, i))
                        {
                            switch ((BinaryFunc)funcIter)
                            {
                                case BinaryFunc.Add:
                                    builder.AppendLine("{0} AddApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = lhs.m_val + rhs.m_val;");
                                    builder.AppendLine("ret.m_ddx = lhs.m_ddx + rhs.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = lhs.m_ddy + rhs.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case BinaryFunc.Sub:
                                    builder.AppendLine("{0} SubApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = lhs.m_val - rhs.m_val;");
                                    builder.AppendLine("ret.m_ddx = lhs.m_ddx - rhs.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = lhs.m_ddy - rhs.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case BinaryFunc.Mul:
                                    builder.AppendLine("{0} MulApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = lhs.m_val * rhs.m_val;");
                                    builder.AppendLine("ret.m_ddx = lhs.m_val * rhs.m_ddx + lhs.m_ddx * rhs.m_val;");
                                    builder.AppendLine("ret.m_ddy = lhs.m_val * rhs.m_ddy + lhs.m_ddy * rhs.m_val;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case BinaryFunc.Div:
                                    builder.AppendLine("{0} DivApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = lhs.m_val / rhs.m_val;");
                                    builder.AppendLine("ret.m_ddx = (lhs.m_val * rhs.m_ddx + lhs.m_ddx * rhs.m_val) / (rhs.m_val * rhs.m_val);");
                                    builder.AppendLine("ret.m_ddy = (lhs.m_val * rhs.m_ddy + lhs.m_ddy * rhs.m_val) / (rhs.m_val * rhs.m_val);");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case BinaryFunc.Min:
                                    builder.AppendLine("{0} MinApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = min(lhs.m_val,rhs.m_val);");
                                    builder.AppendLine("ret.m_ddx = lhs.m_val < rhs.m_val ? lhs.m_ddx : rhs.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = lhs.m_val < rhs.m_val ? lhs.m_ddy : rhs.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case BinaryFunc.Max:
                                    builder.AppendLine("{0} MaxApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = max(lhs.m_val,rhs.m_val);");
                                    builder.AppendLine("ret.m_ddx = lhs.m_val > rhs.m_val ? lhs.m_ddx : rhs.m_ddx;");
                                    builder.AppendLine("ret.m_ddy = lhs.m_val > rhs.m_val ? lhs.m_ddy : rhs.m_ddy;");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case BinaryFunc.Pow:
                                    // main:
                                    // pow(x,y) = exp(y*ln(x))
                                    //
                                    // ingredients:
                                    // exp'(y) = exp(y)*y'
                                    // log'(y) = y'/y
                                    //
                                    // chain rule:
                                    // dz(pow(x,y)) =  dz(exp(y*ln(x)))
                                    //              = exp'( (y*ln(x)) ) * dz(y*ln(x))
                                    //              = exp'( (y*ln(x)) ) * (dy * ln(x) + y * ln'(x) )
                                    //              = exp ( (y*ln(x)) ) * (dy * ln(x) + y * dx/x)
                                    //
                                    builder.AppendLine("{0} PowApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} ret;", structName);
                                    builder.AppendLine("ret.m_val = pow(lhs.m_val,rhs.m_val);");
                                    builder.AppendLine("{0} logLhs = log(lhs.m_val);", baseName);
                                    builder.AppendLine("{0} temp = exp(rhs.m_val * logLhs);", baseName); // this line is actually just pow(x,y)
                                    builder.AppendLine("ret.m_ddx = temp * (rhs.m_ddx * logLhs + rhs.m_val * (lhs.m_ddx/lhs.m_val));");
                                    builder.AppendLine("ret.m_ddy = temp * (rhs.m_ddy * logLhs + rhs.m_val * (lhs.m_ddy/lhs.m_val));");
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case BinaryFunc.Reflect:
                                    HlslUtil.ParserAssert(m_func2[prec].Get((int)Func2.Dot, i));
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Add, 0));
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Mul, 0));
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Sub, 0));
                                    HlslUtil.ParserAssert(m_mergeToApd[prec][i]);
                                    HlslUtil.ParserAssert(m_splatStructFromScalar[prec][i]);

                                    builder.AppendLine("{0} ReflectApd({0} lhs, {0} rhs)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0}Apd dotVal = DotApd(lhs,rhs);", precUpper);
                                    builder.AppendLine("{0}Apd dotVal2 = AddApd(dotVal,dotVal);", precUpper);
                                    builder.AppendLine("{0} scale = Splat{0}(dotVal2);", structName);
                                    builder.AppendLine("{0} ret = SubApd(lhs,MulApd(scale,rhs));", structName);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                default:
                                    HlslUtil.ParserAssert(false);
                                    break;
                            }
                        }

                    }
                }

                Func1[] funcReorderList = new Func1[4]
                {
                    Func1.LenSqr,
                    Func1.Len,
                    Func1.InvLen,
                    Func1.InvLenSqr
                };

                // do a few that are used by other functions
                for (int funcReorder = 0; funcReorder < funcReorderList.Length; funcReorder++)
                {
                    int funcIter = (int)funcReorderList[funcReorder];

                    for (int i = 0; i < 4; i++)
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        if (m_func1[prec].Get((int)funcIter, i))
                        {
                            switch ((Func1)funcIter)
                            {
                                case Func1.LenSqr:
                                    {
                                        HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Add, 0));
                                        HlslUtil.ParserAssert(m_singleFunc[prec].Get((int)SingleFunc.Sqr, 0));

                                        for (int debugIter = 0; debugIter <= i; debugIter++)
                                        {
                                            HlslUtil.ParserAssert(m_extractFromApd[prec].Get(i, debugIter));
                                        }

                                        builder.AppendLine("{0}Apd LenSqrApd({1} src)", precUpper, structName);
                                        builder.AppendLine("{");
                                        builder.IncreaseIndent();
                                        builder.AppendLine("{0}Apd ret = SqrApd(Extract{1}_0(src));", precUpper, structName);
                                        for (int chan = 1; chan < i+1; chan++)
                                        {
                                            builder.AppendLine("ret = AddApd(ret,SqrApd(Extract{0}_{1}(src)));", structName, i.ToString());
                                        }
                                        builder.AppendLine("return ret;");
                                        builder.DecreaseIndent();
                                        builder.AppendLine("}");

                                        builder.AppendLine("");
                                    }
                                    break;
                                case Func1.Len:
                                    HlslUtil.ParserAssert(m_func1[prec].Get((int)Func1.Len, i));
                                    HlslUtil.ParserAssert(m_singleFunc[prec].Get((int)SingleFunc.Sqrt, 0));
                                    builder.AppendLine("{0}Apd LenApd({1} src)", precUpper, structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0}Apd ret = SqrtApd(LenSqrApd(src));",precUpper);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case Func1.InvLen:
                                    HlslUtil.ParserAssert(m_func1[prec].Get((int)Func1.LenSqr, i));
                                    HlslUtil.ParserAssert(m_singleFunc[prec].Get((int)SingleFunc.Rsqrt, 0));
                                    builder.AppendLine("{0}Apd InvLenApd({1} src)", precUpper, structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0}Apd ret = RsqrtApd(LenSqrApd(src));",precUpper);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                case Func1.InvLenSqr:
                                    HlslUtil.ParserAssert(m_func1[prec].Get((int)Func1.LenSqr, i));
                                    HlslUtil.ParserAssert(m_singleFunc[prec].Get((int)SingleFunc.Rcp, 0));
                                    builder.AppendLine("{0}Apd InvLenSqrApd({1} src)", precUpper, structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0}Apd ret = RcpApd(LenSqrApd(src));",precUpper);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                            }
                        }
                    }
                }



                for (int funcIter = 0; funcIter < kSingleFuncNum; funcIter++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);
                        string boolName = "bool" + (i + 1).ToString();

                        if (m_singleFunc[prec].Get(funcIter, i))
                        {
                            switch ((SingleFunc)funcIter)
                            {
                                // these are implemented above
                                case SingleFunc.Saturate:
                                case SingleFunc.Frac:
                                case SingleFunc.Log:
                                case SingleFunc.Log2:
                                case SingleFunc.Log10:
                                case SingleFunc.Exp:
                                case SingleFunc.Rcp:
                                case SingleFunc.Sqrt:
                                case SingleFunc.Rsqrt:
                                case SingleFunc.Sqr:
                                case SingleFunc.Cos:
                                case SingleFunc.CosH:
                                case SingleFunc.Sin:
                                case SingleFunc.SinH:
                                case SingleFunc.Tan:
                                case SingleFunc.TanH:
                                case SingleFunc.Abs:
                                case SingleFunc.Negate:
                                case SingleFunc.Floor:
                                case SingleFunc.Ceil:
                                    break;

                                case SingleFunc.Normalize:
                                    HlslUtil.ParserAssert(m_func1[prec].Get((int)Func1.InvLen, i));
                                    HlslUtil.ParserAssert(m_splatStructFromScalar[prec][i]);
                                    HlslUtil.ParserAssert(m_singleFunc[prec].Get((int)BinaryFunc.Mul, i));
                                    builder.AppendLine("{0} NormalizeApd({0} src)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0}Apd invLen = InvLenApd(src);",precUpper);
                                    builder.AppendLine("{0} invLenSplat = Splat{0}(invLen);", structName, structName);
                                    builder.AppendLine("{0} ret = MulApd(invLenSplat,src);", structName);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;

                                default:
                                    HlslUtil.ParserAssert(false);
                                    break;
                            }
                        }
                    }
                }

                for (int funcIter = 0; funcIter < kFunc2Num; funcIter++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        if (m_func2[prec].Get(funcIter, i))
                        {
                            switch ((Func2)funcIter)
                            {
                                case Func2.Dot:
                                    {
                                        HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Add, 0));
                                        HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Mul, 0));

                                        for (int debugIter = 0; debugIter <= i; debugIter++)
                                        {
                                            HlslUtil.ParserAssert(m_extractFromApd[prec].Get(i, debugIter));
                                        }

                                        builder.AppendLine("{0}Apd DotApd({1} lhs, {1} rhs)", precUpper, structName);
                                        builder.AppendLine("{");
                                        builder.IncreaseIndent();
                                        builder.AppendLine("{0} mulVal = MulApd(lhs,rhs);", structName);
                                        builder.AppendLine("{0}Apd ret = Extract{1}_0(mulVal);", precUpper, structName);
                                        for (int chan = 1; chan <= i; chan++)
                                        {
                                            builder.AppendLine("ret = AddApd(ret,Extract{0}_{1}(mulVal));", structName, i.ToString());
                                        }
                                        builder.AppendLine("return ret;");
                                        builder.DecreaseIndent();
                                        builder.AppendLine("}");
                                        builder.AppendLine("");
                                    }
                                    break;
                                case Func2.Cross:
                                    HlslUtil.ParserAssert(i == 2); // only valid for vec3 data
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Add, 0));
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Mul, 0));
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Sub, 0));
                                    for (int debugIter = 0; debugIter < 3; debugIter++)
                                    {
                                        HlslUtil.ParserAssert(m_extractFromApd[prec].Get(2, debugIter));
                                    }
                                    HlslUtil.ParserAssert(m_mergeToApd[prec][2]);

                                    builder.AppendLine("{0}Apd3 CrossApd({0}Apd3 lhs, {0}Apd3 rhs)", precUpper);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0}Apd lhs_x = Extract{0}Apd3_0(lhs);",precUpper);
                                    builder.AppendLine("{0}Apd lhs_y = Extract{0}Apd3_1(lhs);",precUpper);
                                    builder.AppendLine("{0}Apd lhs_z = Extract{0}Apd3_2(lhs);",precUpper);
                                    builder.AppendLine("{0}Apd rhs_x = Extract{0}Apd3_0(rhs);",precUpper);
                                    builder.AppendLine("{0}Apd rhs_y = Extract{0}Apd3_1(rhs);",precUpper);
                                    builder.AppendLine("{0}Apd rhs_z = Extract{0}Apd3_2(rhs);",precUpper);
                                    builder.AppendLine("{0}Apd ret_x = SubApd(MulApd(lhs_y,rhs_z),MulApd(lhs_z,rhs_y));",precUpper);
                                    builder.AppendLine("{0}Apd ret_y = SubApd(MulApd(lhs_z,rhs_x),MulApd(lhs_x,rhs_z));",precUpper);
                                    builder.AppendLine("{0}Apd ret_z = SubApd(MulApd(lhs_x,rhs_y),MulApd(lhs_y,rhs_x));",precUpper);
                                    builder.AppendLine("{0}Apd3 ret = Merge{0}Apd3(ret_x,ret_y,ret_z);",precUpper);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");

                                    break;
                                default:
                                    HlslUtil.ParserAssert(false);
                                    break;
                            }
                        }
                    }
                }


                for (int funcIter = 0; funcIter < kFunc3Num; funcIter++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        string structName = GetApdStructName(i, (ConcretePrecision)prec);
                        string baseName = GetApdBaseName(i, (ConcretePrecision)prec);

                        if (m_func3[prec].Get(funcIter, i))
                        {
                            switch ((Func3)funcIter)
                            {
                                case Func3.Lerp:
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Add, i));
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Mul, i));
                                    HlslUtil.ParserAssert(m_binaryFunc[prec].Get((int)BinaryFunc.Sub, i));
                                    HlslUtil.ParserAssert(m_splatStructFromScalar[prec][i]);
                                    HlslUtil.ParserAssert(m_makeStructFromFpd[prec][i]);
                                    builder.AppendLine("{0} LerpApd({0} lhs, {0} rhs, {0} t)", structName);
                                    builder.AppendLine("{");
                                    builder.IncreaseIndent();
                                    builder.AppendLine("{0} one = Splat{0}(Make{1}Apd(1.0f));", structName,precUpper);
                                    builder.AppendLine("{0} invT = SubApd(one,t);", structName);
                                    builder.AppendLine("{0} ret = AddApd(MulApd(lhs,invT),MulApd(rhs,t));", structName);
                                    builder.AppendLine("return ret;");
                                    builder.DecreaseIndent();
                                    builder.AppendLine("}");
                                    builder.AppendLine("");
                                    break;
                                default:
                                    HlslUtil.ParserAssert(false);
                                    break;
                            }
                        }
                    }
                }

                for (int funcIter = 0; funcIter < kTexSampleTypeNum; funcIter++)
                {
                    if (m_texSample2d[prec][funcIter])
                    {
                        switch ((TexSampleType)funcIter)
                        {
                            case TexSampleType.Lod0:
                                builder.AppendLine("{0}4 TextureSample2d{1}Lod0(Texture2D tex, SamplerState sam, float2 uv)", precLower,precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return ({0}4)tex.SampleLevel(sam,uv,0);", precLower);
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Fpd:
                                builder.AppendLine("{0}4 TextureSample2d{1}Fpd(Texture2D tex, SamplerState sam, float2 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.Sample(sam,uv);");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd:
                                builder.AppendLine("{0}4 TextureSample2d{1}Apd(Texture2D tex, SamplerState sam, FloatApd2 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.SampleGrad(sam,uv.m_val,uv.m_ddx,uv.m_ddy);");
                                builder.DecreaseIndent();
                                //builder.AppendLine("return tex.SampleLevel(sam,uv.m_val,0);");
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd_3x:
                                builder.AppendLine("{1}Apd4 TextureSample2d{1}Apd3x(Texture2D tex, SamplerState sam, FloatApd2 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("{0}4 val0 = tex.SampleGrad(sam,uv.m_val           ,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}4 val1 = tex.SampleGrad(sam,uv.m_val + uv.m_ddx,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}4 val2 = tex.SampleGrad(sam,uv.m_val + uv.m_ddy,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}Apd4 ret;", precUpper);
                                builder.AppendLine("ret.m_val = val0;");
                                builder.AppendLine("ret.m_ddx = val1 - val0;");
                                builder.AppendLine("ret.m_ddy = val2 - val0;");
                                builder.AppendLine("return ret;");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            default:
                                HlslUtil.ParserAssert(false);
                                break;
                        }
                    }
                    if (m_texSample2dArray[prec][funcIter])
                    {
                        switch ((TexSampleType)funcIter)
                        {
                            case TexSampleType.Lod0:
                                builder.AppendLine("{0}4 TextureSample2dArray{1}Lod0(Texture2DArray tex, SamplerState sam, float3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return ({0})tex.SampleLevel(sam,uv,0);", precLower);
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Fpd:
                                builder.AppendLine("{0}4 TextureSample2dArray{1}Fpd(Texture2DArray tex, SamplerState sam, float3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.Sample(sam,uv);");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd:
                                builder.AppendLine("{0}4 TextureSample2dArray{1}Apd(Texture2DArray tex, SamplerState sam, FloatApd3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.SampleGrad(sam,uv.m_val,uv.m_ddx.xy,uv.m_ddy.xy);");
                                builder.DecreaseIndent();
                                //builder.AppendLine("return tex.SampleLevel(sam,uv.m_val,0);");
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd_3x:
                                builder.AppendLine("{1}Apd4 TextureSample2dArray{1}Apd3x(Texture2DArray tex, SamplerState sam, FloatApd3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("{0}4 val0 = tex.SampleGrad(sam,uv.m_val                        ,uv.m_ddx.xy,uv.m_ddy.xy);", precLower);
                                builder.AppendLine("{0}4 val1 = tex.SampleGrad(sam,uv.m_val + float3(uv.m_ddx.xy,0),uv.m_ddx.xy,uv.m_ddy.xy);", precLower);
                                builder.AppendLine("{0}4 val2 = tex.SampleGrad(sam,uv.m_val + float3(uv.m_ddy.xy,0),uv.m_ddx.xy,uv.m_ddy.xy);", precLower);
                                builder.AppendLine("{0}Apd4 ret;", precUpper);
                                builder.AppendLine("ret.m_val = val0;");
                                builder.AppendLine("ret.m_ddx = val1 - val0;");
                                builder.AppendLine("ret.m_ddy = val2 - val0;");
                                builder.AppendLine("return ret;");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            default:
                                HlslUtil.ParserAssert(false);
                                break;
                        }
                    }
                    if (m_texSampleCube[prec][funcIter])
                    {
                        switch ((TexSampleType)funcIter)
                        {
                            case TexSampleType.Lod0:
                                builder.AppendLine("{0}4 TextureSampleCube{1}Lod0(TextureCube tex, SamplerState sam, float3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return ({0})tex.SampleLevel(sam,uv,0);", precLower);
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Fpd:
                                builder.AppendLine("{0}4 TextureSampleCube{1}Fpd(TextureCube tex, SamplerState sam, float3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.Sample(sam,uv);");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd:
                                builder.AppendLine("{0}4 TextureSampleCube{1}Apd(TextureCube tex, SamplerState sam, FloatApd3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.SampleGrad(sam,uv.m_val,uv.m_ddx,uv.m_ddy);");
                                builder.DecreaseIndent();
                                //builder.AppendLine("return tex.SampleLevel(sam,uv.m_val,0);");
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd_3x:
                                builder.AppendLine("{1}Apd4 TextureSampleCube{1}Apd3x(TextureCube tex, SamplerState sam, FloatApd3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("{0}4 val0 = tex.SampleGrad(sam,uv.m_val           ,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}4 val1 = tex.SampleGrad(sam,uv.m_val + uv.m_ddx,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}4 val2 = tex.SampleGrad(sam,uv.m_val + uv.m_ddy,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}Apd4 ret;", precUpper);
                                builder.AppendLine("ret.m_val = val0;");
                                builder.AppendLine("ret.m_ddx = val1 - val0;");
                                builder.AppendLine("ret.m_ddy = val2 - val0;");
                                builder.AppendLine("return ret;");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            default:
                                HlslUtil.ParserAssert(false);
                                break;
                        }
                    }
                    if (m_texSampleCubeArray[prec][funcIter])
                    {
                        switch ((TexSampleType)funcIter)
                        {
                            case TexSampleType.Lod0:
                                builder.AppendLine("{0}4 TextureSampleCubeArray{1}Lod0(TextureCubeArray tex, SamplerState sam, float4 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return ({0})tex.SampleLevel(sam,uv,0);", precLower);
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Fpd:
                                builder.AppendLine("{0}4 TextureSampleCubeArray{1}Fpd(TextureCubeArray tex, SamplerState sam, float4 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.Sample(sam,uv);");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd:
                                builder.AppendLine("{0}4 TextureSampleCubeArray{1}Apd(TextureCubeArray tex, SamplerState sam, FloatApd4 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.SampleGrad(sam,uv.m_val,uv.m_ddx.xyz,uv.m_ddy.xyz);");
                                builder.DecreaseIndent();
                                //builder.AppendLine("return tex.SampleLevel(sam,uv.m_val,0);");
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd_3x:
                                builder.AppendLine("{1}Apd4 TextureSampleCubeArray{1}Apd3x(TextureCubeArray tex, SamplerState sam, FloatApd4 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("{0}4 val0 = tex.SampleGrad(sam,uv.m_val                         ,uv.m_ddx.xyz,uv.m_ddy.xyz);", precLower);
                                builder.AppendLine("{0}4 val1 = tex.SampleGrad(sam,uv.m_val + float4(uv.m_ddx.xyz,0),uv.m_ddx.xyz,uv.m_ddy.xyz);", precLower);
                                builder.AppendLine("{0}4 val2 = tex.SampleGrad(sam,uv.m_val + float4(uv.m_ddy.xyz,0),uv.m_ddx.xyz,uv.m_ddy.xyz);", precLower);
                                builder.AppendLine("{0}Apd4 ret;", precUpper);
                                builder.AppendLine("ret.m_val = val0;");
                                builder.AppendLine("ret.m_ddx = val1 - val0;");
                                builder.AppendLine("ret.m_ddy = val2 - val0;");
                                builder.AppendLine("return ret;");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            default:
                                HlslUtil.ParserAssert(false);
                                break;
                        }
                    }
                    if (m_texSample3d[prec][funcIter])
                    {
                        switch ((TexSampleType)funcIter)
                        {
                            case TexSampleType.Lod0:
                                builder.AppendLine("{0}4 TextureSample3d{1}Lod0(Texture3D tex, SamplerState sam, float3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return ({0})tex.SampleLevel(sam,uv,0);", precLower);
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Fpd:
                                builder.AppendLine("{0}4 TextureSample3d{1}Fpd(Texture3D tex, SamplerState sam, float3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.Sample(sam,uv);");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd:
                                builder.AppendLine("{0}4 TextureSample3d{1}Apd(Texture3D tex, SamplerState sam, FloatApd3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("return tex.SampleGrad(sam,uv.m_val,uv.m_ddx,uv.m_ddy);");
                                builder.DecreaseIndent();
                                //builder.AppendLine("return tex.SampleLevel(sam,uv.m_val,0);");
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            case TexSampleType.Apd_3x:
                                builder.AppendLine("{1}Apd4 TextureSample3d{1}Apd3x(Texture3D tex, SamplerState sam, FloatApd3 uv)", precLower, precUpper);
                                builder.AppendLine("{");
                                builder.IncreaseIndent();
                                builder.AppendLine("{0}4 val0 = tex.SampleGrad(sam,uv.m_val           ,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}4 val1 = tex.SampleGrad(sam,uv.m_val + uv.m_ddx,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}4 val2 = tex.SampleGrad(sam,uv.m_val + uv.m_ddy,uv.m_ddx,uv.m_ddy);", precLower);
                                builder.AppendLine("{0}Apd4 ret;", precUpper);
                                builder.AppendLine("ret.m_val = val0;");
                                builder.AppendLine("ret.m_ddx = val1 - val0;");
                                builder.AppendLine("ret.m_ddy = val2 - val0;");
                                builder.AppendLine("return ret;");
                                builder.DecreaseIndent();
                                builder.AppendLine("}");
                                builder.AppendLine("");
                                break;
                            default:
                                HlslUtil.ParserAssert(false);
                                break;
                        }
                    }
                }

                for (int uvIndex = 0; uvIndex < kUvChanNum; uvIndex++)
                {
                    for (int apdIndex = 0; apdIndex < 2; apdIndex++)
                    {
                        if (m_fetchUv[prec].Get(uvIndex, apdIndex))
                        {
                            if (apdIndex == 1)
                            {
                                // apd
                                // TODO

                            }
                            else
                            {
                                // fpd
                                // TODO

                            }
                        }
                    }
                }
            }
            // todo: color fetch

        }



        static bool IsFloatType(ConcreteSlotValueType type)
        {
            switch(type)
            {
                case ConcreteSlotValueType.Vector1:
                case ConcreteSlotValueType.Vector2:
                case ConcreteSlotValueType.Vector3:
                case ConcreteSlotValueType.Vector4:
                    return true;
                default:
                    break;
            }

            return false;
        }


        static int FloatTypeToIndex(ConcreteSlotValueType type)
        {
            switch(type)
            {
                case ConcreteSlotValueType.Boolean:
                case ConcreteSlotValueType.Vector1:
                    return 0;
                case ConcreteSlotValueType.Vector2:
                    return 1;
                case ConcreteSlotValueType.Vector3:
                    return 2;
                case ConcreteSlotValueType.Vector4:
                    return 3;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return -1;
        }


        static bool IsFloatTypeNotAny(ConcreteSlotValueType type)
        {
            switch(type)
            {
                case ConcreteSlotValueType.Vector1:
                case ConcreteSlotValueType.Vector2:
                case ConcreteSlotValueType.Vector3:
                case ConcreteSlotValueType.Vector4:
                    return true;
                default:
                    break;
            }

            return false;
        }


        static bool IsApdStruct(ApdStatus status)
        {
            return status == ApdStatus.Valid;
        }

        internal static bool IsAnyVectorType(ConcreteSlotValueType paramType)
        {
            bool isAnyVector = (paramType == ConcreteSlotValueType.Vector1 || paramType == ConcreteSlotValueType.Vector2 || paramType == ConcreteSlotValueType.Vector3 || paramType == ConcreteSlotValueType.Vector4);
            return isAnyVector;
        }

        // loose conversion rules. Any float1/2/3/4 to/from any float1/2/3/4
        static bool ConvertBaseVariableIsLegal(ConcreteSlotValueType dstParam, ConcreteSlotValueType srcParam)
        {
            if (dstParam == srcParam)
            {
                return true;
            }

            bool ret = false;
            switch(dstParam)
            {
                case ConcreteSlotValueType.Vector1:
                case ConcreteSlotValueType.Vector2:
                case ConcreteSlotValueType.Vector3:
                case ConcreteSlotValueType.Vector4:
                    ret = IsAnyVectorType(srcParam);
                    break;

                case ConcreteSlotValueType.Matrix4:
                case ConcreteSlotValueType.Matrix3:
                case ConcreteSlotValueType.Matrix2:
                    // The only time we can implicitly convert to a matrix is from a float
                    ret = (srcParam == ConcreteSlotValueType.Vector1);
                    break;
                case ConcreteSlotValueType.SamplerState:
                case ConcreteSlotValueType.Texture2D:
                case ConcreteSlotValueType.Texture2DArray:
                case ConcreteSlotValueType.Texture3D:
                case ConcreteSlotValueType.Cubemap:
                case ConcreteSlotValueType.Gradient:
                case ConcreteSlotValueType.Boolean:
                case ConcreteSlotValueType.VirtualTexture:
                case ConcreteSlotValueType.PropertyConnectionState:
                    ret = false;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        // for now this will always be single precision, but we will need to add half precision as an option
        static string ConvertBaseVariable(ConcreteSlotValueType dstParam, ConcretePrecision dstPrecision, ConcreteSlotValueType srcParam, ConcretePrecision srcPrecision, string paramName)
        {
            bool isValid = ConvertBaseVariableIsLegal(dstParam,srcParam);
            if (!isValid)
            {
                HlslUtil.ParserAssert(isValid);
            }

            bool isDstMatrix = ShaderTokenUtil.IsMatrixType(dstParam);
            bool isDstVector = ShaderTokenUtil.IsVectorType(dstParam);
            bool isSrcMatrix = ShaderTokenUtil.IsMatrixType(srcParam);

            string ret = "<invalid>";
            if (isDstMatrix)
            {
                // the only type that we can legally convert to a matrix is a scalar, and we can implicitly conver the precision
                HlslUtil.ParserAssert(isSrcMatrix || srcParam == ConcreteSlotValueType.Vector1);
                ret = paramName;
            }
            else if (isDstVector)
            {
                int srcIndex = FloatTypeToIndex(srcParam);
                int dstIndex = FloatTypeToIndex(dstParam);

                bool needsPrecisionChange = (dstPrecision != srcPrecision);
                string dstPrecisionName = GetApdBaseName(dstIndex, dstPrecision);
                string castPrefix = needsPrecisionChange ? ("((" + dstPrecisionName + ")") : "";
                string castSuffix = needsPrecisionChange ? (")") : "";
                string constructorPrefix = dstPrecisionName + "(";
                string constructorSuffix = ")";

                switch (dstParam)
                {
                    case ConcreteSlotValueType.Vector1:
                    case ConcreteSlotValueType.Boolean:
                        if (srcParam == ConcreteSlotValueType.Vector1 || srcParam == ConcreteSlotValueType.Boolean)
                        {
                            ret = castPrefix + paramName + castSuffix;
                        }
                        else
                        {
                            ret = castPrefix + paramName + ".x" + castSuffix;
                        }
                        break;
                    case ConcreteSlotValueType.Vector2:
                        if (srcParam == ConcreteSlotValueType.Vector2)
                        {
                            ret = castPrefix + paramName + castSuffix;
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector1 || srcParam == ConcreteSlotValueType.Boolean)
                        {
                            //ret = castPrefix + paramName + ".xx" + castSuffix;
                            ret = castPrefix + paramName + castSuffix;
                        }
                        else
                        {
                            ret = castPrefix + paramName + ".xy" + castSuffix;
                        }
                        break;
                    case ConcreteSlotValueType.Vector3:
                        if (srcParam == ConcreteSlotValueType.Vector3)
                        {
                            ret = castPrefix + paramName + castSuffix;
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector1 || srcParam == ConcreteSlotValueType.Boolean)
                        {
                            ret = castPrefix + paramName + castSuffix;
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector2)
                        {
                            ret = constructorPrefix + paramName + ".x," + paramName + ".y," + "0" + constructorSuffix;
                        }
                        else
                        {
                            HlslUtil.ParserAssert(srcParam == ConcreteSlotValueType.Vector4);
                            ret = castPrefix + paramName + ".xyz" + castSuffix;
                        }
                        break;
                    case ConcreteSlotValueType.Vector4:
                        if (srcParam == ConcreteSlotValueType.Vector4)
                        {
                            ret = castPrefix + paramName + castSuffix;
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector1 || srcParam == ConcreteSlotValueType.Boolean)
                        {
                            //ret = castPrefix + paramName + ".xxxx" + castSuffix;
                            ret = castPrefix + paramName + castSuffix;
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector2)
                        {
                            ret = constructorPrefix + paramName + ".x," + paramName + ".y,0,0" + constructorSuffix;
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector3)
                        {
                            ret = constructorPrefix + paramName + ".x," + paramName + ".y," + paramName + ".z,0" + constructorSuffix;
                        }
                        else
                        {
                            HlslUtil.ParserAssert(false);
                        }
                        break;
                    default:
                        HlslUtil.ParserAssert(false);
                        break;
                }
            }
            else
            {
                HlslUtil.ParserAssert(srcParam == dstParam);
                ret = paramName;
            }

            return ret;
        }

        string SplatStructFromScalar(ConcreteSlotValueType dstParam, string paramName, ConcretePrecision precision)
        {
            int index = FloatTypeToIndex(dstParam);

            m_splatStructFromScalar[(int)precision][index] = true;

            string structName = GetApdStructName(index,precision);
            string adjName = ConvertToSinglePrecision(dstParam, paramName, precision);

            string ret = "Splat" + structName + "(" + adjName + ")";

            return ret;
        }

        internal string InsertIndexApd(ConcreteSlotValueType lhsParam, ApdStatus lhsStatus, string lhsName, ConcretePrecision lhsPrecision,
                               ConcreteSlotValueType rhsParam, ApdStatus rhsStatus, string rhsName, ConcretePrecision rhsPrecision,
                               string indexName)
        {
            int index = FloatTypeToIndex(lhsParam);

            HlslUtil.ParserAssert(rhsParam == ConcreteSlotValueType.Vector1);
            HlslUtil.ParserAssert(lhsStatus == ApdStatus.Valid);

            ConcretePrecision dstPrecision = lhsPrecision;

            m_insertIndexApd[(int)lhsPrecision][index] = true;

            string structName = GetApdStructName(index, lhsPrecision);
            string scalarName = GetApdStructName(0, lhsPrecision);

            // convert precision if necessary
            string rhsAdj = MakeImplicitCast(rhsParam, ApdStatus.Valid, dstPrecision,
                rhsParam, rhsStatus, rhsPrecision,
                rhsName);

            string ret = "Insert" + structName + "(" + lhsName + "," + indexName + "," + rhsAdj + ")";
            return ret;
        }

        internal string ExtractIndexApd(ConcreteSlotValueType lhsParam, string lhsName, ConcretePrecision lhsPrecision,
            string indexName)
        {
            int index = FloatTypeToIndex(lhsParam);

            m_extractIndexApd[(int)lhsPrecision][index] = true;

            string structName = GetApdStructName(index, lhsPrecision);

            string ret = "Extract" + structName + "(" + lhsName + "," + indexName +")";
            return ret;
        }

        // Note that we allow loose conversion rules. Like float2 to float3 and vice versa. We follow standard rules for float1 to float2/3/4 conversions,
        // but if we float from a float2/3/4 to a different float2/3/4, we shrink or expand with zeroes.
        string ConvertStructVariable(ConcreteSlotValueType dstParam, ConcreteSlotValueType srcParam, string paramName, ConcretePrecision precision)
        {
            // struct conversions have same legality as base conversions
            bool isValid =  ConvertBaseVariableIsLegal(dstParam,srcParam);
            HlslUtil.ParserAssert(isValid);

            int index = FloatTypeToIndex(dstParam);
            string structName = GetApdStructName(index, precision);

            string ret = "<invalid>";
            switch(dstParam)
            {
                case ConcreteSlotValueType.Vector1:
                    if (srcParam == ConcreteSlotValueType.Vector1)
                    {
                        ret = paramName;
                    }
                    else
                    {
                        m_makeStructDirect[(int)precision][0] = true;

                        string funcName = "Make" + structName + "Direct";
                        ret = funcName + "(" + paramName + ".m_val.x,"+ paramName + ".m_ddx.x," + paramName + ".m_ddy.x)";
                    }
                    break;
                case ConcreteSlotValueType.Vector2:
                    if (srcParam == ConcreteSlotValueType.Vector2)
                    {
                        ret = paramName;
                    }
                    else if (srcParam == ConcreteSlotValueType.Vector1)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector2,paramName,precision);
                    }
                    else
                    {
                        m_makeStructDirect[(int)precision][1] = true;

                        string funcName = "Make" + structName + "Direct";
                        ret = funcName + "(" + paramName + ".m_val.xy," + paramName + ".m_ddx.xy," + paramName + ".m_ddy.xy)";
                    }
                    break;
                case ConcreteSlotValueType.Vector3:
                    if (srcParam == ConcreteSlotValueType.Vector3)
                    {
                        ret = paramName;
                    }
                    else if (srcParam == ConcreteSlotValueType.Vector1)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector3,paramName,precision);
                    }
                    else
                    {
                        m_makeStructDirect[(int)precision][2] = true;

                        string funcName = "Make" + structName + "Direct";

                        if (srcParam == ConcreteSlotValueType.Vector2)
                        {
                            ret = funcName + "(float3(" + paramName + ".m_val.xy,0),float3(" + paramName + ".m_ddx.xy,0),float3(" + paramName + ".m_ddy.xy,0))";
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector4)
                        {
                            ret = funcName + "(" + paramName + ".m_val.xyz," + paramName + ".m_ddx.xyz," + paramName + ".m_ddy.xyz)";
                        }
                        else
                        {
                            HlslUtil.ParserAssert(false);
                        }
                    }
                    break;
                case ConcreteSlotValueType.Vector4:
                    if (srcParam == ConcreteSlotValueType.Vector4)
                    {
                        ret = paramName;
                    }
                    else if (srcParam == ConcreteSlotValueType.Vector1)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector4,paramName,precision);
                    }
                    else
                    {
                        m_makeStructDirect[(int)precision][3] = true;

                        string funcName = "Make" + structName + "Direct";

                        if (srcParam == ConcreteSlotValueType.Vector2)
                        {
                            ret = funcName + "(float4(" + paramName + ".m_val.xy,0,0),float4(" + paramName + ".m_ddx.xy,0,0),float4(" + paramName + ".m_ddy.xy,0,0))";
                        }
                        else if (srcParam == ConcreteSlotValueType.Vector3)
                        {
                            ret = funcName + "(float4(" + paramName + ".m_val.xyz,0),float4(" + paramName + ".m_ddx.xyz,0),float4(" + paramName + ".m_ddy.xyz,0))";
                        }
                        else
                        {
                            HlslUtil.ParserAssert(false);
                        }
                    }
                    break;

                case ConcreteSlotValueType.SamplerState:
                case ConcreteSlotValueType.Matrix4:
                case ConcreteSlotValueType.Matrix3:
                case ConcreteSlotValueType.Matrix2:
                case ConcreteSlotValueType.Texture2D:
                case ConcreteSlotValueType.Texture2DArray:
                case ConcreteSlotValueType.Texture3D:
                case ConcreteSlotValueType.Cubemap:
                case ConcreteSlotValueType.Gradient:
                case ConcreteSlotValueType.Boolean:
                case ConcreteSlotValueType.VirtualTexture:
                case ConcreteSlotValueType.PropertyConnectionState:
                    HlslUtil.ParserAssert(srcParam == dstParam);
                    ret = paramName;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        static string ConvertToSinglePrecision(ConcreteSlotValueType paramType, string paramName, ConcretePrecision precision)
        {
            int index = FloatTypeToIndex(paramType);
            string baseName = GetApdBaseName(index, precision);
            string dstParam = (precision == ConcretePrecision.Single) ? paramName : ("(" + baseName + ")" + paramName);
            return dstParam;
        }

        string MakeStructFromFpd(ConcreteSlotValueType dstParam, string paramName, ConcretePrecision precision)
        {
            int index = FloatTypeToIndex(dstParam);

            m_makeStructFromFpd[(int)precision][index] = true;

            string structName = GetApdStructName(index,precision);
            string baseName = GetApdBaseName(index,precision);

            string adjName = ConvertToSinglePrecision(dstParam, paramName, precision);
            string ret = "Make" + structName + "(" + adjName + ")";

            return ret;
        }

        internal string MakeImplicitCast(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType srcParam, ApdStatus srcStatus, ConcretePrecision srcPrecision,
            string srcName)
        {
            bool isVectorSrc = IsVectorType(srcParam);
            bool isVectorDst = IsVectorType(dstParam);

            int dstApdVal = (dstStatus == ApdStatus.Valid) ? 1 : 0;
            int srcApdVal = (srcStatus == ApdStatus.Valid) ? 1 : 0;

            bool bothNotApdValid = (dstApdVal == 0 && srcApdVal == 0);

            int dstPrecisionVal = (dstPrecision == ConcretePrecision.Half) ? 0 : 1;
            int srcPrecisionVal = (srcPrecision == ConcretePrecision.Half) ? 0 : 1;
            int dstChanVal = IndexFromParamType(dstParam);
            int srcChanVal = IndexFromParamType(srcParam);

            string ret = "";


            // Several cases
            //  1. If dstChanVal < 0, then it's not a vector/scalar so directly convert and hope for the best.
            //        Cases like float->int or uint->int will be fine, but texture2d->int will not.
            //  2. If dstChanVal >= 0
            //     a) If srcChanVal >= 0, then do a conversion from vector->vector
            //     b) If srcChanVal < 0, then convert to float hope that it's legal
            if (dstChanVal < 0)
            {
                ret = srcName;
            }
            else
            {
                string name = srcName;
                if (srcChanVal < 0)
                {
                    srcChanVal = 0;
                    srcParam = ConcreteSlotValueType.Vector1;

                    string baseName = (srcPrecision == ConcretePrecision.Single) ? "float" : "half";
                    name = "((" + baseName + ")" + srcName + ")";
                }

                if (dstParam == srcParam && dstApdVal == srcApdVal && dstPrecision == srcPrecision)
                {
                    // special case: everything is the same
                    ret = name;
                }
                else if (bothNotApdValid && dstChanVal <= srcChanVal)
                {
                    string castPrefix = "";
                    string castSuffix = "";
                    if (srcPrecision != dstPrecision)
                    {
                        castPrefix = "((" + GetApdBaseName(dstChanVal, dstPrecision) + ")";
                        castSuffix = ")";
                    }

                    if (srcChanVal == 0 || srcChanVal == dstChanVal)
                    {
                        // single channel, so a clean implicit conversion, or same so still clean
                        ret = castPrefix + name + castSuffix;
                    }
                    else
                    {
                        // more than one channel, so we need a swizzle
                        string[] chanSwizzleVec = new string[4] { ".x", ".xy", ".xyz", ".xyzw" };

                        ret = castPrefix + name + chanSwizzleVec[dstChanVal] + castSuffix;
                    }
                }
                else if (srcStatus == ApdStatus.Valid && dstStatus != ApdStatus.Valid)
                {
                    // if we are converting from apd to fpd, just grab the m_val member and then implicitly cast
                    ret = MakeImplicitCast(dstParam, ApdStatus.Invalid, dstPrecision,
                        srcParam, ApdStatus.Invalid, srcPrecision,
                        name + ".m_val");
                }
                else
                {
                    // otherwise, do the case function
                    m_implicitCast.Set(dstApdVal, srcApdVal, dstPrecisionVal, srcPrecisionVal, dstChanVal, srcChanVal, true);
                    ret = GetImplicitCastFuncName(dstApdVal, srcApdVal, dstPrecisionVal, srcPrecisionVal, dstChanVal, srcChanVal) + "(" + name + ")";
                }
            }
            return ret;
        }

        string MakeStructFromFpdFinite(ConcreteSlotValueType dstParam, string paramName, ConcretePrecision precision)
        {
            int index = FloatTypeToIndex(dstParam);

            m_makeStructFromFpdFinite[(int)precision][index] = true;

            string structName = GetApdStructName(index, precision);
            string baseName = GetApdBaseName(index, precision);

            string adjName = ConvertToSinglePrecision(dstParam, paramName, precision);
            string ret = "Make" + structName + "Finite(" + adjName + ")";

            return ret;
        }

        internal string MakeStructFromApdDirect(ConcreteSlotValueType dstParam, string paramName, string paramNameDdx, string paramNameDdy, ConcretePrecision precision)
        {
            int index = FloatTypeToIndex(dstParam);

            m_makeStructDirect[(int)precision][index] = true;

            string structName = GetApdStructName(index, precision);
            string baseName = GetApdBaseName(index, precision);

            string adjName = ConvertToSinglePrecision(dstParam, paramName, precision);
            string adjNameDdx = ConvertToSinglePrecision(dstParam, paramNameDdx, precision);
            string adjNameDdy = ConvertToSinglePrecision(dstParam, paramNameDdy, precision);
            string ret = "Make" + structName + "Direct(" + adjName + "," + adjNameDdx + "," + adjNameDdy + ")";

            return ret;
        }

        internal string ConvertVariable(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision, ConcreteSlotValueType srcParam, ApdStatus srcStatus, ConcretePrecision srcPrecision, string name)
        {
            bool srcStruct = IsApdStruct(srcStatus);
            bool dstStruct = IsApdStruct(dstStatus);

            string ret;
            if (srcStruct == dstStruct)
            {
                if (srcStruct)
                {
                    // both are Structs
                    ret = ConvertStructVariable(dstParam,srcParam,name,dstPrecision);
                }
                else
                {
                    // both are Base
                    ret = ConvertBaseVariable(dstParam,dstPrecision,srcParam,srcPrecision,name);
                }
            }
            else
            {
                if (dstStruct)
                {
                    // convert from base to struct

                    // 1. convert from srcBase to dstBase
                    string dstBase = ConvertBaseVariable(dstParam,dstPrecision,srcParam,srcPrecision,name);

                    // 2. convert from base to struct
                    ret = MakeStructFromFpd(dstParam,dstBase,dstPrecision);
                }
                else
                {
                    // convert from struct to base

                    // 1. convert to base
                    string dstBase = name + ".m_val";
                    ret = ConvertBaseVariable(dstParam,dstPrecision,srcParam,srcPrecision,dstBase);
                }
            }

            return ret;
        }

        // Anytime we have a binary function (like add or subtract) we want to convert both the lhs and rhs to the destination type, and then
        // apply the operation. The one exception is the matrix multiplication, where we have a special rule to skip the convertion if the
        // original type is a matrix.
        internal string ConvertVariableUnlessMatrixMul(BinaryFunc func, ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision, ConcreteSlotValueType srcParam, ApdStatus srcStatus, ConcretePrecision srcPrecision, string name)
        {
            string ret;
            bool isSrcMatrix = ShaderTokenUtil.IsMatrixType(srcParam);
            if (func == BinaryFunc.Mul && isSrcMatrix)
            {
                ret = ConvertVariable(srcParam, dstStatus, dstPrecision, srcParam, srcStatus, srcPrecision, name);
            }
            else
            {
                ret = ConvertVariable(dstParam, dstStatus, dstPrecision, srcParam, srcStatus, srcPrecision, name);
            }
            return ret;
        }

        internal static ConcreteSlotValueType GetFunc1ReturnType(Func1 func, ConcreteSlotValueType lhsParam)
        {
            ConcreteSlotValueType ret = ConcreteSlotValueType.Vector1;

            switch (func)
            {
                case Func1.Len:
                case Func1.LenSqr:
                case Func1.InvLen:
                case Func1.InvLenSqr:
                    ret = ConcreteSlotValueType.Vector1;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }
            return ret;
        }

        internal static ConcreteSlotValueType GetFunc2ReturnType(Func2 func, ConcreteSlotValueType lhsParam, ConcreteSlotValueType rhsParam)
        {
            ConcreteSlotValueType ret = ConcreteSlotValueType.Vector1;

            switch (func)
            {
                case Func2.Dot:
                    ret = ConcreteSlotValueType.Vector1;
                    break;
                case Func2.Cross:
                    ret = ConcreteSlotValueType.Vector3;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }
            return ret;
        }

        internal static ConcreteSlotValueType GetFunc3ReturnType(Func3 func, ConcreteSlotValueType param0, ConcreteSlotValueType param1, ConcreteSlotValueType param2)
        {
            ConcreteSlotValueType ret = ConcreteSlotValueType.Vector1;

            switch (func)
            {
                case Func3.Lerp:
                    ret = GetBinaryOpReturnTypeVector(param0,param1);
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }
            return ret;
        }

        internal static ConcretePrecision GetBinaryOpPrecisionType(ConcretePrecision lhsPrecision, ConcretePrecision rhsPrecision)
        {
            if (lhsPrecision == ConcretePrecision.Half && rhsPrecision == ConcretePrecision.Half)
            {
                return ConcretePrecision.Half;
            }

            return ConcretePrecision.Single;
        }

        // binary op return types if both lhs and rhs are float1/2/3/4, but not matrices
        internal static ConcreteSlotValueType GetBinaryOpReturnTypeVector(ConcreteSlotValueType lhsParam, ConcreteSlotValueType rhsParam)
        {
            bool isLhsMat = ShaderTokenUtil.IsMatrixType(lhsParam);
            bool isRhsMat = ShaderTokenUtil.IsMatrixType(rhsParam);
            bool isLhsScalar = (lhsParam == ConcreteSlotValueType.Vector1);
            bool isRhsScalar = (rhsParam == ConcreteSlotValueType.Vector1);

            // if one is scalar, and one is matrix, then we can convert, otherwise matrix and vector types are illegal
            if (isLhsMat && isRhsScalar)
            {
                return lhsParam;
            }

            if (isRhsMat && isLhsScalar)
            {
                return rhsParam;
            }

            if (!IsFloatTypeNotAny(lhsParam))
            {
                HlslUtil.ParserAssert(false);
            }

            if (!IsFloatTypeNotAny(rhsParam))
            {
                HlslUtil.ParserAssert(false);
            }

            // both are the same, so arbitrarily choose lhs
            if (lhsParam == rhsParam)
            {
                return lhsParam;
            }

            // lhs is scalar, rhs is vector, so return rhs
            if (lhsParam == ConcreteSlotValueType.Vector1)
            {
                return rhsParam;
            }

            // lhs is vector, rhs is scalar, so return lhs
            if (rhsParam == ConcreteSlotValueType.Vector1)
            {
                return lhsParam;
            }

            // otherwise, not sure what the right answer is. for now, truncate to the smaller type
            ConcreteSlotValueType ret = (ConcreteSlotValueType)Math.Min((int)lhsParam, (int)rhsParam);
            return ret;
        }

        // special logic only for multiply nodes to handle matrix multiplication
        internal static ConcreteSlotValueType GetBinaryOpReturnTypeMultiply(ConcreteSlotValueType lhsParam, ConcreteSlotValueType rhsParam)
        {
            bool isLhsVec = ShaderTokenUtil.IsVectorType(lhsParam);
            bool isRhsVec = ShaderTokenUtil.IsVectorType(rhsParam);
            bool isLhsMat = ShaderTokenUtil.IsMatrixType(lhsParam);
            bool isRhsMat = ShaderTokenUtil.IsMatrixType(rhsParam);
            bool isLhsScalar = (lhsParam == ConcreteSlotValueType.Vector1);
            bool isRhsScalar = (rhsParam == ConcreteSlotValueType.Vector1);

            // only valid types are vector and matrix
            HlslUtil.ParserAssert(isLhsVec || isLhsMat);
            HlslUtil.ParserAssert(isRhsVec || isRhsMat);

            ConcreteSlotValueType ret;
            if (isLhsVec && isRhsVec)
            {
                // both are vectors
                ret = GetBinaryOpReturnTypeVector(lhsParam, rhsParam);
            }
            else if (isLhsMat && isRhsMat)
            {
                // both are matrices, better be the same size
                HlslUtil.ParserAssert(lhsParam == rhsParam);
                ret = lhsParam;
            }
            else if (isLhsMat && isRhsScalar)
            {
                // lhs is mat, rhs is scalar, so we just go with lhs
                ret = lhsParam;
            }
            else if (isRhsMat && isLhsScalar)
            {
                // rhs is mat, lhs is scalar, so return rhs
                ret = rhsParam;
            }
            else
            {
                // one is a matrix, other is vector, which is easy since we only allow square matrices
                int lhsRows = ShaderTokenUtil.GetTypeNumRows(lhsParam);
                int lhsCols = ShaderTokenUtil.GetTypeNumCols(lhsParam);
                int rhsRows = ShaderTokenUtil.GetTypeNumRows(rhsParam);
                int rhsCols = ShaderTokenUtil.GetTypeNumCols(rhsParam);

                int lowDim = Math.Min(Math.Min(lhsRows, lhsCols), Math.Min(rhsRows, rhsCols));
                int highDim = Math.Max(Math.Max(lhsRows, lhsCols), Math.Max(rhsRows, rhsCols));

                HlslUtil.ParserAssert(lowDim == 1);
                HlslUtil.ParserAssert(highDim > 1);

                ret = ShaderTokenUtil.GetVectorTypeFromNumCols(highDim);
            }

            return ret;
        }

        internal static ConcreteSlotValueType GetBinaryOpReturnTypeAddSub(ConcreteSlotValueType lhsParam, ConcreteSlotValueType rhsParam)
        {
            bool isLhsVec = ShaderTokenUtil.IsVectorType(lhsParam);
            bool isRhsVec = ShaderTokenUtil.IsVectorType(rhsParam);
            bool isLhsMat = ShaderTokenUtil.IsMatrixType(lhsParam);
            bool isRhsMat = ShaderTokenUtil.IsMatrixType(rhsParam);

            // only valid types are vector and matrix
            HlslUtil.ParserAssert(isLhsVec || isLhsMat);
            HlslUtil.ParserAssert(isRhsVec || isRhsMat);

            ConcreteSlotValueType ret;
            if (isLhsVec && isRhsVec)
            {
                // both are vectors
                ret = GetBinaryOpReturnTypeVector(lhsParam, rhsParam);
            }
            else
            {
                // if one is not a vector, then they must be the same type. we could add logic
                // for other cases if we wanted to (like adding a Matrix4 with a scalar constant),
                // but it's forbidden for now.
                HlslUtil.ParserAssert(lhsParam == rhsParam);
                ret = lhsParam;
            }

            return ret;
        }

        internal static ConcreteSlotValueType GetBinaryOpReturnType(BinaryFunc func, ConcreteSlotValueType lhsParam, ConcreteSlotValueType rhsParam)
        {
            ConcreteSlotValueType ret;
            if (func == BinaryFunc.Mul)
            {
                ret = GetBinaryOpReturnTypeMultiply(lhsParam, rhsParam);
            }
            else
            {
                ret = GetBinaryOpReturnTypeVector(lhsParam, rhsParam);
            }
            return ret;
        }

        internal string MakeBinaryFunc(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType lhsParam, ApdStatus lhsStatus, ConcretePrecision lhsPrecision, string lhsName,
            ConcreteSlotValueType rhsParam, ApdStatus rhsStatus, ConcretePrecision rhsPrecision, string rhsName,
            BinaryFunc func)
        {
            ConcreteSlotValueType funcParam = dstParam;//GetBinaryOpReturnType(lhsParam,rhsParam);

            bool isDstStruct = IsApdStruct(dstStatus);

            ConcretePrecision expectedDstPrecision = GetBinaryOpPrecisionType(lhsPrecision, rhsPrecision);

            HlslUtil.ParserAssert(expectedDstPrecision == dstPrecision);

            string ret;
            if (isDstStruct)
            {
                // apd version

                bool isMatrixLhs = ShaderTokenUtil.IsMatrixType(lhsParam);
                bool isMatrixRhs = ShaderTokenUtil.IsMatrixType(rhsParam);
                int dstIndex = FloatTypeToIndex(dstParam);

                if (isMatrixLhs || isMatrixRhs)
                {
                    // matrix operations for analytic derivatives should only happen for mul operations
                    HlslUtil.ParserAssert(func == BinaryFunc.Mul);

                    // only one of the two should be a matrix
                    HlslUtil.ParserAssert(!isMatrixLhs || !isMatrixRhs);

                    string structName = GetApdStructName(dstIndex, dstPrecision);

                    if (isMatrixLhs)
                    {
                        // matrix is on left side
                        string rhsVariable = ConvertVariable(funcParam, dstStatus, dstPrecision, rhsParam, rhsStatus, rhsPrecision, rhsName);

                        m_mulMatVecApd[(int)dstPrecision][dstIndex] = true;
                        string funcName = "MulMatVec" + structName;

                        ret = funcName + "(" + lhsName + "," + rhsVariable + ")";
                    }
                    else
                    {
                        // matrix is on right side
                        string lhsVariable = ConvertVariable(funcParam, dstStatus, dstPrecision, lhsParam, lhsStatus, lhsPrecision, lhsName);

                        m_mulVecMatApd[(int)dstPrecision][dstIndex] = true;
                        string funcName = "MulVecMat" + structName;

                        ret = funcName + "(" + lhsVariable + "," + rhsName + ")";
                    }
                }
                else
                {
                    // get both versions as apd
                    string lhsVal = ConvertVariable(funcParam, dstStatus, dstPrecision, lhsParam, lhsStatus, lhsPrecision, lhsName);
                    string rhsVal = ConvertVariable(funcParam, dstStatus, dstPrecision, rhsParam, rhsStatus, rhsPrecision, rhsName);

                    int funcTypeIndex = FloatTypeToIndex(funcParam);
                    m_binaryFunc[(int)dstPrecision].Set((int)func, funcTypeIndex, true);

                    string mergedOp;

                    switch (func)
                    {
                        case BinaryFunc.Add:
                            mergedOp = "AddApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        case BinaryFunc.Sub:
                            mergedOp = "SubApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        case BinaryFunc.Mul:
                            mergedOp = "MulApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        case BinaryFunc.Div:
                            mergedOp = "DivApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        case BinaryFunc.Min:
                            mergedOp = "MinApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        case BinaryFunc.Max:
                            mergedOp = "MaxApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        case BinaryFunc.Pow:
                            mergedOp = "PowApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        case BinaryFunc.Reflect:
                            mergedOp = "ReflectApd(" + lhsVal + "," + rhsVal + ")";
                            break;
                        default:
                            mergedOp = "<invalid>";
                            HlslUtil.ParserAssert(false);
                            break;
                    }

                    ret = ConvertVariable(dstParam, dstStatus, dstPrecision, funcParam, dstStatus, dstPrecision, mergedOp);
                }
            }
            else
            {
                // first, get both variables as base
                string lhsVal = ConvertVariableUnlessMatrixMul(func,funcParam,dstStatus,dstPrecision,lhsParam,lhsStatus, lhsPrecision, lhsName);
                string rhsVal = ConvertVariableUnlessMatrixMul(func,funcParam,dstStatus,dstPrecision,rhsParam,rhsStatus,rhsPrecision,rhsName);

                string mergedOp;

                switch(func)
                {
                    case BinaryFunc.Add:
                        mergedOp = "(" + lhsVal + "+" + rhsVal + ")";
                        break;
                    case BinaryFunc.Sub:
                        mergedOp = "(" + lhsVal + "-" + rhsVal + ")";
                        break;
                    case BinaryFunc.Mul:
                        {
                            bool isLhsVec = ShaderTokenUtil.IsVectorType(lhsParam);
                            bool isRhsVec = ShaderTokenUtil.IsVectorType(rhsParam);
                            if (isLhsVec && isRhsVec)
                            {
                                mergedOp = "(" + lhsVal + "*" + rhsVal + ")";
                            }
                            else
                            {
                                mergedOp = "mul(" + lhsVal + "," + rhsVal + ")";
                            }
                        }
                        break;
                    case BinaryFunc.Div:
                        mergedOp = "(" + lhsVal + "/" + rhsVal + ")";
                        break;
                    case BinaryFunc.Min:
                        mergedOp = "min(" + lhsVal + "," + rhsVal + ")";
                        break;
                    case BinaryFunc.Max:
                        mergedOp = "max(" + lhsVal + "," + rhsVal + ")";
                        break;
                    case BinaryFunc.Pow:
                        mergedOp = "pow(" + lhsVal + "," + rhsVal + ")";
                        break;
                    case BinaryFunc.Reflect:
                        mergedOp = "reflect(" + lhsVal + "," + rhsVal + ")";
                        break;
                    default:
                        mergedOp = "<invalid>";
                        HlslUtil.ParserAssert(false);
                        break;
                }

                ret = ConvertVariable(dstParam,dstStatus,dstPrecision,funcParam,dstStatus, dstPrecision, mergedOp);
            }

            return ret;
        }

        internal string MakeSingleFunc(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType srcParam, ApdStatus srcStatus, ConcretePrecision srcPrecision, string srcName,
            SingleFunc func)
        {
            ConcreteSlotValueType funcParam = dstParam;
            bool isDstStruct = IsApdStruct(dstStatus);

            string ret;
            if (isDstStruct)
            {
                // apd version

                // get both versions as apd
                string srcVal = ConvertVariable(funcParam, dstStatus, dstPrecision, srcParam, srcStatus, srcPrecision, srcName);

                int prec = (int)dstPrecision;
                int funcTypeIndex = FloatTypeToIndex(funcParam);
                m_singleFunc[prec].Set((int)func, funcTypeIndex, true);

                string mergedOp;

                switch (func)
                {
                    case SingleFunc.Saturate:
                        mergedOp = "SaturateApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Rcp:
                        mergedOp = "RcpApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Sqr:
                        mergedOp = "SqrApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Log:
                        mergedOp = "LogApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Log2:
                        mergedOp = "Log2Apd(" + srcVal + ")";
                        break;
                    case SingleFunc.Log10:
                        mergedOp = "Log10Apd(" + srcVal + ")";
                        break;
                    case SingleFunc.Exp:
                        mergedOp = "ExpApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Exp2:
                        mergedOp = "Exp2Apd(" + srcVal + ")";
                        break;
                    case SingleFunc.Sqrt:
                        mergedOp = "SqrtApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Rsqrt:
                        m_singleFunc[prec].Set((int)SingleFunc.Sqrt, 0, true);
                        m_singleFunc[prec].Set((int)SingleFunc.Rcp, 0, true);
                        mergedOp = "RsqrtApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Normalize:
                        SetLenSqrDependencies(funcTypeIndex, dstPrecision);
                        m_func1[prec].Set((int)Func1.InvLen, funcTypeIndex, true);
                        m_singleFunc[prec].Set((int)SingleFunc.Rsqrt, 0,true);
                        m_singleFunc[prec].Set((int)SingleFunc.Sqrt, 0, true);
                        m_singleFunc[prec].Set((int)SingleFunc.Rcp, 0, true);

                        m_splatStructFromScalar[(int)dstPrecision][funcTypeIndex] = true;
                        m_singleFunc[(int)dstPrecision].Set((int)BinaryFunc.Mul, funcTypeIndex, true);
                        mergedOp = "NormalizeApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Frac:
                        mergedOp = "FracApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Cos:
                        mergedOp = "CosApd(" + srcVal + ")";
                        break;
                    case SingleFunc.CosH:
                        mergedOp = "CosHApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Sin:
                        mergedOp = "SinApd(" + srcVal + ")";
                        break;
                    case SingleFunc.SinH:
                        mergedOp = "SinHApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Tan:
                        mergedOp = "TanApd(" + srcVal + ")";
                        break;
                    case SingleFunc.TanH:
                        mergedOp = "TanHApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Abs:
                        mergedOp = "AbsApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Negate:
                        mergedOp = "NegateApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Floor:
                        mergedOp = "FloorApd(" + srcVal + ")";
                        break;
                    case SingleFunc.Ceil:
                        mergedOp = "CeilApd(" + srcVal + ")";
                        break;
                    default:
                        mergedOp = "<invalid>";
                        HlslUtil.ParserAssert(false);
                        break;
                }

                ret = ConvertVariable(dstParam, dstStatus, dstPrecision, funcParam, dstStatus, dstPrecision, mergedOp);
            }
            else
            {
                // regular fpd version

                // first, get both variables as base
                string srcVal = ConvertVariable(funcParam, dstStatus, dstPrecision, srcParam, srcStatus, srcPrecision, srcName);

                string mergedOp;

                switch (func)
                {
                    case SingleFunc.Saturate:
                        mergedOp = "saturate(" + srcVal + ")";
                        break;
                    case SingleFunc.Rcp:
                        mergedOp = "rcp(" + srcVal + ")";
                        break;
                    case SingleFunc.Sqr:
                        mergedOp = "((" + srcVal + ")*(" + srcVal + "))";
                        break;
                    case SingleFunc.Log:
                        mergedOp = "log(" + srcVal + ")";
                        break;
                    case SingleFunc.Log2:
                        mergedOp = "log2(" + srcVal + ")";
                        break;
                    case SingleFunc.Log10:
                        mergedOp = "log10(" + srcVal + ")";
                        break;
                    case SingleFunc.Exp:
                        mergedOp = "exp(" + srcVal + ")";
                        break;
                    case SingleFunc.Exp2:
                        mergedOp = "exp2(" + srcVal + ")";
                        break;
                    case SingleFunc.Sqrt:
                        mergedOp = "sqrt(" + srcVal + ")";
                        break;
                    case SingleFunc.Rsqrt:
                        mergedOp = "rcp(sqrt(" + srcVal + "))";
                        break;
                    case SingleFunc.Normalize:
                        mergedOp = "normalize(" + srcVal + ")";
                        break;
                    case SingleFunc.Frac:
                        mergedOp = "frac(" + srcVal + ")";
                        break;
                    case SingleFunc.Cos:
                        mergedOp = "cos(" + srcVal + ")";
                        break;
                    case SingleFunc.CosH:
                        mergedOp = "cosh(" + srcVal + ")";
                        break;
                    case SingleFunc.Sin:
                        mergedOp = "sin(" + srcVal + ")";
                        break;
                    case SingleFunc.SinH:
                        mergedOp = "sinh(" + srcVal + ")";
                        break;
                    case SingleFunc.Tan:
                        mergedOp = "tan(" + srcVal + ")";
                        break;
                    case SingleFunc.TanH:
                        mergedOp = "tanh(" + srcVal + ")";
                        break;
                    case SingleFunc.Abs:
                        mergedOp = "abs(" + srcVal + ")";
                        break;
                    case SingleFunc.Negate:
                        mergedOp = "(-(" + srcVal + "))";
                        break;
                    case SingleFunc.Floor:
                        mergedOp = "floor(" + srcVal + ")";
                        break;
                    case SingleFunc.Ceil:
                        mergedOp = "ceil(" + srcVal + ")";
                        break;
                    default:
                        mergedOp = "<invalid>";
                        HlslUtil.ParserAssert(false);
                        break;
                }

                ret = ConvertVariable(dstParam, dstStatus, dstPrecision, funcParam, dstStatus, dstPrecision, mergedOp);
            }

            return ret;
        }

        // this is common enough that it needs a helper function
        void SetLenSqrDependencies(int funcTypeIndex, ConcretePrecision dstPrecision)
        {
            int prec = (int)dstPrecision;

            m_func1[prec].Set((int)Func1.LenSqr, funcTypeIndex, true);
            m_binaryFunc[prec].Set((int)BinaryFunc.Add, 0, true);
            m_singleFunc[prec].Set((int)SingleFunc.Sqr, 0, true);

            for (int iter = 0; iter <= funcTypeIndex; iter++)
            {
                m_extractFromApd[prec].Set(funcTypeIndex, iter, true);
            }
        }

        internal string MakeFunc1(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType srcParam, ApdStatus srcStatus, ConcretePrecision srcPrecision, string srcName,
            Func1 func)
        {
            ConcreteSlotValueType funcParam = dstParam;

            bool isDstStruct = IsApdStruct(dstStatus);

            ConcreteSlotValueType expectedDstType = GetFunc1ReturnType(func, srcParam);
            HlslUtil.ParserAssert(dstParam == expectedDstType);

            string ret;
            if (isDstStruct)
            {
                // apd version

                // convert precision and apd but preserve type
                string srcVal = ConvertVariable(srcParam, dstStatus, dstPrecision, srcParam, srcStatus, srcPrecision, srcName);

                int prec = (int)dstPrecision;

                int funcTypeIndex = FloatTypeToIndex(srcParam);
                m_func1[prec].Set((int)func, funcTypeIndex, true);

                // all of these functions requires LenSqr() and it's dependencies (add, sqr, and extract)
                SetLenSqrDependencies(funcTypeIndex,dstPrecision);

                string mergedOp;

                switch (func)
                {
                    case Func1.LenSqr:
                        mergedOp = "LenSqrApd(" + srcVal + ")";
                        break;
                    case Func1.Len:
                        m_singleFunc[prec].Set((int)SingleFunc.Sqrt, 0, true);
                        mergedOp = "LenApd(" + srcVal + ")";
                        break;
                    case Func1.InvLen:
                        m_singleFunc[prec].Set((int)SingleFunc.Sqrt, 0, true);
                        m_singleFunc[prec].Set((int)SingleFunc.Rcp, 0, true);
                        m_singleFunc[prec].Set((int)SingleFunc.Rsqrt, 0, true);
                        mergedOp = "InvLenApd(" + srcVal + ")";
                        break;
                    case Func1.InvLenSqr:
                        m_singleFunc[prec].Set((int)SingleFunc.Rcp, 0, true);
                        mergedOp = "InvLenSqrApd(" + srcVal + ")";
                        break;
                    default:
                        mergedOp = "<invalid>";
                        HlslUtil.ParserAssert(false);
                        break;
                }

                ret = mergedOp;
            }
            else
            {
                // regular fpd version

                // first, get both variables as base
                string srcVal = ConvertVariable(srcParam, dstStatus, dstPrecision, srcParam, srcStatus, srcPrecision, srcName);

                string mergedOp;

                switch (func)
                {
                    case Func1.Len:
                        mergedOp = "length(" + srcVal + ")";
                        break;
                    case Func1.LenSqr:
                        mergedOp = "dot(" + srcVal + "," + srcVal + ")";
                        break;
                    case Func1.InvLen:
                        mergedOp = "rcp(length(" + srcVal + "))";
                        break;
                    case Func1.InvLenSqr:
                        mergedOp = "rcp(dot(" + srcVal + "," + srcVal + "))"; ;
                        break;
                    default:
                        mergedOp = "<invalid>";
                        HlslUtil.ParserAssert(false);
                        break;
                }

                ret = mergedOp;
            }
            return ret;
        }

        internal string MakeFunc2(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType param0, ApdStatus status0, ConcretePrecision precision0, string name0,
            ConcreteSlotValueType param1, ApdStatus status1, ConcretePrecision precision1, string name1,
            Func2 func)
        {
            string ret;
            switch (func)
            {
                case Func2.Dot:
                    ret = MakeDot(dstParam, dstStatus, dstPrecision,
                            param0, status0, precision0, name0,
                            param1, status1, precision1, name1);
                    break;
                case Func2.Cross:
                    ret = MakeCross(dstParam, dstStatus, dstPrecision,
                            param0, status0, precision0, name0,
                            param1, status1, precision1, name1);
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    ret = "<invalid>";
                    break;
            }
            return ret;
        }

        internal string MakeFunc3(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType param0, ApdStatus status0, ConcretePrecision precision0, string name0,
            ConcreteSlotValueType param1, ApdStatus status1, ConcretePrecision precision1, string name1,
            ConcreteSlotValueType param2, ApdStatus status2, ConcretePrecision precision2, string name2,
            Func3 func)
        {
            string ret;
            switch (func)
            {
                case Func3.Lerp:
                    ret = MakeLerp(dstParam, dstStatus, dstPrecision,
                            param0, status0, precision0, name0,
                            param1, status1, precision1, name1,
                            param2, status2, precision2, name2);
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    ret = "<invalid>";
                    break;
            }
            return ret;
        }

        internal string MakeComparison(
            ComparisonType comparison,
            ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType param0, ApdStatus status0, ConcretePrecision precision0, string name0,
            ConcreteSlotValueType param1, ApdStatus status1, ConcretePrecision precision1, string name1)
        {
            HlslUtil.ParserAssert(dstParam == ConcreteSlotValueType.Boolean);
            HlslUtil.ParserAssert(dstStatus == ApdStatus.Zero);

            string op = "<invalid>";
            switch (comparison)
            {
                case ComparisonType.Equal:
                    op = "==";
                    break;
                case ComparisonType.NotEqual:
                    op = "!=";
                    break;
                case ComparisonType.Less:
                    op = "<";
                    break;
                case ComparisonType.LessOrEqual:
                    op = "<=";
                    break;
                case ComparisonType.Greater:
                    op = ">";
                    break;
                case ComparisonType.GreaterOrEqual:
                    op = ">=";
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            // convert both types to scalars
            string lhsName = ConvertVariable(ConcreteSlotValueType.Vector1, ApdStatus.Zero, dstPrecision, param0, status0, precision0, name0);
            string rhsName = ConvertVariable(ConcreteSlotValueType.Vector1, ApdStatus.Zero, dstPrecision, param1, status1, precision1, name1);

            string ret = "(" + lhsName + " " + op + " " + rhsName + ") ? 1.0f : 0.0f";

            return ret;
        }

        string AsStruct(ConcreteSlotValueType srcParam, ApdStatus srcStatus, ConcretePrecision srcPrecision, string paramName)
        {
            bool isSrcStruct = IsApdStruct(srcStatus);
            string ret = paramName;
            if (!isSrcStruct)
            {
                ret = MakeStructFromFpd(srcParam,paramName,srcPrecision);
            }
            return ret;
        }

        string AsBase(ConcreteSlotValueType srcParam, ApdStatus srcStatus, string paramName)
        {
            bool isSrcStruct = IsApdStruct(srcStatus);
            string ret = paramName;
            if (isSrcStruct)
            {
                ret = paramName + ".m_val";
            }
            return ret;
        }

        string ExtractIndexApd(ConcreteSlotValueType srcParam, ConcretePrecision prec, int index, string paramName)
        {
            string dstName;
            // early out, if the struct is a float1, and we are returning the first index, just return the source
            if (srcParam == ConcreteSlotValueType.Vector1 && index == 0)
            {
                dstName = paramName;
            }
            else
            {
                int typeIndex = FloatTypeToIndex(srcParam);
                m_extractFromApd[(int)prec].Set(typeIndex, index, true);

                string structName = GetApdStructName(typeIndex, prec);

                dstName = "Extract" + structName + "_" + index.ToString() + "(" + paramName + ")";
            }
            return dstName;
        }

        string MergeApd2(ConcretePrecision precision, string name0, string name1)
        {
            string precUpper = GetPrecUpper((int)precision);
            m_mergeToApd[(int)precision][1] = true;
            string ret = "Merge" + precUpper + "Apd2(" + name0 + "," + name1 + ")";
            return ret;
        }

        string MergeApd3(ConcretePrecision precision, string name0, string name1, string name2)
        {
            string precUpper = GetPrecUpper((int)precision);
            m_mergeToApd[(int)precision][2] = true;
            string ret = "Merge" + precUpper + "Apd3(" + name0 + "," + name1 + "," + name2 + ")";
            return ret;
        }

        string MergeApd4(ConcretePrecision precision, string name0, string name1, string name2, string name3)
        {
            string precUpper = GetPrecUpper((int)precision);
            m_mergeToApd[(int)precision][3] = true;
            string ret = "Merge" + precUpper + "Apd4(" + name0 + "," + name1 + "," + name2 + "," + name3 + ")";
            return ret;
        }

        string NormalizeApd(ConcreteSlotValueType param, ConcretePrecision precision, string name)
        {
            int index = FloatTypeToIndex(param);
            m_singleFunc[(int)precision].Set((int)SingleFunc.Normalize, index, true);

            SetLenSqrDependencies(index, precision);

            m_singleFunc[(int)precision].Set((int)SingleFunc.Rsqrt, 0, true);

            m_func1[(int)precision].Set((int)Func1.InvLen,index,true);
            m_splatStructFromScalar[(int)precision][index] = true;
            m_singleFunc[(int)precision].Set((int)BinaryFunc.Mul,index,true);

            string ret = "NormalizeApd(" + name + ")";
            return ret;
        }

        string DotApd(ConcreteSlotValueType param, ConcretePrecision prec, string lhsName, string rhsName)
        {
            int index = FloatTypeToIndex(param);
            m_func2[(int)prec].Set((int)Func2.Dot,index,true);

            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Add,0,true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Mul,0,true);

            for (int iter = 0; iter <= index; iter++)
            {
                m_extractFromApd[(int)prec].Set(index,iter,true);
            }

            string ret = "DotApd(" + lhsName + "," + rhsName + ")";
            return ret;
        }

        string CrossApd(ConcreteSlotValueType param, ConcretePrecision prec, string lhsName, string rhsName)
        {
            int index = FloatTypeToIndex(param);
            HlslUtil.ParserAssert(index == 2); // only valid for vec3 data

            m_func2[(int)prec].Set((int)Func2.Cross,index,true);

            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Add,0,true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Mul,0,true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Sub,0,true);
            for (int iter = 0; iter < 3; iter++)
            {
                m_extractFromApd[(int)prec].Set((int)2,iter,true);
            }
            m_mergeToApd[(int)prec][2] = true;

            string ret = "CrossApd(" + lhsName + "," + rhsName + ")";
            return ret;
        }

        string ReflectApd(ConcreteSlotValueType param, ConcretePrecision prec, string lhsName, string rhsName)
        {
            int index = FloatTypeToIndex(param);
            //HlslUtil.ParserAssert(index == 2); // only valid for vec3 data

            m_func2[(int)prec].Set((int)BinaryFunc.Reflect,index,true);

            m_func2[(int)prec].Set((int)Func2.Dot, index, true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Add,0,true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Mul,0,true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Sub,0,true);
            m_mergeToApd[(int)prec][index] = true;
            m_splatStructFromScalar[(int)prec][index] = true;

            string ret = "ReflectApd(" + lhsName + "," + rhsName + ")";
            return ret;
        }

        string LerpApd(ConcreteSlotValueType param, ConcretePrecision prec, string lhsName, string rhsName, string tName)
        {
            int index = FloatTypeToIndex(param);

            m_func3[(int)prec].Set((int)Func3.Lerp,index,true);

            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Add,index,true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Mul,index,true);
            m_binaryFunc[(int)prec].Set((int)BinaryFunc.Sub,index,true);
            m_splatStructFromScalar[(int)prec][index] = true;
            m_makeStructFromFpd[(int)prec][index] = true;

            string ret = "LerpApd(" + lhsName + "," + rhsName + "," + tName + ")";
            return ret;
        }

        internal string MakeMerge2(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType param0, ApdStatus status0, ConcretePrecision precision0, string name0,
            ConcreteSlotValueType param1, ApdStatus status1, ConcretePrecision precision1, string name1)
        {
            bool isDstStruct = IsApdStruct(dstStatus);

            string ret;
            if (isDstStruct)
            {
                string val0 = ConvertVariable(param0, dstStatus,dstPrecision,param0,status0,precision0,name0);
                string val1 = ConvertVariable(param1, dstStatus, dstPrecision, param1,status1,precision1,name1);
                string scalar0 = ExtractIndexApd(param0, precision0,0,val0);
                string scalar1 = ExtractIndexApd(param1, precision1,0,val1);
                ret = MergeApd2(dstPrecision,scalar0,scalar1);
            }
            else
            {
                string precName = GetPrecisionName(dstPrecision);

                string suffix0 = (param0 == ConcreteSlotValueType.Vector1) ? "" : ".x";
                string suffix1 = (param1 == ConcreteSlotValueType.Vector1) ? "" : ".x";

                ret = precName + "2(" + name0 + suffix0 + "," + name1 + suffix1 + ")";
            }
            return ret;
        }

        internal string MakeMerge3(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType param0, ApdStatus status0, ConcretePrecision precision0, string name0,
            ConcreteSlotValueType param1, ApdStatus status1, ConcretePrecision precision1, string name1,
            ConcreteSlotValueType param2, ApdStatus status2, ConcretePrecision precision2, string name2)
        {
            bool isDstStruct = IsApdStruct(dstStatus);

            string ret;
            if (isDstStruct)
            {
                string val0 = ConvertVariable(param0, dstStatus,dstPrecision,param0,status0,precision0,name0);
                string val1 = ConvertVariable(param1, dstStatus, dstPrecision, param1,status1,precision1,name1);
                string val2 = ConvertVariable(param2, dstStatus, dstPrecision, param2,status2,precision2,name2);
                string scalar0 = ExtractIndexApd(param0,precision0,0,val0);
                string scalar1 = ExtractIndexApd(param1,precision1,0,val1);
                string scalar2 = ExtractIndexApd(param2,precision2,0,val2);
                ret = MergeApd3(dstPrecision,scalar0,scalar1,scalar2);
            }
            else
            {
                string precName = GetPrecisionName(dstPrecision);

                string suffix0 = (param0 == ConcreteSlotValueType.Vector1) ? "" : ".x";
                string suffix1 = (param1 == ConcreteSlotValueType.Vector1) ? "" : ".x";
                string suffix2 = (param2 == ConcreteSlotValueType.Vector1) ? "" : ".x";

                ret = precName + "3(" + name0 + suffix0 + "," + name1 + suffix1 + "," + name2 + suffix2 + ")";
            }
            return ret;
        }

        internal string MakeMerge4(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType param0, ApdStatus status0, ConcretePrecision precision0, string name0,
            ConcreteSlotValueType param1, ApdStatus status1, ConcretePrecision precision1, string name1,
            ConcreteSlotValueType param2, ApdStatus status2, ConcretePrecision precision2, string name2,
            ConcreteSlotValueType param3, ApdStatus status3, ConcretePrecision precision3, string name3)
        {
            bool isDstStruct = IsApdStruct(dstStatus);

            string ret;
            if (isDstStruct)
            {
                // does this work correctly for mixed precision? probably not?
                string val0 = ConvertVariable(param0, dstStatus, dstPrecision, param0,status0, precision0, name0);
                string val1 = ConvertVariable(param1, dstStatus,dstPrecision,param1,status1, precision1, name1);
                string val2 = ConvertVariable(param2, dstStatus, dstPrecision, param2,status2, precision2, name2);
                string val3 = ConvertVariable(param3, dstStatus, dstPrecision, param3,status3, precision3, name3);
                string scalar0 = ExtractIndexApd(param0,precision0, 0,val0);
                string scalar1 = ExtractIndexApd(param1,precision1,0,val1);
                string scalar2 = ExtractIndexApd(param2,precision2,0,val2);
                string scalar3 = ExtractIndexApd(param3,precision3,0,val3);
                ret = MergeApd4(dstPrecision,scalar0,scalar1,scalar2,scalar3);
            }
            else
            {
                string precName = GetPrecisionName(dstPrecision);

                string suffix0 = (param0 == ConcreteSlotValueType.Vector1) ? "" : ".x";
                string suffix1 = (param1 == ConcreteSlotValueType.Vector1) ? "" : ".x";
                string suffix2 = (param2 == ConcreteSlotValueType.Vector1) ? "" : ".x";
                string suffix3 = (param3 == ConcreteSlotValueType.Vector1) ? "" : ".x";

                ret = precName + "4(" + name0 + suffix0 + "," + name1 + suffix1 + "," + name2 + suffix2 + "," + name3 + suffix3 + ")";
            }
            return ret;
        }

        internal string MakeMergeMatrix(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType[] paramVec, ApdStatus[] statusVec, ConcretePrecision[] precisionVec, string[] nameVec,
            int dim)
        {
            bool isDstStruct = IsApdStruct(dstStatus);
            HlslUtil.ParserAssert(!isDstStruct);

            HlslUtil.ParserAssert(paramVec.Length == dim*dim);
            HlslUtil.ParserAssert(statusVec.Length == dim * dim);
            HlslUtil.ParserAssert(precisionVec.Length == dim * dim);
            HlslUtil.ParserAssert(nameVec.Length == dim * dim);

            string[] vals = new string[dim * dim];
            for (int i = 0; i < dim * dim; i++)
            {
                vals[i] = ConvertVariable(ConcreteSlotValueType.Vector1, ApdStatus.Zero, dstPrecision, paramVec[i], statusVec[i], precisionVec[i], nameVec[i]);
            }

            string precName = GetPrecisionName(dstPrecision);

            string ret = precName + dim.ToString() + "x" + dim.ToString() + "(";

            for (int i = 0; i < dim*dim; i++)
            {
                ret += vals[i];
                if (i < dim*dim-1)
                {
                    ret += ",";
                }
            }
            ret += ")";
            return ret;
        }

        internal string MakeScalarZero(ApdStatus dstStatus, ConcretePrecision dstPrecision)
        {
            string ret;
            bool isDstStruct = IsApdStruct(dstStatus);
            if (isDstStruct)
            {
                ret = MakeStructFromFpd(ConcreteSlotValueType.Vector1, "0", dstPrecision);
            }
            else
            {
                ret = "0";
            }

            return ret;
        }

        internal string MakeSplit(int dstIndex,
            ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType param, ApdStatus status, ConcretePrecision precision, string name)
        {
            int srcTypeIndex = FloatTypeToIndex(param);
            bool isDstStruct = IsApdStruct(dstStatus);

            bool needsPrecisionChange = (dstPrecision != precision);
            string dstPrecisionCast = needsPrecisionChange ? "(" + GetPrecisionName(dstPrecision) + ")" : "";

            string ret;
            if (dstIndex > srcTypeIndex)
            {
                ret = MakeScalarZero(dstStatus, dstPrecision);
            }
            else
            {
                if (isDstStruct)
                {
                    string srcApd = AsStruct(param, status, precision, name);
                    ret = ExtractIndexApd(param, precision, dstIndex, srcApd);
                }
                else
                {
                    string srcFpd = AsBase(param, status, name);
                    ret = dstPrecisionCast + srcFpd + "." + GetSuffixLower()[dstIndex];
                }
            }
            return ret;
        }

        // only valid on non-struct parameters
        internal static string GetPrecisionCast(ConcreteSlotValueType paramType, ConcretePrecision dstPrecision, ConcretePrecision srcPrecision)
        {
            string ret = "";
            if (dstPrecision != srcPrecision)
            {
                int index = FloatTypeToIndex(paramType);
                string baseName = GetApdBaseName(index,dstPrecision);

                ret = "(" + baseName + ")";
            }
            return ret;
        }

        internal string MakeDot(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType lhsParam, ApdStatus lhsStatus, ConcretePrecision lhsPrecision, string lhsName,
            ConcreteSlotValueType rhsParam, ApdStatus rhsStatus, ConcretePrecision rhsPrecision, string rhsName)
        {
            bool isDstStruct = IsApdStruct(dstStatus);
            //HlslUtil.ParserAssert(lhsParam == rhsParam);

            ConcretePrecision binaryPrecision = GetBinaryOpPrecisionType(lhsPrecision, rhsPrecision);
            ConcreteSlotValueType binaryParam = GetBinaryOpReturnTypeVector(lhsParam, rhsParam);

            string ret;
            if (isDstStruct)
            {
                string lhsApd = ConvertVariable(binaryParam, dstStatus, dstPrecision, lhsParam,lhsStatus,lhsPrecision,lhsName);
                string rhsApd = ConvertVariable(binaryParam, dstStatus, dstPrecision, rhsParam,rhsStatus,rhsPrecision,rhsName);
                ret = DotApd(lhsParam,binaryPrecision,lhsApd, rhsApd);
            }
            else
            {
                string lhsFpd = AsBase(binaryParam, lhsStatus,lhsName);
                string rhsFpd = AsBase(binaryParam, rhsStatus,rhsName);
                ret = GetPrecisionCast(dstParam, dstPrecision, binaryPrecision) + "dot(" + lhsFpd + "," + rhsFpd + ")";
            }

            return ret;
        }

        internal string MakeBranch(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType predicateParam, ApdStatus predicateStatus, ConcretePrecision predicatePrecision, string predicateName,
            ConcreteSlotValueType lhsParam, ApdStatus lhsStatus, ConcretePrecision lhsPrecision, string lhsName,
            ConcreteSlotValueType rhsParam, ApdStatus rhsStatus, ConcretePrecision rhsPrecision, string rhsName)
        {
            // basic function:
            //     return predicate ? lhs : rhs;

            bool isDstStruct = IsApdStruct(dstStatus);

            // Predicate apd status should NEVER be valid. Since it's a bool, it is known to be zero, so it will always be known zero, not
            // needed, or invalid.
            HlslUtil.ParserAssert(!IsApdStruct(predicateStatus));

            // precision is based on lhs and rhs, predicate is irrelevant for precision
            ConcretePrecision binaryPrecision = GetBinaryOpPrecisionType(lhsPrecision, rhsPrecision);

            // convert lhs and rhs to the preferred type
            string lhsVariable = ConvertVariable(dstParam, dstStatus, dstPrecision, lhsParam, lhsStatus, lhsPrecision, lhsName);
            string rhsVariable = ConvertVariable(dstParam, dstStatus, dstPrecision, rhsParam, rhsStatus, rhsPrecision, rhsName);

            string ret;
            if (isDstStruct)
            {
                int dstIndex = FloatTypeToIndex(dstParam);

                m_selectApd[(int)dstPrecision][dstIndex] = true;

                string structName = GetApdStructName(dstIndex, dstPrecision);
                string baseName = GetApdBaseName(dstIndex, dstPrecision);
                string funcName = "Make" + structName + "Select";

                ret = funcName + "(" + predicateName + "," + lhsVariable + "," + rhsVariable + ")";
            }
            else
            {
                ret = predicateName + " ? " + lhsVariable + " : " + rhsVariable;
            }

            return ret;
        }

        internal string MakeCross(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType lhsParam, ApdStatus lhsStatus, ConcretePrecision lhsPrecision, string lhsName,
            ConcreteSlotValueType rhsParam, ApdStatus rhsStatus, ConcretePrecision rhsPrecision, string rhsName)
        {
            bool isDstStruct = IsApdStruct(dstStatus);
            HlslUtil.ParserAssert(lhsParam == ConcreteSlotValueType.Vector3);
            HlslUtil.ParserAssert(rhsParam == ConcreteSlotValueType.Vector3);

            ConcretePrecision binaryPrecision = GetBinaryOpPrecisionType(lhsPrecision, rhsPrecision);

            string ret;
            if (isDstStruct)
            {
                string lhsApd = ConvertVariable(dstParam, dstStatus, dstPrecision, lhsParam, lhsStatus,lhsPrecision,lhsName);
                string rhsApd = ConvertVariable(dstParam, dstStatus, dstPrecision, rhsParam, rhsStatus,rhsPrecision,rhsName);
                ret = CrossApd(lhsParam,binaryPrecision,lhsApd,rhsApd);
            }
            else
            {
                string lhsFpd = AsBase(lhsParam,lhsStatus,lhsName);
                string rhsFpd = AsBase(rhsParam,rhsStatus,rhsName);
                ret = GetPrecisionCast(dstParam, dstPrecision, binaryPrecision) + "cross(" + lhsFpd + "," + rhsFpd + ")";
            }

            return ret;
        }

        internal string MakeReflect(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType lhsParam, ApdStatus lhsStatus, ConcretePrecision lhsPrecision, string lhsName,
            ConcreteSlotValueType rhsParam, ApdStatus rhsStatus, ConcretePrecision rhsPrecision, string rhsName)
        {
            bool isDstStruct = IsApdStruct(dstStatus);
            HlslUtil.ParserAssert(lhsParam == rhsParam);

            ConcretePrecision binaryPrecision = GetBinaryOpPrecisionType(lhsPrecision, rhsPrecision);

            string ret;
            if (isDstStruct)
            {
                string lhsApd = ConvertVariable(dstParam, dstStatus, dstPrecision, lhsParam, lhsStatus,lhsPrecision,lhsName);
                string rhsApd = ConvertVariable(dstParam, dstStatus, dstPrecision, rhsParam, rhsStatus,rhsPrecision,rhsName);
                ret = ReflectApd(dstParam,dstPrecision,lhsApd,rhsApd);
            }
            else
            {
                string lhsFpd = AsBase(lhsParam,lhsStatus,lhsName);
                string rhsFpd = AsBase(rhsParam,rhsStatus,rhsName);
                ret = GetPrecisionCast(dstParam, dstPrecision, binaryPrecision) + "reflect(" + lhsFpd + "," + rhsFpd + ")";
            }

            return ret;
        }

        internal string MakeLerp(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            ConcreteSlotValueType lhsParam, ApdStatus lhsStatus, ConcretePrecision lhsPrecision, string lhsName,
            ConcreteSlotValueType rhsParam, ApdStatus rhsStatus, ConcretePrecision rhsPrecision, string rhsName,
            ConcreteSlotValueType tParam, ApdStatus tStatus, ConcretePrecision tPrecision, string tName)
        {
            ConcreteSlotValueType funcParam = GetBinaryOpReturnTypeVector(lhsParam,rhsParam);

            ConcretePrecision tripletPrecision = GetBinaryOpPrecisionType(tPrecision,GetBinaryOpPrecisionType(lhsPrecision, rhsPrecision));

            bool isDstStruct = IsApdStruct(dstStatus);

            string ret;
            if (isDstStruct)
            {
                string lhsV = ConvertVariable(funcParam,dstStatus,dstPrecision,lhsParam,lhsStatus, lhsPrecision, lhsName);
                string rhsV = ConvertVariable(funcParam,dstStatus,dstPrecision,rhsParam,rhsStatus,rhsPrecision,rhsName);
                string tV = ConvertVariable(funcParam,dstStatus,dstPrecision,tParam,tStatus, tPrecision, tName);

                ret = LerpApd(funcParam,dstPrecision,lhsV,rhsV,tV);
            }
            else
            {
                string lhsV = ConvertVariable(funcParam,dstStatus,dstPrecision,lhsParam,lhsStatus, lhsPrecision, lhsName);
                string rhsV = ConvertVariable(funcParam,dstStatus,dstPrecision,rhsParam,rhsStatus,rhsPrecision,rhsName);
                string tV = ConvertVariable(funcParam,dstStatus,dstPrecision,tParam,tStatus, tPrecision, tName);

                ret = GetPrecisionCast(dstParam, dstPrecision, tripletPrecision) + "lerp(" + lhsV + "," + rhsV + "," + tV + ")";
            }

            return ret;
        }

        // maybe we should merge these 5 funcs?
        internal string MakeTextureSample2d(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            string texName,
            string samplerName,
            ConcreteSlotValueType uvParamSrc, ApdStatus uvStatus, ConcretePrecision uvPrecision, string srcName,
            bool useApd,
            bool isPixelShader)
        {
            HlslUtil.ParserAssert(dstParam == ConcreteSlotValueType.Vector4);

            // for now uv, will always be float precision, but we could add a variation later
            string castedName = MakeImplicitCast(ConcreteSlotValueType.Vector2, uvStatus, ConcretePrecision.Single, uvParamSrc, uvStatus, uvPrecision, srcName);

            string ret;
            string precUpper = GetPrecUpper((int)dstPrecision);

            if (!useApd)
            {
                m_texSample2d[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                ret = GetPrecisionCast(dstParam, dstPrecision, uvPrecision) + "TextureSample2d" + precUpper + "Fpd(" + texName + "," + samplerName + "," + castedName + ")";
            }
            else
            {
                if (uvStatus != ApdStatus.Valid)
                {
                    // In this case, we don't know the derivative. If it's a pixel shader, we can fall back to APD, hope for the best and
                    // live with artifacts along edges. However, if it's a CS, we have no choice but to fall back to level 0.
                    string uvFpd = AsBase(ConcreteSlotValueType.Vector2, uvStatus, castedName);

                    if (isPixelShader)
                    {
                        m_texSample2d[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                        ret = "TextureSample2d" + precUpper + "Fpd(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }
                    else
                    {
                        m_texSample2d[(int)dstPrecision][(int)TexSampleType.Lod0] = true;
                        ret = "TextureSample2d" + precUpper + "Lod0(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }

                    bool isDstStruct = IsApdStruct(dstStatus);
                    if (isDstStruct)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector4, ret, uvPrecision);
                    }
                }
                else
                {
                    bool isDstStruct = IsApdStruct(dstStatus);

                    string uvApd = AsStruct(ConcreteSlotValueType.Vector2, uvStatus, uvPrecision, castedName);
                    if (!isDstStruct)
                    {
                        m_texSample2d[(int)dstPrecision][(int)TexSampleType.Apd] = true;

                        ret = "TextureSample2d" + precUpper + "Apd(" + texName + "," + samplerName + "," + uvApd + ")";
                    }
                    else
                    {
                        m_texSample2d[(int)dstPrecision][(int)TexSampleType.Apd_3x] = true;

                        ret = "TextureSample2d" + precUpper + "Apd3x(" + texName + "," + samplerName + "," + uvApd + ")";
                    }

                }

            }

            return ret;
        }

        internal string MakeTextureSample2dArray(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            string texName,
            string samplerName,
            ConcreteSlotValueType uvParamSrc, ApdStatus uvStatus, ConcretePrecision uvPrecision, string srcName,
            bool useApd,
            bool isPixelShader)
        {
            HlslUtil.ParserAssert(dstParam == ConcreteSlotValueType.Vector4);

            // for now uv, will always be float precision, but we could add a variation later
            string castedName = MakeImplicitCast(ConcreteSlotValueType.Vector3, uvStatus, ConcretePrecision.Single, uvParamSrc, uvStatus, uvPrecision, srcName);

            string ret;
            string precUpper = GetPrecUpper((int)dstPrecision);

            if (!useApd)
            {
                m_texSample2dArray[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                ret = GetPrecisionCast(dstParam, dstPrecision, uvPrecision) + "TextureSample2dArray" + precUpper + "Fpd(" + texName + "," + samplerName + "," + castedName + ")";
            }
            else
            {
                //HlslUtil.ParserAssert(uvStatus != ApdStatus.Unknown);

                if (uvStatus != ApdStatus.Valid)
                {

                    // In this case, we don't know the derivative. If it's a pixel shader, we can fall back to APD, hope for the best and
                    // live with artifacts along edges. However, if it's a CS, we have no choice but to fall back to level 0.
                    string uvFpd = AsBase(ConcreteSlotValueType.Vector3, uvStatus, castedName);

                    if (isPixelShader)
                    {
                        m_texSample2dArray[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                        ret = "TextureSample2dArray" + precUpper + "Fpd(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }
                    else
                    {
                        m_texSample2dArray[(int)dstPrecision][(int)TexSampleType.Lod0] = true;
                        ret = "TextureSample2dArray" + precUpper + "Lod0(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }

                    bool isDstStruct = IsApdStruct(dstStatus);
                    if (isDstStruct)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector4, ret, uvPrecision);
                    }
                }
                else
                {
                    bool isDstStruct = IsApdStruct(dstStatus);

                    string uvApd = AsStruct(ConcreteSlotValueType.Vector3, uvStatus, uvPrecision, castedName);
                    if (!isDstStruct)
                    {
                        m_texSample2dArray[(int)dstPrecision][(int)TexSampleType.Apd] = true;

                        ret = "TextureSample2dArray" + precUpper + "Apd(" + texName + "," + samplerName + "," + uvApd + ")";
                    }
                    else
                    {
                        m_texSample2dArray[(int)dstPrecision][(int)TexSampleType.Apd_3x] = true;

                        ret = "TextureSample2dArray" + precUpper + "Apd3x(" + texName + "," + samplerName + "," + uvApd + ")";
                    }

                }

            }

            return ret;
        }

        internal string MakeTextureSampleCube(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            string texName,
            string samplerName,
            ConcreteSlotValueType uvParamSrc, ApdStatus uvStatus, ConcretePrecision uvPrecision, string srcName,
            bool useApd,
            bool isPixelShader)
        {
            HlslUtil.ParserAssert(dstParam == ConcreteSlotValueType.Vector4);

            // for now uv, will always be float precision, but we could add a variation later
            string castedName = MakeImplicitCast(ConcreteSlotValueType.Vector3, uvStatus, ConcretePrecision.Single, uvParamSrc, uvStatus, uvPrecision, srcName);

            string ret;
            string precUpper = GetPrecUpper((int)dstPrecision);

            if (!useApd)
            {
                m_texSampleCube[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                ret = GetPrecisionCast(dstParam, dstPrecision, uvPrecision) + "TextureSampleCube" + precUpper + "Fpd(" + texName + "," + samplerName + "," + castedName + ")";
            }
            else
            {
                if (uvStatus != ApdStatus.Valid)
                {
                    // In this case, we don't know the derivative. If it's a pixel shader, we can fall back to APD, hope for the best and
                    // live with artifacts along edges. However, if it's a CS, we have no choice but to fall back to level 0.
                    string uvFpd = AsBase(ConcreteSlotValueType.Vector3, uvStatus, castedName);

                    if (isPixelShader)
                    {
                        m_texSampleCube[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                        ret = "TextureSampleCube" + precUpper + "Fpd(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }
                    else
                    {
                        m_texSampleCube[(int)dstPrecision][(int)TexSampleType.Lod0] = true;
                        ret = "TextureSampleCube" + precUpper + "Lod0(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }

                    bool isDstStruct = IsApdStruct(dstStatus);
                    if (isDstStruct)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector4, ret, uvPrecision);
                    }
                }
                else
                {
                    bool isDstStruct = IsApdStruct(dstStatus);

                    string uvApd = AsStruct(ConcreteSlotValueType.Vector3, uvStatus, uvPrecision, castedName);
                    if (!isDstStruct)
                    {
                        m_texSampleCube[(int)dstPrecision][(int)TexSampleType.Apd] = true;

                        ret = "TextureSampleCube" + precUpper + "Apd(" + texName + "," + samplerName + "," + uvApd + ")";
                    }
                    else
                    {
                        m_texSampleCube[(int)dstPrecision][(int)TexSampleType.Apd_3x] = true;

                        ret = "TextureSampleCube" + precUpper + "Apd3x(" + texName + "," + samplerName + "," + uvApd + ")";
                    }

                }

            }

            return ret;
        }

        internal string MakeTextureSampleCubeArray(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            string texName,
            string samplerName,
            ConcreteSlotValueType uvParamSrc, ApdStatus uvStatus, ConcretePrecision uvPrecision, string srcName,
            bool useApd,
            bool isPixelShader)
        {
            HlslUtil.ParserAssert(dstParam == ConcreteSlotValueType.Vector4);

            // for now uv, will always be float precision, but we could add a variation later
            string castedName = MakeImplicitCast(ConcreteSlotValueType.Vector4, uvStatus, ConcretePrecision.Single, uvParamSrc, uvStatus, uvPrecision, srcName);

            string ret;
            string precUpper = GetPrecUpper((int)dstPrecision);

            if (!useApd)
            {
                m_texSampleCubeArray[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                ret = GetPrecisionCast(dstParam, dstPrecision, uvPrecision) + "TextureSampleCubeArray" + precUpper + "Fpd(" + texName + "," + samplerName + "," + castedName + ")";
            }
            else
            {
                if (uvStatus != ApdStatus.Valid)
                {
                    // In this case, we don't know the derivative. If it's a pixel shader, we can fall back to APD, hope for the best and
                    // live with artifacts along edges. However, if it's a CS, we have no choice but to fall back to level 0.
                    string uvFpd = AsBase(ConcreteSlotValueType.Vector4, uvStatus, castedName);

                    if (isPixelShader)
                    {
                        m_texSampleCubeArray[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                        ret = "TextureSampleCubeArray" + precUpper + "Fpd(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }
                    else
                    {
                        m_texSampleCubeArray[(int)dstPrecision][(int)TexSampleType.Lod0] = true;
                        ret = "TextureSampleCubeArray" + precUpper + "Lod0(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }

                    bool isDstStruct = IsApdStruct(dstStatus);
                    if (isDstStruct)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector4, ret, uvPrecision);
                    }
                }
                else
                {
                    bool isDstStruct = IsApdStruct(dstStatus);

                    string uvApd = AsStruct(ConcreteSlotValueType.Vector4, uvStatus, uvPrecision, castedName);
                    if (!isDstStruct)
                    {
                        m_texSampleCubeArray[(int)dstPrecision][(int)TexSampleType.Apd] = true;

                        ret = "TextureSampleCubeArray" + precUpper + "Apd(" + texName + "," + samplerName + "," + uvApd + ")";
                    }
                    else
                    {
                        m_texSampleCubeArray[(int)dstPrecision][(int)TexSampleType.Apd_3x] = true;

                        ret = "TextureSampleCubeArray" + precUpper + "Apd3x(" + texName + "," + samplerName + "," + uvApd + ")";
                    }

                }

            }

            return ret;
        }

        internal string MakeTextureSample3d(ConcreteSlotValueType dstParam, ApdStatus dstStatus, ConcretePrecision dstPrecision,
            string texName,
            string samplerName,
            ConcreteSlotValueType uvParamSrc, ApdStatus uvStatus, ConcretePrecision uvPrecision, string srcName,
            bool useApd,
            bool isPixelShader)
        {
            HlslUtil.ParserAssert(dstParam == ConcreteSlotValueType.Vector4);

            // for now uv, will always be float precision, but we could add a variation later
            string castedName = MakeImplicitCast(ConcreteSlotValueType.Vector3, uvStatus, ConcretePrecision.Single, uvParamSrc, uvStatus, uvPrecision, srcName);

            string ret;
            string precUpper = GetPrecUpper((int)dstPrecision);

            if (!useApd)
            {
                m_texSample3d[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                ret = GetPrecisionCast(dstParam, dstPrecision, uvPrecision) + "TextureSample3d" + precUpper + "Fpd(" + texName + "," + samplerName + "," + castedName + ")";
            }
            else
            {
                if (uvStatus != ApdStatus.Valid)
                {
                    // In this case, we don't know the derivative. If it's a pixel shader, we can fall back to APD, hope for the best and
                    // live with artifacts along edges. However, if it's a CS, we have no choice but to fall back to level 0.
                    string uvFpd = AsBase(ConcreteSlotValueType.Vector3, uvStatus, castedName);

                    if (isPixelShader)
                    {
                        m_texSample3d[(int)dstPrecision][(int)TexSampleType.Fpd] = true;
                        ret = "TextureSample3d" + precUpper + "Fpd(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }
                    else
                    {
                        m_texSample3d[(int)dstPrecision][(int)TexSampleType.Lod0] = true;
                        ret = "TextureSample3d" + precUpper + "Lod0(" + texName + "," + samplerName + "," + uvFpd + ")";
                    }

                    bool isDstStruct = IsApdStruct(dstStatus);
                    if (isDstStruct)
                    {
                        ret = SplatStructFromScalar(ConcreteSlotValueType.Vector4, ret, uvPrecision);
                    }
                }
                else
                {
                    bool isDstStruct = IsApdStruct(dstStatus);

                    string uvApd = AsStruct(ConcreteSlotValueType.Vector3, uvStatus, uvPrecision, castedName);
                    if (!isDstStruct)
                    {
                        m_texSample3d[(int)dstPrecision][(int)TexSampleType.Apd] = true;

                        ret = "TextureSample3d" + precUpper + "Apd(" + texName + "," + samplerName + "," + uvApd + ")";
                    }
                    else
                    {
                        m_texSample3d[(int)dstPrecision][(int)TexSampleType.Apd_3x] = true;

                        ret = "TextureSample3d" + precUpper + "Apd3x(" + texName + "," + samplerName + "," + uvApd + ")";
                    }

                }

            }

            return ret;
        }

        internal string MakeSwizzleApd(ConcreteSlotValueType paramType, ConcretePrecision prec, string srcName,
            int numChars,
            int i0,
            int i1,
            int i2,
            int i3)
        {
            int srcTypeIndex = FloatTypeToIndex(paramType);
            HlslUtil.ParserAssert(0 <= srcTypeIndex && srcTypeIndex < 4);
            HlslUtil.ParserAssert(1 <= numChars && numChars <= 4);
            HlslUtil.ParserAssert(0 <= i0 && i0 < 4);
            HlslUtil.ParserAssert(0 <= i1 && i1 < 4);
            HlslUtil.ParserAssert(0 <= i2 && i2 < 4);
            HlslUtil.ParserAssert(0 <= i3 && i3 < 4);

            m_swizzles[(int)prec].Set(srcTypeIndex,numChars-1,i0,i1,i2,i3,true);

            string swizzle = GetSwizzleName(numChars,i0,i1,i2,i3,true);

            string srcStructName = GetApdStructName(srcTypeIndex,prec);

            string ret = "Swizzle" + srcStructName + "_" + swizzle + "(" + srcName + ")";

            return ret;
        }

        internal string MakeSwizzleAssignApd(ConcreteSlotValueType paramType, ConcretePrecision prec, ApdStatus apdStatus, string lhsName,
            string rhsName,
            int numChars,
            int i0,
            int i1,
            int i2,
            int i3)
        {
            int srcTypeIndex = FloatTypeToIndex(paramType);
            HlslUtil.ParserAssert(0 <= srcTypeIndex && srcTypeIndex < 4);
            HlslUtil.ParserAssert(1 <= numChars && numChars <= 4);
            HlslUtil.ParserAssert(0 <= i0 && i0 < 4);
            HlslUtil.ParserAssert(0 <= i1 && i1 < 4);
            HlslUtil.ParserAssert(0 <= i2 && i2 < 4);
            HlslUtil.ParserAssert(0 <= i3 && i3 < 4);

            HlslUtil.ParserAssert(i0 <= srcTypeIndex);
            HlslUtil.ParserAssert(i1 <= srcTypeIndex);
            HlslUtil.ParserAssert(i2 <= srcTypeIndex);
            HlslUtil.ParserAssert(i3 <= srcTypeIndex);

            string ret = "";
            if (apdStatus == ApdStatus.Valid)
            {
                m_swizzlesAssign[(int)prec].Set(srcTypeIndex, numChars - 1, i0, i1, i2, i3, true);

                string swizzle = GetSwizzleName(numChars, i0, i1, i2, i3, true);

                string srcStructName = GetApdStructName(srcTypeIndex, prec);

                ret = "SwizzleAssign" + srcStructName + "_" + swizzle + "(" + lhsName + "," + rhsName + ")";
            }
            else
            {
                string swizzle = GetSwizzleName(numChars, i0, i1, i2, i3, false);
                ret = "(" + lhsName + "." + swizzle + " = " + rhsName + ")";
            }

            return ret;
        }

        internal string MakeUv(ApdStatus status, ConcretePrecision prec, int index)
        {
            HlslUtil.ParserAssert(0 <= index);
            HlslUtil.ParserAssert(index < 4); // really should have a limit of 8 (as in vs streams) but it's 4 for now because of the shader struct

            bool isDstStruct = IsApdStruct(status);

            string ret;
            if (isDstStruct)
            {
                m_fetchUv[(int)prec].Set(index,1,true);

                m_makeStructDirect[(int)prec][3] = true;

                string attrib = "IN.uv" + index.ToString();
                string attribDdx = "IN.uv" + index.ToString() + "Ddx";
                string attribDdy = "IN.uv" + index.ToString() + "Ddy";

                string val = MakeStructFromApdDirect(ConcreteSlotValueType.Vector4, attrib, attribDdx, attribDdy, ConcretePrecision.Single);

                ret = ConvertVariable(ConcreteSlotValueType.Vector4, status, prec, ConcreteSlotValueType.Vector4, ApdStatus.Valid, ConcretePrecision.Single, val);
            }
            else
            {
                m_fetchUv[(int)prec].Set(index,0,true);
                string val = "IN.uv" + index.ToString();
                ret = ConvertVariable(ConcreteSlotValueType.Vector4, status, prec, ConcreteSlotValueType.Vector4, ApdStatus.Zero, ConcretePrecision.Single, val);
            }
            return ret;
        }

        internal string MakeFloatData(ConcreteSlotValueType type, ConcretePrecision precision, float[] data)
        {
            string ret = "";

            string baseName = precision == ConcretePrecision.Single ? "float" : "half";
            switch (type)
            {
                case ConcreteSlotValueType.Vector1:
                    HlslUtil.ParserAssert(data.Length == 1);
                    ret = data[0].ToString();
                    break;
                case ConcreteSlotValueType.Vector2:
                    HlslUtil.ParserAssert(data.Length == 2);
                    ret = baseName + "2(" + data[0].ToString() + "," + data[1].ToString() + ")";
                    break;
                case ConcreteSlotValueType.Vector3:
                    HlslUtil.ParserAssert(data.Length == 3);
                    ret = baseName + "3(" + data[0].ToString() + "," + data[1].ToString() + "," + data[2].ToString() + ")";
                    break;
                case ConcreteSlotValueType.Vector4:
                    HlslUtil.ParserAssert(data.Length == 4);
                    ret = baseName + "4(" + data[0].ToString() + "," + data[1].ToString() + "," + data[2].ToString() + "," + data[3].ToString() + ")";
                    break;
                case ConcreteSlotValueType.Matrix2:
                    HlslUtil.ParserAssert(data.Length == 4);
                    ret = baseName + "2x2("
                        + data[2 * 0 + 0].ToString() + "," + data[2 * 0 + 1].ToString() + ","
                        + data[2 * 1 + 0].ToString() + "," + data[2 * 1 + 1].ToString() + ")";
                    break;
                case ConcreteSlotValueType.Matrix3:
                    HlslUtil.ParserAssert(data.Length == 9);
                    ret = baseName + "3x3("
                        + data[3 * 0 + 0].ToString() + "," + data[3 * 0 + 1].ToString() + "," + data[3 * 0 + 2].ToString() + ","
                        + data[3 * 1 + 0].ToString() + "," + data[3 * 1 + 1].ToString() + "," + data[3 * 1 + 2].ToString() + ","
                        + data[3 * 2 + 0].ToString() + "," + data[3 * 2 + 1].ToString() + "," + data[3 * 2 + 2].ToString() + ")";
                    break;
                case ConcreteSlotValueType.Matrix4:
                    HlslUtil.ParserAssert(data.Length == 16);
                    ret = baseName + "4x4("
                        + data[4 * 0 + 0].ToString() + "," + data[4 * 0 + 1].ToString() + "," + data[4 * 0 + 2].ToString() + "," + data[4 * 0 + 3].ToString() + ","
                        + data[4 * 1 + 0].ToString() + "," + data[4 * 1 + 1].ToString() + "," + data[4 * 1 + 2].ToString() + "," + data[4 * 1 + 3].ToString() + ","
                        + data[4 * 2 + 0].ToString() + "," + data[4 * 2 + 1].ToString() + "," + data[4 * 2 + 2].ToString() + "," + data[4 * 2 + 3].ToString() + ","
                        + data[4 * 3 + 0].ToString() + "," + data[4 * 3 + 1].ToString() + "," + data[4 * 3 + 2].ToString() + "," + data[4 * 3 + 3].ToString() + ")";
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        internal string AdaptNodeOutputForPreview(ConcreteSlotValueType paramType, ApdStatus apdType, ConcretePrecision precision, string variableName)
        {
            string ret = "half4(0, 0, 0, 0)";

            // preview is always dimension 4
            switch (paramType)
            {
                case ConcreteSlotValueType.Vector1:
                    {
                        string adjName = MakeImplicitCast(ConcreteSlotValueType.Vector1, ApdStatus.Zero, ConcretePrecision.Half,
                            paramType, apdType, precision, variableName);
                        ret = string.Format("half4({0}, {0}, {0}, 1.0)", adjName);
                        break;
                    }
                case ConcreteSlotValueType.Vector2:
                    {
                        string adjName = MakeImplicitCast(ConcreteSlotValueType.Vector2, ApdStatus.Zero, ConcretePrecision.Half,
                            paramType, apdType, precision, variableName);
                        ret = string.Format("half4({0}.x, {0}.y, 0.0, 1.0)", adjName);
                        break;
                    }
                case ConcreteSlotValueType.Vector3:
                    {
                        string adjName = MakeImplicitCast(ConcreteSlotValueType.Vector3, ApdStatus.Zero, ConcretePrecision.Half,
                            paramType, apdType, precision, variableName);
                        ret = string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", adjName);
                        break;
                    }
                case ConcreteSlotValueType.Vector4:
                    {
                        string adjName = MakeImplicitCast(ConcreteSlotValueType.Vector4, ApdStatus.Zero, ConcretePrecision.Half,
                            paramType, apdType, precision, variableName);
                        ret = string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", adjName);
                        break;
                    }
                case ConcreteSlotValueType.Boolean:
                    {
                        string adjName = MakeImplicitCast(ConcreteSlotValueType.Boolean, ApdStatus.Zero, ConcretePrecision.Half,
                            paramType, apdType, precision, variableName);

                        // a boolean is really an int
                        ret = string.Format("half4({0}, {0}, {0}, 1.0)", variableName);
                        break;
                    }
                default:
                    // invalid type, return default
                    break;
            }

            return ret;
        }

        // making this a separate struct, for now it is implemented as a vector of bools
        // but we have the option to change it to a bitfield
        struct BoolDim2
        {
            internal BoolDim2(int dim0, int dim1)
            {
                m_Dim0 = dim0;
                m_Dim1 = dim1;
                m_Vec = new bool[dim0 * dim1];
                System.Array.Fill(m_Vec, false);
            }

            internal void Set(int idx0, int idx1, bool val)
            {
                int vecIdx = idx0 * m_Dim1 + idx1;
                m_Vec[vecIdx] = val;
            }

            internal bool Get(int idx0, int idx1)
            {
                int vecIdx = idx0 * m_Dim1 + idx1;
                bool ret = m_Vec[vecIdx];
                return ret;
            }

            int m_Dim0;
            int m_Dim1;
            bool[] m_Vec;
        }

        // might change this to a bitfield too
        struct BoolDim6
        {
            internal BoolDim6(int dim0, int dim1, int dim2, int dim3, int dim4, int dim5)
            {
                m_Dim0 = dim0;
                m_Dim1 = dim1;
                m_Dim2 = dim2;
                m_Dim3 = dim3;
                m_Dim4 = dim4;
                m_Dim5 = dim5;
                m_Vec = new bool[dim0 * dim1 * dim2 * dim3 * dim4 * dim5];
                System.Array.Fill(m_Vec, false);
            }

            private int GetIndex(int idx0, int idx1, int idx2, int idx3, int idx4, int idx5)
            {
                int idx = 0;
                idx += idx0 * (m_Dim1 * m_Dim2 * m_Dim3 * m_Dim4 * m_Dim5);
                idx += idx1 * (m_Dim1 * m_Dim2 * m_Dim3 * m_Dim4);
                idx += idx2 * (m_Dim1 * m_Dim2 * m_Dim3);
                idx += idx3 * (m_Dim1 * m_Dim2);
                idx += idx4 * (m_Dim1);
                idx += idx5;
                return idx;
            }
            internal void Set(int idx0, int idx1, int idx2, int idx3, int idx4, int idx5, bool val)
            {
                int vecIdx = GetIndex(idx0, idx1, idx2, idx3, idx4, idx5);
                m_Vec[vecIdx] = val;
            }

            internal bool Get(int idx0, int idx1, int idx2, int idx3, int idx4, int idx5)
            {
                int vecIdx = GetIndex(idx0, idx1, idx2, idx3, idx4, idx5);
                bool ret = m_Vec[vecIdx];
                return ret;
            }

            int m_Dim0;
            int m_Dim1;
            int m_Dim2;
            int m_Dim3;
            int m_Dim4;
            int m_Dim5;
            bool[] m_Vec;
        }

        BoolDim2[] m_extractFromApd;

        // src is Apd, dst is Apd, src is float, dst is float, src len, dst len
        BoolDim6 m_implicitCast;

        bool[][] m_mergeToApd;
        bool[][] m_makeStructFromFpd;
        bool[][] m_makeStructFromFpdFinite;
        bool[][] m_splatStructFromScalar;
        bool[][] m_extractIndexApd;
        bool[][] m_insertIndexApd;

        // We have separate functions for Tex2D, Tex2DArray, TexCube, TexCubeArray, and Tex3D.
        // I tried to find an algorithmic way to merge these together and reduce the code
        // but trying to merge them caused so much subtle complexity and corner cases that
        // it wasn't worth it.
        bool[][] m_texSample2d;
        bool[][] m_texSample2dArray;
        bool[][] m_texSampleCube;
        bool[][] m_texSampleCubeArray;
        bool[][] m_texSample3d;

        bool[][] m_makeStructDirect;

        bool[][] m_selectApd;

        bool[][] m_mulMatVecApd;
        bool[][] m_mulVecMatApd;

        BoolDim2[] m_binaryFunc;
        BoolDim2[] m_singleFunc;

        BoolDim2[] m_fetchUv;
        BoolDim2[] m_fetchColor;

        BoolDim2[] m_func1;
        BoolDim2[] m_func2;
        BoolDim2[] m_func3;

        // 6-dimentional, but array total size is only 4096
        // type, swizzle length, 0, 1, 2, 3,
        BoolDim6[] m_swizzles;

        // 6-dimentional, but array total size is only 4096
        // type, swizzle length , 0, 1, 2, 3,
        BoolDim6[] m_swizzlesAssign;

    }

}
