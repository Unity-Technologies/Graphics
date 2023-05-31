using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;


namespace UnityEditor.Rendering.HighDefinition
{
    internal class HlslGenerator
    {
        internal HlslGenerator()
        {
        }

        Dictionary<string, Func1> func1s;
        Dictionary<string, Func2> func2s;
        Dictionary<string, Func3> func3s;
        Dictionary<string, SingleFunc> singleFuncs;
        Dictionary<string, BinaryFunc> binaryFuncs;

        internal void Init(HlslTokenizer srcTokenizer, HlslUnityReserved srcUnityReserved, HlslTree srcTree, bool emulate)
        {
            tokenizer = srcTokenizer;
            unityReserved = srcUnityReserved;
            tree = srcTree;

            isValid = false;

            tokenToNativeTable = HlslParser.GenerateTokenToNativeTable();

            apdWriter = new PartialDerivUtilWriter();
            applyEmulatedDerivatives = emulate;


            singleFuncs = new Dictionary<string, SingleFunc>();
            singleFuncs.Add("saturate", SingleFunc.Saturate);
            singleFuncs.Add("rcp", SingleFunc.Rcp);
            singleFuncs.Add("log", SingleFunc.Log);
            singleFuncs.Add("log2", SingleFunc.Log2);
            singleFuncs.Add("log10", SingleFunc.Log10);
            singleFuncs.Add("exp", SingleFunc.Exp);
            singleFuncs.Add("exp2", SingleFunc.Exp2);
            singleFuncs.Add("sqrt", SingleFunc.Sqrt);
            singleFuncs.Add("rsqrt", SingleFunc.Rsqrt);
            singleFuncs.Add("normalize", SingleFunc.Normalize);
            singleFuncs.Add("frac", SingleFunc.Frac);
            singleFuncs.Add("cos", SingleFunc.Cos);
            singleFuncs.Add("cosh", SingleFunc.CosH);
            singleFuncs.Add("sin", SingleFunc.Sin);
            singleFuncs.Add("sinh", SingleFunc.SinH);
            singleFuncs.Add("tan", SingleFunc.Tan);
            singleFuncs.Add("tanh", SingleFunc.TanH);
            singleFuncs.Add("abs", SingleFunc.Abs);
            singleFuncs.Add("floor", SingleFunc.Floor);
            singleFuncs.Add("ceil", SingleFunc.Ceil);

            binaryFuncs = new Dictionary<string, BinaryFunc>();
            binaryFuncs.Add("min", BinaryFunc.Min);
            binaryFuncs.Add("max", BinaryFunc.Max);
            binaryFuncs.Add("pow", BinaryFunc.Pow);
            binaryFuncs.Add("reflect", BinaryFunc.Reflect);

            func1s = new Dictionary<string, Func1>();
            func1s.Add("length", Func1.Len);

            func2s = new Dictionary<string, Func2>();
            func2s.Add("dot", Func2.Dot);
            func2s.Add("cross", Func2.Cross);

            func3s = new Dictionary<string, Func3>();
            func3s.Add("lerp", Func3.Lerp);
        }

        internal void LogError(string error)
        {
            isValid = false;
            errList.Add(error);
        }

        string GetFunctionNameFromNodeId(int nodeId)
        {
            string ret = "(invalid)";

            HlslTree.Node baseNode = tree.allNodes[nodeId];
            if (baseNode is HlslTree.NodeFunctionPrototype nodeProto)
            {
                ret = tokenizer.GetTokenData(nodeProto.nameTokenId);
            }
            else if (baseNode is HlslTree.NodeFunctionPrototypeExternal nodeProtoExternal)
            {
                ret = nodeProtoExternal.protoInfo.identifier;
            }
            else if (baseNode is HlslTree.NodeToken nodeToken)
            {
                // if a function was used without a prototype being declared, we assume that this is a function with unknown parameters.
                ret = tokenizer.GetTokenData(nodeToken.srcTokenId);
            }
            else
            {
                // invalid prototype id
                HlslUtil.ParserAssert(false);
            }

            return ret;
        }

        // note: assumes the children are processed first
        // traverse through the nodes, and:
        //     for each node
        //         find all the children nodes
        //     for each variable
        //         find all the children nodes
        void FindChildrenFromParentsRecurse(List<int>[] childList, List<int>[] varibleList, int nodeId)
        {
            for (int nodeIter = 0; nodeIter < nodeParents.Length; nodeIter++)
            {
                int currParent = nodeParents[nodeIter];
                if (currParent >= 0)
                {
                    childList[currParent].Add(nodeIter);
                }
            }
        }

        void FindChildrenFromParents()
        {
            List<int>[] childList = new List<int>[tree.allNodes.Count];
            List<int>[] variableList = new List<int>[tree.allNodes.Count];

            for (int i = 0; i < childList.Length; i++)
            {
                childList[i] = new List<int>();
            }

            // now that we know the parent, create a helper list for children
            FindChildrenFromParentsRecurse(childList, variableList, tree.topLevelNode);

            nodeChildren = new int[tree.allNodes.Count][];
            for (int i = 0; i < nodeChildren.Length; i++)
            {
                nodeChildren[i] = childList[i].ToArray();
            }
        }

        void MarkApdNodesAndVariablesIsLegal(out bool[] isNodeApdLegalVec, out bool[] isVariableApdLegalVec)
        {
            int numNodes = tree.allNodes.Count;
            int numVariables = tree.allVariables.Count;

            isNodeApdLegalVec = new bool[numNodes];
            isVariableApdLegalVec = new bool[numVariables];

            for (int varIter = 0; varIter < numVariables; varIter++)
            {
                HlslTree.VariableInfo variable = tree.allVariables[varIter];

                bool isLegal = HlslUtil.IsNativeTypeLegalForApd(variable.typeInfo.nativeType);
                isVariableApdLegalVec[varIter] = isLegal;
            }

            for (int nodeIter = 0; nodeIter < numNodes; nodeIter++)
            {
                HlslTree.Node baseNode = tree.allNodes[nodeIter];


                HlslTree.FullTypeInfo fullType = tree.fullTypeInfo[nodeIter];

                bool isLegal = false;

                if (baseNode is HlslTree.NodeTopLevel nodeTopLevel)
                {
                    isLegal = false;// nope
                }
                else if (baseNode is HlslTree.NodeStruct nodeStruct)
                {
                    // struct definitions are not allowed to be APD. The declarations
                    // are child nodes which can be legl, but the actual struct is not.
                    isLegal = false;// nope
                }
                else if (baseNode is HlslTree.NodeUnityStruct nodeUnityStruct)
                {
                    isLegal = false;// nope
                }
                else if (baseNode is HlslTree.NodeFunctionPrototype nodeFunctionPrototype)
                {
                    isLegal = true;
                }
                else if (baseNode is HlslTree.NodeFunctionPrototypeExternal nodeFunctionPrototypeExternal)
                {
                    // we can modify user-defined function prototypes, but we can not modify builtin
                    // function prototypes
                    isLegal = false;
                }
                else if (baseNode is HlslTree.NodeBlock nodeBlock)
                {
                    isLegal = true; // sure, we might change the return type to legal
                }
                else if (baseNode is HlslTree.NodeStatement nodeStatement)
                {
                    isLegal = true;
                }
                else if (baseNode is HlslTree.NodeFunction nodeFunction)
                {
                    // when a function converts to APD, we're only changing the return type. however, all of
                    // the child nodes in the function->prototype->declarations can be APD
                    isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                }
                else if (baseNode is HlslTree.NodeDeclaration nodeDeclaration)
                {
                    // is it able to become apd? depends on the type.
                    isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                }
                else if (baseNode is HlslTree.NodeVariable nodeVariable)
                {
                    // always depends on type
                    isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                }
                else if (baseNode is HlslTree.NodeNativeConstructor nodeNativeConstructor)
                {
                    // constructors can be changed as long as the native type is derivativeable
                    isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                }
                else if (baseNode is HlslTree.NodeToken nodeToken)
                {
                    // a token is a token. while we can coerce the format after, we can't change the original token.
                    isLegal = false;
                }
                else if (baseNode is HlslTree.NodeNativeType nodeNativeType)
                {
                    // native types can not be promoted
                    isLegal = false;
                }
                else if (baseNode is HlslTree.NodeLiteralOrBool nodeLiteralOrBool)
                {
                    // constants can be promoted, as long as they are not bools or ints
                    isLegal = false;

                }
                else if (baseNode is HlslTree.NodePassthrough nodePassthrough)
                {
                    // we are passing through the text without understanding it, so no type
                    isLegal = false;
                }
                else if (baseNode is HlslTree.NodeExpression nodeExpression)
                {
                    switch (nodeExpression.op)
                    {
                        case HlslOp.Invalid:            //
                        case HlslOp.ScopeReslution:     // ::
                        case HlslOp.LogicalNot:         // !
                        case HlslOp.BitwiseNot:         // ~
                        case HlslOp.CStyleCast:         // (int)
                        case HlslOp.Dereference:        // *    - not legal in HLSL?
                        case HlslOp.AddressOf:          // &    - not legal in HLSL?
                        case HlslOp.Sizeof:             // sizeof
                        case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                        case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                        case HlslOp.Mod:             // %
                        case HlslOp.ShiftL:             // <<
                        case HlslOp.ShiftR:             // >>
                        case HlslOp.ThreeWayCompare:    // <=>  - never used this
                        case HlslOp.LessThan:           // <
                        case HlslOp.GreaterThan:        // >
                        case HlslOp.LessEqual:          // <=
                        case HlslOp.GreaterEqual:       // >=
                        case HlslOp.CompareEqual:       // ==
                        case HlslOp.NotEqual:           // !=
                        case HlslOp.BitwiseAnd:         // &
                        case HlslOp.BitwiseXor:         // ^
                        case HlslOp.BitwiseOr:          // |
                        case HlslOp.LogicalAnd:         // &&
                        case HlslOp.LogicalOr:          // ||
                        case HlslOp.ModEquals:    // %=
                        case HlslOp.ShiftLEquals:       // <<=
                        case HlslOp.ShiftREquals:       // >>=
                        case HlslOp.AndEquals:          // &=
                        case HlslOp.XorEquals:          // ^=
                        case HlslOp.OrEquals:           // |=
                            isLegal = false;
                            break;
                        case HlslOp.PostIncrement:      // ++
                        case HlslOp.PostDecrement:      //  --
                        case HlslOp.FunctionalCast:     // int()
                        case HlslOp.FunctionalCall:     // func()
                        case HlslOp.Subscript:          // []
                        case HlslOp.MemberAccess:       // .
                        case HlslOp.PreIncrement:       // ++
                        case HlslOp.PreDecrement:       // --
                        case HlslOp.UnaryPlus:          // +
                        case HlslOp.UnaryMinus:         // -
                        case HlslOp.Mul:                // *
                        case HlslOp.Div:                // /
                        case HlslOp.Add:                // +
                        case HlslOp.Sub:                // -
                        case HlslOp.TernaryQuestion:    // ?
                        case HlslOp.TernaryColon:       // :
                        case HlslOp.Assignment:         // =
                        case HlslOp.AddEquals:         // +=
                        case HlslOp.SubEquals:          // -=
                        case HlslOp.MulEquals:          // *=
                        case HlslOp.DivEquals:          // /=
                        case HlslOp.Comma:              // :
                            isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                            break;
                        default:
                            HlslUtil.ParserAssert(false);
                            break;
                    }
                }
                else if (baseNode is HlslTree.NodeMemberVariable nodeMemberVariable)
                {
                    bool isUnityBuiltin = tree.IsStructUnityBuiltin(nodeMemberVariable.structNodeId);

                    if (isUnityBuiltin)
                    {
                        isLegal = false;
                    }
                    else
                    {
                        isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                    }
                }
                else if (baseNode is HlslTree.NodeParenthesisGroup nodeParenthesisGroup)
                {
                    isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                }
                else if (baseNode is HlslTree.NodeMemberFunction nodeMemberFunction)
                {
                    bool isUnityBuiltin = tree.IsStructUnityBuiltin(nodeMemberFunction.structNodeId);

                    if (isUnityBuiltin)
                    {
                        isLegal = false;
                    }
                    else
                    {
                        isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                    }
                }
                else if (baseNode is HlslTree.NodeSwizzle nodeSwizzle)
                {
                    isLegal = HlslUtil.IsNativeTypeLegalForApd(fullType.nativeType);
                }
                else if (baseNode is HlslTree.NodeBlockInitializer nodeblockInit)
                {
                    isLegal = false; // block initializers are forbidden from being apd
                }
                else
                {
                    // if we got here, then we are missing a type in this if tree
                    HlslUtil.ParserAssert(false);
                }

                isNodeApdLegalVec[nodeIter] = isLegal;
            }

        }

        void MarkApdSinks(out bool[] isNodeApdSink)
        {
            int numNodes = tree.allNodes.Count;
            // initialized to false
            isNodeApdSink = new bool[numNodes];

            for (int i = 0; i < numNodes; i++)
            {
                HlslTree.Node node = tree.allNodes[i];
                if (node is HlslTree.NodeExpression nodeExpression)
                {
                    if (nodeExpression.op == HlslOp.FunctionalCall)
                    {
                        // for a function, lhs is the function name and paramIds[] are the parameters
                        HlslTree.Node lhsNode = tree.allNodes[nodeExpression.lhsNodeId];

                        // rhs node can either be a user function or an external function. The only
                        // true sinks are from external functions (such as SAMPLE_TEXTURE2D)
                        if (lhsNode is HlslTree.NodeFunctionPrototypeExternal nodeExternal)
                        {
                            for (int paramIter = 0; paramIter < nodeExternal.protoInfo.paramInfoVec.Length; paramIter++)
                            {
                                HlslParser.TypeInfo typeInfo = nodeExternal.protoInfo.paramInfoVec[paramIter];
                                if (typeInfo.allowedState == ApdAllowedState.OnlyApd)
                                {
                                    int sinkNodeId = nodeExpression.paramIds[paramIter];
                                    isNodeApdSink[sinkNodeId] = true;
                                }
                            }
                        }
                    }
                    else if (nodeExpression.op == HlslOp.MemberAccess)
                    {
                        // for a member acces, the lhs op is variable and the rhs is the member
                        HlslTree.Node rhsNode = tree.allNodes[nodeExpression.rhsNodeId];

                        // if it's a member function, it could be a texture sample
                        if (rhsNode is HlslTree.NodeMemberFunction nodeMemberFunction)
                        {
                            // todo
                        }

                    }
                }
            }
        }

        static ApdStatus MergeOpApdStatus(ApdStatus lhs, ApdStatus rhs)
        {
            ApdStatus ret = ApdStatus.Unknown;
            if (lhs == ApdStatus.Valid || rhs == ApdStatus.Valid)
            {
                ret = ApdStatus.Valid;
            }
            else if (lhs == ApdStatus.Invalid || rhs == ApdStatus.Invalid)
            {
                ret = ApdStatus.Invalid;
            }
            else if (lhs == ApdStatus.Zero || rhs == ApdStatus.Zero)
            {
                ret = ApdStatus.Zero;
            }
            else
            {
                ret = ApdStatus.Unknown;
            }
            return ret;
        }

        ApdStatus GetNodeApdStatusForExpression(HlslApdInfo apdInfo, int nodeIdx)
        {
            ApdStatus ret = ApdStatus.Unknown;
            HlslTree.Node baseNode = tree.allNodes[nodeIdx];

            HlslUtil.ParserAssert(baseNode is HlslTree.NodeExpression);
            HlslTree.NodeExpression nodeExpression = (HlslTree.NodeExpression)baseNode;

            switch (nodeExpression.op)
            {
                // these opssimply accept the lhs
                case HlslOp.PostIncrement:      // ++
                case HlslOp.PostDecrement:      //  --
                case HlslOp.PreIncrement:       // ++
                case HlslOp.PreDecrement:       // --
                case HlslOp.UnaryPlus:          // +
                case HlslOp.UnaryMinus:         // -
                    ret = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];
                    break;

                // shift, bitwise, logical, and mod operations are invalid
                case HlslOp.LogicalNot:         // !
                case HlslOp.BitwiseNot:         // ~
                case HlslOp.ShiftL:             // <<
                case HlslOp.ShiftR:             // >>
                case HlslOp.ShiftLEquals:       // <<=
                case HlslOp.ShiftREquals:       // >>=
                case HlslOp.AndEquals:          // &=
                case HlslOp.XorEquals:          // ^=
                case HlslOp.OrEquals:           // |=
                case HlslOp.BitwiseAnd:         // &
                case HlslOp.BitwiseXor:         // ^
                case HlslOp.BitwiseOr:          // |
                case HlslOp.LessThan:           // <
                case HlslOp.GreaterThan:        // >
                case HlslOp.LessEqual:          // <=
                case HlslOp.GreaterEqual:       // >=
                case HlslOp.CompareEqual:       // ==
                case HlslOp.NotEqual:           // !=
                case HlslOp.LogicalAnd:         // &&
                case HlslOp.LogicalOr:          // ||
                case HlslOp.ModEquals:    // %=
                case HlslOp.Mod:                // %
                    ret = ApdStatus.Invalid;
                    break;

                // for pure assignment, take the rhs
                case HlslOp.Assignment:         // =
                    ret = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                    break;

                // for binary ops merge the lhs and rhs together
                case HlslOp.Mul:                // *
                case HlslOp.Div:                // /
                case HlslOp.Add:                // +
                case HlslOp.Sub:                // -
                case HlslOp.AddEquals:          // +=
                case HlslOp.SubEquals:          // -=
                case HlslOp.MulEquals:          // *=
                case HlslOp.DivEquals:          // /=
                    {
                        ApdStatus lhsStatus = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];
                        ApdStatus rhsStatus = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                        ret = MergeOpApdStatus(lhsStatus, rhsStatus);
                    }
                    break;

                // these ops are not supported
                case HlslOp.Invalid:            //
                case HlslOp.ScopeReslution:     // ::
                case HlslOp.Dereference:        // *    - not legal in HLSL?
                case HlslOp.AddressOf:          // &    - not legal in HLSL?
                case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                case HlslOp.ThreeWayCompare:    // <=>  - never used this
                    ret = ApdStatus.Invalid;
                    break;

                // use the lhs type
                case HlslOp.Subscript:          // []
                    ret = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];
                    break;
                // these ops are not supported now, but really should be at some point;
                case HlslOp.FunctionalCast:     // int()
                    ret = ApdStatus.Invalid;
                    break;

                case HlslOp.FunctionalCall:     // func()
                    // lhs node is the function call prototype node id, so we actually just use lhs
                    ret = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];
                    break;
                case HlslOp.MemberAccess:       // .
                    {
                        HlslTree.Node nodeRhs = tree.allNodes[nodeExpression.rhsNodeId];

                        if (nodeRhs is HlslTree.NodeMemberVariable nodeMemberVariable)
                        {
                            // use this field
                            ret = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                        }
                        else if (nodeRhs is HlslTree.NodeMemberFunction nodeMemberFunction)
                        {
                            // use the return type of the member function
                            ret = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                        }
                        else if (nodeRhs is HlslTree.NodeSwizzle nodeSwizzle)
                        {
                            // the swizzle will not change the apd status of the lhs side of the expression
                            ret = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];
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
                        HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(nodeExpression.cstyleCastStructId, unityReserved);
                        ret = ApdStatus.Invalid;
                    }
                    else
                    {
                        HlslToken tokenType = tokenizer.GetTokenType(nodeExpression.opTokenId);

                        HlslNativeType nativeType = HlslNativeType._invalid;
                        if (tokenToNativeTable.ContainsKey(tokenType))
                        {
                            nativeType = tokenToNativeTable[tokenType];
                        }

                        bool isApdLegal = HlslUtil.IsNativeTypeLegalForApd(nativeType);
                        if (isApdLegal)
                        {
                            ret = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];
                        }
                        else
                        {
                            ret = ApdStatus.Invalid;
                        }
                    }
                    break;
                case HlslOp.Sizeof:             // sizeof
                    ret = ApdStatus.Invalid;
                    break;
                case HlslOp.TernaryQuestion:    // ?
                    // treat this as the binary op of the middle and right terms?
                    {
                        ApdStatus lhsStatus = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                        ApdStatus rhsStatus = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                        ApdStatus rhsRhsStatus = apdInfo.nodeApdStatus[nodeExpression.rhsRhsNodeId];

                        ret = MergeOpApdStatus(MergeOpApdStatus(lhsStatus, rhsStatus), rhsRhsStatus);
                    }
                    break;
                case HlslOp.TernaryColon:       // :
                    // should never happen, since the three terms of the ternary are grouped into a single expression
                    // with op as HlslOp.TernaryQuestion
                    break;
                case HlslOp.Comma:              // ,
                    // simply returns rhs
                    ret = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                    break;
                default:
                    // should never happen
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        List<int> FindAllReturnNodesInBlock(HlslApdInfo apdInfo, int nodeId)
        {
            List<int> retNodes = new List<int>();

            HlslTree.Node node = tree.allNodes[nodeId];
            HlslUtil.ParserAssert(node is HlslTree.NodeBlock);

            HlslTree.NodeBlock nodeBlock = (HlslTree.NodeBlock)node;

            // simple depth first search
            Stack<int> nodeStack = new Stack<int>();

            for (int i = 0; i < nodeBlock.statements.Length; i++)
            {
                nodeStack.Push(nodeBlock.statements[i]);
            }

            while (nodeStack.Count > 0)
            {
                int topNodeId = nodeStack.Pop();
                HlslTree.Node topNode = tree.allNodes[topNodeId];
                if (topNode is HlslTree.NodeStatement topNodeStatement)
                {
                    if (topNodeStatement.type == HlslStatementType.Return)
                    {
                        retNodes.Add(topNodeId);
                    }
                }

                // add the children
                for (int i = 0; i < nodeChildren[topNodeId].Length; i++)
                {
                    nodeStack.Push(nodeChildren[topNodeId][i]);
                }
            }

            return retNodes;
        }

        List<int> FindAllNodesWithStructFieldFromIdPair(HlslApdInfo apdInfo, int structId, int fieldId)
        {
            List<int> ret = new List<int>();

            // TODO: Add a reverse lookup instead of going through all the nodes
            int numNodes = tree.allNodes.Count;
            for (int i = 0; i < numNodes; i++)
            {
                HlslTree.Node currNode = tree.allNodes[i];
                if (currNode is HlslTree.NodeExpression nodeExpression)
                {
                    if (nodeExpression.op == HlslOp.MemberAccess)
                    {
                        HlslTree.Node rhsNode = tree.allNodes[nodeExpression.rhsNodeId];

                        HlslTree.FullTypeInfo lhsTypeInfo = tree.fullTypeInfo[nodeExpression.lhsNodeId];

                        // lhs of MemberAccess should always be a struct
                        if (lhsTypeInfo.nativeType == HlslNativeType._struct)
                        {
                            // if this is the struct we are working with
                            if (lhsTypeInfo.structId == structId)
                            {
                                // get the right side, which should be a field or a member func. Ignore if it is a MemberFunction.
                                if (rhsNode is HlslTree.NodeMemberVariable nodeMemberVariable)
                                {
                                    if (nodeMemberVariable.fieldIndex == fieldId)
                                    {
                                        ret.Add(nodeExpression.rhsNodeId);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return ret;
        }


        // nodeIdx is for the decl
        List<int> FindAllNodesWithStructFieldFromDecl(HlslApdInfo apdInfo, int nodeIdx)
        {

            int parentId = nodeParents[nodeIdx];

            // parentId should never be top-level
            HlslUtil.ParserAssert(parentId >= 0);
            HlslTree.Node parentNode = tree.allNodes[parentId];

            HlslUtil.ParserAssert(parentNode is HlslTree.NodeStruct);
            HlslTree.NodeStruct parentNodeStruct = (HlslTree.NodeStruct)parentNode;

            List<int> ret;

            // for each instance of calling this struct, mark the field
            {
                int foundFieldId = -1;
                for (int i = 0; i < parentNodeStruct.declarations.Length; i++)
                {
                    if (parentNodeStruct.declarations[i] == nodeIdx)
                    {
                        foundFieldId = i;
                    }
                }
                HlslUtil.ParserAssert(foundFieldId >= 0);

                ret = FindAllNodesWithStructFieldFromIdPair(apdInfo, parentId, foundFieldId);

            }

            return ret;
        }


        int FindVariableInfoIdForDecl(HlslApdInfo apdInfo, int nodeIdx)
        {
            // First, find the variable that refers to this declaration.
            // TODO: Make an optimization structure for this operation.
            int foundVariableInfoId = -1;
            {
                for (int i = 0; i < tree.allVariables.Count; i++)
                {
                    if (tree.allVariables[i].declId == nodeIdx)
                    {
                        foundVariableInfoId = i;
                        break;
                    }
                }
            }

            HlslUtil.ParserAssert(foundVariableInfoId >= 0);
            return foundVariableInfoId;
        }


        List<int> FindAllFuncCallExpressionsForPrototype(HlslApdInfo apdInfo, int nodeIdx)
        {
            List<int> ret = new List<int>();
            HlslTree.Node baseNode = tree.allNodes[nodeIdx];

            HlslUtil.ParserAssert(baseNode is HlslTree.NodeFunctionPrototype);

            HlslTree.NodeFunctionPrototype nodeFunctionPrototype = (HlslTree.NodeFunctionPrototype)baseNode;

            int numNodes = tree.allNodes.Count;

            // TODO: Make an accelleration structure.
            for (int i = 0; i < numNodes; i++)
            {
                HlslTree.Node currNode = tree.allNodes[i];
                if (currNode is HlslTree.NodeExpression nodeExpression)
                {
                    if (nodeExpression.op == HlslOp.FunctionalCall)
                    {
                        HlslTree.Node lhsNode = tree.allNodes[nodeExpression.lhsNodeId];

                        if (lhsNode is HlslTree.NodeFunctionPrototype nodeProto)
                        {
                            string funcExpectedName = tokenizer.GetTokenData(nodeFunctionPrototype.nameTokenId);
                            string funcActualName = tokenizer.GetTokenData(nodeProto.nameTokenId);
                            if (string.Compare(funcExpectedName, funcActualName) == 0)
                            {
                                ret.Add(nodeExpression.lhsNodeId);
                            }
                        }
                        else if (lhsNode is HlslTree.NodeFunctionPrototypeExternal nodeProtoExternal)
                        {
                            // external prototypes can not be converted to hlsl
                        }
                        else
                        {
                            // should never happen because the lhs of a node with a function call op should be either NodeFunctionPrototype or NodeFunctionPrototypeExternal
                            HlslUtil.ParserAssert(false);
                        }
                    }
                }
            }

            return ret;
        }

        List<int> FindAllNodeArgumentsForFunctionParam(HlslApdInfo apdInfo, int nodeIdx)
        {
            List<int> ret = new List<int>();

            int parentId = nodeParents[nodeIdx];

            // parentId should never be top-level
            HlslUtil.ParserAssert(parentId >= 0);
            HlslTree.Node parentNode = tree.allNodes[parentId];

            HlslUtil.ParserAssert(parentNode is HlslTree.NodeFunctionPrototype);
            HlslTree.NodeFunctionPrototype nodeFuncProto = (HlslTree.NodeFunctionPrototype)parentNode;

            // figure out which paramter it is
            int foundParamIndex = -1;
            for (int i = 0; i < nodeFuncProto.declarations.Length; i++)
            {
                if (nodeFuncProto.declarations[i] == nodeIdx)
                {
                    foundParamIndex = i;
                    break;
                }
            }
            HlslUtil.ParserAssert(foundParamIndex >= 0);

            // for each instance of calling this function, mark the parameter
            {
                // TODO: Add a reverse lookup instead of going through all the nodes
                int numNodes = tree.allNodes.Count;
                for (int i = 0; i < numNodes; i++)
                {
                    HlslTree.Node currNode = tree.allNodes[i];
                    if (currNode is HlslTree.NodeExpression nodeExpression)
                    {
                        if (nodeExpression.op == HlslOp.FunctionalCall)
                        {
                            HlslTree.Node lhsNode = tree.allNodes[nodeExpression.lhsNodeId];

                            if (lhsNode is HlslTree.NodeFunctionPrototype nodeProto)
                            {
                                string funcExpectedName = tokenizer.GetTokenData(nodeFuncProto.nameTokenId);
                                string funcActualName = tokenizer.GetTokenData(nodeProto.nameTokenId);
                                if (string.Compare(funcExpectedName, funcActualName) == 0)
                                {
                                    // we don't need to update every variable, just the declaration that we changed
                                    int nodeToUpdate = nodeExpression.paramIds[foundParamIndex];

                                    ret.Add(nodeToUpdate);
                                }
                            }
                            else if (lhsNode is HlslTree.NodeFunctionPrototypeExternal nodeProtoExternal)
                            {
                                // external prototypes can not be converted to hlsl
                            }
                            else
                            {
                                // should never happen because the lhs of a node with a function call op should be either NodeFunctionPrototype or NodeFunctionPrototypeExternal
                                HlslUtil.ParserAssert(false);
                            }
                        }
                    }
                }
            }

            return ret;
        }

        class HlslApdInfo
        {
            internal string[] debugNodeNames;
            internal string[] debugVariableNames;
            internal bool[] isNodeApdLegalVec;
            internal bool[] isVariableApdLegalVec;
            internal bool[] isNodeApdSink;
            internal int[][] variableNodeVec;
            internal bool[] isNodeDesiredApdVec;
            internal bool[] isVariableDesiredApdVec;
            internal ApdStatus[] nodeApdStatus;

            internal bool[] isNodeOnApdPath;

            // total num entries
            internal int[][] nodeApdDependencies;
            internal int[][] nodeApdDependenciesRev;
        }

        // maybe instead of an array of lists it should be an array of dictionaries?
        static void AddApdDependency(List<int>[] allNodeApdDeps, int src, int dst)
        {
            if (dst < 0)
            {
                // no op
            }
            else if (!allNodeApdDeps[src].Contains(dst))
            {
                allNodeApdDeps[src].Add(dst);
            }
        }

        // returns the declaration node id
        int GetProtoParamDeclAndModifier(out HlslToken modifierType, int protoNodeId, int paramIndex)
        {
            HlslTree.Node baseNode = tree.allNodes[protoNodeId];
            HlslUtil.ParserAssert(baseNode is HlslTree.NodeFunctionPrototype);

            HlslTree.NodeFunctionPrototype nodeProto = (HlslTree.NodeFunctionPrototype)baseNode;

            int declId = nodeProto.declarations[paramIndex];

            HlslTree.Node baseDecl = tree.allNodes[declId];
            HlslUtil.ParserAssert(baseDecl is HlslTree.NodeDeclaration);

            HlslTree.NodeDeclaration nodeDecl = (HlslTree.NodeDeclaration)baseDecl;

            modifierType = HlslToken._invalid;
            if (nodeDecl.modifierTokenId >= 0)
            {
                modifierType = tokenizer.GetTokenType(nodeDecl.modifierTokenId);
            }

            return declId;
        }

        void CalculateApdDependenciesRecurse(List<int>[] allNodeApdDeps, int nodeId)
        {
            if (nodeId < 0)
            {
                return;
            }

            HlslTree.Node baseNode = tree.allNodes[nodeId];
            if (baseNode is HlslTree.NodeTopLevel nodeTopLevel)
            {
                // top level node doesn't have a type. Just recurse.
                for (int i = 0; i < nodeTopLevel.statements.Length; i++)
                {
                    CalculateApdDependenciesRecurse(allNodeApdDeps, nodeTopLevel.statements[i]);
                }
            }
            else if (baseNode is HlslTree.NodeStruct nodeStruct)
            {
                // a struct can't be apd, so just recurse
                for (int i = 0; i < nodeStruct.declarations.Length; i++)
                {
                    CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStruct.declarations[i]);
                }
            }
            else if (baseNode is HlslTree.NodeUnityStruct nodeUnityStruct)
            {
                // unity structs can't be apd and have nothing to recurse, so no op
            }
            else if (baseNode is HlslTree.NodeFunctionPrototype nodeFunctionPrototype)
            {
                // the apd status of the prototype depends on the nodeFunction, which is its parent
                int parentId = nodeParents[nodeId];
                AddApdDependency(allNodeApdDeps, nodeId, parentId);

                // all of the individual parameters will be handled by the actual statements that call the function
                for (int i = 0; i < nodeFunctionPrototype.declarations.Length; i++)
                {
                    CalculateApdDependenciesRecurse(allNodeApdDeps, nodeFunctionPrototype.declarations[i]);
                }

            }
            else if (baseNode is HlslTree.NodeFunctionPrototypeExternal nodeFunctionPrototypeExternal)
            {
                // External functions can not be converted. However expressions which call external functions
                // may be converted in specific cases (such as texture reads)
            }
            else if (baseNode is HlslTree.NodeBlock nodeBlock)
            {
                // A block node is APD if any of the functions have a return type of APD.
                // We now need to go through the code block and change any return types.
                List<int> retNodes = FindAllReturnNodesInBlock(apdInfo, nodeId);

                for (int i = 0; i < retNodes.Count; i++)
                {
                    int retNodeId = retNodes[i];
                    AddApdDependency(allNodeApdDeps, nodeId, retNodeId);
                }

                for (int i = 0; i < nodeBlock.statements.Length; i++)
                {
                    CalculateApdDependenciesRecurse(allNodeApdDeps, nodeBlock.statements[i]);
                }
            }
            else if (baseNode is HlslTree.NodeStatement nodeStatement)
            {
                switch (nodeStatement.type)
                {

                    case HlslStatementType.If:
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.expression);
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.childBlockOrStatement);
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.elseBlockOrStatement);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.expression);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.childBlockOrStatement);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.elseBlockOrStatement);
                        break;
                    case HlslStatementType.Switch:
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.expression);
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.childBlockOrStatement);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.expression);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.childBlockOrStatement);
                        break;
                    case HlslStatementType.Case:
                        // no apd expressions since it must be a compile time constant
                        break;
                    case HlslStatementType.Break:
                        // no dependencies or expressions
                        break;
                    case HlslStatementType.Continue:
                        // no dependencies or expressions
                        break;
                    case HlslStatementType.Default:
                        // no dependencies or expressions
                        break;
                    case HlslStatementType.Goto:
                        // no apd expressions since label is constant
                        break;
                    case HlslStatementType.Label:
                        // label is constant
                        break;
                    case HlslStatementType.For:

                        HlslUtil.ParserAssert(nodeStatement.forExpressions.Length == 3);
                        for (int i = 0; i < 3; i++)
                        {
                            AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.forExpressions[i]);
                            CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.forExpressions[i]);
                        }
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.childBlockOrStatement);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.childBlockOrStatement);
                        break;
                    case HlslStatementType.Do:
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.expression);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.expression);

                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.childBlockOrStatement);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.childBlockOrStatement);
                        break;
                    case HlslStatementType.While:
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.expression);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.expression);

                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.childBlockOrStatement);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.childBlockOrStatement);
                        break;
                    case HlslStatementType.Expression:
                        // the apd type of a single statement expression is just the expression
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.expression);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.expression);
                        break;
                    case HlslStatementType.Return:
                        // the block depends on all of the return statements in the function, and
                        // the return statements in the function depend on the expressions.
                        AddApdDependency(allNodeApdDeps, nodeId, nodeStatement.expression);
                        CalculateApdDependenciesRecurse(allNodeApdDeps, nodeStatement.expression);
                        break;
                    default:
                        HlslUtil.ParserAssert(false);
                        break;
                }
            }
            else if (baseNode is HlslTree.NodeFunction nodeFunction)
            {
                // the function return type depends on the blockId
                AddApdDependency(allNodeApdDeps, nodeId, nodeFunction.blockId);

                // the prototypeID depends on the block, but that will be handled by the prototype recursion
                CalculateApdDependenciesRecurse(allNodeApdDeps, nodeFunction.prototypeId);
                CalculateApdDependenciesRecurse(allNodeApdDeps, nodeFunction.blockId);
            }
            else if (baseNode is HlslTree.NodeDeclaration nodeDeclaration)
            {
                // the actual type node depends on the decl parent
                AddApdDependency(allNodeApdDeps, nodeDeclaration.typeNodeId, nodeId);
                CalculateApdDependenciesRecurse(allNodeApdDeps, nodeDeclaration.typeNodeId);

                // node declarations depend on the initializer.
                if (nodeDeclaration.initializerId >= 0)
                {
                    AddApdDependency(allNodeApdDeps, nodeId, nodeDeclaration.initializerId);

                    CalculateApdDependenciesRecurse(allNodeApdDeps, nodeDeclaration.initializerId);
                }

            }
            else if (baseNode is HlslTree.NodeVariable nodeVariable)
            {
                // this dependency goes both ways. the declaration depends on the variable and the variable
                // depend on the declaration.
                int variableId = nodeVariable.nameVariableId;
                if (variableId >= 0)
                {
                    // only applicable if the variable is defined in this function, obviously
                    // not true for a global
                    int declNodeId = tree.allVariables[variableId].declId;

                    AddApdDependency(allNodeApdDeps, nodeId, declNodeId);
                    AddApdDependency(allNodeApdDeps, declNodeId, nodeId);
                }
            }
            else if (baseNode is HlslTree.NodeNativeConstructor nodeNativeConstructor)
            {
                // this depends on the children
                for (int i = 0; i < nodeNativeConstructor.paramNodeIds.Length; i++)
                {
                    int childId = nodeNativeConstructor.paramNodeIds[i];
                    AddApdDependency(allNodeApdDeps, nodeId, childId);
                    CalculateApdDependenciesRecurse(allNodeApdDeps, childId);
                }
            }
            else if (baseNode is HlslTree.NodeToken nodeToken)
            {
                // tokens can't be changed. no op
            }
            else if (baseNode is HlslTree.NodeNativeType nodeNativeType)
            {
                // it is what it is with no dependencies. no op.
            }
            else if (baseNode is HlslTree.NodeLiteralOrBool nodeLiteralOrBool)
            {
                // a constant is a constant, no op
            }
            else if (baseNode is HlslTree.NodePassthrough nodePassthrough)
            {
                // passthrough can't be changed, no op
            }
            else if (baseNode is HlslTree.NodeExpression nodeExpression)
            {
                // recurse everything that can recurse
                CalculateApdDependenciesRecurse(allNodeApdDeps, nodeExpression.lhsNodeId);
                CalculateApdDependenciesRecurse(allNodeApdDeps, nodeExpression.rhsNodeId);
                CalculateApdDependenciesRecurse(allNodeApdDeps, nodeExpression.rhsRhsNodeId);

                for (int i = 0; i < nodeExpression.paramIds.Length; i++)
                {
                    CalculateApdDependenciesRecurse(allNodeApdDeps, nodeExpression.paramIds[i]);
                }

                // depends on the type
                switch (nodeExpression.op)
                {
                    // these are not supported:
                    case HlslOp.Invalid:            //
                    case HlslOp.ScopeReslution:     // ::
                    case HlslOp.ThreeWayCompare:    // <=>  - spaceship---watch out for men in black
                    case HlslOp.Dereference:        // *    - not legal in HLSL?
                    case HlslOp.AddressOf:          // &    - not legal in HLSL?
                    case HlslOp.Sizeof:             // sizeof
                    case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                    case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                        break;

                    // increment/decrement/unary plus/minus have no effect on apd status, so just link
                    // the parent (curr) and child together.
                    case HlslOp.PostIncrement:      // ++
                    case HlslOp.PostDecrement:      //  --
                    case HlslOp.PreIncrement:       // ++
                    case HlslOp.PreDecrement:       // --
                    case HlslOp.UnaryPlus:          // +
                    case HlslOp.UnaryMinus:         // -
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.lhsNodeId);
                        break;

                    case HlslOp.FunctionalCast:     // int()
                        // functional cast depends on rhs
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.rhsNodeId);
                        break;

                    case HlslOp.FunctionalCall:     // func()
                        // the return type of this node depends on the return type of the function.
                        // lhsNodeId refers to the prototype node
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.lhsNodeId);

                        {
                            HlslTree.Node baseProtoNode = tree.allNodes[nodeExpression.lhsNodeId];
                            if (baseProtoNode is HlslTree.NodeFunctionPrototype nodeFuncProto)
                            {
                                for (int i = 0; i < nodeExpression.paramIds.Length; i++)
                                {
                                    HlslToken modifier;
                                    int declId = GetProtoParamDeclAndModifier(out modifier, nodeExpression.lhsNodeId, i);

                                    bool isIn = (modifier == HlslToken._invalid || modifier == HlslToken._in || modifier == HlslToken._inout);
                                    bool isOut = (modifier == HlslToken._out || modifier == HlslToken._inout);

                                    int argId = nodeExpression.paramIds[i];
                                    if (isIn)
                                    {
                                        AddApdDependency(allNodeApdDeps, declId, argId);
                                    }

                                    if (isOut)
                                    {
                                        // for out, the types need to be exactly the same so that the output
                                        // always has a legal l-value. For example, suppose we have the following:
                                        //
                                        // void SomeFunc(in float A, out float B)
                                        // { /*stuff*/ }
                                        //
                                        //  SumFunc(srcVal,val0);
                                        //     /* val0 used as apd */
                                        //  SumFunc(srcVal,val1);
                                        //     /* val1 only used as fpd */
                                        //
                                        // In this case, the parameter must be APD, (since val0 needs APD). Val1
                                        // doesn't need to be APD for it's value, but it does need to be APD so that
                                        // it can be a valid l-value in the function call. The easiest solution is
                                        // make sure that all out args are dependent both ways with the parameter,
                                        // ensuring that both are always identical.
                                        AddApdDependency(allNodeApdDeps, declId, argId);
                                        AddApdDependency(allNodeApdDeps, argId, declId);
                                    }
                                }
                            }
                            else if (baseProtoNode is HlslTree.NodeFunctionPrototypeExternal nodeFuncProtoExternal)
                            {

                                Dictionary<string, int> textureSinkList = new Dictionary<string, int>();

                                for (int paramIter = 0; paramIter < nodeFuncProtoExternal.protoInfo.paramInfoVec.Length; paramIter++)
                                {
                                    HlslParser.TypeInfo typeInfo = nodeFuncProtoExternal.protoInfo.paramInfoVec[paramIter];
                                    if (typeInfo.allowedState == ApdAllowedState.OnlyApd)
                                    {
                                        //int sinkIndex = textureSinkList[nodeFuncProtoExternal.protoInfo.identifier];
                                        int sinkParamId = nodeExpression.paramIds[paramIter];

                                        AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, sinkParamId);
                                    }
                                }

                                // also, link up all the AllowApdVariation params and return types
                                List<int> allApdVariationIds = new List<int>();

                                if (nodeFuncProtoExternal.protoInfo.returnType.allowedState == ApdAllowedState.AllowApdVariation)
                                {
                                    allApdVariationIds.Add(nodeExpression.lhsNodeId);
                                }

                                for (int paramIter = 0; paramIter < nodeFuncProtoExternal.protoInfo.paramInfoVec.Length; paramIter++)
                                {
                                    HlslParser.TypeInfo typeInfo = nodeFuncProtoExternal.protoInfo.paramInfoVec[paramIter];
                                    if (typeInfo.allowedState == ApdAllowedState.AllowApdVariation)
                                    {
                                        int paramId = nodeExpression.paramIds[paramIter];
                                        allApdVariationIds.Add(paramId);
                                    }
                                }

                                if (allApdVariationIds.Count >= 2)
                                {
                                    for (int lhs = 0; lhs < allApdVariationIds.Count; lhs++)
                                    {
                                        for (int rhs = 0; rhs < allApdVariationIds.Count; rhs++)
                                        {
                                            if (lhs != rhs)
                                            {
                                                AddApdDependency(allNodeApdDeps, allApdVariationIds[lhs], allApdVariationIds[rhs]);
                                            }
                                        }
                                    }
                                }

                            }
                        }

                        break;

                    // partially supported
                    case HlslOp.Subscript:          // []
                        {
                            AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.lhsNodeId);
                            AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, nodeId);
                        }
                        break;

                    // affect the struct
                    case HlslOp.MemberAccess:       // .
                        {
                            HlslTree.Node nodeRhs = tree.allNodes[nodeExpression.rhsNodeId];

                            if (nodeRhs is HlslTree.NodeMemberVariable nodeMemberVariable)
                            {
                                // we will use what the rhs child returns (lhs is struct, rhs is member)
                                AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.rhsNodeId);
                                AddApdDependency(allNodeApdDeps, nodeExpression.rhsNodeId, nodeId);
                            }
                            else if (nodeRhs is HlslTree.NodeMemberFunction nodeMemberFunction)
                            {
                                // also uses rhs, since lhs is struct, and rhs is member
                                AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.rhsNodeId);
                            }
                            else if (nodeRhs is HlslTree.NodeSwizzle nodeSwizzle)
                            {
                                // swizzle, use the lhs (which is the main type) since rhs simply describes the swizzle
                                // of tthat type, but the type is unchanged
                                AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.lhsNodeId);
                                AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, nodeId);
                            }
                            else
                            {
                                // should never happen, since these are the only 3 nodes allowed
                                HlslUtil.ParserAssert(false);
                            }
                        }
                        break;

                    // these are not valid with floats/halfs so no apd dependency
                    case HlslOp.LogicalNot:         // !
                    case HlslOp.BitwiseNot:         // ~
                    case HlslOp.ShiftL:             // <<
                    case HlslOp.ShiftR:             // >>
                    case HlslOp.LessThan:           // <
                    case HlslOp.GreaterThan:        // >
                    case HlslOp.LessEqual:          // <=
                    case HlslOp.GreaterEqual:       // >=
                    case HlslOp.CompareEqual:       // ==
                    case HlslOp.NotEqual:           // !=
                    case HlslOp.BitwiseAnd:         // &
                    case HlslOp.BitwiseXor:         // ^
                    case HlslOp.BitwiseOr:          // |
                    case HlslOp.LogicalAnd:         // &&
                    case HlslOp.LogicalOr:          // ||
                    case HlslOp.ShiftLEquals:       // <<=
                    case HlslOp.ShiftREquals:       // >>=
                    case HlslOp.AndEquals:          // &=
                    case HlslOp.XorEquals:          // ^=
                    case HlslOp.OrEquals:           // |=
                    case HlslOp.Mod:                // %
                    case HlslOp.ModEquals:          // %=
                        break;

                    case HlslOp.CStyleCast:         // (int)
                        // in a regular Cstyle case, the lhs depend on the rhs since it's essentially an assignment
                        AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, nodeExpression.rhsNodeId);
                        break;

                    case HlslOp.Mul:                // *
                    case HlslOp.Div:                // /
                    case HlslOp.Add:                // +
                    case HlslOp.Sub:                // -
                        // for the 4 basic math ops, the status depends on both
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.lhsNodeId);
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.rhsNodeId);
                        break;

                    case HlslOp.TernaryQuestion:    // ?
                        // for the statement:
                        //    value = A ? B : C;
                        // the APD status of the expresssion (A ? B : C) depends only on B and C.
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.rhsNodeId);
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.rhsRhsNodeId);
                        break;

                    // should never happen, since the TernaryQuestion stores all three expression ids
                    case HlslOp.TernaryColon:       // :
                        HlslUtil.ParserAssert(false);
                        break;

                    case HlslOp.Assignment:         // =
                        // for assignment, we simply take the apd status of the right and return it.
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.lhsNodeId);
                        AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, nodeId);
                        AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, nodeExpression.rhsNodeId);
                        break;
                    case HlslOp.AddEquals:         // +=
                    case HlslOp.SubEquals:          // -=
                    case HlslOp.MulEquals:          // *=
                    case HlslOp.DivEquals:          // /=
                    case HlslOp.Comma:              // ,
                        // while assignment should lose the lhs status and completely replace it with
                        // rhs, whereas for +=, -=, etc we should merge the status. However, we don't have a concise
                        // way to describe that relationship with current data structures.
                        AddApdDependency(allNodeApdDeps, nodeId, nodeExpression.lhsNodeId);
                        AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, nodeId);
                        AddApdDependency(allNodeApdDeps, nodeExpression.lhsNodeId, nodeExpression.rhsNodeId);
                        break;
                    default:
                        HlslUtil.ParserAssert(false);
                        break;
                }
            }
            else if (baseNode is HlslTree.NodeMemberVariable nodeMemberVariable)
            {
                // A member variable depend on the declaration and the declaration depends
                // on the member variable. That's because it's the union of all types.
                HlslTree.Node baseStructNode = tree.allNodes[nodeMemberVariable.structNodeId];

                if (baseStructNode is HlslTree.NodeUnityStruct unityStruct)
                {
                    // these are locked as non-apd, so no dependency to add
                }
                else if (baseStructNode is HlslTree.NodeStruct srcStructNode)
                {
                    int declId = srcStructNode.declarations[nodeMemberVariable.fieldIndex];
                    AddApdDependency(allNodeApdDeps, nodeId, declId);
                    AddApdDependency(allNodeApdDeps, declId, nodeId);
                    // also, nothing to recurse
                }
                else
                {
                    // should never happen
                    HlslUtil.ParserAssert(false);
                }
            }
            else if (baseNode is HlslTree.NodeParenthesisGroup nodeParenthesisGroup)
            {
                // both depeend on the other since they are essentially the same
                //int parentId = nodeParents[nodeId];
                AddApdDependency(allNodeApdDeps, nodeParenthesisGroup.childNodeId, nodeId);
                AddApdDependency(allNodeApdDeps, nodeId, nodeParenthesisGroup.childNodeId);

                // recurse of course
                CalculateApdDependenciesRecurse(allNodeApdDeps, nodeParenthesisGroup.childNodeId);
            }
            else if (baseNode is HlslTree.NodeMemberFunction nodeMemberFunction)
            {
                // For now, we aren't processing custom member functions in structs, but if we decide to
                // support that we would have to tweak the function, and then tweak all the return values.
                // For a proper solution:
                //    1. The return type depends on the function return type.
                //    2. Each func argument depends on the function parameter.
                //    3. For out parameters, we also have the reverse where each parameter depends on the argument.

                // one case that we might have to work with (sooner than later) is tex.Sample(sampler,uv);
                for (int i = 0; i < nodeMemberFunction.funcParamNodeIds.Length; i++)
                {
                    int childId = nodeMemberFunction.funcParamNodeIds[i];

                    AddApdDependency(allNodeApdDeps, nodeId, childId);
                    CalculateApdDependenciesRecurse(allNodeApdDeps, childId);
                }

            }
            else if (baseNode is HlslTree.NodeSwizzle nodeSwizzle)
            {
                // the swizzle doesn't actually have a type. In the parent expression,
                // such as:
                //    something.rgb;
                // something will be lhs of the parent, and the .rgb swizzle will be this
                // node in the rhs. The type is defined by the lhs so the .rgb node doesn't
                // actually have an apd type since the parent will just take the lhs.
            }
            else if (baseNode is HlslTree.NodeBlockInitializer nodeBlockInit)
            {
                // no dependencies, as everything inside a block initializer is forbidden from
                // being apd
            }
            else
            {
                // if we got here, then we are missing a type in this if tree
                HlslUtil.ParserAssert(false);
            }
        }

        void CalculateApdDependencies()
        {
            int numNodes = tree.allNodes.Count;
            List<int>[] allNodeApdDeps = new List<int>[numNodes];
            for (int i = 0; i < numNodes; i++)
            {
                allNodeApdDeps[i] = new List<int>();
            }

            HlslUtil.ParserAssert(tree.topLevelNode >= 0);
            CalculateApdDependenciesRecurse(allNodeApdDeps, tree.topLevelNode);

            apdInfo.nodeApdDependencies = new int[numNodes][];
            for (int i = 0; i < numNodes; i++)
            {
                apdInfo.nodeApdDependencies[i] = allNodeApdDeps[i].ToArray();
            }

            // also, create reverse dependencies
            {
                List<int>[] allNodeApdDepsRev = new List<int>[numNodes];

                for (int i = 0; i < numNodes; i++)
                {
                    allNodeApdDepsRev[i] = new List<int>();
                }

                for (int i = 0; i < numNodes; i++)
                {
                    for (int j = 0; j < apdInfo.nodeApdDependencies[i].Length; j++)
                    {
                        int lhs = apdInfo.nodeApdDependencies[i][j];
                        int rhs = i;

                        HlslUtil.ParserAssert(!allNodeApdDepsRev[lhs].Contains(rhs));

                        allNodeApdDepsRev[lhs].Add(rhs);
                    }
                }

                apdInfo.nodeApdDependenciesRev = new int[numNodes][];
                for (int i = 0; i < numNodes; i++)
                {
                    apdInfo.nodeApdDependenciesRev[i] = allNodeApdDepsRev[i].ToArray();
                }
            }
        }

        bool IsDeclPossibleInterpolatorApdInput(HlslApdInfo apdInfo, int nodeId)
        {
            bool isApdInput = false;

            HlslTree.Node baseNode = tree.allNodes[nodeId];
            if (baseNode is HlslTree.NodeDeclaration nodeDeclaration)
            {
                // is it a global variable? to find out, check if the parent is a top level node.
                int parentId = nodeParents[nodeId];

                HlslUtil.ParserAssert(parentId >= 0);

                HlslTree.Node parentNode = tree.allNodes[parentId];
                if (parentNode is HlslTree.NodeStruct parentStruct)
                {
                    string parentName = tokenizer.GetTokenData(parentStruct.nameTokenId);
                    string childName = tokenizer.GetTokenData(nodeDeclaration.nameTokenId);

                    // hardcode the names for now
                    if (string.Compare(parentName, "SurfaceDescriptionInputs") == 0)
                    {
                        if (string.Compare(childName, "uv0") == 0 ||
                            string.Compare(childName, "uv1") == 0 ||
                            string.Compare(childName, "uv2") == 0 ||
                            string.Compare(childName, "uv3") == 0)
                        {
                            isApdInput = true;
                        }
                    }
                }
            }

            return isApdInput;
        }

        void MarkInitialApdStatusForNode(ApdStatus[] initialStatus, int nodeId)
        {
            HlslTree.Node baseNode = tree.allNodes[nodeId];

            ApdStatus dstStatus = ApdStatus.Unknown;

            // if a node does not have a parent, it is an orphaned node (unless it's a top level node, which
            // is marked invalid anyways
            int parentId = nodeParents[nodeId];
            if (parentId >= 0)
            {
                if (baseNode is HlslTree.NodeTopLevel nodeTopLevel)
                {
                    // top level nods have no derivatives
                    dstStatus = ApdStatus.Invalid;
                }
                else if (baseNode is HlslTree.NodeStruct nodeStruct)
                {
                    // struct types do not have derivatives
                    dstStatus = ApdStatus.Invalid;
                }
                else if (baseNode is HlslTree.NodeUnityStruct nodeUnityStruct)
                {
                    // unity struct types do not have derivatives
                    dstStatus = ApdStatus.Invalid;
                }
                else if (baseNode is HlslTree.NodeFunctionPrototype nodeFunctionPrototype)
                {
                    // not sure, this will depend
                }
                else if (baseNode is HlslTree.NodeFunctionPrototypeExternal nodeFunctionPrototypeExternal)
                {
                    // will depend on inputs
                }
                else if (baseNode is HlslTree.NodeBlock nodeBlock)
                {
                    // depends on inputs
                }
                else if (baseNode is HlslTree.NodeStatement nodeStatement)
                {
                    // depends on parsing expression
                }
                else if (baseNode is HlslTree.NodeFunction nodeFunction)
                {
                    // depends on parsing
                }
                else if (baseNode is HlslTree.NodeDeclaration nodeDeclaration)
                {
                    // a few cases:
                    //    1. If it's a toplevel node and the native type is apd-able, then set to zero.
                    //    2. If it's not apd-able, then obviously invalid.
                    //    3. It could be an interpolator, making it known.
                    //    4. Otherwise, it's unknown.
                    HlslNativeType nativeType = tree.fullTypeInfo[nodeId].nativeType;
                    bool isApdLegal = HlslUtil.IsNativeTypeLegalForApd(nativeType);
                    if (!isApdLegal)
                    {
                        dstStatus = ApdStatus.Invalid;
                    }
                    else
                    {
                        bool isApdInterpolator = IsDeclPossibleInterpolatorApdInput(apdInfo, nodeId);
                        if (isApdInterpolator)
                        {
                            dstStatus = ApdStatus.Valid;
                        }
                        else
                        {
                            HlslTree.Node parentNode = tree.allNodes[parentId];
                            if (parentNode is HlslTree.NodeTopLevel)
                            {
                                dstStatus = ApdStatus.Zero;
                            }
                            else
                            {
                                dstStatus = ApdStatus.Unknown;
                            }
                        }
                    }
                }
                else if (baseNode is HlslTree.NodeVariable nodeVariable)
                {
                    // will depend on parsing, unknown for now.
                }
                else if (baseNode is HlslTree.NodeNativeConstructor nodeNativeConstructor)
                {
                    // will depend on parsing
                }
                else if (baseNode is HlslTree.NodeToken nodeToken)
                {
                    // unknown
                }
                else if (baseNode is HlslTree.NodeNativeType nodeNativeType)
                {
                    // nothing to change
                }
                else if (baseNode is HlslTree.NodeLiteralOrBool nodeLiteralOrBool)
                {
                    // never allowed to change, as a constant is a constant
                    HlslNativeType nativeType = tree.fullTypeInfo[nodeId].nativeType;
                    bool isApdLegal = HlslUtil.IsNativeTypeLegalForApd(nativeType);
                    dstStatus = isApdLegal ? ApdStatus.Zero : ApdStatus.Invalid;
                }
                else if (baseNode is HlslTree.NodePassthrough nodePassthrough)
                {
                    // passthrough can't be changed
                    dstStatus = ApdStatus.Invalid;
                }
                else if (baseNode is HlslTree.NodeExpression nodeExpression)
                {
                    // definitely needs to be parsed, unknown
                }
                else if (baseNode is HlslTree.NodeMemberVariable nodeMemberVariable)
                {
                    // in most cases we don't know yet, unless it's a known interpolator
                    HlslNativeType nativeType = tree.fullTypeInfo[nodeId].nativeType;
                    bool isApdLegal = HlslUtil.IsNativeTypeLegalForApd(nativeType);
                    dstStatus = isApdLegal ? ApdStatus.Unknown : ApdStatus.Invalid;
                }
                else if (baseNode is HlslTree.NodeParenthesisGroup nodeParenthesisGroup)
                {
                    // let it parse
                }
                else if (baseNode is HlslTree.NodeMemberFunction nodeMemberFunction)
                {
                    // also should be parsed
                }
                else if (baseNode is HlslTree.NodeSwizzle nodeSwizzle)
                {
                    // maybe make this invalid?
                }
                else if (baseNode is HlslTree.NodeBlockInitializer nodeBlockInit)
                {
                    // always invalid
                    dstStatus = ApdStatus.Invalid;
                }
                else
                {
                    // if we got here, then we are missing a type in this if tree
                    HlslUtil.ParserAssert(false);
                }
            }

            initialStatus[nodeId] = dstStatus;
        }

        void UpdateNodesOnSinkPathRecurse(bool[] nodesOnSinkPath, int nodeId)
        {
            if (nodeId < 0)
            {
                // no op
            }
            else if (nodesOnSinkPath[nodeId])
            {
                // also a no op, if this was already marked
            }
            else
            {
                nodesOnSinkPath[nodeId] = true;

                int[] nodeDeps = apdInfo.nodeApdDependencies[nodeId];

                for (int depIter = 0; depIter < nodeDeps.Length; depIter++)
                {
                    int depId = nodeDeps[depIter];
                    UpdateNodesOnSinkPathRecurse(nodesOnSinkPath, depId);
                }
            }
        }

        void UpdateApdNodeStatusRecurse(int nodeId)
        {
            if (nodeId < 0)
            {
                // no op
            }
            else
            {

                HlslTree.FullTypeInfo nodeTypeInfo = tree.fullTypeInfo[nodeId];
                bool isApdLegal = HlslUtil.IsNativeTypeLegalForApd(nodeTypeInfo.nativeType);

                ApdStatus startStatus = apdInfo.nodeApdStatus[nodeId];

                // if the initial status is unknown, there isn't much to propogate
                if (startStatus != ApdStatus.Unknown && isApdLegal)
                {
                    // nodeApdDependencies is which nodes this current node depends on.
                    // nodeApdDependenciesRev is the reverse, i.e. which nodes depend on this one.
                    int[] dependentNodes = apdInfo.nodeApdDependenciesRev[nodeId];

                    for (int baseDepIter = 0; baseDepIter < dependentNodes.Length; baseDepIter++)
                    {
                        int childId = dependentNodes[baseDepIter];
                        ApdStatus childStartStatus = apdInfo.nodeApdStatus[childId];

                        ApdStatus childDstStatus = childStartStatus;
                        int[] childDependencies = apdInfo.nodeApdDependencies[childId];
                        for (int childDepIter = 0; childDepIter < childDependencies.Length; childDepIter++)
                        {
                            int childDepId = childDependencies[childDepIter];
                            ApdStatus depStatus = apdInfo.nodeApdStatus[childDepId];

                            childDstStatus = MergeOpApdStatus(depStatus, childDstStatus);
                        }

                        if (childDstStatus != childStartStatus)
                        {
                            apdInfo.nodeApdStatus[childId] = childDstStatus;
                            UpdateApdNodeStatusRecurse(childId);
                        }

                    }

                }

            }

        }

        static string ApdStatusAsOneCharacter(ApdStatus status)
        {
            string ret = "";
            switch (status)
            {
                case ApdStatus.Unknown:
                    ret = "u";
                    break;
                case ApdStatus.Zero:
                    ret = "z";
                    break;
                case ApdStatus.NotNeeded:
                    ret = "n";
                    break;
                case ApdStatus.Valid:
                    ret = "V";
                    break;
                case ApdStatus.Invalid:
                    ret = "i";
                    break;
            }
            return ret;
        }

        void MarkApdNodesAndVariables()
        {
            int numNodes = tree.allNodes.Count;
            int numVariables = tree.allVariables.Count;


            apdInfo.debugNodeNames = new string[numNodes];
            for (int nodeIter = 0; nodeIter < numNodes; nodeIter++)
            {
                apdInfo.debugNodeNames[nodeIter] = tree.allNodes[nodeIter].GetShortName();
            }

            apdInfo.debugVariableNames = new string[numVariables];
            for (int variableIter = 0; variableIter < numVariables; variableIter++)
            {
                apdInfo.debugVariableNames[variableIter] = tree.allVariables[variableIter].variableName;
            }

            // first, is each variable and node allowed to be APD, and is each node
            // allowed to be ADP
            MarkApdNodesAndVariablesIsLegal(out apdInfo.isNodeApdLegalVec, out apdInfo.isVariableApdLegalVec);

            apdInfo.isNodeApdSink = new bool[numNodes];
            MarkApdSinks(out apdInfo.isNodeApdSink);

            {
                List<int>[] variableNodeList = new List<int>[numVariables];
                for (int i = 0; i < numVariables; i++)
                {
                    variableNodeList[i] = new List<int>();
                }

                for (int nodeIter = 0; nodeIter < numNodes; nodeIter++)
                {
                    HlslTree.Node childNode = tree.allNodes[nodeIter];

                    if (childNode is HlslTree.NodeVariable childNodeVariable)
                    {
                        if (childNodeVariable.nameVariableId >= 0)
                        {
                            variableNodeList[childNodeVariable.nameVariableId].Add(nodeIter);
                        }
                    }
                }

                apdInfo.variableNodeVec = new int[numVariables][];
                for (int i = 0; i < numVariables; i++)
                {
                    apdInfo.variableNodeVec[i] = variableNodeList[i].ToArray();
                }
            }

            ApdStatus[] initialStatus = new ApdStatus[numNodes];

            // start with the nodes that we know, and mark what we know
            {
                System.Array.Fill(initialStatus, ApdStatus.Unknown);

                for (int i = 0; i < numNodes; i++)
                {
                    MarkInitialApdStatusForNode(initialStatus, i);
                }

                apdInfo.nodeApdStatus = new ApdStatus[numNodes];
                System.Array.Copy(initialStatus, apdInfo.nodeApdStatus, numNodes);
            }

            bool[] nodesOnSinkPath = new bool[numNodes];

            ApdStatus[] preTrimmedPath;

            // propagate
            {
                List<int> knownNodes = new List<int>();
                for (int i = 0; i < numNodes; i++)
                {
                    if (apdInfo.nodeApdStatus[i] != ApdStatus.Unknown)
                    {
                        knownNodes.Add(i);
                    }
                }

                for (int i = 0; i < knownNodes.Count; i++)
                {
                    int nodeId = knownNodes[i];
                    UpdateApdNodeStatusRecurse(nodeId);
                }

                preTrimmedPath = new ApdStatus[numNodes];
                System.Array.Copy(apdInfo.nodeApdStatus, preTrimmedPath, numNodes);

                // initialized to false implicitly

                for (int i = 0; i < numNodes; i++)
                {
                    if (apdInfo.isNodeApdSink[i])
                    {
                        UpdateNodesOnSinkPathRecurse(nodesOnSinkPath, i);
                    }
                }

                for (int i = 0; i < numNodes; i++)
                {
                    if (!nodesOnSinkPath[i])
                    {
                        apdInfo.nodeApdStatus[i] = ApdStatus.Invalid;
                    }

                    HlslTree.FullTypeInfo currTypeInfo = tree.fullTypeInfo[i];
                    bool isApdLegal = HlslUtil.IsNativeTypeLegalForApd(currTypeInfo.nativeType);
                    if (!isApdLegal)
                    {
                        apdInfo.nodeApdStatus[i] = ApdStatus.Invalid;
                    }
                }

            }

            debugNodeLines = new string[numNodes];
            for (int i = 0; i < numNodes; i++)
            {
                string debugName = apdInfo.debugNodeNames[i];
                if (debugName.Length >= 30)
                {
                    debugName = debugName.Substring(0, 30);
                }

                HlslTree.FullTypeInfo fullType = tree.fullTypeInfo[i];
                string typeName = fullType.GetTypeString();

                if (typeName.Length >= 15)
                {
                    typeName = typeName.Substring(0, 15);
                }

                string fullLine = "";
                fullLine += string.Format("{0,5}:", i);
                fullLine += string.Format(" {0,-30}", debugName);
                fullLine += string.Format(" {0,-15}", typeName);

                fullLine += string.Format(" {0}", ApdStatusAsOneCharacter(initialStatus[i]));

                fullLine += string.Format(" {0}", ApdStatusAsOneCharacter(preTrimmedPath[i]));

                fullLine += string.Format(" {0}", ApdStatusAsOneCharacter(apdInfo.nodeApdStatus[i]));

                fullLine += string.Format(" {0}", apdInfo.isNodeApdSink[i] ? "T" : " "); // very few sinks to make the false version empty for readability

                fullLine += string.Format(" {0}", nodesOnSinkPath[i] ? "T" : " ");

                string strDeps = "";
                string strDepsRev = "";
                {
                    int[] deps = apdInfo.nodeApdDependencies[i];

                    for (int depIter = 0; depIter < deps.Length; depIter++)
                    {
                        if (depIter != 0)
                        {
                            strDeps += ", ";
                        }
                        strDeps += deps[depIter].ToString();
                    }

                    int[] depsRev = apdInfo.nodeApdDependenciesRev[i];
                    for (int depIter = 0; depIter < depsRev.Length; depIter++)
                    {
                        if (depIter != 0)
                        {
                            strDepsRev += ", ";
                        }
                        strDepsRev += depsRev[depIter].ToString();
                    }
                }

                fullLine += string.Format(" [{0,-40}]", strDeps);
                fullLine += string.Format(" [{0,-40}]", strDepsRev);

                debugNodeLines[i] = fullLine;
            }

        }

        void ResolveTypeInfoRecurse(int nodeId, int parentNodeId)
        {
            // Silently succeed for invalid nodes. Many nodes can have invalid children and
            // this is easier than having a ton of if statements.
            if (nodeId < 0)
            {
                return;
            }

            // while recursing, store the parent for each child
            nodeParents[nodeId] = parentNodeId;

            // and for the parent, add this to the list
            if (parentNodeId >= 0)
            {
            }

            HlslTree.Node baseNode = tree.allNodes[nodeId];
            if (baseNode is HlslTree.NodeTopLevel nodeTopLevel)
            {
                for (int i = 0; i < nodeTopLevel.statements.Length; i++)
                {
                    ResolveTypeInfoRecurse(nodeTopLevel.statements[i], nodeId);
                }
            }
            else if (baseNode is HlslTree.NodeStruct nodeStruct)
            {
                for (int i = 0; i < nodeStruct.declarations.Length; i++)
                {
                    ResolveTypeInfoRecurse(nodeStruct.declarations[i], nodeId);
                }

            }
            else if (baseNode is HlslTree.NodeUnityStruct nodeUnityStruct)
            {
            }
            else if (baseNode is HlslTree.NodeFunctionPrototype nodeFunctionPrototype)
            {
                for (int i = 0; i < nodeFunctionPrototype.declarations.Length; i++)
                {
                    ResolveTypeInfoRecurse(nodeFunctionPrototype.declarations[i], nodeId);
                }

            }
            else if (baseNode is HlslTree.NodeFunctionPrototypeExternal nodeFunctionPrototypeExternal)
            {
            }
            else if (baseNode is HlslTree.NodeBlock nodeBlock)
            {
                for (int i = 0; i < nodeBlock.statements.Length; i++)
                {
                    ResolveTypeInfoRecurse(nodeBlock.statements[i], nodeId);
                }
            }
            else if (baseNode is HlslTree.NodeStatement nodeStatement)
            {
                HlslTree.FullTypeInfo dstType = HlslTree.FullTypeInfo.MakeValidNoType();
                switch (nodeStatement.type)
                {
                    case HlslStatementType.If:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        ResolveTypeInfoRecurse(nodeStatement.childBlockOrStatement, nodeId);
                        ResolveTypeInfoRecurse(nodeStatement.elseBlockOrStatement, nodeId);
                        break;
                    case HlslStatementType.Switch:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        ResolveTypeInfoRecurse(nodeStatement.childBlockOrStatement, nodeId);
                        break;
                    case HlslStatementType.Case:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        break;
                    case HlslStatementType.Break:
                        break;
                    case HlslStatementType.Continue:
                        break;
                    case HlslStatementType.Default:
                        break;
                    case HlslStatementType.Goto:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        break;
                    case HlslStatementType.Label:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        break;
                    case HlslStatementType.For:
                        for (int i = 0; i < 3; i++)
                        {
                            ResolveTypeInfoRecurse(nodeStatement.forExpressions[i], nodeId);
                        }
                        ResolveTypeInfoRecurse(nodeStatement.childBlockOrStatement, nodeId);
                        break;
                    case HlslStatementType.Do:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        ResolveTypeInfoRecurse(nodeStatement.childBlockOrStatement, nodeId);
                        break;
                    case HlslStatementType.While:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        ResolveTypeInfoRecurse(nodeStatement.childBlockOrStatement, nodeId);
                        break;

                    case HlslStatementType.Expression:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        break;
                    case HlslStatementType.Return:
                        ResolveTypeInfoRecurse(nodeStatement.expression, nodeId);
                        break;
                    default:
                        HlslUtil.ParserAssert(false);
                        break;
                }
            }
            else if (baseNode is HlslTree.NodeFunction nodeFunction)
            {
                ResolveTypeInfoRecurse(nodeFunction.prototypeId, nodeId);
                ResolveTypeInfoRecurse(nodeFunction.blockId, nodeId);

            }
            else if (baseNode is HlslTree.NodeDeclaration nodeDeclaration)
            {
                ResolveTypeInfoRecurse(nodeDeclaration.typeNodeId, nodeId);
                ResolveTypeInfoRecurse(nodeDeclaration.subTypeNodeId, nodeId);
                ResolveTypeInfoRecurse(nodeDeclaration.initializerId, nodeId);

            }
            else if (baseNode is HlslTree.NodeVariable nodeVariable)
            {
            }
            else if (baseNode is HlslTree.NodeNativeConstructor nodeNativeConstructor)
            {
                for (int i = 0; i < nodeNativeConstructor.paramNodeIds.Length; i++)
                {
                    int childId = nodeNativeConstructor.paramNodeIds[i];
                    ResolveTypeInfoRecurse(childId, nodeId);
                }

            }
            else if (baseNode is HlslTree.NodeToken nodeToken)
            {
            }
            else if (baseNode is HlslTree.NodeNativeType nodeNativeType)
            {
            }
            else if (baseNode is HlslTree.NodeLiteralOrBool nodeLiteralOrBool)
            {
            }
            else if (baseNode is HlslTree.NodePassthrough nodePassthrough)
            {
            }
            else if (baseNode is HlslTree.NodeExpression nodeExpression)
            {
                ResolveTypeInfoRecurse(nodeExpression.lhsNodeId, nodeId);
                ResolveTypeInfoRecurse(nodeExpression.rhsNodeId, nodeId);
                ResolveTypeInfoRecurse(nodeExpression.cstyleCastStructId, nodeId);

                for (int i = 0; i < nodeExpression.paramIds.Length; i++)
                {
                    ResolveTypeInfoRecurse(nodeExpression.paramIds[i], nodeId);
                }

            }
            else if (baseNode is HlslTree.NodeMemberVariable nodeMemberVariable)
            {
            }
            else if (baseNode is HlslTree.NodeParenthesisGroup nodeParenthesisGroup)
            {
                ResolveTypeInfoRecurse(nodeParenthesisGroup.childNodeId, nodeId);
            }
            else if (baseNode is HlslTree.NodeMemberFunction nodeMemberFunction)
            {
                for (int i = 0; i < nodeMemberFunction.funcParamNodeIds.Length; i++)
                {
                    ResolveTypeInfoRecurse(nodeMemberFunction.funcParamNodeIds[i], nodeId);
                }

            }
            else if (baseNode is HlslTree.NodeSwizzle nodeSwizzle)
            {
            }
            else if (baseNode is HlslTree.NodeBlockInitializer nodeBlockInit)
            {
                for (int i = 0; i < nodeBlockInit.initNodeIds.Length; i++)
                {
                    ResolveTypeInfoRecurse(nodeBlockInit.initNodeIds[i], nodeId);
                }
            }
            else
            {
                // if we got here, then we are missing a type in this if tree
                HlslUtil.ParserAssert(false);
            }

        }

        void ResolveTypeInfoAndParents()
        {
            nodeParents = new int[tree.allNodes.Count];
            System.Array.Fill(nodeParents, -1);

            if (tree.topLevelNode < 0)
            {
                LogError("Missing top level node");
            }
            else
            {
                ResolveTypeInfoRecurse(tree.topLevelNode, -1);

                // now that we know the parent, create a helper list for children
                FindChildrenFromParents();

                apdInfo = new HlslApdInfo();

                CalculateApdDependencies();

                MarkApdNodesAndVariables();

            }

        }

        void AppendString(string data)
        {
            currLine += data;
        }

        void FinishLine()
        {
            string indent = "";
            for (int i = 0; i < indentLevel; i++)
            {
                indent += "    ";
            }

            allLines.Add(indent + currLine);
            currLine = "";
        }

        void IndentPush()
        {
            indentLevel++;
        }

        void IndentPop()
        {
            indentLevel--;
        }

        string MakeImplicitCast(HlslNativeType lhsType, ApdStatus lhsStatus,
                                HlslNativeType srcRhsType, ApdStatus rhsStatus,
                                string rhsText)
        {
            // Special hack. If the rhs is (void) and has status invalid, then we actually don't know what type it is,
            //      so we will assume that it is the type that we are expecting and hope for the best. This can happen
            //      when the code snippet calls a function that is not declared in this code snippet.
            HlslNativeType rhsType = (srcRhsType == HlslNativeType._void) ? lhsType : srcRhsType;

            bool isLhsApd = (lhsStatus == ApdStatus.Valid);
            bool isRhsApd = (rhsStatus == ApdStatus.Valid);

            HlslNativeType lhsNativeBase = HlslUtil.GetNativeBaseType(lhsType);

            HlslNativeType rhsNativeBase = HlslUtil.GetNativeBaseType(rhsType);

            bool isLhsScalar = HlslUtil.IsNativeTypeScalar(lhsType);
            bool isRhsScalar = HlslUtil.IsNativeTypeScalar(rhsType);


            SgType lhsSgType = new SgType();
            lhsSgType = ConvertNativeTypeToSg(lhsType);
            lhsSgType.apdStatus = lhsStatus;

            SgType rhsSgType = new SgType();
            rhsSgType = ConvertNativeTypeToSg(rhsType);
            rhsSgType.apdStatus = rhsStatus;

            // Main rules;
            // 0. If they are exactly the same, then do nothing
            // 1. If either is a struct or non-scalar, then we can't do any casting.
            // 2. If either is APD, then we'll use the writer to handle it
            // 3. Otherwise, we are going to assume the conversion is safe, and only change the base type.

            string ret = "";
            if (lhsType == rhsType && lhsStatus == rhsStatus)
            {
                // exactly the same, so no op
                ret = rhsText;
            }
            else if (!isLhsScalar || !isRhsScalar)
            {
                // nothing we can do
                ret = rhsText;
            }
            else if (isLhsApd || isRhsApd)
            {
                ret = apdWriter.MakeImplicitCast(lhsSgType.slotType, lhsSgType.apdStatus, lhsSgType.precision,
                                                    rhsSgType.slotType, rhsSgType.apdStatus, rhsSgType.precision,
                                                    rhsText);
            }
            else
            {
                // change the base type and hope for the best
                int rhsRows = HlslUtil.GetNumRows(rhsType);
                int rhsCols = HlslUtil.GetNumCols(rhsType);

                HlslNativeType adjRhsType = HlslUtil.GetBaseTypeWithDims(rhsNativeBase, rhsRows, rhsCols);

                ret = "((" + HlslUtil.GetNativeTypeString(adjRhsType) + ")" + rhsText + ")";
            }

            return ret;
        }

        bool SplitSwizzleAssignFromParentIfAny(out int foundNodeId, out string foundSwizzle, int nodeId)
        {
            bool ret = false;
            foundNodeId = -1;
            foundSwizzle = "";

            HlslTree.Node baseNode = tree.allNodes[nodeId];
            if (baseNode is HlslTree.NodeExpression nodeExpression)
            {
                if (nodeExpression.op == HlslOp.MemberAccess)
                {
                    HlslTree.Node rhsNode = tree.allNodes[nodeExpression.rhsNodeId];
                    if (rhsNode is HlslTree.NodeSwizzle nodeSwizzle)
                    {
                        foundSwizzle = nodeSwizzle.swizzle;
                        foundNodeId = nodeExpression.lhsNodeId;
                        ret = true;
                    }
                }
            }
            return ret;
        }

        static int[] GetSwizzleIndices(string swizzle)
        {
            int[] swizzleIndices = new int[4] { 0, 0, 0, 0 };
            HlslUtil.ParserAssert(swizzle.Length <= 4);

            for (int i = 0; i < swizzle.Length; i++)
            {
                swizzleIndices[i] = HlslUtil.GetSwizzleIndex(swizzle[i]);
            }
            return swizzleIndices;
        }


        string GenerateNodeExpression(HlslTree.NodeExpression nodeExpression, bool isCurrApd, int currId)
        {
            string opName = tokenizer.GetTokenData(nodeExpression.opTokenId);

            string retStr = "";

            bool useExtraParens = true;
            bool isPixelShader = true;

            SgType dstSgType = new SgType();
            dstSgType = ConvertNativeTypeToSg(tree.fullTypeInfo[currId].nativeType);
            dstSgType.apdStatus = apdInfo.nodeApdStatus[currId];

            HlslNativeType dstNativeType = tree.fullTypeInfo[currId].nativeType;
            ApdStatus dstApdStatus = apdInfo.nodeApdStatus[currId];
            if (nodeExpression.op == HlslOp.FunctionalCall)
            {
                HlslTree.Node lhsNode = tree.allNodes[nodeExpression.lhsNodeId];

                string[] argText = new string[nodeExpression.paramIds.Length];
                SgType[] argSgType = new SgType[nodeExpression.paramIds.Length];
                HlslNativeType[] argNativeType = new HlslNativeType[nodeExpression.paramIds.Length];

                for (int i = 0; i < nodeExpression.paramIds.Length; i++)
                {
                    int argId = nodeExpression.paramIds[i];

                    argText[i] = GenerateLinesRecurse(argId);
                    argSgType[i] = ConvertNativeTypeToSg(tree.fullTypeInfo[argId].nativeType);
                    argNativeType[i] = tree.fullTypeInfo[argId].nativeType;
                    argSgType[i].apdStatus = apdInfo.nodeApdStatus[argId];
                }

                string funcName = GetFunctionNameFromNodeId(nodeExpression.lhsNodeId);

                if (funcName == "SAMPLE_TEXTURE2D")
                {
                    string currText = apdWriter.MakeTextureSample2d(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                                                    argText[0],
                                                                    argText[1],
                                                                    argSgType[2].slotType, argSgType[2].apdStatus, argSgType[2].precision, argText[2],
                                                                    true,
                                                                    isPixelShader);
                    retStr = currText;
                }
                else if (funcName == "SAMPLE_TEXTURE2D_ARRAY")
                {
                    string currText = apdWriter.MakeTextureSample2dArray(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                                                    argText[0],
                                                                    argText[1],
                                                                    argSgType[2].slotType, argSgType[2].apdStatus, argSgType[2].precision, argText[2],
                                                                    true,
                                                                    isPixelShader);
                    retStr = currText;
                }
                else if (funcName == "SAMPLE_TEXTURECUBE")
                {
                    string currText = apdWriter.MakeTextureSampleCube(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                                                    argText[0],
                                                                    argText[1],
                                                                    argSgType[2].slotType, argSgType[2].apdStatus, argSgType[2].precision, argText[2],
                                                                    true,
                                                                    isPixelShader);
                    retStr = currText;
                }
                else if (funcName == "SAMPLE_TEXTURECUBE_ARRAY")
                {
                    string currText = apdWriter.MakeTextureSampleCubeArray(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                                                    argText[0],
                                                                    argText[1],
                                                                    argSgType[2].slotType, argSgType[2].apdStatus, argSgType[2].precision, argText[2],
                                                                    true,
                                                                    isPixelShader);
                    retStr = currText;
                }
                else if (funcName == "SAMPLE_TEXTURE3D")
                {
                    string currText = apdWriter.MakeTextureSample3d(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                                                    argText[0],
                                                                    argText[1],
                                                                    argSgType[2].slotType, argSgType[2].apdStatus, argSgType[2].precision, argText[2],
                                                                    true,
                                                                    isPixelShader);
                    retStr = currText;
                }
                else if (funcName == "ddx" || funcName == "ddy")
                {
                    string baseStr = "";
                    if (argSgType[0].apdStatus == ApdStatus.Valid)
                    {
                        baseStr = argText[0] + ".m_" + funcName;
                    }
                    else
                    {
                        baseStr = funcName + "(" + argText[0] + ")";
                    }
                    retStr = MakeImplicitCast(dstNativeType, dstApdStatus, argNativeType[0], ApdStatus.Invalid, baseStr);
                }
                else if (singleFuncs.ContainsKey(funcName))
                {
                    SingleFunc func = singleFuncs[funcName];
                    retStr = apdWriter.MakeSingleFunc(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                            argSgType[0].slotType, argSgType[0].apdStatus, argSgType[0].precision, argText[0],
                                            func);
                }
                else if (binaryFuncs.ContainsKey(funcName))
                {
                    BinaryFunc func = binaryFuncs[funcName];
                    retStr = apdWriter.MakeBinaryFunc(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                            argSgType[0].slotType, argSgType[0].apdStatus, argSgType[0].precision, argText[0],
                                            argSgType[1].slotType, argSgType[1].apdStatus, argSgType[1].precision, argText[1],
                                            func);
                }
                else if (func1s.ContainsKey(funcName))
                {
                    Func1 func = func1s[funcName];
                    retStr = apdWriter.MakeFunc1(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                            argSgType[0].slotType, argSgType[0].apdStatus, argSgType[0].precision, argText[0],
                                            func);
                }
                else if (func2s.ContainsKey(funcName))
                {
                    Func2 func = func2s[funcName];
                    retStr = apdWriter.MakeFunc2(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                            argSgType[0].slotType, argSgType[0].apdStatus, argSgType[0].precision, argText[0],
                                            argSgType[1].slotType, argSgType[1].apdStatus, argSgType[1].precision, argText[1],
                                            func);
                }
                else if (func3s.ContainsKey(funcName))
                {
                    Func3 func = func3s[funcName];
                    retStr = apdWriter.MakeFunc3(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                            argSgType[0].slotType, argSgType[0].apdStatus, argSgType[0].precision, argText[0],
                                            argSgType[1].slotType, argSgType[1].apdStatus, argSgType[1].precision, argText[1],
                                            argSgType[2].slotType, argSgType[2].apdStatus, argSgType[2].precision, argText[2],
                                            func);
                }
                else
                {

                    // the lhs of a function call expression should always be the node prototype
                    HlslTree.Node baseProtoNode = tree.allNodes[nodeExpression.lhsNodeId];

                    int numParams = nodeExpression.paramIds.Length;
                    SgType[] paramSgType = new SgType[numParams];
                    HlslNativeType[] paramNativeType = new HlslNativeType[numParams];


                    if (baseProtoNode is HlslTree.NodeFunctionPrototype nodeFuncProto)
                    {
                        Assert.IsTrue(numParams == nodeFuncProto.declarations.Length);
                        for (int i = 0; i < numParams; i++)
                        {
                            int paramId = nodeFuncProto.declarations[i];

                            paramSgType[i] = ConvertNativeTypeToSg(tree.fullTypeInfo[paramId].nativeType);
                            paramNativeType[i] = tree.fullTypeInfo[paramId].nativeType;
                            paramSgType[i].apdStatus = apdInfo.nodeApdStatus[paramId];
                        }
                    }
                    else if (baseProtoNode is HlslTree.NodeFunctionPrototypeExternal nodeFuncProtoExternal)
                    {
                        Assert.IsTrue(nodeFuncProtoExternal.protoInfo.paramInfoVec.Length == numParams);
                        for (int i = 0; i < numParams; i++)
                        {
                            HlslParser.TypeInfo typeInfo = nodeFuncProtoExternal.protoInfo.paramInfoVec[i];

                            paramSgType[i] = ConvertNativeTypeToSg(typeInfo.nativeType);
                            paramNativeType[i] = typeInfo.nativeType;
                            paramSgType[i].apdStatus = ApdStatus.Invalid; // if it's an unknown external type, the apd is invalid
                        }
                    }
                    else if (baseProtoNode is HlslTree.NodeToken nodeToken)
                    {
                        // if it's a token, then we don't know the prototype so just use it as is with apd invalid
                        for (int i = 0; i < numParams; i++)
                        {
                            paramSgType[i] = argSgType[i];
                            paramNativeType[i] = argNativeType[i];

                            // sgtype is a struct so this operation is safe
                            paramSgType[i].apdStatus = ApdStatus.Invalid;
                        }
                    }
                    else
                    {
                        // should never happen
                        HlslUtil.ParserAssert(false);
                    }

                    retStr += (funcName);

                    retStr += ("(");
                    for (int i = 0; i < nodeExpression.paramIds.Length; i++)
                    {
                        if (i >= 1)
                        {
                            retStr += ", ";
                        }

                        string currArg = GenerateLinesRecurse(nodeExpression.paramIds[i]);

                        currArg = apdWriter.MakeImplicitCast(paramSgType[i].slotType, paramSgType[i].apdStatus, paramSgType[i].precision,
                                                                argSgType[i].slotType, argSgType[i].apdStatus, argSgType[i].precision,
                                                                currArg);
                        retStr += currArg;
                    }
                    retStr += ")";
                }
            }
            else
            {

                string lhsText = "";
                string rhsText = "";
                string rhsRhsText = "";

                SgType lhsSgType = new SgType();
                SgType rhsSgType = new SgType();
                SgType rhsRhsSgType = new SgType();

                HlslNativeType lhsNativeType = HlslNativeType._invalid;
                HlslNativeType rhsNativeType = HlslNativeType._invalid;
                HlslNativeType rhsRhsNativeType = HlslNativeType._invalid;

                ApdStatus lhsApdStatus = ApdStatus.Invalid;
                ApdStatus rhsApdStatus = ApdStatus.Invalid;
                ApdStatus rhsRhsApdStatus = ApdStatus.Invalid;

                // Node the tweak so that we don't parse function calls. This is to avoid outputting the prototype again.
                if (nodeExpression.lhsNodeId >= 0)
                {
                    lhsNativeType = tree.fullTypeInfo[nodeExpression.lhsNodeId].nativeType;
                    lhsApdStatus = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];

                    lhsText = GenerateLinesRecurse(nodeExpression.lhsNodeId);
                    lhsSgType = ConvertNativeTypeToSg(tree.fullTypeInfo[nodeExpression.lhsNodeId].nativeType);
                    lhsSgType.apdStatus = apdInfo.nodeApdStatus[nodeExpression.lhsNodeId];
                }
                if (nodeExpression.rhsNodeId >= 0)
                {
                    rhsNativeType = tree.fullTypeInfo[nodeExpression.rhsNodeId].nativeType;
                    rhsApdStatus = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];

                    rhsText = GenerateLinesRecurse(nodeExpression.rhsNodeId);
                    rhsSgType = ConvertNativeTypeToSg(tree.fullTypeInfo[nodeExpression.rhsNodeId].nativeType);
                    rhsSgType.apdStatus = apdInfo.nodeApdStatus[nodeExpression.rhsNodeId];
                }
                if (nodeExpression.rhsRhsNodeId >= 0)
                {
                    rhsRhsNativeType = tree.fullTypeInfo[nodeExpression.rhsRhsNodeId].nativeType;
                    rhsRhsApdStatus = apdInfo.nodeApdStatus[nodeExpression.rhsRhsNodeId];

                    rhsRhsText = GenerateLinesRecurse(nodeExpression.rhsRhsNodeId);
                    rhsRhsSgType = ConvertNativeTypeToSg(tree.fullTypeInfo[nodeExpression.rhsRhsNodeId].nativeType);
                    rhsRhsSgType.apdStatus = apdInfo.nodeApdStatus[nodeExpression.rhsRhsNodeId];
                }

                switch (nodeExpression.op)
                {
                    // the standard path, output lhs, then op, then rhs if valid
                    case HlslOp.Assignment:         // =
                        {
                            bool isLhsApd = (lhsSgType.apdStatus == ApdStatus.Valid);

                            // first assing the rhs to lhs, and then convert from lhs to dst. That's because lhs might be apd
                            // whereas dst might not be.

                            string rhsModified = MakeImplicitCast(lhsNativeType, lhsApdStatus,
                                                                rhsNativeType, rhsApdStatus,
                                                                rhsText);

                            int foundNodeId;
                            string foundSwizzle;
                            bool foundSplit = SplitSwizzleAssignFromParentIfAny(out foundNodeId, out foundSwizzle, nodeExpression.lhsNodeId);

                            string fullExp;
                            if (foundSplit)
                            {
                                string splitLhsText = GenerateLinesRecurse(foundNodeId);


                                SgType foundSgType = ConvertNativeTypeToSg(tree.fullTypeInfo[foundNodeId].nativeType);
                                foundSgType.apdStatus = apdInfo.nodeApdStatus[foundNodeId];


                                bool isSrcApd = (lhsApdStatus == ApdStatus.Valid);
                                bool isDstApd = (isCurrApd);

                                int[] swizzleIndices = GetSwizzleIndices(foundSwizzle);

                                string lhsFromRhs = apdWriter.MakeSwizzleAssignApd(foundSgType.slotType, foundSgType.precision, foundSgType.apdStatus, splitLhsText, rhsModified,
                                        foundSwizzle.Length,
                                        swizzleIndices[0],
                                        swizzleIndices[1],
                                        swizzleIndices[2],
                                        swizzleIndices[3]);

                                fullExp = MakeImplicitCast(dstNativeType, dstApdStatus,
                                                                    lhsNativeType, lhsApdStatus,
                                                                    lhsFromRhs);
                            }
                            else
                            {

                                string lhsFromRhs = lhsText + " = " + rhsModified;

                                fullExp = MakeImplicitCast(dstNativeType, dstApdStatus,
                                                                    lhsNativeType, lhsApdStatus,
                                                                    lhsFromRhs);
                            }

                            retStr = fullExp;
                        }
                        break;

                    case HlslOp.Mul:                // *
                    case HlslOp.Div:                // /
                    case HlslOp.Mod:             // %
                    case HlslOp.Add:                // +
                    case HlslOp.Sub:                // -
                        {
                            if (isCurrApd)
                            {
                                BinaryFunc func = (BinaryFunc)(-1);
                                if (nodeExpression.op == HlslOp.Mul)
                                {
                                    func = BinaryFunc.Mul;
                                }
                                else if (nodeExpression.op == HlslOp.Div)
                                {
                                    func = BinaryFunc.Div;
                                }
                                else if (nodeExpression.op == HlslOp.Add)
                                {
                                    func = BinaryFunc.Add;
                                }
                                else if (nodeExpression.op == HlslOp.Sub)
                                {
                                    func = BinaryFunc.Sub;
                                }
                                else
                                {
                                    HlslUtil.ParserAssert(false);
                                }

                                retStr = apdWriter.MakeBinaryFunc(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                                        lhsSgType.slotType, lhsSgType.apdStatus, lhsSgType.precision, lhsText,
                                                        rhsSgType.slotType, rhsSgType.apdStatus, rhsSgType.precision, rhsText,
                                                        func);
                            }
                            else
                            {
                                retStr += "(";
                                retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                                retStr += opName;
                                retStr += GenerateLinesRecurse(nodeExpression.rhsNodeId);
                                retStr += ")";
                            }

                        }

                        break;

                    case HlslOp.AddEquals:         // +=
                    case HlslOp.SubEquals:          // -=
                    case HlslOp.MulEquals:          // *=
                    case HlslOp.DivEquals:          // /=
                    case HlslOp.ModEquals:    // %=
                    case HlslOp.ShiftLEquals:       // <<=
                    case HlslOp.ShiftREquals:       // >>=
                    case HlslOp.AndEquals:          // &=
                    case HlslOp.XorEquals:          // ^=
                    case HlslOp.OrEquals:           // |=
                        {

                            if (isCurrApd || lhsApdStatus == ApdStatus.Valid)
                            {
                                BinaryFunc func = (BinaryFunc)(-1);
                                if (nodeExpression.op == HlslOp.MulEquals)
                                {
                                    func = BinaryFunc.Mul;
                                }
                                else if (nodeExpression.op == HlslOp.DivEquals)
                                {
                                    func = BinaryFunc.Div;
                                }
                                else if (nodeExpression.op == HlslOp.AddEquals)
                                {
                                    func = BinaryFunc.Add;
                                }
                                else if (nodeExpression.op == HlslOp.SubEquals)
                                {
                                    func = BinaryFunc.Sub;
                                }
                                else
                                {
                                    HlslUtil.ParserAssert(false);
                                }


                                string opStr = apdWriter.MakeBinaryFunc(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                                        lhsSgType.slotType, lhsSgType.apdStatus, lhsSgType.precision, lhsText,
                                                        rhsSgType.slotType, rhsSgType.apdStatus, rhsSgType.precision, rhsText,
                                                        func);


                                int foundNodeId;
                                string foundSwizzle;
                                bool foundSplit = SplitSwizzleAssignFromParentIfAny(out foundNodeId, out foundSwizzle, nodeExpression.lhsNodeId);

                                if (foundSplit)
                                {
                                    string splitLhsText = GenerateLinesRecurse(foundNodeId);

                                    SgType foundSgType = ConvertNativeTypeToSg(tree.fullTypeInfo[foundNodeId].nativeType);
                                    foundSgType.apdStatus = apdInfo.nodeApdStatus[foundNodeId];


                                    bool isSrcApd = (lhsApdStatus == ApdStatus.Valid);
                                    bool isDstApd = (isCurrApd);

                                    int[] swizzleIndices = GetSwizzleIndices(foundSwizzle);
                                    //string lhsFromRhs = "SwizzleAssign(" + splitLhsText + "," + foundSwizzle + "," + rhsModified + ")";
                                    //string lhsFromRhs = apdWriter.MakeSwizzleAssignApd()

                                    string lhsFromRhs = apdWriter.MakeSwizzleAssignApd(foundSgType.slotType, foundSgType.precision, foundSgType.apdStatus, splitLhsText, opStr,
                                            foundSwizzle.Length,
                                            swizzleIndices[0],
                                            swizzleIndices[1],
                                            swizzleIndices[2],
                                            swizzleIndices[3]);



                                    retStr = MakeImplicitCast(dstNativeType, dstApdStatus,
                                                                        lhsNativeType, lhsApdStatus,
                                                                        lhsFromRhs);
                                }
                                else
                                {
                                    // note that we can actually have some failure cases here I believe, such as:
                                    //   A++ += B
                                    // would turn into:
                                    //   A++ = AddApd(A++,B)
                                    // but this approximation should be good enough for now.
                                    retStr = "(" + lhsText + "=" + opStr + ")";
                                }


                            }
                            else
                            {
                                if (useExtraParens)
                                {
                                    retStr += "(";
                                }
                                retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                                retStr += opName;
                                retStr += GenerateLinesRecurse(nodeExpression.rhsNodeId);
                                if (useExtraParens)
                                {
                                    retStr += ")";
                                }
                            }
                        }
                        break;

                    case HlslOp.FunctionalCall:     // func()
                        HlslUtil.ParserAssert(false);
                        {

                        }
                        break;

                    case HlslOp.PostIncrement:      // ++
                    case HlslOp.PostDecrement:      //  --
                        HlslUtil.ParserAssert(!isCurrApd);
                        retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId) + opName;
                        break;


                    case HlslOp.UnaryMinus:         // -
                        if (isCurrApd || lhsApdStatus == ApdStatus.Valid)
                        {
                            retStr = apdWriter.MakeSingleFunc(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                                lhsSgType.slotType, lhsSgType.apdStatus, lhsSgType.precision, lhsText,
                                SingleFunc.Negate);
                        }
                        else
                        {
                            retStr += opName + GenerateLinesRecurse(nodeExpression.lhsNodeId);
                        }
                        break;

                    case HlslOp.UnaryPlus:          // +
                    case HlslOp.LogicalNot:         // !
                    case HlslOp.BitwiseNot:         // ~
                        HlslUtil.ParserAssert(!isCurrApd);
                        retStr += opName + GenerateLinesRecurse(nodeExpression.lhsNodeId);
                        break;

                    case HlslOp.PreIncrement:       // ++
                    case HlslOp.PreDecrement:       // --
                        HlslUtil.ParserAssert(!isCurrApd);
                        retStr += opName + GenerateLinesRecurse(nodeExpression.lhsNodeId);
                        break;
                    case HlslOp.Invalid:            //
                    case HlslOp.FunctionalCast:     // int()
                        break;
                    case HlslOp.Subscript:          // []
                        {
                            bool isLhsApd = (lhsApdStatus == ApdStatus.Valid);

                            // is the lhs type an array (meaning that the subscript is an index into the array)
                            // or is the lhs type a non-array, meaning the index refers to a channel (equivalent
                            // to a swizzle). We are only going to do a special path for the swizzle equivalent.

                            int arrayDim = tree.fullTypeInfo[nodeExpression.lhsNodeId].arrayDims;

                            if (isLhsApd && arrayDim == 0)
                            {
                                // This path is illegal on the left side of an assignment operator. To support it,
                                // we would have to add support similar to swizzle.
                                retStr = apdWriter.ExtractIndexApd(lhsSgType.slotType, lhsText, lhsSgType.precision, rhsText);
                            }
                            else
                            {
                                retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                                retStr += "[";
                                retStr += GenerateLinesRecurse(nodeExpression.rhsNodeId);
                                retStr += "]";
                            }
                        }
                        break;
                    case HlslOp.MemberAccess:       // .
                        {
                            // is this a swizzle
                            HlslTree.Node rhsNode = tree.allNodes[nodeExpression.rhsNodeId];
                            if (rhsNode is HlslTree.NodeSwizzle nodeSwizzle)
                            {
                                bool isSrcApd = (lhsApdStatus == ApdStatus.Valid);
                                bool isDstApd = (isCurrApd);

                                int[] swizzleIndices = GetSwizzleIndices(nodeSwizzle.swizzle);

                                if (isSrcApd)
                                {
                                    if (isDstApd)
                                    {
                                        // both are apd, so use the writer swizzle
                                        retStr = apdWriter.MakeSwizzleApd(lhsSgType.slotType, lhsSgType.precision, lhsText,
                                            nodeSwizzle.swizzle.Length,
                                            swizzleIndices[0],
                                            swizzleIndices[1],
                                            swizzleIndices[2],
                                            swizzleIndices[3]);
                                    }
                                    else
                                    {
                                        // src is apd, but dst is not, so just use .m_val
                                        retStr += lhsText + ".m_val." + nodeSwizzle.swizzle;
                                    }
                                }
                                else
                                {
                                    if (isDstApd)
                                    {
                                        // Dst is apd, but src is not, which should never happen but we'll handle it anways.
                                        // Swizzle and then cast to apd

                                        string baseText = lhsText + "." + nodeSwizzle.swizzle;

                                        // get the base type
                                        HlslNativeType baseType = HlslUtil.GetNativeBaseType(lhsNativeType);
                                        HlslNativeType swizzleType = HlslUtil.GetBaseTypeWithDims(baseType, 1, nodeSwizzle.swizzle.Length);

                                        retStr += MakeImplicitCast(swizzleType, lhsApdStatus, dstNativeType, dstApdStatus, baseText);
                                    }
                                    else
                                    {
                                        // both src and dst are not apd, so the vanilla swizzle is fine
                                        retStr += lhsText + "." + nodeSwizzle.swizzle;
                                    }
                                }
                            }
                            else if (rhsNode is HlslTree.NodeMemberFunction nodeMemberFunc)
                            {
                                HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(nodeMemberFunc.structNodeId, unityReserved);
                                HlslUtil.PrototypeInfo protoInfo = structInfo.prototypes[nodeMemberFunc.funcIndex];

                                if (structInfo.identifier == "UnityTexture2D" && protoInfo.identifier == "GetTransformedUV")
                                {
                                    HlslUtil.ParserAssert(protoInfo.paramInfoVec.Length == 1);
                                    string texText = lhsText;
                                    int uvNodeId = nodeMemberFunc.funcParamNodeIds[0];
                                    string uvText = GenerateLinesRecurse(uvNodeId);

                                    HlslNativeType uvNativeType = tree.fullTypeInfo[uvNodeId].nativeType;
                                    ApdStatus uvApdStatus = apdInfo.nodeApdStatus[uvNodeId];

                                    // the internal is always APD, so coerce the input to a float2 apd and coerce the output from a float2 apd
                                    string uvCoerceText = MakeImplicitCast(HlslNativeType._float2, ApdStatus.Valid,
                                                                           uvNativeType, uvApdStatus,
                                                                           uvText);
                                    string fullFuncCall = "GetTransformedUVApd(" + texText + "," + uvCoerceText + ")";

                                    string dstExpression = MakeImplicitCast(dstNativeType, dstApdStatus,
                                                                           HlslNativeType._float2, ApdStatus.Valid,
                                                                           fullFuncCall);
                                    retStr = dstExpression;
                                }
                                else
                                {
                                    retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                                    retStr += ".";
                                    retStr += GenerateLinesRecurse(nodeExpression.rhsNodeId);
                                }
                            }
                            else if (rhsNode is HlslTree.NodeMemberVariable)
                            {
                                // we have a special exception if we are fetching a uv with known derivatives from SurfaceDescriptionInputs
                                int foundUv = -1;
                                {
                                    HlslTree.FullTypeInfo lhsInfo = tree.fullTypeInfo[nodeExpression.lhsNodeId];
                                    if (lhsInfo.identifier == "SurfaceDescriptionInputs")
                                    {
                                        // check the identifer on rhs
                                        //HlslTree.NodeMemberVariable rhsMemberNode = (HlslTree.NodeMemberVariable)tree.allNodes[nodeExpression.rhsNodeId];
                                        HlslTree.Node rhsBaseNode = tree.allNodes[nodeExpression.rhsNodeId];

                                        if (rhsBaseNode is HlslTree.NodeMemberVariable rhsMemberNode)
                                        {
                                            HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(sdInputsStructId, unityReserved);
                                            HlslUtil.FieldInfo fieldInfo = structInfo.fields[rhsMemberNode.fieldIndex];

                                            UVChannel uvIndex = (UVChannel)GetUvIndex(fieldInfo.identifier);
                                            if (uvIndex >= 0 && uvDerivatives.Contains(uvIndex))
                                            {
                                                foundUv = (int)uvIndex;
                                            }
                                        }
                                    }
                                }

                                if (foundUv >= 0 && isCurrApd)
                                {
                                    retStr = "_unityUv" + foundUv.ToString();
                                }
                                else
                                {
                                    retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                                    retStr += ".";
                                    retStr += GenerateLinesRecurse(nodeExpression.rhsNodeId);
                                }
                            }
                            else
                            {
                                // should be no other legal rhs types
                                HlslUtil.ParserAssert(false);
                            }
                        }
                        break;
                    case HlslOp.CStyleCast:         // (int)
                        {
                            retStr += "(";
                            if (nodeExpression.cstyleCastStructId >= 0)
                            {
                                string nativeTypeName = tokenizer.GetTokenData(nodeExpression.opTokenId);
                                retStr += nativeTypeName;
                            }
                            else
                            {
                                HlslTree.FullTypeInfo structInfo = tree.fullTypeInfo[nodeExpression.cstyleCastStructId];
                                retStr += structInfo.identifier;
                            }
                            retStr += ")";

                            retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                        }
                        break;

                    case HlslOp.TernaryQuestion:    // ?
                        {
                            retStr += "(";
                            retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                            retStr += " ? ";
                            retStr += GenerateLinesRecurse(nodeExpression.rhsNodeId);
                            retStr += " : ";
                            retStr += GenerateLinesRecurse(nodeExpression.rhsRhsNodeId);
                            retStr += ")";
                        }
                        break;


                    case HlslOp.ShiftL:             // <<
                    case HlslOp.ShiftR:             // >>
                    case HlslOp.ThreeWayCompare:    // <=>  - never used this
                    case HlslOp.ScopeReslution:     // ::
                    case HlslOp.LessThan:           // <
                    case HlslOp.GreaterThan:        // >
                    case HlslOp.LessEqual:          // <=
                    case HlslOp.GreaterEqual:       // >=
                    case HlslOp.CompareEqual:       // ==
                    case HlslOp.NotEqual:           // !=
                    case HlslOp.BitwiseAnd:         // &
                    case HlslOp.BitwiseXor:         // ^
                    case HlslOp.BitwiseOr:          // |
                    case HlslOp.LogicalAnd:         // &&
                    case HlslOp.LogicalOr:          // ||
                    case HlslOp.Comma:              // ,
                        {
                            if (useExtraParens)
                            {
                                retStr += "(";
                            }
                            retStr += GenerateLinesRecurse(nodeExpression.lhsNodeId);
                            retStr += opName;
                            retStr += GenerateLinesRecurse(nodeExpression.rhsNodeId);
                            if (useExtraParens)
                            {
                                retStr += ")";
                            }
                        }
                        break;

                    // unsupported
                    case HlslOp.Dereference:        // *    - not legal in HLSL?
                    case HlslOp.AddressOf:          // &    - not legal in HLSL?
                    case HlslOp.Sizeof:             // sizeof
                    case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                    case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                    case HlslOp.TernaryColon:       // :
                        retStr = "<invalid " + opName + ">";
                        break;
                    default:
                        retStr = "<unknown op>";
                        break;
                }

            }

            return retStr;
        }

        struct SgType
        {
            internal bool isValidSg;
            internal ConcretePrecision precision;
            internal ConcreteSlotValueType slotType;
            internal ApdStatus apdStatus;
        }

        static bool IsValidSgType(HlslNativeType nativeType)
        {
            bool ret = false;
            switch (nativeType)
            {
                case HlslNativeType._float:
                case HlslNativeType._float1:
                case HlslNativeType._float2:
                case HlslNativeType._float3:
                case HlslNativeType._float4:
                case HlslNativeType._half:
                case HlslNativeType._half1:
                case HlslNativeType._half2:
                case HlslNativeType._half3:
                case HlslNativeType._half4:
                case HlslNativeType._float2x2:
                case HlslNativeType._half2x2:
                case HlslNativeType._float3x3:
                case HlslNativeType._half3x3:
                case HlslNativeType._float4x4:
                case HlslNativeType._half4x4:

                case HlslNativeType._int:
                case HlslNativeType._int1:
                case HlslNativeType._int2:
                case HlslNativeType._int3:
                case HlslNativeType._int4:
                case HlslNativeType._int2x2:
                case HlslNativeType._int3x3:
                case HlslNativeType._int4x4:

                case HlslNativeType._uint:
                case HlslNativeType._uint1:
                case HlslNativeType._uint2:
                case HlslNativeType._uint3:
                case HlslNativeType._uint4:
                case HlslNativeType._uint2x2:
                case HlslNativeType._uint3x3:
                case HlslNativeType._uint4x4:
                    ret = true;
                    break;

                default:
                    ret = false;
                    break;
            }

            return ret;
        }

        static ConcreteSlotValueType GetSgSlotType(HlslNativeType nativeType)
        {
            ConcreteSlotValueType slotType = (ConcreteSlotValueType)(-1);
            switch (nativeType)
            {
                case HlslNativeType._int:
                case HlslNativeType._int1:
                case HlslNativeType._uint:
                case HlslNativeType._uint1:
                case HlslNativeType._half:
                case HlslNativeType._half1:
                case HlslNativeType._float:
                case HlslNativeType._float1:
                    slotType = ConcreteSlotValueType.Vector1;
                    break;

                case HlslNativeType._int2:
                case HlslNativeType._uint2:
                case HlslNativeType._half2:
                case HlslNativeType._float2:
                    slotType = ConcreteSlotValueType.Vector2;
                    break;

                case HlslNativeType._int3:
                case HlslNativeType._uint3:
                case HlslNativeType._half3:
                case HlslNativeType._float3:
                    slotType = ConcreteSlotValueType.Vector3;
                    break;

                case HlslNativeType._int4:
                case HlslNativeType._uint4:
                case HlslNativeType._half4:
                case HlslNativeType._float4:
                    slotType = ConcreteSlotValueType.Vector4;
                    break;

                case HlslNativeType._int2x2:
                case HlslNativeType._uint2x2:
                case HlslNativeType._half2x2:
                case HlslNativeType._float2x2:
                    slotType = ConcreteSlotValueType.Matrix2;
                    break;

                case HlslNativeType._int3x3:
                case HlslNativeType._uint3x3:
                case HlslNativeType._half3x3:
                case HlslNativeType._float3x3:
                    slotType = ConcreteSlotValueType.Matrix3;
                    break;

                case HlslNativeType._int4x4:
                case HlslNativeType._uint4x4:
                case HlslNativeType._half4x4:
                case HlslNativeType._float4x4:
                    slotType = ConcreteSlotValueType.Matrix4;
                    break;

                default:
                    slotType = ConcreteSlotValueType.Vector1;
                    break;
            }

            return slotType;
        }

        static ConcretePrecision GetSgPrecision(HlslNativeType nativeType)
        {
            ConcretePrecision precision = ConcretePrecision.Single;
            switch (nativeType)
            {
                case HlslNativeType._float:
                case HlslNativeType._float1:
                case HlslNativeType._float2:
                case HlslNativeType._float3:
                case HlslNativeType._float4:
                case HlslNativeType._float2x2:
                case HlslNativeType._float3x3:
                case HlslNativeType._float4x4:
                    precision = ConcretePrecision.Single;
                    break;

                case HlslNativeType._half:
                case HlslNativeType._half1:
                case HlslNativeType._half2:
                case HlslNativeType._half3:
                case HlslNativeType._half4:
                case HlslNativeType._half2x2:
                case HlslNativeType._half3x3:
                case HlslNativeType._half4x4:
                    precision = ConcretePrecision.Half;
                    break;
                // treat ints as floats
                case HlslNativeType._int:
                case HlslNativeType._int1:
                case HlslNativeType._int2:
                case HlslNativeType._int3:
                case HlslNativeType._int4:
                case HlslNativeType._int2x2:
                case HlslNativeType._int3x3:
                case HlslNativeType._int4x4:
                    precision = ConcretePrecision.Single;
                    break;
                // treat uints as floats
                case HlslNativeType._uint:
                case HlslNativeType._uint1:
                case HlslNativeType._uint2:
                case HlslNativeType._uint3:
                case HlslNativeType._uint4:
                case HlslNativeType._uint2x2:
                case HlslNativeType._uint3x3:
                case HlslNativeType._uint4x4:
                    precision = ConcretePrecision.Single;
                    break;

                default:
                    break;
            }

            return precision;
        }

        static SgType ConvertNativeTypeToSg(HlslNativeType nativeType)
        {
            SgType sgType = new SgType();
            sgType.isValidSg = IsValidSgType(nativeType);
            if (sgType.isValidSg)
            {
                sgType.precision = GetSgPrecision(nativeType);
                sgType.slotType = GetSgSlotType(nativeType);
            }
            return sgType;
        }

        bool IsTokenNativeTypeAndApdValid(HlslToken token)
        {
            bool ret = false;
            if (tokenToNativeTable.ContainsKey(token))
            {
                HlslNativeType nativeType = tokenToNativeTable[token];
                ret = HlslUtil.IsNativeTypeLegalForApd(nativeType);
            }
            return ret;
        }

        // isApd refers to if the calling function is expecting an Apd type. For example, an expression node with
        // a mul node decides if the child should be Apd or not, and then the child function must coerce
        // the parameter to match what the parent expects.
        string GenerateLinesRecurse(int nodeId)
        {
            // Silently succeed for invalid nodes. Should fix this later.
            if (nodeId < 0)
            {
                return "<invalid>";
            }

            nodeStack.Add(nodeId);


            HlslTree.Node baseNode = tree.allNodes[nodeId];

            int parentId = nodeParents[nodeId];

            bool isCurrApd = (apdInfo.nodeApdStatus[nodeId] == ApdStatus.Valid);

            bool isParentApd = false;
            if (parentId >= 0)
            {
                isParentApd = (apdInfo.nodeApdStatus[parentId] == ApdStatus.Valid);
            }

            string retStr = "<unknown>";

            HlslTree.FullTypeInfo currTypeInfo = tree.fullTypeInfo[nodeId];

            if (isCurrApd)
            {
                bool isApdLegal = HlslUtil.IsNativeTypeLegalForApd(currTypeInfo.nativeType);
                if (!isApdLegal)
                {
                    HlslUtil.ParserAssert(isApdLegal);
                }
            }

            if (baseNode is HlslTree.NodeTopLevel nodeTopLevel)
            {
                AppendString("<Top level>");
                FinishLine();
                for (int i = 0; i < nodeTopLevel.statements.Length; i++)
                {
                    GenerateLinesRecurse(nodeTopLevel.statements[i]);
                }
                retStr = "";
            }
            else if (baseNode is HlslTree.NodeExpression nodeExpression)
            {
                retStr = GenerateNodeExpression(nodeExpression, isCurrApd, nodeId);
            }
            else if (baseNode is HlslTree.NodeStruct nodeStruct)
            {
                string typeName = tokenizer.GetTokenData(nodeStruct.nameTokenId);

                AppendString("struct " + typeName);
                FinishLine();

                AppendString("{");
                FinishLine();

                {
                    IndentPush();
                    for (int i = 0; i < nodeStruct.declarations.Length; i++)
                    {
                        string currLine = GenerateLinesRecurse(nodeStruct.declarations[i]);
                        //AppendString(";");
                        currLine += ";";
                        AppendString(currLine);
                        FinishLine();
                    }
                    IndentPop();
                }

                AppendString("};");
                FinishLine();
                FinishLine();

                retStr = "";
            }
            else if (baseNode is HlslTree.NodeUnityStruct nodeUnityStruct)
            {
                retStr = "<unity struct>";
            }
            else if (baseNode is HlslTree.NodeFunctionPrototype nodeFunctionPrototype)
            {
                string retName = tokenizer.GetTokenData(nodeFunctionPrototype.returnTokenId);
                string funcName = tokenizer.GetTokenData(nodeFunctionPrototype.nameTokenId);

                AppendString(retName + " " + funcName + "(");

                string fullProto = "";
                for (int i = 0; i < nodeFunctionPrototype.declarations.Length; i++)
                {
                    if (i >= 1)
                    {
                        fullProto += ", ";
                    }
                    fullProto += GenerateLinesRecurse(nodeFunctionPrototype.declarations[i]);
                }

                AppendString(fullProto);
                AppendString(")");

                retStr = "";

            }
            else if (baseNode is HlslTree.NodeFunctionPrototypeExternal nodeFunctionPrototypeExternal)
            {
                // no need to write external protypes, so no op
                retStr = "";
            }
            else if (baseNode is HlslTree.NodeBlock nodeBlock)
            {
                AppendString("{");
                FinishLine();

                IndentPush();

                // if this block is the initial block of SurfaceDescriptionFunction(), then add special UVs variables.
                if (parentId >= 0)
                {
                    HlslTree.Node parentNode = tree.allNodes[parentId];
                    if (parentNode is HlslTree.NodeFunction nodeFunc)
                    {
                        HlslTree.Node node = tree.allNodes[nodeFunc.prototypeId];
                        if (node is HlslTree.NodeFunctionPrototype nodeFuncProto)
                        {
                            string identifier = tokenizer.GetTokenData(nodeFuncProto.nameTokenId);
                            if (identifier == "SurfaceDescriptionFunction")
                            {
                                AppendString("// main surface function, so adding uv derivatives here");
                                FinishLine();
                                for (int uvIter = 0; uvIter < uvDerivatives.Count; uvIter++)
                                {
                                    int uv = (int)uvDerivatives[uvIter];

                                    string attrib = "IN.uv" + uv.ToString();
                                    string attribDdx = "IN.uv" + uv.ToString() + "Ddx";
                                    string attribDdy = "IN.uv" + uv.ToString() + "Ddy";

                                    if (applyEmulatedDerivatives)
                                    {
                                        attribDdx = "ddx(IN.uv" + uv.ToString() + ")";
                                        attribDdy = "ddy(IN.uv" + uv.ToString() + ")";
                                    }

                                    string dstVal = apdWriter.MakeStructFromApdDirect(ConcreteSlotValueType.Vector4, attrib, attribDdx, attribDdy, ConcretePrecision.Single);

                                    AppendString("FloatApd4 _unityUv" + uv.ToString() + " = " + dstVal + ";");
                                    FinishLine();
                                }
                            }
                            else
                            {
                                AppendString("// not main surface function, so no IN.uv derivatives here");
                                FinishLine();
                            }

                        }

                    }
                }


                for (int i = 0; i < nodeBlock.statements.Length; i++)
                {
                    GenerateLinesRecurse(nodeBlock.statements[i]);
                }

                IndentPop();

                AppendString("}");
                FinishLine();
                retStr = "";
            }
            else if (baseNode is HlslTree.NodeStatement nodeStatement)
            {
                switch (nodeStatement.type)
                {
                    case HlslStatementType.If:
                        {
                            string currExp = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString("if (" + currExp + ")");
                            FinishLine();
                            {
                                IndentPush();
                                string childText = GenerateLinesRecurse(nodeStatement.childBlockOrStatement);
                                IndentPop();
                            }

                            if (nodeStatement.elseBlockOrStatement >= 0)
                            {
                                AppendString("else ");
                                IndentPush();
                                string childText = GenerateLinesRecurse(nodeStatement.elseBlockOrStatement);
                                IndentPop();
                            }
                        }
                        break;
                    case HlslStatementType.Switch:
                        {
                            string currExp = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString("switch (" + currExp + ")");
                            FinishLine();
                            GenerateLinesRecurse(nodeStatement.childBlockOrStatement);
                        }
                        break;
                    case HlslStatementType.Case:
                        {
                            string currExp = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString("case " + currExp + ":");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.Break:
                        {
                            AppendString("break;");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.Continue:
                        {
                            AppendString("continue;");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.Default:
                        {
                            AppendString("default:");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.Goto:
                        {
                            string currExp = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString("goto " + currExp + ";");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.Label:
                        {
                            string currExp = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString(currExp + ":");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.For:
                        {
                            string firstLine = "for(";
                            firstLine += GenerateLinesRecurse(nodeStatement.forExpressions[0]);
                            firstLine += ";";
                            firstLine += GenerateLinesRecurse(nodeStatement.forExpressions[1]);
                            firstLine += ";";
                            firstLine += GenerateLinesRecurse(nodeStatement.forExpressions[2]);
                            firstLine += ")";
                            AppendString(firstLine);
                            FinishLine();

                            GenerateLinesRecurse(nodeStatement.childBlockOrStatement);
                        }
                        break;
                    case HlslStatementType.Do:
                        {
                            AppendString("do");
                            FinishLine();

                            GenerateLinesRecurse(nodeStatement.childBlockOrStatement);

                            string currExp = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString("while(" + currExp + ")");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.While:
                        {
                            string currExp = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString("while(" + currExp + ")");
                            FinishLine();

                            GenerateLinesRecurse(nodeStatement.childBlockOrStatement);
                        }
                        break;

                    case HlslStatementType.Expression:
                        {
                            string currLine = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString(currLine + ";");
                            FinishLine();
                        }
                        break;
                    case HlslStatementType.Return:
                        {
                            AppendString("return ");
                            string currLine = GenerateLinesRecurse(nodeStatement.expression);
                            AppendString(currLine + ";");
                            FinishLine();
                        }
                        break;
                    default:
                        HlslUtil.ParserAssert(false);
                        break;
                }

                retStr = "";
            }
            else if (baseNode is HlslTree.NodeFunction nodeFunction)
            {
                if (nodeFunction.parseErr.Length > 0)
                {
                    AppendString("// Parse error, resorting to direct text: ");
                    FinishLine();
                    AppendString("// " + nodeFunction.parseErr);
                    FinishLine();
                }

                GenerateLinesRecurse(nodeFunction.prototypeId);
                FinishLine();
                GenerateLinesRecurse(nodeFunction.blockId);
                FinishLine();
            }
            else if (baseNode is HlslTree.NodeDeclaration nodeDeclaration)
            {
                string currStr = "";
                currStr = "";
                bool isDeclApd = (apdInfo.nodeApdStatus[nodeId] == ApdStatus.Valid);

                if (nodeDeclaration.isMacroDecl)
                {
                    string tokenName = tokenizer.GetTokenData(nodeDeclaration.nameTokenId);
                    currStr += (nodeDeclaration.macroDeclString + "(" + tokenName + ")");
                }
                else
                {
                    bool isLegalForApd = HlslUtil.IsNativeTypeLegalForApd(currTypeInfo.nativeType);


                    if (nodeDeclaration.modifierTokenId >= 0)
                    {
                        string modifierName = tokenizer.GetTokenData(nodeDeclaration.modifierTokenId);

                        currStr += (modifierName);
                        currStr += (" ");
                    }

                    string typeName = currTypeInfo.GetTypeString();

                    if (isLegalForApd && isCurrApd)
                    {
                        typeName = HlslUtil.GetNativeTypeStringApd(currTypeInfo.nativeType);
                    }

                    currStr += (typeName);
                    currStr += (" ");

                    string tokenName = tokenizer.GetTokenData(nodeDeclaration.nameTokenId);

                    if (nodeDeclaration.subTypeNodeId >= 0)
                    {
                        currStr += ("<");
                        currStr += (nodeDeclaration.subTypeNodeId);
                        currStr += ("> ");
                    }

                    currStr += (tokenName);

                    for (int i = 0; i < nodeDeclaration.arrayDims.Length; i++)
                    {
                        currStr += "[";
                        currStr += GenerateLinesRecurse(nodeDeclaration.arrayDims[i]);
                        currStr += "]";
                    }

                    if (nodeDeclaration.initializerId >= 0)
                    {
                        currStr += (" = ");

                        string intializerText = GenerateLinesRecurse(nodeDeclaration.initializerId);

                        HlslTree.FullTypeInfo declInfo = tree.fullTypeInfo[nodeId];
                        HlslTree.FullTypeInfo initInfo = tree.fullTypeInfo[nodeDeclaration.initializerId];

                        ApdStatus declStatus = apdInfo.nodeApdStatus[nodeId];
                        ApdStatus initStatus = apdInfo.nodeApdStatus[nodeDeclaration.initializerId];

                        string coerceText = MakeImplicitCast(declInfo.nativeType, declStatus,
                                                               initInfo.nativeType, initStatus,
                                                               intializerText);
                        currStr += coerceText;
                    }
                }

                // parent should always be valid, since the only allowed node without
                // a parent is the top-level node
                HlslTree.Node parentNode = tree.allNodes[parentId];

                if (parentNode is HlslTree.NodeTopLevel || parentNode is HlslTree.NodeBlock)
                {
                    AppendString(currStr + ";");
                    FinishLine();
                    retStr = "";
                }
                else
                {
                    retStr = currStr;
                }
            }
            else if (baseNode is HlslTree.NodeVariable nodeVariable)
            {
                string tokenName = tokenizer.GetTokenData(nodeVariable.nameTokenId);

                if (nodeVariable.nameVariableId >= 0)
                {
                    // if it's defined in this hlsl code, fetch it
                    HlslTree.VariableInfo variableInfo = tree.allVariables[nodeVariable.nameVariableId];
                    ApdStatus declStatus = apdInfo.nodeApdStatus[variableInfo.declId];
                    ApdStatus variableStatus = apdInfo.nodeApdStatus[nodeId];
                    HlslTree.FullTypeInfo fti = tree.fullTypeInfo[nodeId];

                    string coerceText = MakeImplicitCast(fti.nativeType, variableStatus,
                                                         fti.nativeType, declStatus,
                                                         tokenName);

                    retStr = (coerceText);
                }
                else
                {
                    // if it's a global, treat it as is
                    ApdStatus declStatus = ApdStatus.Zero;
                    ApdStatus variableStatus = apdInfo.nodeApdStatus[nodeId];
                    HlslTree.FullTypeInfo fti = tree.fullTypeInfo[nodeId];

                    string coerceText = MakeImplicitCast(fti.nativeType, variableStatus,
                                                         fti.nativeType, declStatus,
                                                         tokenName);

                    retStr = (coerceText);
                }
            }
            else if (baseNode is HlslTree.NodeNativeConstructor nodeNativeConstructor)
            {
                retStr = "";

                ApdStatus dstStatus = apdInfo.nodeApdStatus[nodeId];

                int numCols = HlslUtil.GetNumCols(nodeNativeConstructor.nativeType);

                bool isOneChanPerArg = (numCols == nodeNativeConstructor.paramNodeIds.Length);

                if (dstStatus == ApdStatus.Valid && isOneChanPerArg)
                {
                    // assume that all constructors are only one component per parameter, otherwise force to fpd

                    // the parser rejects and native constructors with mixed types
                    HlslUtil.ParserAssert(numCols == nodeNativeConstructor.paramNodeIds.Length);

                    HlslNativeType dstBaseType = HlslUtil.GetNativeBaseType(nodeNativeConstructor.nativeType);

                    string[] paramTextVec = new string[numCols];
                    SgType[] paramSgType = new SgType[numCols];

                    for (int i = 0; i < numCols; i++)
                    {
                        int argId = nodeNativeConstructor.paramNodeIds[i];
                        string argText = GenerateLinesRecurse(argId);

                        HlslTree.FullTypeInfo argInfo = tree.fullTypeInfo[argId];

                        ApdStatus argStatus = apdInfo.nodeApdStatus[argId];

                        // always convert to apd, might need to convert to float/half
                        string coerceText = MakeImplicitCast(dstBaseType, ApdStatus.Valid,
                                                               argInfo.nativeType, argStatus,
                                                               argText);
                        paramTextVec[i] = coerceText;
                        paramSgType[i] = ConvertNativeTypeToSg(argInfo.nativeType);
                        paramSgType[i].apdStatus = ApdStatus.Valid;
                    }

                    SgType dstSgType = new SgType();
                    dstSgType = ConvertNativeTypeToSg(nodeNativeConstructor.nativeType);
                    dstSgType.apdStatus = dstStatus;

                    // note: args have already been converted to single apd values
                    if (numCols == 1)
                    {
                        retStr = paramTextVec[0];
                    }
                    else if (numCols == 2)
                    {
                        retStr = apdWriter.MakeMerge2(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                            paramSgType[0].slotType, paramSgType[0].apdStatus, paramSgType[0].precision, paramTextVec[0],
                            paramSgType[1].slotType, paramSgType[1].apdStatus, paramSgType[1].precision, paramTextVec[1]);
                    }
                    else if (numCols == 3)
                    {
                        retStr = apdWriter.MakeMerge3(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                            paramSgType[0].slotType, paramSgType[0].apdStatus, paramSgType[0].precision, paramTextVec[0],
                            paramSgType[1].slotType, paramSgType[1].apdStatus, paramSgType[1].precision, paramTextVec[1],
                            paramSgType[2].slotType, paramSgType[2].apdStatus, paramSgType[2].precision, paramTextVec[2]);
                    }
                    else if (numCols == 4)
                    {
                        retStr = apdWriter.MakeMerge4(dstSgType.slotType, dstSgType.apdStatus, dstSgType.precision,
                            paramSgType[0].slotType, paramSgType[0].apdStatus, paramSgType[0].precision, paramTextVec[0],
                            paramSgType[1].slotType, paramSgType[1].apdStatus, paramSgType[1].precision, paramTextVec[1],
                            paramSgType[2].slotType, paramSgType[2].apdStatus, paramSgType[2].precision, paramTextVec[2],
                            paramSgType[3].slotType, paramSgType[3].apdStatus, paramSgType[3].precision, paramTextVec[3]);
                    }
                    else
                    {
                        HlslUtil.ParserAssert(false);
                    }
                }
                else
                {
                    string nativeTypeName = HlslUtil.GetNativeTypeString(nodeNativeConstructor.nativeType);
                    retStr += (nativeTypeName + "(");

                    for (int i = 0; i < nodeNativeConstructor.paramNodeIds.Length; i++)
                    {
                        if (i >= 1)
                        {
                            retStr += ", ";
                        }

                        int argId = nodeNativeConstructor.paramNodeIds[i];
                        string argText = GenerateLinesRecurse(argId);

                        HlslTree.FullTypeInfo argInfo = tree.fullTypeInfo[argId];

                        ApdStatus argStatus = apdInfo.nodeApdStatus[argId];

                        // always convert to fpd
                        string coerceText = MakeImplicitCast(argInfo.nativeType, ApdStatus.Invalid,
                                                               argInfo.nativeType, argStatus,
                                                               argText);

                        retStr += coerceText;
                    }

                    retStr += ")";
                }
            }
            else if (baseNode is HlslTree.NodeToken nodeToken)
            {
                retStr = "";

                string tokenData = tokenizer.GetTokenData(nodeToken.srcTokenId);
                retStr += tokenData;
            }
            else if (baseNode is HlslTree.NodeNativeType nodeNativeType)
            {
                retStr = "";

                string nativeTypeName = HlslUtil.GetNativeTypeString(nodeNativeType.nativeType);
                retStr += nativeTypeName;
            }
            else if (baseNode is HlslTree.NodeLiteralOrBool nodeLiteralOrBool)
            {
                retStr = "";

                string literalToken = tokenizer.GetTokenData(nodeLiteralOrBool.nameTokenId);
                retStr += literalToken;
            }
            else if (baseNode is HlslTree.NodePassthrough nodePassthrough)
            {
                retStr = "";
                for (int i = 0; i < nodePassthrough.tokenIds.Length; i++)
                {
                    int tokenId = nodePassthrough.tokenIds[i];
                    string passthroughToken = tokenizer.GetTokenData(tokenId);
                    HlslToken tokenType = tokenizer.GetTokenType(tokenId);

                    if (tokenType == HlslToken._preprocessor)
                    {
                        string sectionPreproc = "#section";
                        if (passthroughToken.StartsWith(sectionPreproc))
                        {
                            passthroughToken = "";
                        }
                        else
                        {
                            passthroughToken = "\n" + passthroughToken + "\n";
                        }
                    }
                    else if (tokenType == HlslToken._comment_single)
                    {
                        passthroughToken += "\n";
                    }

                    if (tokenId < tokenizer.foundTokens.Count - 1)
                    {
                        HlslTokenizer.SingleToken currToken = tokenizer.foundTokens[tokenId + 0];
                        HlslTokenizer.SingleToken nextToken = tokenizer.foundTokens[tokenId + 1];
                        if (!currToken.data.Contains('\n') &&
                            currToken.marker.indexLine < nextToken.marker.indexLine)
                        {
                            currLine += "\n";
                        }
                    }

                    retStr += passthroughToken;
                }

                // for cases like a passthrough node for an entire block, write out the text. otherwise return
                // the text and let the parent handle it.
                if (nodePassthrough.writeDirect)
                {
                    AppendString(retStr);
                    FinishLine();
                    retStr = "";
                }
            }
            else if (baseNode is HlslTree.NodeMemberVariable nodeMemberVariable)
            {
                retStr = "";
                HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(nodeMemberVariable.structNodeId, unityReserved);
                HlslUtil.FieldInfo fieldInfo = structInfo.fields[nodeMemberVariable.fieldIndex];

                retStr += fieldInfo.identifier;
            }
            else if (baseNode is HlslTree.NodeParenthesisGroup nodeParenthesisGroup)
            {
                retStr = "";
                retStr += "(";
                retStr += GenerateLinesRecurse(nodeParenthesisGroup.childNodeId);
                retStr += ")";
            }
            else if (baseNode is HlslTree.NodeMemberFunction nodeMemberFunction)
            {
                retStr = "";

                HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(nodeMemberFunction.structNodeId, unityReserved);
                HlslUtil.PrototypeInfo protoInfo = structInfo.prototypes[nodeMemberFunction.funcIndex];

                retStr += protoInfo.identifier;
                retStr += "(";
                for (int i = 0; i < nodeMemberFunction.funcParamNodeIds.Length; i++)
                {
                    if (i >= 1)
                    {
                        retStr += ", ";
                    }
                    retStr += GenerateLinesRecurse(nodeMemberFunction.funcParamNodeIds[i]);
                }
                retStr += ")";
            }
            else if (baseNode is HlslTree.NodeSwizzle nodeSwizzle)
            {
                // should never happen, since the swizzle is handled by the parent node (because
                // it needs to handle the apd conversions)
                retStr = "";
            }
            else if (baseNode is HlslTree.NodeBlockInitializer nodeBlockInit)
            {
                retStr = "";

                retStr += "{";

                for (int i = 0; i < nodeBlockInit.initNodeIds.Length; i++)
                {
                    if (i > 0)
                    {
                        retStr += ", ";
                    }

                    // if this node is apd, convert to fps
                    int argId = nodeBlockInit.initNodeIds[i];
                    string argText = GenerateLinesRecurse(argId);

                    HlslTree.FullTypeInfo argInfo = tree.fullTypeInfo[argId];
                    ApdStatus argStatus = apdInfo.nodeApdStatus[argId];

                    // always convert to fpd
                    string coerceText = MakeImplicitCast(argInfo.nativeType, ApdStatus.Invalid,
                                                           argInfo.nativeType, argStatus,
                                                           argText);

                    retStr += coerceText;
                }

                retStr += "}";
            }
            else
            {
                // if we got here, then we are missing a type in this if tree
                HlslUtil.ParserAssert(false);
            }

            {
                // Each push should have a corresponding pop, unless we have an error/exception, in
                // which case this node stack should let us track down where it went wrong.
                int topIndex = nodeStack.Count - 1;
                HlslUtil.ParserAssert(nodeStack[topIndex] == nodeId);
                nodeStack.RemoveAt(topIndex);
            }
            return retStr;
        }

        int FindSurfaceDescriptionInputsStructId()
        {
            int sdInputsStructId = -1;
            int numNodes = tree.allNodes.Count;
            for (int nodeIter = 0; nodeIter < numNodes; nodeIter++)
            {
                HlslTree.Node node = tree.allNodes[nodeIter];
                if (node is HlslTree.NodeStruct nodeStruct)
                {
                    string structIdentifier = tokenizer.GetTokenData(nodeStruct.nameTokenId);
                    if (structIdentifier == "SurfaceDescriptionInputs")
                    {
                        sdInputsStructId = nodeIter;
                    }
                }
            }
            return sdInputsStructId;
        }

        static int GetUvIndex(string name)
        {
            int uvIndex = -1;
            if (name.Length == 3)
            {
                if (name[0] == 'u' && name[1] == 'v')
                {
                    uvIndex = name[2] - '0';
                    if (uvIndex < 0 || uvIndex >= 4)
                    {
                        uvIndex = -1;
                    }
                }
            }
            return uvIndex;
        }

        internal static string FlattenStringList(List<string> strList)
        {
            string ret = "";
            for (int i = 0; i < strList.Count; i++)
            {
                ret += strList[i] + "\n";
            }
            return ret;
        }

        internal static string FlattenStringVec(string[] strList)
        {
            string ret = "";
            for (int i = 0; i < strList.Length; i++)
            {
                ret += strList[i] + "\n";
            }
            return ret;
        }

        internal string[] GenerateLines(List<int> debugNodeStack, string shaderName)
        {
            nodeStack = debugNodeStack;

            // start as valid, disable if we find an error
            isValid = true;
            errList = new List<string>();

            allLines = new List<string>();
            currLine = "";
            indentLevel = 0;

            if (tree.topLevelNode < 0)
            {
                LogError("Missing top level node");
            }

            ResolveTypeInfoAndParents();

            uvDerivatives = new List<UVChannel>();

            sdInputsStructId = FindSurfaceDescriptionInputsStructId();

            {
                // find the struct id of SurfaceDescriptionInputs

                if (sdInputsStructId >= 0)
                {
                    HlslUtil.ParserAssert(tree.allNodes[sdInputsStructId] is HlslTree.NodeStruct);

                    HlslTree.NodeStruct node = (HlslTree.NodeStruct)tree.allNodes[sdInputsStructId];

                    for (int declIter = 0; declIter < node.declarations.Length; declIter++)
                    {
                        int declId = node.declarations[declIter];

                        HlslUtil.ParserAssert(tree.allNodes[declId] is HlslTree.NodeDeclaration);
                        HlslTree.NodeDeclaration nodeDecl = (HlslTree.NodeDeclaration)tree.allNodes[declId];

                        string name = tokenizer.GetTokenData(nodeDecl.nameTokenId);
                        UVChannel uvIndex = (UVChannel)GetUvIndex(name);
                        if (uvIndex >= 0)
                        {
                            uvDerivatives.Add(uvIndex);
                        }
                    }

                }


            }

            List<string> sectionUnknown = new List<string>();
            List<string> sectionGraphFunction = new List<string>();
            List<string> sectionGraphPixel = new List<string>();

            HlslTree.NodeTopLevel nodeTopLevel = (HlslTree.NodeTopLevel)tree.allNodes[tree.topLevelNode];

            debugLog = "";
            try
            {
                for (int i = 0; i < nodeTopLevel.statements.Length; i++)
                {
                    GenerateLinesRecurse(nodeTopLevel.statements[i]);

                    HlslTokenizer.CodeSection codeSection = nodeTopLevel.codeSections[i];
                    switch (codeSection)
                    {
                        case HlslTokenizer.CodeSection.Unknown:
                            sectionUnknown.AddRange(allLines);
                            break;
                        case HlslTokenizer.CodeSection.GraphFunction:
                            sectionGraphFunction.AddRange(allLines);
                            break;
                        case HlslTokenizer.CodeSection.GraphPixel:
                            sectionGraphPixel.AddRange(allLines);
                            break;
                        default:
                            HlslUtil.ParserAssert(false);
                            break;
                    }

                    allLines = new List<string>();
                }
            }
            catch (Exception e)
            {
                string warnText = e.Message + " (derivative generation: " + shaderName + ")";
                Debug.LogWarning(warnText);
                debugLog += warnText + "\n";
                debugLog += e.StackTrace;

                // also, add the generated node stack if we have one
                debugLog += "\n";
                debugLog += "NodeStack: " + debugNodeStack.Count.ToString() + "\n";
                for (int i = 0; i < debugNodeStack.Count; i++)
                {
                    int nodeId = debugNodeStack[i];
                    debugLog += "    " + nodeId.ToString() + "\n";
                }

                isValid = false;
            }

            ShaderStringBuilder builder = new ShaderStringBuilder(humanReadable: true);

            apdWriter.GenerateDefinitionsAndFuncs(builder);
            dstUnknown = FlattenStringList(sectionUnknown);
            dstApdFuncs = builder.ToCodeBlock();
            dstGraphFunction = FlattenStringList(sectionGraphFunction);
            dstGraphPixel = FlattenStringList(sectionGraphPixel);

            List<string> dstLines = new List<string>();

            dstLines.Add("// unknown section");
            dstLines.Add(dstUnknown);

            dstLines.Add("// apd funcs");
            dstLines.Add(dstApdFuncs);

            dstLines.Add("// graph function section");
            dstLines.Add(dstGraphFunction);

            dstLines.Add("// graph pixel section");
            dstLines.Add(dstGraphPixel);

            return dstLines.ToArray();
        }

        HlslApdInfo apdInfo;

        int indentLevel;

        internal HlslTokenizer tokenizer;
        internal HlslUnityReserved unityReserved;

        internal HlslTree tree;
        internal Dictionary<HlslToken, HlslNativeType> tokenToNativeTable;

        internal bool isValid;
        internal List<string> errList;

        int[] nodeParents;
        int[][] nodeChildren;

        int sdInputsStructId;

        bool applyEmulatedDerivatives;

        PartialDerivUtilWriter apdWriter;

        internal List<UVChannel> uvDerivatives;

        internal string[] debugNodeLines;

        internal string dstUnknown;
        internal string dstApdFuncs;
        internal string dstGraphFunction;
        internal string dstGraphPixel;

        internal List<int> nodeStack;
        internal string debugLog;


        List<string> allLines;
        string currLine;

    }
}
