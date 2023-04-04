using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering.HighDefinition
{
    internal enum HlslNativeType
    {
        _unknown, // default, unset
        _invalid, // known to be invalid because something went wrong with parsing
        _void,

        _float,
        _float1,
        _float2,
        _float3,
        _float4,

        _int,
        _int1,
        _int2,
        _int3,
        _int4,

        _half,
        _half1,
        _half2,
        _half3,
        _half4,

        _uint,
        _uint1,
        _uint2,
        _uint3,
        _uint4,

        _bool,
        _bool1,
        _bool2,
        _bool3,
        _bool4,

        _float1x2,
        _float2x2,
        _float3x2,
        _float4x2,

        _int1x2,
        _int2x2,
        _int3x2,
        _int4x2,

        _half1x2,
        _half2x2,
        _half3x2,
        _half4x2,

        _uint1x2,
        _uint2x2,
        _uint3x2,
        _uint4x2,

        _bool1x2,
        _bool2x2,
        _bool3x2,
        _bool4x2,

        _float1x3,
        _float2x3,
        _float3x3,
        _float4x3,

        _int1x3,
        _int2x3,
        _int3x3,
        _int4x3,

        _half1x3,
        _half2x3,
        _half3x3,
        _half4x3,

        _uint1x3,
        _uint2x3,
        _uint3x3,
        _uint4x3,

        _bool1x3,
        _bool2x3,
        _bool3x3,
        _bool4x3,

        _float1x4,
        _float2x4,
        _float3x4,
        _float4x4,

        _int1x4,
        _int2x4,
        _int3x4,
        _int4x4,

        _half1x4,
        _half2x4,
        _half3x4,
        _half4x4,

        _uint1x4,
        _uint2x4,
        _uint3x4,
        _uint4x4,

        _bool1x4,
        _bool2x4,
        _bool3x4,
        _bool4x4,

        _Texture,
        _Texture1D,
        _Texture1DArray,
        _Texture2D,
        _Texture2DArray,
        _Texture3D,
        _TextureCUBE,
        _TextureCUBEArray,
        _SamplerState,
        _SamplerComparisonState,

        _RWTexture2D,
        _RWTexture2DArray,
        _RWTexture3D,

        _struct, // user defined, of course
    }

    internal enum HlslOp
    {
        Invalid,            //
        ScopeReslution,     // ::
        PostIncrement,      // ++
        PostDecrement,      //  --
        FunctionalCast,     // int()
        FunctionalCall,     // func()
        Subscript,          // []
        MemberAccess,       // .
        PreIncrement,       // ++
        PreDecrement,       // --
        UnaryPlus,          // +
        UnaryMinus,         // -
        LogicalNot,         // !
        BitwiseNot,         // ~
        CStyleCast,         // (int)
        Dereference,        // *    - not legal in HLSL?
        AddressOf,          // &    - not legal in HLSL?
        Sizeof,             // sizeof
        PointerToMemberDot, // .*   - not legal in HLSL
        PointerToMemorArrow, // ->* - not legal in HLSL
        Mul,                // *
        Div,                // /
        Mod,             // %
        Add,                // +
        Sub,                // -
        ShiftL,             // <<
        ShiftR,             // >>
        ThreeWayCompare,    // <=>  - never used this
        LessThan,           // <
        GreaterThan,        // >
        LessEqual,          // <=
        GreaterEqual,       // >=
        CompareEqual,       // ==
        NotEqual,           // !=
        BitwiseAnd,         // &
        BitwiseXor,         // ^
        BitwiseOr,          // |
        LogicalAnd,         // &&
        LogicalOr,          // ||
        TernaryQuestion,    // ?
        TernaryColon,       // :
        Assignment,         // =
        AddEquals,         // +=
        SubEquals,          // -=
        MulEquals,          // *=
        DivEquals,          // /=
        ModEquals,    // %=
        ShiftLEquals,       // <<=
        ShiftREquals,       // >>=
        AndEquals,          // &=
        XorEquals,          // ^=
        OrEquals,           // |=
        Comma,              // ,
    }

    internal enum HlslStatementType
    {
        If,
        Switch,
        Case,
        Break,
        Continue,
        Default,
        Goto,
        Label,
        For,
        Do,
        While,
        Expression,
        Return,
    }


    internal enum ApdAllowedState
    {
        OnlyApd,
        OnlyFpd,
        AllowApdVariation,
        Any
    }

    internal class HlslTree
    {
        internal class Node
        {
            internal Node()
            {
            }

            internal virtual List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                return new List<string>();
            }

            internal virtual bool IsNodeValid()
            {
                return true;
            }


            internal virtual HlslParser.TypeInfo GetNodeTypeInfo(HlslTree tree)
            {
                HlslParser.TypeInfo ret = new HlslParser.TypeInfo();
                ret.identifier = "";
                ret.nativeType = HlslNativeType._unknown;
                ret.allowedState = ApdAllowedState.Any;
                return ret;
            }

            internal virtual string GetShortName()
            {
                return "Node";
            }
        }

        static internal string IndentFromDepth(int depth)
        {
            char[] data = new char[depth * 4];
            System.Array.Fill(data, ' ');
            return new string(data);
        }

        static internal void AppendNodeVecLinesToList(ref List<string> debugLines, int[] nodeIds, HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth)
        {
            for (int i = 0; i < nodeIds.Length; i++)
            {
                AppendNodeLinesToList(ref debugLines, nodeIds[i], tree, tokenizer, unityReserved, depth);
            }
        }

        static string DebugIdHelper(int id)
        {
            return " (" + id.ToString() + ")";
        }

        static internal void AppendNodeLinesToList(ref List<string> debugLines, int nodeId, HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth)
        {
            {
                int id = nodeId;
                if (id >= 0)
                {
                    Node node = tree.allNodes[id];
                    List<string> childLines = node.DebugLines(tree, tokenizer, unityReserved, depth + 1, id);
                    debugLines.AddRange(childLines);
                }
                else
                {
                    debugLines.Add(IndentFromDepth(depth + 1) + "<invalid node>");
                }
            }
        }

        internal class NodeTopLevel : Node
        {
            internal NodeTopLevel()
            {
                statements = new int[0];
            }
            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "TopLevel" + DebugIdHelper(nodeId));

                for (int i = 0; i < statements.Length; i++)
                {
                    AppendNodeLinesToList(ref ret, statements[i], tree, tokenizer, unityReserved, depth + 1);
                }

                return ret;
            }

            internal override string GetShortName()
            {
                return "TopLevel";
            }

            internal int[] statements; // struct declarations and functions. will add globals later.
            internal HlslTokenizer.CodeSection[] codeSections;
        }

        internal class NodeStruct : Node
        {
            internal NodeStruct()
            {
                nameTokenId = -1;
                structInfoId = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                string myName = tokenizer.GetTokenData(nameTokenId);

                ret.Add(IndentFromDepth(depth) + "struct " + myName + DebugIdHelper(nodeId));
                AppendNodeVecLinesToList(ref ret, declarations, tree, tokenizer, unityReserved, depth);
                return ret;
            }

            internal override string GetShortName()
            {
                return "Struct";
            }

            internal int nameTokenId;
            internal int[] declarations;
            internal int structInfoId;
        }

        // reserved structs used by unity HLSL
        internal class NodeUnityStruct : Node
        {
            internal NodeUnityStruct()
            {
                unityName = null;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "unity_struct " + unityName + DebugIdHelper(nodeId));
                return ret;
            }

            internal override string GetShortName()
            {
                return "UnityStruct";
            }

            internal string unityName;
        }

        internal class NodeFunctionPrototype : Node
        {
            internal int returnTokenId;

            internal int nameTokenId;
            internal int[] declarations;

            internal int protoId = -1;

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                string myReturn = tokenizer.GetTokenData(returnTokenId);
                string myName = tokenizer.GetTokenData(nameTokenId);
                ret.Add(IndentFromDepth(depth) + "prototype " + myReturn + " " + myName + DebugIdHelper(nodeId));
                AppendNodeVecLinesToList(ref ret, declarations, tree, tokenizer, unityReserved, depth + 1);
                return ret;
            }

            internal override string GetShortName()
            {
                return "FuncProto";
            }

        }

        // a prototype into an external function that we don't actually have tokens for
        internal class NodeFunctionPrototypeExternal : Node
        {
            internal HlslUtil.PrototypeInfo protoInfo;

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();

                ret.Add(IndentFromDepth(depth) + "func_proto_external " + protoInfo.identifier + DebugIdHelper(nodeId));
                ret.Add(IndentFromDepth(depth + 1) + "returns - " + protoInfo.returnType.DebugString());
                for (int i = 0; i < protoInfo.paramInfoVec.Length; i++)
                {
                    ret.Add(IndentFromDepth(depth + 1) + "param - " + protoInfo.paramInfoVec[i].DebugString());
                }
                return ret;
            }

            internal override string GetShortName()
            {
                return "FuncProtoExternal";
            }

        }

        internal class NodeBlock : Node
        {
            internal int[] statements;

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "block" + DebugIdHelper(nodeId));
                AppendNodeVecLinesToList(ref ret, statements, tree, tokenizer, unityReserved, depth);
                return ret;
            }

            internal override string GetShortName()
            {
                return "Block";
            }

        }

        internal class NodeStatement : Node
        {
            internal int expression;
            internal HlslStatementType type;
            internal int childBlockOrStatement;

            internal int[] forExpressions;
            internal int elseBlockOrStatement;

            internal NodeStatement()
            {
                expression = -1;
                childBlockOrStatement = -1;
                elseBlockOrStatement = -1;
                forExpressions = new int[0];
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "statement: " + type.ToString() + DebugIdHelper(nodeId));

                switch (type)
                {
                    case HlslStatementType.If:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        AppendNodeLinesToList(ref ret, childBlockOrStatement, tree, tokenizer, unityReserved, depth + 1);
                        if (elseBlockOrStatement >= 0)
                        {
                            AppendNodeLinesToList(ref ret, elseBlockOrStatement, tree, tokenizer, unityReserved, depth + 1);
                        }
                        break;
                    case HlslStatementType.Switch:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    case HlslStatementType.Case:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    case HlslStatementType.Break:
                        break;
                    case HlslStatementType.Continue:
                        break;
                    case HlslStatementType.Default:
                        break;
                    case HlslStatementType.Goto:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    case HlslStatementType.Label:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    case HlslStatementType.For:
                        HlslUtil.ParserAssert(forExpressions.Length == 3);
                        AppendNodeLinesToList(ref ret, forExpressions[0], tree, tokenizer, unityReserved, depth + 1);
                        AppendNodeLinesToList(ref ret, forExpressions[1], tree, tokenizer, unityReserved, depth + 1);
                        AppendNodeLinesToList(ref ret, forExpressions[2], tree, tokenizer, unityReserved, depth + 1);
                        AppendNodeLinesToList(ref ret, childBlockOrStatement, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    case HlslStatementType.Do:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        AppendNodeLinesToList(ref ret, childBlockOrStatement, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    case HlslStatementType.While:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        AppendNodeLinesToList(ref ret, childBlockOrStatement, tree, tokenizer, unityReserved, depth + 1);
                        break;

                    case HlslStatementType.Expression:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    case HlslStatementType.Return:
                        AppendNodeLinesToList(ref ret, expression, tree, tokenizer, unityReserved, depth + 1);
                        break;
                    default:
                        // no op? error?
                        break;
                }

                return ret;
            }

            internal override string GetShortName()
            {
                return "Statement - " + type.ToString();
            }
        }

        internal class NodeFunction : Node
        {
            internal NodeFunction()
            {
                prototypeId = -1;
                blockId = -1;
                parseErr = "";
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "function" + DebugIdHelper(nodeId));
                if (parseErr.Length > 0)
                {
                    ret.Add(IndentFromDepth(depth + 1) + "error: " + parseErr);
                }
                AppendNodeLinesToList(ref ret, prototypeId, tree, tokenizer, unityReserved, depth + 1);
                AppendNodeLinesToList(ref ret, blockId, tree, tokenizer, unityReserved, depth + 1);
                return ret;
            }

            internal override string GetShortName()
            {
                return "Function";
            }

            internal int prototypeId; // NodeFunctionPrototype
            internal int blockId;

            internal string parseErr;
        }

        internal class NodeDeclaration : Node
        {
            internal NodeDeclaration()
            {
                typeNodeId = -1;
                nameTokenId = -1;
                subTypeNodeId = -1;
                modifierTokenId = -1;
                initializerId = -1;
                structId = -1;

                isMacroDecl = false;
                macroDeclString = "";

            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                string myModifier = (modifierTokenId >= 0) ? tokenizer.GetTokenData(modifierTokenId) + " " : "";
                string myName = tokenizer.GetTokenData(nameTokenId);

                ret.Add(IndentFromDepth(depth) + "decl " + myName + DebugIdHelper(nodeId));

                AppendNodeLinesToList(ref ret, typeNodeId, tree, tokenizer, unityReserved, depth + 1);
                if (subTypeNodeId >= 0)
                {
                    AppendNodeLinesToList(ref ret, subTypeNodeId, tree, tokenizer, unityReserved, depth + 1);
                }

                if (initializerId >= 0)
                {
                    AppendNodeLinesToList(ref ret, initializerId, tree, tokenizer, unityReserved, depth + 1);
                }

                AppendNodeVecLinesToList(ref ret, arrayDims, tree, tokenizer, unityReserved, depth + 1);

                return ret;
            }

            internal HlslNativeType nativeType;
            internal int structId; // if it's a struct, the node containing it

            internal int modifierTokenId; // token for the type

            //internal int typeTokenId; // token for the type
            //internal int subTypeTokenId; // subtype, i.e. for Texture2D<float3> type is Texture2D and subType is float3;

            internal int typeNodeId; // token for the type
            internal int subTypeNodeId; // subtype, i.e. for Texture2D<float3> type is Texture2D and subType is float3;

            internal int nameTokenId; // token for the param name
            internal int initializerId;

            internal bool isMacroDecl;
            internal string macroDeclString;

            internal string variableName;
            internal string debugType;

            internal int[] arrayDims;

            internal override string GetShortName()
            {
                return "Decl: " + variableName;
            }
        }


        internal class NodeBlockInitializer : Node
        {
            internal NodeBlockInitializer()
            {
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                AppendNodeVecLinesToList(ref ret, initNodeIds, tree, tokenizer, unityReserved, depth);
                return ret;
            }

            internal override string GetShortName()
            {
                return "BlockInitializer";
            }

            internal int[] initNodeIds;
        }

        internal class NodeVariable : Node
        {
            internal NodeVariable()
            {
                nameTokenId = -1;
                nameVariableId = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                string myName = tokenizer.GetTokenData(nameTokenId);
                ret.Add(IndentFromDepth(depth) + "variable " + myName + DebugIdHelper(nodeId));
                return ret;
            }

            internal override HlslParser.TypeInfo GetNodeTypeInfo(HlslTree tree)
            {
                HlslUtil.ParserAssert(nameVariableId >= 0);
                VariableInfo info = tree.allVariables[nameVariableId];

                HlslParser.TypeInfo ret = new HlslParser.TypeInfo();
                ret = info.typeInfo;
                return ret;
            }

            internal override string GetShortName()
            {
                return "Variable: " + debugName;
            }

            internal int nameTokenId; // token for the param name
            internal int nameVariableId;
            internal string debugName;

        }

        internal class NodeNativeConstructor : Node
        {
            internal NodeNativeConstructor()
            {
                typeTokenId = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "native_constructor " + nativeType.ToString() + DebugIdHelper(nodeId));
                AppendNodeVecLinesToList(ref ret, paramNodeIds, tree, tokenizer, unityReserved, depth);
                return ret;
            }

            internal override HlslParser.TypeInfo GetNodeTypeInfo(HlslTree tree)
            {
                HlslParser.TypeInfo ret = new HlslParser.TypeInfo();
                ret.nativeType = nativeType;
                ret.allowedState = ApdAllowedState.Any;
                return ret;
            }

            internal int typeTokenId; // token for the param name
            internal HlslNativeType nativeType;
            internal int[] paramNodeIds;

            internal override string GetShortName()
            {
                return "NativeConstructor";
            }
        }


        internal class NodeToken : Node
        {
            internal NodeToken()
            {
                srcTokenId = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                string myName = tokenizer.GetTokenData(srcTokenId);
                ret.Add(IndentFromDepth(depth) + "token " + myName + DebugIdHelper(nodeId));
                return ret;
            }

            internal override string GetShortName()
            {
                return "Token";
            }

            internal int srcTokenId; // token for the param name

        }

        internal class NodeNativeType : Node
        {
            internal NodeNativeType()
            {
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "nativeType " + nativeType.ToString() + DebugIdHelper(nodeId));
                return ret;
            }

            internal override string GetShortName()
            {
                return "NativeType: " + nativeType.ToString();
            }

            internal HlslNativeType nativeType; // token for the param name
        }

        internal class NodeLiteralOrBool : Node
        {
            internal NodeLiteralOrBool()
            {
                nameTokenId = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                string myName = tokenizer.GetTokenData(nameTokenId);
                ret.Add(IndentFromDepth(depth) + "literal_or_bool " + myName + DebugIdHelper(nodeId));
                return ret;
            }

            internal int nameTokenId; // token for the param name
            internal string debugName;

            internal override string GetShortName()
            {
                return "Literal: " + debugName;
            }

        }

        internal class NodePassthrough : Node
        {
            internal NodePassthrough()
            {
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();

                string fullLine = "";

                for (int i = 0; i < tokenIds.Length; i++)
                {
                    int tokenId = tokenIds[i];

                    fullLine += tokenizer.GetTokenData(tokenId);
                }

                ret.Add(fullLine);

                return ret;
            }

            internal override string GetShortName()
            {
                return "Passthough";
            }

            internal int[] tokenIds; // token for the param name
            internal bool writeDirect; // if true, we should write this node directly. if fals, this node append it's text in some way to a parent naode
        }

        internal class NodeExpression : Node
        {
            internal NodeExpression()
            {
                lhsNodeId = -1;
                rhsNodeId = -1;
                opTokenId = -1;
                cstyleCastStructId = -1;
                paramIds = new int[0];
                rhsRhsNodeId = -1; // for ternary
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();
                ret.Add(IndentFromDepth(depth) + "expression " + op.ToString() + " " + tokenizer.GetTokenData(opTokenId) + DebugIdHelper(nodeId));
                AppendNodeLinesToList(ref ret, lhsNodeId, tree, tokenizer, unityReserved, depth + 1);

                bool isBinary = HlslParser.GetOperatorIsBinary(op);
                if (isBinary || op == HlslOp.MemberAccess || op == HlslOp.TernaryQuestion || op == HlslOp.Subscript)
                {
                    AppendNodeLinesToList(ref ret, rhsNodeId, tree, tokenizer, unityReserved, depth + 1);
                }

                if (op == HlslOp.TernaryQuestion)
                {
                    AppendNodeLinesToList(ref ret, rhsRhsNodeId, tree, tokenizer, unityReserved, depth + 1);
                }

                AppendNodeVecLinesToList(ref ret, paramIds, tree, tokenizer, unityReserved, depth + 1);
                return ret;
            }

            internal override string GetShortName()
            {
                return "Expression: " + op.ToString();
            }


            internal int lhsNodeId;
            internal int rhsNodeId;
            internal int[] paramIds;
            internal HlslOp op;
            internal int opTokenId;
            internal int cstyleCastStructId;
            internal int rhsRhsNodeId; // for ternary

            internal string debugName;
        }

        internal class NodeMemberVariable : Node
        {
            internal NodeMemberVariable()
            {
                structNodeId = -1;
                fieldIndex = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();

                HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(structNodeId, unityReserved);

                HlslUtil.FieldInfo fieldInfo = structInfo.fields[fieldIndex];

                ret.Add(IndentFromDepth(depth) + "member_variable: " + structInfo.identifier + "." + fieldInfo.identifier + DebugIdHelper(nodeId));
                return ret;
            }

            internal override string GetShortName()
            {
                return "MemberVariable";
            }

            internal int structNodeId;
            internal int fieldIndex;
        }

        internal class NodeParenthesisGroup : Node
        {
            internal NodeParenthesisGroup()
            {
                childNodeId = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();

                ret.Add(IndentFromDepth(depth) + "( ... ): " + DebugIdHelper(nodeId));
                AppendNodeLinesToList(ref ret, childNodeId, tree, tokenizer, unityReserved, depth + 1);

                return ret;
            }

            internal override string GetShortName()
            {
                return "()";
            }

            internal int childNodeId;
        }

        internal class NodeMemberFunction : Node
        {
            internal NodeMemberFunction()
            {
                structNodeId = -1;
                funcIndex = -1;
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();

                HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(structNodeId, unityReserved);
                HlslUtil.PrototypeInfo protoInfo = structInfo.prototypes[funcIndex];

                ret.Add(IndentFromDepth(depth) + "member_function: " + structInfo.identifier + "." + protoInfo.identifier + DebugIdHelper(nodeId));

                AppendNodeVecLinesToList(ref ret, funcParamNodeIds, tree, tokenizer, unityReserved, depth);

                return ret;
            }

            internal override string GetShortName()
            {
                return "MemberFunc";
            }

            internal int structNodeId;
            internal int funcIndex = -1;

            internal int[] funcParamNodeIds;
        }

        internal class NodeSwizzle : Node
        {
            internal NodeSwizzle()
            {
                swizzle = "";
            }

            internal override List<string> DebugLines(HlslTree tree, HlslTokenizer tokenizer, HlslUnityReserved unityReserved, int depth, int nodeId)
            {
                List<string> ret = new List<string>();

                ret.Add(IndentFromDepth(depth) + "swizzle: ." + swizzle + DebugIdHelper(nodeId));
                return ret;
            }

            internal override string GetShortName()
            {
                return "Swizzle: " + swizzle;
            }


            internal string swizzle;
        }

        internal string[] CalcDebugLines(HlslTokenizer tokenizer, HlslUnityReserved unityReserved)
        {
            List<string> allLines = new List<string>();
            if (topLevelNode >= 0)
            {
                Node node = allNodes[topLevelNode];
                allLines = node.DebugLines(this, tokenizer, unityReserved, 0, topLevelNode);
            }

            List<string> errLines = GetErrorText(tokenizer);
            allLines.AddRange(errLines);

            return allLines.ToArray();
        }

        internal static HlslParser.TypeInfo TypeInfoFromToken(int tokenId, HlslParser parser, ApdAllowedState allowedState)
        {
            HlslToken rawToken = parser.tokenizer.GetTokenType(tokenId);
            string tokenName = parser.tokenizer.GetTokenData(tokenId);

            HlslParser.TypeInfo ret = new HlslParser.TypeInfo();

            if (HlslTokenizer.IsTokenNativeType(rawToken))
            {
                HlslNativeType nativeType = parser.tokenToNativeTable[rawToken];
                ret = HlslParser.TypeInfo.MakeNativeType(nativeType, 0, allowedState);
            }
            else if (rawToken == HlslToken._identifier)
            {
                ret = HlslParser.TypeInfo.MakeStruct(tokenName, 0);
            }
            else
            {
                HlslUtil.ParserAssert(false);
            }

            return ret;

        }

        // for the struct id, check if it contains name either as a field or a prototype. Assumes no operator overloading for prototypes.
        internal void DoesStructHaveDeclaration(out int fieldIndex, out int protoIndex, int structNodeId, string name, HlslUnityReserved unityReserved)
        {
            HlslTree.Node baseNode = allNodes[structNodeId];

            fieldIndex = -1;
            protoIndex = -1;

            HlslUtil.StructInfo structInfo = GetStructInfoFromNodeId(structNodeId, unityReserved);

            // Brute force linear search seams fine. Maybe do a hash lookup later.
            for (int i = 0; i < structInfo.fields.Length; i++)
            {
                if (string.Compare(structInfo.fields[i].identifier, name) == 0)
                {
                    // there should be no duplicate names
                    HlslUtil.ParserAssert(fieldIndex < 0);
                    fieldIndex = i;
                }
            }

            for (int i = 0; i < structInfo.prototypes.Length; i++)
            {
                if (string.Compare(structInfo.prototypes[i].identifier, name) == 0)
                {
                    // there should be no duplicate names
                    HlslUtil.ParserAssert(protoIndex < 0);
                    protoIndex = i;
                }
            }
        }

        internal int AddNode(Node data, HlslParser parser, bool stillValid)
        {
            if (!stillValid)
            {
                return -1;
            }

            HlslUtil.ParserAssert(allNodes.Count == fullTypeInfo.Count);

            int dstNodeId = allNodes.Count;

            if (data is NodeFunctionPrototype nodeProto)
            {
                HlslUtil.PrototypeInfo protoInfo = new HlslUtil.PrototypeInfo();

                string functionName = parser.tokenizer.GetTokenData(nodeProto.nameTokenId);

                protoInfo.uniqueId = dstNodeId;
                protoInfo.identifier = functionName;

                // by default, a function prototype return type can be any
                protoInfo.returnType = TypeInfoFromToken(nodeProto.returnTokenId, parser, ApdAllowedState.Any);

                protoInfo.paramInfoVec = new HlslParser.TypeInfo[nodeProto.declarations.Length];

                for (int i = 0; i < nodeProto.declarations.Length; i++)
                {
                    int nodeId = nodeProto.declarations[i];
                    Node currNode = allNodes[nodeId];

                    HlslParser.TypeInfo currType = new HlslParser.TypeInfo();

                    if (currNode is HlslTree.NodeDeclaration decl)
                    {
                        int declTypeNode = decl.typeNodeId;
                        Node declNode = allNodes[declTypeNode];

                        currType = declNode.GetNodeTypeInfo(this);
                    }

                    protoInfo.paramInfoVec[i] = currType;
                }

                int dstProtoId = parsedFuncStructData.AddPrototype(protoInfo);

                nodeProto.protoId = dstProtoId;
            }
            else if (data is NodeExpression nodeExp)
            {
                if (nodeExp.op == HlslOp.FunctionalCall)
                {
                    if (nodeExp.lhsNodeId < 0)
                    {
                        HlslUtil.ParserAssert(nodeExp.lhsNodeId >= 0);
                    }
                }

                if (nodeExp.op == HlslOp.MemberAccess)
                {
                    HlslUtil.ParserAssert(nodeExp.lhsNodeId >= 0);
                    HlslUtil.ParserAssert(nodeExp.rhsNodeId >= 0);
                }
            }
            else if (data is NodeDeclaration nodeDecl)
            {
                if (nodeDecl.nativeType == HlslNativeType._struct &&
                    nodeDecl.structId < 0)
                {
                    HlslUtil.ParserAssert(false);
                }
            }

            allNodes.Add(data);

            FullTypeInfo fti = ResolveFullTypeForNode(dstNodeId);

            // if we have a failure, check it one more time so that we can easily set a breakpoint and trace into it.
            if (fti.nativeType == HlslNativeType._struct && fti.structId < 0 && !fti.isUnityBuiltin)
            {
                HlslUtil.ParserAssert(false);
            }

            fullTypeInfo.Add(fti);

            return dstNodeId;
        }

        internal bool IsStructUnityBuiltin(int nodeId)
        {
            bool isUnityBuiltin = false;
            HlslTree.Node node = allNodes[nodeId];
            if (node is HlslTree.NodeStruct nodeStruct)
            {
                isUnityBuiltin = false;
            }
            else if (node is HlslTree.NodeUnityStruct nodeUnityStruct)
            {
                isUnityBuiltin = true;
            }
            else
            {
                HlslUtil.ParserAssert(false);
            }

            return isUnityBuiltin;
        }

        internal HlslUtil.StructInfo GetStructInfoFromNodeId(int nodeId, HlslUnityReserved unityReserved)
        {
            HlslUtil.StructInfo structInfo = new HlslUtil.StructInfo();

            HlslTree.Node node = allNodes[nodeId];
            if (node is HlslTree.NodeStruct nodeStruct)
            {
                structInfo = parsedFuncStructData.allStructs[nodeStruct.structInfoId];
            }
            else if (node is HlslTree.NodeUnityStruct nodeUnityStruct)
            {
                int structId = unityReserved.parsedFuncStructData.structFromIdentifer[nodeUnityStruct.unityName];

                structInfo = unityReserved.parsedFuncStructData.allStructs[structId];
            }
            else
            {
                HlslUtil.ParserAssert(false);
            }

            return structInfo;
        }

        internal bool IsIdentifierFunction(string name)
        {
            return parsedFuncStructData.overloadFromIdentifer.ContainsKey(name);
        }

        internal int[] GetNodePrototypeIdsForFunction(string name)
        {
            int overloadGroupId = parsedFuncStructData.overloadFromIdentifer[name];
            HlslUtil.ParserAssert(overloadGroupId >= 0);
            HlslUtil.OverloadInfo overloadInfo = parsedFuncStructData.allOverloads[overloadGroupId];
            HlslUtil.ParserAssert(string.Compare(overloadInfo.identifier, name) == 0);

            return overloadInfo.prototypeList.ToArray();
        }

        internal void PushScope()
        {
            structLookupStack.Add(new Dictionary<string, int>());
            variableLookupStack.Add(new Dictionary<string, int>());
        }

        internal void PopScope()
        {
            // we should never be removing the top level stack
            HlslUtil.ParserAssert(structLookupStack.Count >= 2);
            HlslUtil.ParserAssert(variableLookupStack.Count >= 2);

            structLookupStack.RemoveAt(structLookupStack.Count - 1);
            variableLookupStack.RemoveAt(variableLookupStack.Count - 1);
        }

        internal int AddStructInfo(HlslUtil.StructInfo structInfo)
        {
            int dstIndex = parsedFuncStructData.allStructs.Count;
            parsedFuncStructData.allStructs.Add(structInfo);

            // If we are at global scope, add this to the name list so that we can look
            // it up later. If not at global scope, skip this step and it we can only
            // access this StructInfo by the id returned from this function.
            int topIndex = structLookupStack.Count - 1;

            if (topIndex == 0)
            {
                parsedFuncStructData.structFromIdentifer.Add(structInfo.identifier, dstIndex);
            }

            return dstIndex;
        }

        internal void AddStructIdentifier(string name, int nodeStructId)
        {
            int topIndex = structLookupStack.Count - 1;
            structLookupStack[topIndex].Add(name, nodeStructId);
        }

        internal void AddVariableIdentifier(VariableInfo variableInfo)
        {
            int dstIndex = allVariables.Count;
            allVariables.Add(variableInfo);

            int topIndex = variableLookupStack.Count - 1;

            if (variableLookupStack[topIndex].ContainsKey(variableInfo.variableName))
            {
                HlslUtil.ParserAssert(false);
            }

            variableLookupStack[topIndex].Add(variableInfo.variableName, dstIndex);
        }

        internal bool FindVariableInfo(out VariableInfo variableInfo, out int variableId, string identifier)
        {
            bool found = false;
            variableId = -1;
            variableInfo = new VariableInfo();

            int topIndex = variableLookupStack.Count - 1;
            while (topIndex >= 0 && !found)
            {
                found = variableLookupStack[topIndex].ContainsKey(identifier);

                if (found)
                {
                    int id = variableLookupStack[topIndex][identifier];
                    variableInfo = allVariables[id];
                    variableId = id;
                }
                else
                {
                    topIndex--;
                }
            }

            // if we failed to find it in the stack, try the unity globals
            if (!found)
            {
                HlslParser.TypeInfo foundInfo;
                found = unityReserved.FindUnityGlobal(out foundInfo, identifier);
                if (found)
                {
                    variableInfo.variableName = identifier;
                    variableInfo.structId = -1;
                    variableInfo.typeInfo = foundInfo;
                    variableInfo.declId = -1;
                    variableId = -1;
                }
            }
            return found;
        }

        internal struct FullTypeInfo
        {
            static internal FullTypeInfo MakeInvalid()
            {
                FullTypeInfo ret = new FullTypeInfo();
                ret.valid = false;
                ret.hasType = false;
                ret.identifier = "";
                ret.nativeType = HlslNativeType._invalid;
                ret.arrayDims = 0;
                ret.structId = -1;
                ret.isUnityBuiltin = false;
                return ret;
            }

            static internal FullTypeInfo MakeValidNoType()
            {
                FullTypeInfo ret = new FullTypeInfo();
                ret.valid = true;
                ret.hasType = false;
                ret.identifier = "";
                ret.nativeType = HlslNativeType._invalid;
                ret.arrayDims = 0;
                ret.structId = -1;
                ret.isUnityBuiltin = false;
                return ret;
            }

            static internal FullTypeInfo MakeStruct(string name, int structId, bool isUnityBuiltin)
            {
                FullTypeInfo ret = new FullTypeInfo();
                ret.valid = true;
                ret.hasType = true;
                ret.identifier = name;
                ret.nativeType = HlslNativeType._struct;
                ret.arrayDims = 0;
                ret.structId = structId;
                ret.isUnityBuiltin = isUnityBuiltin;
                return ret;
            }

            static internal FullTypeInfo MakeNativeType(HlslNativeType nativeType)
            {
                FullTypeInfo ret = new FullTypeInfo();
                ret.valid = true;
                ret.hasType = true;
                ret.identifier = "";
                ret.nativeType = nativeType;
                ret.arrayDims = 0;
                ret.structId = -1;
                ret.isUnityBuiltin = false;
                return ret;
            }

            internal string GetTypeString()
            {
                string ret = "";
                if (structId >= 0)
                {
                    ret = identifier;
                }
                else
                {
                    ret = HlslUtil.GetNativeTypeString(nativeType);
                }
                return ret;
            }

            // valid refers to if this node is processed. hasType refers to if the node has some kind of
            // type associated. For example, a node describing:
            //    (3.0 + x) // x is a float
            // would return a float1 as the type. However, a statement such as
            //    x = y + 32;
            // would not have a "type" because it's a complete statement.
            internal bool valid;
            internal bool hasType;
            internal string identifier; // used if struct
            internal HlslNativeType nativeType; //
            internal int arrayDims;
            internal int structId;
            internal bool isUnityBuiltin;
        }

        internal struct ErrInfo
        {
            internal string errText;
            internal int errToken;
        }

        internal struct VariableInfo
        {
            internal string variableName;
            internal HlslParser.TypeInfo typeInfo;
            internal int structId;
            internal int declId;

            internal bool IsValid()
            {
                return typeInfo.nativeType != HlslNativeType._unknown;
            }
        }

        internal void ApplyError(string text, int tokenId)
        {
            ErrInfo err = new ErrInfo();
            err.errText = text;
            err.errToken = tokenId;
            errList.Add(err);
        }

        internal void ResetErrors()
        {
            errList = new List<ErrInfo>();
        }

        internal List<string> GetErrorText(HlslTokenizer tokenizer)
        {
            List<string> dstText = new List<string>();

            for (int errIter = 0; errIter < errList.Count; errIter++)
            {
                ErrInfo info = errList[errIter];

                string errLine = "";

                int lineIndex = -1;
                int charIndex = -1;
                string actualText = "";

                // we might have errors after EOF (because we skip to EOF on failures), so need to check if token is valid.
                if (info.errToken >= 0 && info.errToken < tokenizer.foundTokens.Count)
                {
                    HlslTokenizer.SingleToken singleToken = tokenizer.foundTokens[info.errToken];

                    lineIndex = singleToken.marker.indexLine;
                    charIndex = singleToken.marker.indexChar;
                    actualText = singleToken.data;

                }

                // TODO: given the line number, we should be parsing #line preprocessor directives to give an accurate source line number,
                // but this is fine for now.
                if (lineIndex >= 0 && charIndex >= 0)
                {
                    errLine = string.Format("Error at line {0}:{1} - {2}: ", lineIndex + 1, charIndex, actualText);
                }
                else
                {
                    errLine = string.Format("Error at line <EOF>: ", lineIndex, charIndex);
                }

                errLine += info.errText;

                dstText.Add(errLine);
            }

            return dstText;
        }

        internal HlslTree(HlslUnityReserved srcUnityReserved, HlslTokenizer srcTokenizer)
        {
            tokenizer = srcTokenizer;
            unityReserved = srcUnityReserved;

            tokenToNativeTable = HlslParser.GenerateTokenToNativeTable();

            allNodes = new List<Node>();
            prototypesReserved = new List<Node>();

            fullTypeInfo = new List<FullTypeInfo>();
            parsedFuncStructData = new HlslUtil.ParsedFuncStructData();

            topLevelNode = -1;
            structLookupStack = new List<Dictionary<string, int>>();
            structLookupStack.Add(new Dictionary<string, int>());

            allVariables = new List<VariableInfo>();
            variableLookupStack = new List<Dictionary<string, int>>();
            variableLookupStack.Add(new Dictionary<string, int>());

            errList = new List<ErrInfo>();
        }

        // funcs for type info
        internal HlslTree.FullTypeInfo GetFullTypeInfoFromParserInfo(HlslParser.TypeInfo parserTypeInfo)
        {
            HlslTree.FullTypeInfo ret = new HlslTree.FullTypeInfo();
            if (parserTypeInfo.nativeType == HlslNativeType._struct)
            {
                bool isUnityBuiltin = unityReserved.parsedFuncStructData.structFromIdentifer.ContainsKey(parserTypeInfo.identifier);
                if (isUnityBuiltin)
                {
                    int structInfoId = unityReserved.parsedFuncStructData.structFromIdentifer[parserTypeInfo.identifier];
                    HlslUtil.StructInfo structInfo = unityReserved.parsedFuncStructData.allStructs[structInfoId];
                    ret = HlslTree.FullTypeInfo.MakeStruct(structInfo.identifier, -1, true);
                }
                else
                {
                    if (!parsedFuncStructData.structFromIdentifer.ContainsKey(parserTypeInfo.identifier))
                    {
                        HlslUtil.ParserAssert(false);
                    }
                    int structInfoId = parsedFuncStructData.structFromIdentifer[parserTypeInfo.identifier];
                    HlslUtil.StructInfo structInfo = parsedFuncStructData.allStructs[structInfoId];
                    ret = HlslTree.FullTypeInfo.MakeStruct(structInfo.identifier, structInfoId, false);
                }
            }
            else
            {
                ret = HlslTree.FullTypeInfo.MakeNativeType(parserTypeInfo.nativeType);
            }
            ret.arrayDims = parserTypeInfo.arrayDims;

            return ret;
        }

        HlslTree.FullTypeInfo GetFullTypeInfoFromRawTokenId(int tokenId)
        {
            HlslParser.TypeInfo typeInfo = new HlslParser.TypeInfo();
            HlslToken tokenType = tokenizer.GetTokenType(tokenId);
            string tokenData = tokenizer.GetTokenData(tokenId);

            if (tokenToNativeTable.ContainsKey(tokenType))
            {
                typeInfo.nativeType = tokenToNativeTable[tokenType];

                if (typeInfo.nativeType == HlslNativeType._struct)
                {
                    typeInfo.identifier = tokenData;
                }
            }
            else
            {
                // is there a better choice?
                typeInfo.nativeType = HlslNativeType._void;
            }

            HlslTree.FullTypeInfo dstTypeInfo = GetFullTypeInfoFromParserInfo(typeInfo);
            return dstTypeInfo;
        }

        HlslTree.FullTypeInfo ResolveExpressionType(HlslTree.NodeExpression nodeExpression)
        {
            HlslTree.FullTypeInfo ret = new HlslTree.FullTypeInfo();

            switch (nodeExpression.op)
            {
                // these opssimply accept the lhs
                case HlslOp.PostIncrement:      // ++
                case HlslOp.PostDecrement:      //  --
                case HlslOp.PreIncrement:       // ++
                case HlslOp.PreDecrement:       // --
                case HlslOp.UnaryPlus:          // +
                case HlslOp.UnaryMinus:         // -
                case HlslOp.LogicalNot:         // !
                case HlslOp.BitwiseNot:         // ~

                // for shift, just accept the lhs because the rhs is the shift amount
                case HlslOp.ShiftL:             // <<
                case HlslOp.ShiftR:             // >>

                // for assignment and the variations, the return type is the lhs side
                case HlslOp.Assignment:         // =
                case HlslOp.AddEquals:          // +=
                case HlslOp.SubEquals:          // -=
                case HlslOp.MulEquals:          // *=
                case HlslOp.DivEquals:          // /=
                case HlslOp.ModEquals:    // %=
                case HlslOp.ShiftLEquals:       // <<=
                case HlslOp.ShiftREquals:       // >>=
                case HlslOp.AndEquals:          // &=
                case HlslOp.XorEquals:          // ^=
                case HlslOp.OrEquals:           // |=
                    ret = fullTypeInfo[nodeExpression.lhsNodeId];
                    break;

                // these ops are not supported
                case HlslOp.Invalid:            //
                case HlslOp.ScopeReslution:     // ::
                case HlslOp.Dereference:        // *    - not legal in HLSL?
                case HlslOp.AddressOf:          // &    - not legal in HLSL?
                case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                case HlslOp.ThreeWayCompare:    // <=>  - never used this
                    ret = HlslTree.FullTypeInfo.MakeInvalid();
                    break;


                case HlslOp.Subscript:          // []
                    {
                        ret = fullTypeInfo[nodeExpression.lhsNodeId];
                        if (ret.arrayDims > 0)
                        {
                            ret.arrayDims--;
                        }
                        else
                        {
                            // if dims are zero, then we might be a swizzle
                            HlslNativeType baseType = HlslUtil.GetNativeBaseType(ret.nativeType);
                            int numRows = HlslUtil.GetNumRows(ret.nativeType);
                            int numCols = HlslUtil.GetNumCols(ret.nativeType);

                            if (numRows >= 2 && numCols >= 2)
                            {
                                // For example, convert a float3x4 into a float4;
                                ret.nativeType = HlslUtil.GetBaseTypeWithDims(baseType, numRows, 1);
                            }
                            else if (numCols >= 2)
                            {
                                // For example, convert a float3 into a float
                                ret.nativeType = baseType;
                            }
                            else
                            {
                                // If we don't know the type, fail silently and keep the type.
                                ret.nativeType = baseType;
                            }
                        }
                    }
                    break;

                // these ops are not supported now, but really should be at some point;
                case HlslOp.FunctionalCast:     // int()
                    ret = HlslTree.FullTypeInfo.MakeInvalid();
                    break;

                case HlslOp.FunctionalCall:     // func()
                    // lhs node is the function call prototype node id, so we actually just use lhs
                    ret = fullTypeInfo[nodeExpression.lhsNodeId];
                    break;
                case HlslOp.MemberAccess:       // .
                    {
                        HlslTree.Node nodeRhs = allNodes[nodeExpression.rhsNodeId];

                        if (nodeRhs is HlslTree.NodeMemberVariable nodeMemberVariable)
                        {
                            // use this field
                            ret = fullTypeInfo[nodeExpression.rhsNodeId];
                        }
                        else if (nodeRhs is HlslTree.NodeMemberFunction nodeMemberFunction)
                        {
                            // use the return type of the member function
                            ret = fullTypeInfo[nodeExpression.rhsNodeId];
                        }
                        else if (nodeRhs is HlslTree.NodeSwizzle nodeSwizzle)
                        {
                            // swizle
                            HlslNativeType lhsType = fullTypeInfo[nodeExpression.lhsNodeId].nativeType;
                            HlslNativeType baseType = HlslUtil.GetNativeBaseType(lhsType); // i.e. convert float3 to just float
                            HlslNativeType vecType = HlslUtil.GetVectorFromBaseType(baseType, nodeSwizzle.swizzle.Length);
                            ret = HlslTree.FullTypeInfo.MakeNativeType(vecType);
                        }
                        else
                        {
                            // should never happen, since these are the only 3 nodes allowed
                            HlslUtil.ParserAssert(false);
                        }
                    }
                    break;
                case HlslOp.CStyleCast:         // (int)
                    // get the struct id
                    if (nodeExpression.cstyleCastStructId >= 0)
                    {
                        // if the cstyle structId is >= 0, then we use this struct
                        HlslUtil.StructInfo structInfo = GetStructInfoFromNodeId(nodeExpression.cstyleCastStructId, unityReserved);
                        ret = HlslTree.FullTypeInfo.MakeStruct(structInfo.identifier, nodeExpression.cstyleCastStructId, false);
                    }
                    else
                    {
                        HlslToken tokenType = tokenizer.GetTokenType(nodeExpression.opTokenId);

                        HlslNativeType nativeType = HlslNativeType._invalid;
                        if (tokenToNativeTable.ContainsKey(tokenType))
                        {
                            nativeType = tokenToNativeTable[tokenType];
                        }

                        ret = HlslTree.FullTypeInfo.MakeNativeType(nativeType);
                    }
                    break;
                case HlslOp.Sizeof:             // sizeof
                    ret = HlslTree.FullTypeInfo.MakeNativeType(HlslNativeType._uint);
                    break;

                // binary ops that will expand to the larger sized vector
                case HlslOp.Mul:                // *
                case HlslOp.Div:                // /
                case HlslOp.Mod:                // %
                case HlslOp.Add:                // +
                case HlslOp.Sub:                // -
                case HlslOp.BitwiseAnd:         // &
                case HlslOp.BitwiseXor:         // ^
                case HlslOp.BitwiseOr:          // |
                    {

                        HlslNativeType lhsType = fullTypeInfo[nodeExpression.lhsNodeId].nativeType;
                        HlslNativeType rhsType = fullTypeInfo[nodeExpression.rhsNodeId].nativeType;

                        bool isMul = (nodeExpression.op == HlslOp.Mul);
                        HlslNativeType dstType = HlslUtil.GetBinaryOpReturnType(lhsType, rhsType, isMul);

                        ret = HlslTree.FullTypeInfo.MakeNativeType(dstType);
                    }
                    break;

                // comparison ops that will expand to the larger sized vector but convert to bool
                case HlslOp.LessThan:           // <
                case HlslOp.GreaterThan:        // >
                case HlslOp.LessEqual:          // <=
                case HlslOp.GreaterEqual:       // >=
                case HlslOp.CompareEqual:       // ==
                case HlslOp.NotEqual:           // !=
                case HlslOp.LogicalAnd:         // &&
                case HlslOp.LogicalOr:          // ||
                    {
                        HlslNativeType lhsType = fullTypeInfo[nodeExpression.lhsNodeId].nativeType;
                        HlslNativeType rhsType = fullTypeInfo[nodeExpression.rhsNodeId].nativeType;

                        int lhsRows = HlslUtil.GetNumRows(lhsType);
                        int lhsCols = HlslUtil.GetNumCols(lhsType);

                        int rhsRows = HlslUtil.GetNumRows(rhsType);
                        int rhsCols = HlslUtil.GetNumCols(rhsType);

                        // just use the max for now
                        int dstRows = Math.Max(lhsRows, rhsRows);
                        int dstCols = Math.Max(lhsRows, rhsRows);

                        HlslNativeType dstType = HlslUtil.GetBaseTypeWithDims(HlslNativeType._bool, dstRows, dstCols);
                        ret = HlslTree.FullTypeInfo.MakeNativeType(dstType);
                    }
                    break;

                case HlslOp.TernaryQuestion:    // ?
                    // treat this as the binary op of the middle and right terms?
                    {
                        HlslNativeType rhsType = fullTypeInfo[nodeExpression.rhsNodeId].nativeType;
                        HlslNativeType rhsRhsType = fullTypeInfo[nodeExpression.rhsRhsNodeId].nativeType;

                        HlslNativeType dstType = HlslUtil.GetBinaryOpReturnType(rhsType, rhsRhsType, false);

                        ret = HlslTree.FullTypeInfo.MakeNativeType(dstType);
                    }
                    break;
                case HlslOp.TernaryColon:       // :
                    // should never happen, since the three terms of the ternary are grouped into a single expression
                    // with op as HlslOp.TernaryQuestion
                    break;
                case HlslOp.Comma:              // ,
                    // simply returns rhs
                    ret = fullTypeInfo[nodeExpression.rhsNodeId];
                    break;
                default:
                    break;

            }

            return ret;
        }

        HlslTree.FullTypeInfo GetFullTypeInfoFromParserInfoAndDims(HlslParser.TypeInfo parserTypeInfo, int dims)
        {
            HlslTree.FullTypeInfo ret = GetFullTypeInfoFromParserInfo(parserTypeInfo);
            ret.arrayDims = dims;
            return ret;
        }

        // assumes that the children have been resolved
        internal HlslTree.FullTypeInfo ResolveFullTypeForNode(int nodeId)
        {
            HlslTree.FullTypeInfo ret = new HlslTree.FullTypeInfo();

            HlslTree.Node baseNode = allNodes[nodeId];
            if (baseNode is HlslTree.NodeTopLevel nodeTopLevel)
            {
                ret = HlslTree.FullTypeInfo.MakeValidNoType();
            }
            else if (baseNode is HlslTree.NodeStruct nodeStruct)
            {
                HlslUtil.StructInfo structInfo = GetStructInfoFromNodeId(nodeId, unityReserved);
                ret = HlslTree.FullTypeInfo.MakeStruct(structInfo.identifier, nodeStruct.structInfoId, false);
            }
            else if (baseNode is HlslTree.NodeUnityStruct nodeUnityStruct)
            {
                HlslUtil.StructInfo structInfo = GetStructInfoFromNodeId(nodeId, unityReserved);
                ret = HlslTree.FullTypeInfo.MakeStruct(structInfo.identifier, -1, true);
            }
            else if (baseNode is HlslTree.NodeFunctionPrototype nodeFunctionPrototype)
            {

                HlslUtil.PrototypeInfo protoInfo = parsedFuncStructData.allPrototypes[nodeFunctionPrototype.protoId];

                ret = GetFullTypeInfoFromParserInfo(protoInfo.returnType);
            }
            else if (baseNode is HlslTree.NodeFunctionPrototypeExternal nodeFunctionPrototypeExternal)
            {
                HlslUtil.PrototypeInfo protoInfo = nodeFunctionPrototypeExternal.protoInfo;

                ret = GetFullTypeInfoFromParserInfo(protoInfo.returnType);
            }
            else if (baseNode is HlslTree.NodeBlock nodeBlock)
            {
                ret = HlslTree.FullTypeInfo.MakeValidNoType();
            }
            else if (baseNode is HlslTree.NodeStatement nodeStatement)
            {
                HlslTree.FullTypeInfo dstType = HlslTree.FullTypeInfo.MakeValidNoType();
                switch (nodeStatement.type)
                {
                    case HlslStatementType.If:
                        break;
                    case HlslStatementType.Switch:
                        break;
                    case HlslStatementType.Case:
                        break;
                    case HlslStatementType.Break:
                        break;
                    case HlslStatementType.Continue:
                        break;
                    case HlslStatementType.Default:
                        break;
                    case HlslStatementType.Goto:
                        break;
                    case HlslStatementType.Label:
                        break;
                    case HlslStatementType.For:
                        break;
                    case HlslStatementType.Do:
                        break;
                    case HlslStatementType.While:
                        break;

                    case HlslStatementType.Expression:
                        break;
                    case HlslStatementType.Return:
                        break;
                    default:
                        HlslUtil.ParserAssert(false);
                        break;
                }
                ret = dstType;
            }
            else if (baseNode is HlslTree.NodeFunction nodeFunction)
            {
                ret = fullTypeInfo[nodeFunction.prototypeId];
            }
            else if (baseNode is HlslTree.NodeDeclaration nodeDeclaration)
            {
                if (nodeDeclaration.structId >= 0)
                {
                    HlslTree.Node typeNode = allNodes[nodeDeclaration.typeNodeId];
                    if (typeNode is HlslTree.NodeToken nodeToken)
                    {
                        string identifier = tokenizer.GetTokenData(nodeToken.srcTokenId);
                        bool isUnityBuiltin = IsStructUnityBuiltin(nodeDeclaration.structId);
                        ret = FullTypeInfo.MakeStruct(identifier, nodeDeclaration.structId, isUnityBuiltin);
                    }
                    else
                    {
                        ret = fullTypeInfo[nodeDeclaration.typeNodeId];
                    }
                }
                else
                {
                    ret = fullTypeInfo[nodeDeclaration.typeNodeId];
                }
                ret.arrayDims = nodeDeclaration.arrayDims.Length;
                //tree.fullTypeInfo[nodeId] = tree.fullTypeInfo[nodeDeclaration.typeNodeId];
                //tree.fullTypeInfo[nodeId].arrayDims = nodeDeclaration.arrayDims.Length;
            }
            else if (baseNode is HlslTree.NodeVariable nodeVariable)
            {
                if (nodeVariable.nameVariableId >= 0)
                {
                    // if it's a variable defined in this code, fetch it
                    HlslParser.TypeInfo typeInfo = nodeVariable.GetNodeTypeInfo(this);
                    ret = GetFullTypeInfoFromParserInfo(typeInfo);
                }
                else
                {
                    // otherwise, it must be a reserved gloval
                    HlslParser.TypeInfo typeInfo;
                    string identifier = tokenizer.GetTokenData(nodeVariable.nameTokenId);
                    bool found = unityReserved.FindUnityGlobal(out typeInfo, identifier);
                    HlslUtil.ParserAssert(found);
                    ret = GetFullTypeInfoFromParserInfo(typeInfo);
                }
            }
            else if (baseNode is HlslTree.NodeNativeConstructor nodeNativeConstructor)
            {
                HlslParser.TypeInfo typeInfo = nodeNativeConstructor.GetNodeTypeInfo(this);
                ret = GetFullTypeInfoFromParserInfo(typeInfo);
            }
            else if (baseNode is HlslTree.NodeToken nodeToken)
            {
                ret = GetFullTypeInfoFromRawTokenId(nodeToken.srcTokenId);
            }
            else if (baseNode is HlslTree.NodeNativeType nodeNativeType)
            {
                ret = HlslTree.FullTypeInfo.MakeNativeType(nodeNativeType.nativeType);
            }
            else if (baseNode is HlslTree.NodeLiteralOrBool nodeLiteralOrBool)
            {
                HlslTree.FullTypeInfo fullInfo = HlslTree.FullTypeInfo.MakeInvalid();
                HlslToken tokenType = tokenizer.GetTokenType(nodeLiteralOrBool.nameTokenId);

                switch (tokenType)
                {
                    case HlslToken._literal_float:
                        fullInfo = HlslTree.FullTypeInfo.MakeNativeType(HlslNativeType._float);
                        break;
                    case HlslToken._literal_half:
                        fullInfo = HlslTree.FullTypeInfo.MakeNativeType(HlslNativeType._half);
                        break;
                    case HlslToken._literal_int:
                        fullInfo = HlslTree.FullTypeInfo.MakeNativeType(HlslNativeType._int);
                        break;
                    case HlslToken._literal_uint:
                        fullInfo = HlslTree.FullTypeInfo.MakeNativeType(HlslNativeType._uint);
                        break;
                    case HlslToken._literal_double:
                        // we don't really support doubles yet. float instead?
                        fullInfo = HlslTree.FullTypeInfo.MakeNativeType(HlslNativeType._float);
                        break;
                    case HlslToken._true:
                    case HlslToken._false:
                        fullInfo = HlslTree.FullTypeInfo.MakeNativeType(HlslNativeType._bool);
                        break;
                    default:
                        // no op, leave fullInfo as invalid
                        break;
                }
                ret = fullInfo;
            }
            else if (baseNode is HlslTree.NodePassthrough nodePassthrough)
            {
                // we are passing through the text without understanding it, so no type
                ret = HlslTree.FullTypeInfo.MakeValidNoType();
            }
            else if (baseNode is HlslTree.NodeExpression nodeExpression)
            {
                ret = ResolveExpressionType(nodeExpression);
            }
            else if (baseNode is HlslTree.NodeMemberVariable nodeMemberVariable)
            {
                HlslUtil.StructInfo structInfo = GetStructInfoFromNodeId(nodeMemberVariable.structNodeId, unityReserved);
                HlslUtil.FieldInfo fieldInfo = structInfo.fields[nodeMemberVariable.fieldIndex];

                ret = GetFullTypeInfoFromParserInfoAndDims(fieldInfo.typeInfo, fieldInfo.arrayDims);
            }
            else if (baseNode is HlslTree.NodeParenthesisGroup nodeParenthesisGroup)
            {
                if (nodeParenthesisGroup.childNodeId < 0)
                {
                    HlslUtil.ParserAssert(false);
                }
                ret = fullTypeInfo[nodeParenthesisGroup.childNodeId];
            }
            else if (baseNode is HlslTree.NodeMemberFunction nodeMemberFunction)
            {
                HlslUtil.StructInfo structInfo = GetStructInfoFromNodeId(nodeMemberFunction.structNodeId, unityReserved);
                HlslUtil.PrototypeInfo protoInfo = structInfo.prototypes[nodeMemberFunction.funcIndex];

                ret = GetFullTypeInfoFromParserInfo(protoInfo.returnType);
            }
            else if (baseNode is HlslTree.NodeSwizzle nodeSwizzle)
            {
                // the swizzle doesn't have a type per se. The expression node above it has both the
                // lhsNode, and the swizzle as children, so the that expression will apply the swizzle
                // to the type

                ret = HlslTree.FullTypeInfo.MakeValidNoType();
            }
            else if (baseNode is HlslTree.NodeBlockInitializer)
            {
                // we could store types, but it would be a pain. for now just ignore them.
                ret = HlslTree.FullTypeInfo.MakeValidNoType();
            }
            else
            {
                // if we got here, then we are missing a type in this if tree
                HlslUtil.ParserAssert(false);
            }

            return ret;
        }

        HlslUnityReserved unityReserved;

        HlslTokenizer tokenizer;
        internal Dictionary<HlslToken, HlslNativeType> tokenToNativeTable;

        // These are prototypes for builtin functions, unity reserved identifiers, or other
        // included functions that are actually used but we won't actually declare in the shader text.
        internal List<Node> prototypesReserved;

        internal HlslUtil.ParsedFuncStructData parsedFuncStructData;
        internal List<Node> allNodes;
        internal int topLevelNode;

        internal List<FullTypeInfo> fullTypeInfo;

        internal List<ErrInfo> errList;

        internal List<Dictionary<string, int>> structLookupStack; // lookup from string -> struct identifier

        internal List<VariableInfo> allVariables;
        internal List<Dictionary<string, int>> variableLookupStack;
    }

}
