using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HlslParser
    {
        internal struct TypeInfo
        {
            // If a non-struct, native type describes it. Otherwise, identifier for lookup. Struct for identifier
            // must be at global scope (no structs defined in structs).
            internal HlslNativeType nativeType;
            internal string identifier;
            internal int arrayDims;
            internal ApdAllowedState allowedState;

            static internal TypeInfo MakeNativeType(HlslNativeType nativeType, int dims, ApdAllowedState allowedState)
            {
                TypeInfo ret = new TypeInfo();
                ret.nativeType = nativeType;
                ret.identifier = "";
                ret.arrayDims = dims;
                ret.allowedState = allowedState;
                return ret;
            }

            static internal TypeInfo MakeStruct(string structName, int dims)
            {
                TypeInfo ret = new TypeInfo();
                ret.nativeType = HlslNativeType._struct;
                ret.identifier = structName;
                ret.arrayDims = dims;
                ret.allowedState = ApdAllowedState.OnlyFpd;
                return ret;
            }

            internal string DebugString()
            {
                string arrayStr = (arrayDims == 0) ? "" : "[" + arrayDims.ToString() + "]";

                if (nativeType == HlslNativeType._struct)
                {
                    return identifier + arrayStr;
                }
                else
                {
                    return nativeType.ToString() + arrayStr;
                }
            }

        }

        internal struct TokenRunner
        {
            internal int currToken;

            internal bool isValid;
            internal string lastErr;
            internal int tokenErr;

            internal HlslParser parser;

            internal bool allowPreprocessor;

            internal string debugPrev;
            internal string debugCurr; // curr = top

            static internal TokenRunner MakeRunner(HlslParser srcParser, int srcToken)
            {
                TokenRunner ret = new TokenRunner();
                ret.currToken = srcToken;
                ret.isValid = true;
                ret.lastErr = "";
                ret.parser = srcParser;
                ret.tokenErr = -1;
                ret.allowPreprocessor = true;

                ret.debugPrev = "";
                ret.debugCurr = ""; // curr = top

                return ret;
            }

            // Not technically needed because this is a struct, but this makes it explicit that it's a copy,
            // and if in the future we have any copyable data (like an array) then we can adjust MakeCopy()
            // and the functions that call it will "just work".
            internal TokenRunner MakeCopy()
            {
                TokenRunner runner = this;
                return runner;
            }

            internal bool IsEof()
            {
                return TopToken() == HlslToken._eof;
            }

            internal HlslToken TopToken()
            {
                if (currToken < parser.tokenizer.foundTokens.Count)
                {
                    return parser.tokenizer.foundTokens[currToken].tokenType;
                }

                return HlslToken._eof;
            }


            internal HlslToken LookAheadToken(int numAhead)
            {
                TokenRunner lookAhead = MakeCopy();
                for (int i = 0; i < numAhead; i++)
                {
                    lookAhead.AdvanceToken();
                }

                return lookAhead.TopToken();
            }


            internal string TopData()
            {
                if (currToken < parser.tokenizer.foundTokens.Count)
                {
                    return parser.tokenizer.foundTokens[currToken].data;
                }

                return "<invalid>";
            }

            internal HlslTokenizer.CodeSection TopCodeSection()
            {
                if (currToken < parser.tokenizer.foundTokens.Count)
                {
                    return parser.tokenizer.foundTokens[currToken].codeSection;
                }

                return HlslTokenizer.CodeSection.Unknown;
            }


            internal string LookAheadData(int numAhead)
            {
                TokenRunner lookAhead = MakeCopy();
                for (int i = 0; i < numAhead; i++)
                {
                    lookAhead.AdvanceToken();
                }

                return lookAhead.TopData();
            }



            // get the next non-whitespace, non-define, non-comment token
            internal void AdvanceToken()
            {
                // only go to the next token if we haven't gone past the edge of the list yet
                if (currToken < parser.tokenizer.foundTokens.Count)
                {
                    currToken++;
                    while (currToken < parser.tokenizer.foundTokens.Count &&
                        !HlslTokenizer.IsTokenReserved(TopToken()) &&
                        !HlslTokenizer.IsTokenOperator(TopToken()) &&
                        !HlslTokenizer.IsTokenLiteral(TopToken()) &&
                        !HlslTokenizer.IsTokenIdentifier(TopToken()))
                    {
                        // If preprocessor is not being passed through, error out if we hit one. In general,
                        // preprocessor macros are allowed in global scope, but not inside functions.
                        if (!allowPreprocessor)
                        {
                            if (TopToken() == HlslToken._preprocessor)
                            {
                                string debugData = TopData();
                                Error("Invalid place for preprocessor token.");
                            }
                        }

                        currToken++;
                    }

                    debugPrev = debugCurr;
                    debugCurr = (currToken < parser.tokenizer.foundTokens.Count) ? parser.tokenizer.foundTokens[currToken].data : "<eof>";
                }
            }

            // Accept: If the condition is expected, then fetch and advance. Otherwise, return false.
            // Check: See if the condition is expected, but do not advance.
            // Expect: Assume that the condition is true. Otherwise, it's an error.
            internal bool Check(HlslToken token)
            {
                HlslToken actual = TopToken();
                return actual == token;
            }

            internal bool CheckLiteralOrBool()
            {
                HlslToken actual = TopToken();
                return HlslTokenizer.IsTokenLiteralOrBool(actual);
            }


            internal void Error(string err)
            {
                // only apply the error if we haven't already applied one
                if (isValid)
                {
                    isValid = false;
                    lastErr = err;
                    tokenErr = currToken;

                    currToken = parser.tokenizer.foundTokens.Count;

                    parser.tree.ApplyError(err, tokenErr);
                }
            }

            // Not really an assert, but unsure of the best word. If condition is true, all good.
            // If condition is false, apply the error, and set the next token to EOF. This allows
            // us to abandon parsing without having "if(condition) return err;" all over the code.
            internal void ParseAssert(bool condition, string err)
            {
                if (!condition)
                {
                    Error(err);
                }
            }

            internal int GetStructDefinitionFromString(string name)
            {
                int ret = -1;

                bool isUnityStruct = parser.unityReserved.IsIdentifierUnityStruct(name);
                if (isUnityStruct)
                {
                    HlslTree.NodeUnityStruct node = new HlslTree.NodeUnityStruct();
                    node.unityName = name;
                    ret = parser.tree.AddNode(node, parser, isValid);
                }
                else
                {
                    int stackIndex = parser.tree.structLookupStack.Count - 1;
                    while (ret < 0 && stackIndex >= 0)
                    {
                        if (parser.tree.structLookupStack[stackIndex].ContainsKey(name))
                        {
                            ret = parser.tree.structLookupStack[stackIndex][name];
                        }
                        stackIndex--;
                    }
                }
                return ret;
            }

            internal int GetTokenStructType()
            {
                int foundNodeId = -1;

                if (TopToken() == HlslToken._identifier)
                {
                    string name = parser.tokenizer.GetTokenData(currToken);
                    foundNodeId = GetStructDefinitionFromString(name);
                }
                return foundNodeId;
            }

            internal bool Expect(HlslToken token)
            {
                if (TopToken() == token)
                {
                    AdvanceToken();
                    return true;
                }

                Error("Expected token: " + token.ToString() + " vs. Actual: " + TopToken().ToString());
                return false;
            }

            internal bool CheckParamModifier()
            {
                if (TopToken() == HlslToken._in ||
                    TopToken() == HlslToken._out ||
                    TopToken() == HlslToken._inout)
                {
                    return true;
                }
                return false;
            }

            internal bool Accept(HlslToken token)
            {
                if (TopToken() == token)
                {
                    AdvanceToken();
                    return true;
                }

                return false;
            }

            internal bool CheckType()
            {
                if (HlslTokenizer.IsTokenNativeType(TopToken()) ||
                    HlslTokenizer.IsTokenIdentifier(TopToken()) ||
                    TopToken() == HlslToken._void)
                {
                    return true;
                }

                return false;
            }

            internal bool AcceptType()
            {
                bool ret = CheckType();
                if (ret)
                {
                    AdvanceToken();
                }
                return ret;
            }
        }

        // https://en.cppreference.com/w/cpp/language/operator_precedence
        internal int GetOperatorPrcedence(HlslOp op)
        {
            int ret = -1;
            switch (op)
            {
                case HlslOp.ScopeReslution:     // ::
                    ret = 1;
                    break;
                case HlslOp.PostIncrement:      // ++
                case HlslOp.PostDecrement:      //  --
                case HlslOp.FunctionalCast:     // func()
                case HlslOp.FunctionalCall:     // func()
                case HlslOp.Subscript:          // []
                case HlslOp.MemberAccess:       // .
                    ret = 2;
                    break;
                case HlslOp.PreIncrement:       // ++
                case HlslOp.PreDecrement:       // --
                case HlslOp.UnaryPlus:          // +
                case HlslOp.UnaryMinus:         // -
                case HlslOp.LogicalNot:         // !
                case HlslOp.BitwiseNot:         // ~
                case HlslOp.CStyleCast:         // (int)
                case HlslOp.Dereference:        // *    - not legal in HLSL?
                case HlslOp.AddressOf:          // &    - not legal in HLSL?
                case HlslOp.Sizeof:             // sizeof
                    ret = 3;
                    break;
                case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                    ret = 4;
                    break;
                case HlslOp.Mul:                // *
                case HlslOp.Div:                // /
                case HlslOp.Mod:          // %
                    ret = 5;
                    break;
                case HlslOp.Add:                // +
                case HlslOp.Sub:                // -
                    ret = 6;
                    break;
                case HlslOp.ShiftL:             // <<
                case HlslOp.ShiftR:             // >>
                    ret = 7;
                    break;
                case HlslOp.ThreeWayCompare:    // <=>  - never used this
                    ret = 8;
                    break;
                case HlslOp.LessThan:           // <
                case HlslOp.GreaterThan:        // >
                case HlslOp.LessEqual:          // <=
                case HlslOp.GreaterEqual:       // >=
                    ret = 9;
                    break;
                case HlslOp.CompareEqual:       // ==
                case HlslOp.NotEqual:           // !=
                    ret = 10;
                    break;
                case HlslOp.BitwiseAnd:         // &
                    ret = 11;
                    break;
                case HlslOp.BitwiseXor:         // ^
                    ret = 12;
                    break;
                case HlslOp.BitwiseOr:          // |
                    ret = 13;
                    break;
                case HlslOp.LogicalAnd:         // &&
                    ret = 14;
                    break;
                case HlslOp.LogicalOr:          // ||
                    ret = 15;
                    break;
                case HlslOp.TernaryQuestion:    // ?
                case HlslOp.TernaryColon:       // :
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
                    ret = 16;
                    break;
                case HlslOp.Comma:              // ,
                    ret = 17;
                    break;
                case HlslOp.Invalid:
                    // When parsing, we generally only want to parse operators with priority lower than the
                    // existing priority, so by making 18 we default to not parsing an invalid operator in
                    // most cases to gracefully error when parsing fails.
                    ret = 18;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        internal int GetWeakestPrcedence()
        {
            return 17;
        }

        internal bool GetOperatorIsLeftToRight(HlslOp op)
        {
            bool ret = true;
            switch (op)
            {
                case HlslOp.ScopeReslution:     // ::
                case HlslOp.PostIncrement:      // ++
                case HlslOp.PostDecrement:      //  --
                case HlslOp.FunctionalCast:     // func()
                case HlslOp.FunctionalCall:     // func()
                case HlslOp.Subscript:          // []
                case HlslOp.MemberAccess:       // .
                    ret = true;
                    break;
                case HlslOp.PreIncrement:       // ++
                case HlslOp.PreDecrement:       // --
                case HlslOp.UnaryPlus:          // +
                case HlslOp.UnaryMinus:         // -
                case HlslOp.LogicalNot:         // !
                case HlslOp.BitwiseNot:         // ~
                case HlslOp.CStyleCast:         // (int)
                case HlslOp.Dereference:        // *    - not legal in HLSL?
                case HlslOp.AddressOf:          // &    - not legal in HLSL?
                case HlslOp.Sizeof:             // sizeof
                    ret = false;
                    break;
                case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                case HlslOp.Mul:                // *
                case HlslOp.Div:                // /
                case HlslOp.Mod:          // %
                case HlslOp.Add:                // +
                case HlslOp.Sub:                // -
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
                    ret = true;
                    break;
                case HlslOp.TernaryQuestion:    // ?
                case HlslOp.TernaryColon:       // :
                case HlslOp.Assignment:         // =
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
                    ret = false;
                    break;
                case HlslOp.Comma:              // ,
                    ret = true;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }

        static internal bool GetOperatorIsBinary(HlslOp op)
        {
            bool ret = true;
            switch (op)
            {
                case HlslOp.ScopeReslution:     // ::
                    ret = true;
                    break;
                case HlslOp.PostIncrement:      // ++
                case HlslOp.PostDecrement:      //  --
                case HlslOp.FunctionalCast:     // func()
                case HlslOp.FunctionalCall:     // func()
                case HlslOp.Subscript:          // []
                case HlslOp.MemberAccess:       // .
                    ret = false;
                    break;
                case HlslOp.PreIncrement:       // ++
                case HlslOp.PreDecrement:       // --
                case HlslOp.UnaryPlus:          // +
                case HlslOp.UnaryMinus:         // -
                    ret = false;
                    break;
                case HlslOp.LogicalNot:         // !
                case HlslOp.BitwiseNot:         // ~
                    ret = false;
                    break;
                case HlslOp.CStyleCast:         // (int)
                case HlslOp.Dereference:        // *    - not legal in HLSL?
                case HlslOp.AddressOf:          // &    - not legal in HLSL?
                case HlslOp.Sizeof:             // sizeof
                    ret = false;
                    break;
                case HlslOp.PointerToMemberDot: // .*   - not legal in HLSL
                case HlslOp.PointerToMemorArrow: // ->* - not legal in HLSL
                    ret = false;
                    break;
                case HlslOp.Mul:                // *
                case HlslOp.Div:                // /
                case HlslOp.Mod:          // %
                case HlslOp.Add:                // +
                case HlslOp.Sub:                // -
                case HlslOp.ShiftL:             // <<
                case HlslOp.ShiftR:             // >>
                    ret = true;
                    break;
                case HlslOp.ThreeWayCompare:    // <=>  - never used this
                    ret = false; // ??
                    break;
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
                    ret = true;
                    break;
                case HlslOp.TernaryQuestion:    // ?
                case HlslOp.TernaryColon:       // :
                    ret = false;
                    break;
                case HlslOp.Assignment:         // =
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
                    ret = true;
                    break;
                case HlslOp.Comma:              // ,
                    ret = true;
                    break;
                default:
                    HlslUtil.ParserAssert(false);
                    break;
            }

            return ret;
        }


        internal static Dictionary<HlslToken, HlslNativeType> GenerateTokenToNativeTable()
        {
            Dictionary<HlslToken, HlslNativeType> table = new Dictionary<HlslToken, HlslNativeType>();

            Dictionary<string, HlslNativeType> nativeStringToType = new Dictionary<string, HlslNativeType>();

            for (HlslNativeType i = HlslNativeType._unknown; i <= HlslNativeType._struct; i++)
            {
                string key = i.ToString();
                nativeStringToType.Add(key, i);
            }

            for (HlslToken i = HlslToken._float; i < HlslToken._struct; i++)
            {
                HlslToken key = i;
                string value = i.ToString();
                if (nativeStringToType.ContainsKey(value))
                {
                    HlslNativeType native = nativeStringToType[value];
                    table.Add(key, native);
                }
            }

            return table;
        }

        internal HlslParser(HlslTokenizer srcTokenizer, HlslUnityReserved srcUnityReserved, List<string> srcIgnoredFuncs)
        {
            tokenizer = srcTokenizer;
            unityReserved = srcUnityReserved;

            tokenToNativeTable = GenerateTokenToNativeTable();

            ignoredFuncs = srcIgnoredFuncs;
        }

        internal void ParseTokens(HlslTree srcTree)
        {
            TokenRunner runner = TokenRunner.MakeRunner(this, 0);

            tree = srcTree;

            int topLevel = ParseTopLevel(runner);
            tree.topLevelNode = topLevel;
        }

        internal int CreateNodeForTopToken(ref TokenRunner runner)
        {
            HlslTree.NodeToken node = new HlslTree.NodeToken();
            node.srcTokenId = runner.currToken;
            int nodeId = runner.parser.tree.AddNode(node, this, runner.isValid);
            return nodeId;
        }

        internal int CreateNodeForTokenId(ref TokenRunner runner, int tokenId)
        {
            HlslTree.NodeToken node = new HlslTree.NodeToken();
            node.srcTokenId = tokenId;
            int nodeId = runner.parser.tree.AddNode(node, this, runner.isValid);
            return nodeId;
        }


        internal int CreateNodeForNativeType(ref TokenRunner runner, HlslNativeType type)
        {
            HlslTree.NodeNativeType node = new HlslTree.NodeNativeType();
            node.nativeType = type;
            int nodeId = runner.parser.tree.AddNode(node, this, runner.isValid);
            return nodeId;
        }

        internal HlslTree.VariableInfo GetVariableInfoFromDecl(HlslTree.NodeDeclaration nodeDecl, int declNodeId)
        {
            HlslTree.VariableInfo variableInfo = new HlslTree.VariableInfo();

            variableInfo.typeInfo.nativeType = nodeDecl.nativeType;
            if (nodeDecl.nativeType == HlslNativeType._struct)
            {
                HlslTree.Node baseNode = tree.allNodes[nodeDecl.structId];
                if (baseNode is HlslTree.NodeToken tokenNode)
                {
                    variableInfo.typeInfo.identifier = tokenizer.GetTokenData(tokenNode.srcTokenId);
                }
                else if (baseNode is HlslTree.NodeStruct structNode)
                {
                    variableInfo.typeInfo.identifier = tokenizer.GetTokenData(structNode.nameTokenId);
                }
                else if (baseNode is HlslTree.NodeUnityStruct unityNode)
                {
                    variableInfo.typeInfo.identifier = unityNode.unityName;
                }
                else
                {
                    variableInfo.typeInfo.nativeType = HlslNativeType._unknown;
                    variableInfo.typeInfo.identifier = "<error>";
                }
                variableInfo.structId = nodeDecl.structId;
            }
            else
            {
                variableInfo.typeInfo.identifier = HlslUtil.GetNativeTypeString(nodeDecl.nativeType);
                variableInfo.structId = -1;
            }

            variableInfo.variableName = nodeDecl.variableName;
            variableInfo.declId = declNodeId;
            variableInfo.typeInfo.arrayDims = nodeDecl.arrayDims.Length;

            return variableInfo;
        }

        internal int ParseDeclaration(out HlslTree.VariableInfo variableInfo, ref TokenRunner runner, bool trailingSemicolon, bool allowModifer)
        {
            variableInfo = new HlslTree.VariableInfo();

            HlslTree.NodeDeclaration nodeDecl = new HlslTree.NodeDeclaration();

            // if we allow modifers, see if we have one
            if (runner.TopToken() == HlslToken._in ||
                runner.TopToken() == HlslToken._out ||
                runner.TopToken() == HlslToken._inout)
            {
                nodeDecl.modifierTokenId = runner.currToken;
                runner.AdvanceToken();
            }

            ParseDeclTypeAndIdentifier(ref nodeDecl, ref runner, false);

            if (trailingSemicolon)
            {
                // for structs, we need a semicolon after each field
                runner.Expect(HlslToken._semicolon);
            }

            int ret = tree.AddNode(nodeDecl, this, runner.isValid);

            variableInfo = GetVariableInfoFromDecl(nodeDecl, ret);
            if (!variableInfo.IsValid())
            {
                runner.Error("Node declaration type invalid.");
            }

            return ret;
        }

        internal int ParseStruct(ref TokenRunner runner)
        {
            {
                bool ok = runner.Accept(HlslToken._struct);
                runner.ParseAssert(ok, "Expected struct.");
            }

            HlslTree.NodeStruct nodeStruct = new HlslTree.NodeStruct();

            {
                bool ok = runner.Check(HlslToken._identifier);
                runner.ParseAssert(ok, "Expected identifier");
                nodeStruct.nameTokenId = runner.currToken;
                runner.AdvanceToken();
            }

            runner.Expect(HlslToken._brace_l);

            List<int> declarations = new List<int>();

            List<HlslTree.VariableInfo> variables = new List<HlslTree.VariableInfo>();
            while (runner.CheckType())
            {
                HlslTree.VariableInfo variableInfo;
                int declId = ParseDeclaration(out variableInfo, ref runner, true, false);
                declarations.Add(declId);
                variables.Add(variableInfo);
            }

            runner.Expect(HlslToken._brace_r);

            nodeStruct.declarations = declarations.ToArray();

            string structName = runner.parser.tokenizer.GetTokenData(nodeStruct.nameTokenId);

            // For now, we won't allow to actually declare member functions in structs. They are
            // allowed in unity reserved structures, but not the ones defined in the surface
            // shader.
            HlslUtil.StructInfo structInfo = new HlslUtil.StructInfo();
            structInfo.identifier = tokenizer.GetTokenData(nodeStruct.nameTokenId);
            structInfo.fields = new HlslUtil.FieldInfo[variables.Count];
            structInfo.prototypes = new HlslUtil.PrototypeInfo[0];
            for (int i = 0; i < variables.Count; i++)
            {
                HlslTree.VariableInfo variableInfo = variables[i];

                HlslUtil.FieldInfo fieldInfo = new HlslUtil.FieldInfo();
                fieldInfo.identifier = variableInfo.variableName;
                fieldInfo.typeInfo = variableInfo.typeInfo;
                //fieldInfo.typeInfo.arrayDims = variableInfo.typeInfo.dims;
                fieldInfo.semantics = new string[0];

                structInfo.fields[i] = fieldInfo;
            }

            nodeStruct.structInfoId = tree.AddStructInfo(structInfo);

            int structNodeId = tree.AddNode(nodeStruct, this, runner.isValid);

            tree.AddStructIdentifier(structName, structNodeId);

            return structNodeId;
        }

        internal int ParseFunctionPrototype(ref TokenRunner runner, out List<HlslTree.VariableInfo> foundVariables)
        {
            foundVariables = new List<HlslTree.VariableInfo>();

            HlslTree.NodeFunctionPrototype proto = new HlslTree.NodeFunctionPrototype();

            // first should be an identifier for return type
            bool valid = runner.CheckType();
            if (!valid)
            {
                runner.Error("Expected type at function.");
                return -1;
            }

            proto.returnTokenId = runner.currToken;
            runner.AdvanceToken();


            // next should be an identifier for the function name
            if (runner.Check(HlslToken._identifier))
            {
                proto.nameTokenId = runner.currToken;
                runner.AdvanceToken();

                // next '('
                runner.Expect(HlslToken._parense_l);

                bool firstIteration = true;

                List<int> declarations = new List<int>();

                // keep parsing declarations until we find a ')'
                while (runner.isValid && !runner.Check(HlslToken._parense_r))
                {
                    // if this is not the first iteration, then we whould expect a comma
                    if (!firstIteration)
                    {
                        runner.Expect(HlslToken._comma);
                    }

                    HlslTree.VariableInfo variableInfo;
                    int foundDecl = ParseDeclaration(out variableInfo, ref runner, false, true);
                    if (foundDecl < 0)
                    {
                        // if we failed to find a declaration, than the runner should no longer be valid, which is
                        // important so that we exit the while loop
                        HlslUtil.ParserAssert(!runner.isValid);
                    }

                    foundVariables.Add(variableInfo);
                    declarations.Add(foundDecl);

                    // no longer first iteration, so following iterations will need a comma separator
                    firstIteration = false;
                }

                runner.Expect(HlslToken._parense_r);

                proto.declarations = declarations.ToArray();
            }

            int protoId = -1;
            if (runner.isValid)
            {
                protoId = tree.AddNode(proto, this, runner.isValid);
            }
            return protoId;
        }

        internal bool TryCStyleCast(ref TokenRunner runner, out int foundTokenId, out HlslToken foundTokenType, out int foundStructId, out string dstCastName)
        {
            dstCastName = "";
            foundTokenId = -1;
            foundTokenType = HlslToken._invalid;
            foundStructId = -1;

            // a big hacky, we really should be pushing/popping these errors
            List<HlslTree.ErrInfo> errs = new List<HlslTree.ErrInfo>(tree.errList);

            runner.Expect(HlslToken._parense_l);

            HlslToken token = runner.TopToken();

            bool isNativeType = HlslTokenizer.IsTokenNativeType(token);
            bool isIdentifier = HlslTokenizer.IsTokenIdentifier(token);

            if (isNativeType && token != HlslToken._struct)
            {
                foundTokenId = runner.currToken;
                foundTokenType = token;
                dstCastName = HlslTokenizer.AsReservedString(token);
            }
            else if (isIdentifier)
            {
                string name = runner.TopData();

                int structId = runner.GetStructDefinitionFromString(name);
                if (structId >= 0)
                {
                    foundTokenId = runner.currToken;
                    foundTokenType = HlslToken._identifier;
                    dstCastName = name;
                    foundStructId = structId;
                    runner.AdvanceToken();
                }
                else
                {
                    runner.isValid = false;
                }
            }
            else
            {
                runner.isValid = false;
            }

            runner.Expect(HlslToken._parense_r);

            if (!runner.isValid)
            {
                dstCastName = "";
                foundTokenId = -1;
                foundTokenType = HlslToken._invalid;
            }

            // revert any errors if we added them
            tree.errList = errs;

            return runner.isValid;
        }

        // try to parse the next token as an operator, but only legal if the op is <= precedence
        internal bool TryParseNextOp(ref TokenRunner parentRunner, out HlslOp dstOp, out string dstCastName, out int dstTokenId, out int dstCastStructId, int precedence, bool hasPreceedingVariable)
        {
            TokenRunner runner = parentRunner.MakeCopy();

            dstOp = HlslOp.Invalid;
            dstCastName = "";
            dstTokenId = -1;
            dstCastStructId = -1;

            HlslToken foundToken = runner.TopToken();
            HlslOp foundOp = HlslOp.Invalid;
            int foundTokenId = runner.currToken;
            string foundCastName = "";
            int foundStructId = -1;

            switch (foundToken)
            {
                case HlslToken._colon_colon:       // ::
                    foundOp = HlslOp.ScopeReslution;
                    break;
                case HlslToken._plus_plus:         // ++
                    foundOp = hasPreceedingVariable ? HlslOp.PostIncrement : HlslOp.PreIncrement;
                    break;
                case HlslToken._minus_minus:       // --
                    foundOp = hasPreceedingVariable ? HlslOp.PostDecrement : HlslOp.PreDecrement;
                    break;
                case HlslToken._parense_l:         // (
                    {
                        int cstyleTokenId;
                        HlslToken cstyleToken;
                        string cstyleCastName;
                        int cstyleStructId;

                        // check for c-style cast
                        bool foundCast = TryCStyleCast(ref runner, out cstyleTokenId, out cstyleToken, out cstyleStructId, out cstyleCastName);
                        if (foundCast)
                        {
                            foundTokenId = cstyleTokenId;
                            foundToken = cstyleToken;
                            foundCastName = cstyleCastName;
                            foundStructId = cstyleStructId;
                            foundOp = HlslOp.CStyleCast;
                        }
                        else
                        {
                            foundOp = HlslOp.Invalid;
                        }
                    }
                    break;
                case HlslToken._parense_r:         // )
                    foundOp = HlslOp.Invalid; // nope
                    break;
                case HlslToken._bracket_l:         // [
                    foundOp = HlslOp.Subscript;
                    break;
                case HlslToken._bracket_r:         // ]
                    foundOp = HlslOp.Invalid; // nope
                    break;
                case HlslToken._period:            // .
                    foundOp = HlslOp.MemberAccess;
                    break;
                case HlslToken._plus:              // +
                    foundOp = hasPreceedingVariable ? HlslOp.Add : HlslOp.UnaryPlus;
                    break;
                case HlslToken._minus:             // -
                    foundOp = hasPreceedingVariable ? HlslOp.Sub : HlslOp.UnaryMinus;
                    break;
                case HlslToken._logical_not:       // !
                    foundOp = HlslOp.LogicalNot;
                    break;
                case HlslToken._bitwise_not:       // ~
                    foundOp = HlslOp.BitwiseNot;
                    break;
                case HlslToken._mul:               // *
                    foundOp = HlslOp.Mul;
                    break;
                case HlslToken._div:               // /
                    foundOp = HlslOp.Div;
                    break;
                case HlslToken._modulo:            // %
                    foundOp = HlslOp.Mod;
                    break;
                case HlslToken._shift_l:           // <<
                    foundOp = HlslOp.ShiftL;
                    break;
                case HlslToken._shift_r:           // >>
                    foundOp = HlslOp.ShiftR;
                    break;
                case HlslToken._less_than:         // <
                    foundOp = HlslOp.LessThan;
                    break;
                case HlslToken._greater_than:      // >
                    foundOp = HlslOp.GreaterThan;
                    break;
                case HlslToken._less_equal:        // <=
                    foundOp = HlslOp.LessEqual;
                    break;
                case HlslToken._greater_equal:     // >=
                    foundOp = HlslOp.GreaterEqual;
                    break;
                case HlslToken._equivalent:        // ==
                    foundOp = HlslOp.CompareEqual;
                    break;
                case HlslToken._not_equal:         // !=
                    foundOp = HlslOp.NotEqual;
                    break;
                case HlslToken._bitwise_and:       // &
                    foundOp = HlslOp.Invalid;
                    break;
                case HlslToken._bitwise_xor:       // ^
                    foundOp = HlslOp.BitwiseXor;
                    break;
                case HlslToken._bitwise_or:        // |
                    foundOp = HlslOp.BitwiseOr;
                    break;
                case HlslToken._logical_and:       // &&
                    foundOp = HlslOp.LogicalAnd;
                    break;
                case HlslToken._logical_or:        // ||
                    foundOp = HlslOp.LogicalOr;
                    break;
                case HlslToken._question:          // ?
                    foundOp = HlslOp.TernaryQuestion;
                    break;
                case HlslToken._colon:             // :
                    // actually, at this part of the code, we actually want to reject
                    // the : operator so that it can be handled by the operator higher
                    // in the tree
                    //foundOp = HlslOp.TernaryColon;
                    foundOp = HlslOp.Invalid;
                    break;
                case HlslToken._assignment:        // =
                    foundOp = HlslOp.Assignment;
                    break;
                case HlslToken._add_equals:        // +=
                    foundOp = HlslOp.AddEquals;
                    break;
                case HlslToken._sub_equals:        // -=
                    foundOp = HlslOp.SubEquals;
                    break;
                case HlslToken._mul_equals:        // *=
                    foundOp = HlslOp.MulEquals;
                    break;
                case HlslToken._div_equals:        // /=
                    foundOp = HlslOp.DivEquals;
                    break;
                case HlslToken._mod_equals:        // %=
                    foundOp = HlslOp.ModEquals;
                    break;
                case HlslToken._shift_l_equals:    // <<=
                    foundOp = HlslOp.ShiftLEquals;
                    break;
                case HlslToken._shift_r_equals:    // >>=
                    foundOp = HlslOp.ShiftREquals;
                    break;
                case HlslToken._and_equals:        // &=
                    foundOp = HlslOp.AndEquals;
                    break;
                case HlslToken._or_equals:         // |=
                    foundOp = HlslOp.OrEquals;
                    break;
                case HlslToken._xor_equals:        // ^=
                    foundOp = HlslOp.XorEquals;
                    break;
                case HlslToken._comma:             // ,
                    foundOp = HlslOp.Comma;
                    break;
                case HlslToken._brace_l:           // {
                    foundOp = HlslOp.Invalid; // nope
                    break;
                case HlslToken._brace_r:           // }
                    foundOp = HlslOp.Invalid; // nope
                    break;
                case HlslToken._semicolon:         // ;
                    foundOp = HlslOp.Invalid; // nope
                    break;
                default:
                    // not found, is already valid
                    break;
            }

            bool doesOpRequirePrecedingVariable = true;
            switch (foundOp)
            {
                case HlslOp.PreDecrement:
                case HlslOp.PreIncrement:
                case HlslOp.UnaryMinus:
                case HlslOp.UnaryPlus:
                case HlslOp.BitwiseNot:
                case HlslOp.LogicalNot:
                case HlslOp.CStyleCast:
                    doesOpRequirePrecedingVariable = false;
                    break;
                default:
                    doesOpRequirePrecedingVariable = true;
                    break;
            }

            if (foundOp != HlslOp.Invalid && doesOpRequirePrecedingVariable == hasPreceedingVariable)
            {
                // cstyle cast advances the full tokens, but all other types do not
                if (foundOp != HlslOp.CStyleCast)
                {
                    runner.AdvanceToken();
                }

                int foundPrecedence = GetOperatorPrcedence(foundOp);
                bool isLeftToRight = GetOperatorIsLeftToRight(foundOp);

                // by default, we parse by making a child node to the rhs of the our chain,
                // so if we are leftToRight we want to fail if precedence is equal, but succeed
                // if we are rightToLeft.

                bool validPrecedence = isLeftToRight ? (foundPrecedence < precedence) : (foundPrecedence <= precedence);

                if (validPrecedence)
                {
                    dstOp = foundOp;
                    dstCastName = foundCastName;
                    dstTokenId = foundTokenId;
                    dstCastStructId = foundStructId;

                    parentRunner = runner;
                }
            }

            return dstOp != HlslOp.Invalid;
        }

        internal int TryParseSingleOpExpression(ref TokenRunner runner, int precedence)
        {
            HlslOp dstOp;
            string dstOpCastName;
            int dstOpTokenId;
            int foundStructId;

            bool foundOp = TryParseNextOp(ref runner, out dstOp, out dstOpCastName, out dstOpTokenId, out foundStructId, precedence, false);
            if (foundOp)
            {
                bool isBinary = GetOperatorIsBinary(dstOp);
                if (isBinary)
                {
                    runner.Error("Should not find binary operator at leftmost side of sub-expression.");
                    foundOp = false;
                }
            }

            int foundExp = -1;
            if (foundOp)
            {
                int opPrecedence = GetOperatorPrcedence(dstOp);
                int lhsExp = ParseOperatorExpression(ref runner, opPrecedence);

                if (lhsExp >= 0)
                {
                    HlslTree.NodeExpression dstExp = new HlslTree.NodeExpression();
                    dstExp.lhsNodeId = lhsExp;
                    dstExp.rhsNodeId = -1;
                    dstExp.op = dstOp;
                    dstExp.opTokenId = dstOpTokenId;

                    dstExp.cstyleCastStructId = foundStructId;

                    foundExp = runner.parser.tree.AddNode(dstExp, this, runner.isValid);
                }
            }

            return foundExp;
        }


        // find either a token, or unary expressions and then a followup expression. it can't be a
        // binary expression because a subexpression needs to be to the left of it
        internal int ParseSingleExpression(ref TokenRunner runner, int precedence)
        {
            // copy for debugging
            TokenRunner debugCopy = runner.MakeCopy();

            TokenRunner singleOpRunner = runner.MakeCopy();
            int foundExpression = TryParseSingleOpExpression(ref singleOpRunner, precedence);

            if (foundExpression >= 0)
            {
                runner = singleOpRunner;
            }
            else
            {
                // is it native? if so, then it's a function cast or a constructor
                bool isTopNative = HlslTokenizer.IsTokenNativeType(runner.TopToken());

                // is it a struct? if so, then it's a function cast
                int topTokenStruct = runner.GetTokenStructType();

                if (isTopNative)
                {
                    HlslTree.NodeNativeConstructor node = new HlslTree.NodeNativeConstructor();
                    node.nativeType = tokenToNativeTable[runner.TopToken()];
                    node.typeTokenId = runner.currToken;

                    string typeName = runner.TopData();
                    runner.AdvanceToken();

                    runner.Expect(HlslToken._parense_l);
                    List<int> parsedParams = ParseParamList(ref runner);
                    runner.Expect(HlslToken._parense_r);

                    node.paramNodeIds = parsedParams.ToArray();

                    int numCols = HlslUtil.GetNumCols(node.nativeType);
                    foundExpression = runner.parser.tree.AddNode(node, this, runner.isValid);
                }
                else if (topTokenStruct >= 0)
                {
                    // function cast to struct
                    runner.Error("Unimplemented");
                }
                else if (runner.Check(HlslToken._identifier))
                {
                    int currTokenId = runner.currToken;

                    // it's an identifier, so it could either be a variable or a function call
                    int nameTokenId = runner.currToken;

                    string name = tokenizer.GetTokenData(nameTokenId);

                    int variableId;
                    HlslTree.VariableInfo variableInfo;
                    bool variableFound = tree.FindVariableInfo(out variableInfo, out variableId, name);

                    if (!variableFound)
                    {
                        // if we didn't find this variable name, then this might actually a function that we don't have
                        // a definition for.
                        runner.AdvanceToken();

                        int parenseTokenId = runner.currToken;
                        if (runner.Accept(HlslToken._parense_l))
                        {

                            HlslTree.NodeExpression node = new HlslTree.NodeExpression();

                            // if it's actually a function, then parse it as an external function that we treat as passthrough
                            List<int> parsedParams = ParseParamList(ref runner);

                            runner.Expect(HlslToken._parense_r);

                            node.lhsNodeId = CreateNodeForTokenId(ref runner, nameTokenId);

                            node.opTokenId = parenseTokenId;
                            node.op = HlslOp.FunctionalCall;

                            node.paramIds = parsedParams.ToArray();
                            foundExpression = runner.parser.tree.AddNode(node, this, runner.isValid);
                        }
                        else
                        {
                            runner.Error("Failed to find variable: " + name);
                        }
                    }
                    else
                    {
                        HlslTree.NodeVariable node = new HlslTree.NodeVariable();
                        node.nameTokenId = nameTokenId;
                        node.nameVariableId = variableId;
                        runner.AdvanceToken();

                        foundExpression = runner.parser.tree.AddNode(node, this, runner.isValid);
                    }
                }
                else if (runner.CheckLiteralOrBool())
                {
                    HlslTree.NodeLiteralOrBool node = new HlslTree.NodeLiteralOrBool();
                    node.nameTokenId = runner.currToken;
                    runner.AdvanceToken();

                    foundExpression = runner.parser.tree.AddNode(node, this, runner.isValid);
                }
                else if (runner.Check(HlslToken._parense_l))
                {
                    // accept '(', parse again with reset precedence, and accept ')'
                    runner.Accept(HlslToken._parense_l);

                    int childExpression = ParseExpression(ref runner);

                    HlslTree.NodeParenthesisGroup nodeExp = new HlslTree.NodeParenthesisGroup();
                    nodeExp.childNodeId = childExpression;

                    foundExpression = runner.parser.tree.AddNode(nodeExp, this, runner.isValid);

                    runner.Accept(HlslToken._parense_r);
                }
            }
            return foundExpression;
        }

        internal bool IsIdentifierFunctionCall(string identifier)
        {
            bool isTreeFunc = tree.IsIdentifierFunction(identifier);
            bool isUnityFunc = unityReserved.IsIdentifierUnityFunction(identifier);
            bool ret = (isTreeFunc || isUnityFunc);
            return ret;
        }

        internal bool IsFunctionCall(TokenRunner runner)
        {
            bool ret = false;

            TokenRunner lookAhead = runner.MakeCopy();
            if (lookAhead.Check(HlslToken._identifier))
            {
                string identifier = lookAhead.TopData();
                lookAhead.AdvanceToken();

                bool isFunctionCall = IsIdentifierFunctionCall(identifier);
                if (isFunctionCall)
                {
                    if (lookAhead.Expect(HlslToken._parense_l))
                    {
                        ret = true;
                    }
                }
            }

            return ret;
        }

        internal HlslParser.TypeInfo[] GetTypeInfoFromNodeArray(int[] nodes)
        {
            HlslParser.TypeInfo[] ret = new HlslParser.TypeInfo[nodes.Length];

            for (int i = 0; i < nodes.Length; i++)
            {
                int nodeId = nodes[i];
                if (nodeId >= 0)
                {
                    HlslTree.FullTypeInfo fti = tree.fullTypeInfo[nodeId];

                    ret[i].allowedState = ApdAllowedState.Any; // need to fix this?
                    ret[i].arrayDims = fti.arrayDims;
                    ret[i].nativeType = fti.nativeType;
                    ret[i].identifier = fti.identifier;
                }
            }

            return ret;
        }

        static int GetVectorLengthConversionCost(int wantedLen, int actualLen)
        {
            int ret = -1;
            if (wantedLen == actualLen)
            {
                // if they are exact, that's the best match
                ret = 0;
            }
            else if (wantedLen > 1 && actualLen == 1)
            {
                // we can promote from a scalar to a vector
                ret = 1;
            }
            else if (actualLen > wantedLen)
            {
                ret = 2;
            }
            else if (actualLen < wantedLen)
            {
                ret = 3;
            }
            else
            {
                HlslUtil.ParserAssert(false);
            }
            return ret;
        }

        static int GetBaseTypeConversionCost(int wantedType, int actualType)
        {
            // base type order:
            // -1: other
            //  0: bool
            //  1: int
            //  2: uint
            //  3: half
            //  4: float

            // in general, we would prefer to be exact, but otherwise going from high to low (i.e. float to half)
            // is better than converting from low to high (i.e. half to float)
            int ret = 0;
            if (wantedType == actualType)
            {
                // if they are the same, that's the best
                ret = 0;
            }
            else if (actualType > wantedType)
            {
                ret = actualType - wantedType;
            }
            else
            {
                ret = 5 + (wantedType - actualType);
            }

            return ret;
        }

        internal bool IsParamListBetter(HlslParser.TypeInfo[] actualParams, HlslParser.TypeInfo[] bestProto, HlslParser.TypeInfo[] currProto)
        {
            bool isCurrBetter = false;

            for (int paramIter = 0; paramIter < actualParams.Length; paramIter++)
            {
                HlslParser.TypeInfo actual = actualParams[paramIter];
                HlslParser.TypeInfo best = bestProto[paramIter];
                HlslParser.TypeInfo curr = currProto[paramIter];

                HlslNativeType actualBaseType = HlslUtil.GetNativeBaseType(actual.nativeType);
                HlslNativeType bestBaseType = HlslUtil.GetNativeBaseType(best.nativeType);
                HlslNativeType currBaseType = HlslUtil.GetNativeBaseType(curr.nativeType);

                int actualCols = HlslUtil.GetNumCols(actual.nativeType);
                int actualRows = HlslUtil.GetNumRows(actual.nativeType);

                int bestCols = HlslUtil.GetNumCols(best.nativeType);
                int bestRows = HlslUtil.GetNumRows(best.nativeType);

                int currCols = HlslUtil.GetNumCols(curr.nativeType);
                int currRows = HlslUtil.GetNumRows(curr.nativeType);

                bool isActualScalar = HlslUtil.IsNativeTypeScalar(actualBaseType);
                bool isBestScalar = HlslUtil.IsNativeTypeScalar(bestBaseType);
                bool isCurrScalar = HlslUtil.IsNativeTypeScalar(currBaseType);

                // 1. If one type is a scalar and the other is not, bias towards that.
                if (isBestScalar != isCurrScalar)
                {
                    isCurrBetter = (isCurrScalar == isActualScalar);
                    break;
                }

                bool isBestExact = (best.nativeType == actual.nativeType);
                bool isCurrExact = (curr.nativeType == actual.nativeType);

                // 2. If one is exact and the other is not, then we have an easy choice
                if (isBestExact && !isCurrExact)
                {
                    isCurrBetter = false;
                    break;
                }

                if (!isBestExact && isCurrExact)
                {
                    isCurrBetter = true;
                    break;
                }

                if (!isActualScalar)
                {
                    // If the param type is not a scalar not much we can
                    // do unless one of them is exact and the other is not.
                    // Best we can do is move onto the next param.
                }
                else
                {
                    int bestCostRows = GetVectorLengthConversionCost(actualRows, bestRows);
                    int bestCostCols = GetVectorLengthConversionCost(actualRows, bestCols);

                    int currCostRows = GetVectorLengthConversionCost(actualRows, currRows);
                    int currCostCols = GetVectorLengthConversionCost(actualRows, currCols);

                    if (bestCostRows != currCostRows)
                    {
                        isCurrBetter = currCostRows < bestCostRows;
                        break;
                    }

                    if (bestCostCols != currCostCols)
                    {
                        isCurrBetter = currCostCols < bestCostCols;
                        break;
                    }

                    int actualIndexHelper = HlslUtil.GetIndexHelperForBaseType(actualBaseType);
                    int bestIndexHelper = HlslUtil.GetIndexHelperForBaseType(bestBaseType);
                    int currIndexHelper = HlslUtil.GetIndexHelperForBaseType(currBaseType);

                    int bestCostType = GetBaseTypeConversionCost(actualIndexHelper, bestIndexHelper);
                    int currCostType = GetBaseTypeConversionCost(actualIndexHelper, currIndexHelper);

                    if (bestCostType != currCostType)
                    {
                        isCurrBetter = currCostType < bestCostType;
                        break;
                    }
                }
            }
            return isCurrBetter;
        }

        internal bool IsValidFunctionParamList(HlslParser.TypeInfo[] actualParams, HlslParser.TypeInfo[] protoParams)
        {
            // todo, for now just be extremely permissive
            bool isValid = (protoParams.Length == actualParams.Length);

            for (int paramIter = 0; paramIter < protoParams.Length; paramIter++)
            {
                // We're going to be extremely permissive here because we allow unknown functions that could return anything.
                // The only rule is that if both the prototype and actual args are numeric types (float, int2, half3x3, etc),
                // then the actualParam can be promototed to larger type, but can not be demoted down. I.e. float->float4 is
                // legal as in implicit conversion but float4->float is not.

                HlslParser.TypeInfo protoType = protoParams[paramIter];
                HlslParser.TypeInfo actualType = actualParams[paramIter];

                int protoRows = HlslUtil.GetNumRows(protoType.nativeType);
                int protoCols = HlslUtil.GetNumCols(protoType.nativeType);

                // non-numeric types have rows/cols set to 0.
                if (protoRows >= 1 && protoCols >= 1)
                {
                    int actualRows = HlslUtil.GetNumRows(actualType.nativeType);
                    int actualCols = HlslUtil.GetNumCols(actualType.nativeType);

                    if (protoRows < actualRows || protoCols < actualCols)
                    {
                        isValid = false;
                        break;
                    }
                }
            }

            return isValid;
        }

        internal int FindBestFunctionCallForPrototype(string identifier, int[] parsedParams, bool isValid)
        {
            int ret = -1;

            bool isTreeFunc = tree.IsIdentifierFunction(identifier);
            bool isUnityFunc = unityReserved.IsIdentifierUnityFunction(identifier);

            if (isTreeFunc)
            {
                HlslParser.TypeInfo[] parsedTypeInfo = GetTypeInfoFromNodeArray(parsedParams);

                // find the node id
                int[] prototypeIds = tree.GetNodePrototypeIdsForFunction(identifier);

                int bestMatch = -1;
                HlslParser.TypeInfo[] bestParamInfo = new HlslParser.TypeInfo[0];
                for (int protoIter = 0; protoIter < prototypeIds.Length; protoIter++)
                {
                    int protoFlatId = prototypeIds[protoIter];
                    int protoNodeId = tree.parsedFuncStructData.allPrototypes[protoFlatId].uniqueId;

                    HlslTree.Node baseNode = tree.allNodes[protoNodeId];
                    if (baseNode is HlslTree.NodeFunctionPrototype)
                    {
                        HlslTree.NodeFunctionPrototype protoNode = (HlslTree.NodeFunctionPrototype)baseNode;

                        // no default params, so size must exactly match
                        if (protoNode.declarations.Length == parsedParams.Length)
                        {
                            // get the parameter types
                            HlslParser.TypeInfo[] protoDeclInfo = GetTypeInfoFromNodeArray(protoNode.declarations);

                            bool isMatch = IsValidFunctionParamList(parsedTypeInfo, protoDeclInfo);
                            if (isMatch)
                            {
                                bool isBetter = false;
                                if (bestMatch < 0)
                                {
                                    isBetter = true;
                                }
                                else
                                {
                                    isBetter = IsParamListBetter(parsedTypeInfo, bestParamInfo, protoDeclInfo);
                                }

                                if (isBetter)
                                {
                                    bestMatch = protoNodeId;
                                    bestParamInfo = protoDeclInfo;
                                }
                            }
                        }
                    }
                    else
                    {
                        HlslUtil.ParserAssert(false);
                    }
                }

                ret = bestMatch;
            }
            else if (isUnityFunc)
            {
                HlslParser.TypeInfo[] parsedTypeInfo = GetTypeInfoFromNodeArray(parsedParams);

                // find the node id
                int[] prototypeIds = unityReserved.GetPrototypeIdsForFunction(identifier);

                int bestMatchProtoId = -1;
                HlslUtil.PrototypeInfo bestProtoInfo = new HlslUtil.PrototypeInfo();

                HlslParser.TypeInfo[] bestParamInfo = new HlslParser.TypeInfo[0];

                for (int protoIter = 0; protoIter < prototypeIds.Length; protoIter++)
                {
                    int protoId = prototypeIds[protoIter];
                    HlslUtil.PrototypeInfo protoInfo = unityReserved.parsedFuncStructData.allPrototypes[protoId];
                    HlslParser.TypeInfo[] protoDeclInfo = protoInfo.paramInfoVec;

                    bool isMatch = IsValidFunctionParamList(parsedTypeInfo, protoDeclInfo);
                    if (isMatch)
                    {
                        bool isBetter = false;
                        if (bestMatchProtoId < 0)
                        {
                            isBetter = true;
                        }
                        else
                        {
                            isBetter = IsParamListBetter(parsedTypeInfo, bestParamInfo, protoDeclInfo);
                        }

                        if (isBetter)
                        {
                            bestMatchProtoId = protoId;
                            bestParamInfo = protoDeclInfo;
                            bestProtoInfo = protoInfo;
                        }
                    }

                }

                if (bestMatchProtoId >= 0)
                {
                    // create a node for this prototype
                    HlslTree.NodeFunctionPrototypeExternal proto = new HlslTree.NodeFunctionPrototypeExternal();

                    proto.protoInfo = bestProtoInfo;

                    ret = tree.AddNode(proto, this, isValid);
                }

            }
            else
            {
                // no op? maybe we should put an error here?
            }

            return ret;
        }

        internal List<int> ParseParamList(ref TokenRunner runner)
        {
            List<int> parsedParams = new List<int>();

            bool isFirst = true;
            while (runner.isValid &&
                !runner.Check(HlslToken._eof) &&
                !runner.Check(HlslToken._parense_r))
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    runner.Expect(HlslToken._comma);
                }

                int foundExp = ParseExpression(ref runner);
                parsedParams.Add(foundExp);
            }

            return parsedParams;
        }

        internal int ParseFunctionCall(ref TokenRunner runner)
        {
            string identifier = runner.TopData();
            runner.AdvanceToken();

            bool isFunctionCall = IsIdentifierFunctionCall(identifier);
            if (!isFunctionCall)
            {
                runner.Error("Expected function call.");
            }

            runner.Expect(HlslToken._parense_l);

            bool startRunnerValid = runner.isValid;
            TokenRunner copyRunner = runner.MakeCopy();

            List<int> parsedParams = ParseParamList(ref runner);
            runner.Expect(HlslToken._parense_r);

            if (startRunnerValid && !runner.isValid)
            {
                runner = copyRunner;
                parsedParams = ParseParamList(ref runner);
                runner.Expect(HlslToken._parense_r);
            }



            int dstId = -1;

            if (runner.isValid)
            {
                HlslTree.NodeExpression node = new HlslTree.NodeExpression();

                node.lhsNodeId = FindBestFunctionCallForPrototype(identifier, parsedParams.ToArray(), runner.isValid);

                node.op = HlslOp.FunctionalCall;
                node.paramIds = parsedParams.ToArray();

                dstId = tree.AddNode(node, this, runner.isValid);
            }
            return dstId;
        }

        static internal HlslOp GetTokenLeftSideOperator(HlslToken token)
        {
            HlslOp op = HlslOp.Invalid;

            switch (token)
            {
                case HlslToken._plus_plus:         // ++
                    op = HlslOp.PreIncrement;
                    break;
                case HlslToken._minus_minus:       // --
                    op = HlslOp.PreDecrement;
                    break;
                case HlslToken._plus:              // +
                    op = HlslOp.UnaryPlus;
                    break;
                case HlslToken._minus:             // -
                    op = HlslOp.UnaryMinus;
                    break;
                case HlslToken._logical_not:       // !
                    op = HlslOp.LogicalNot;
                    break;
                case HlslToken._bitwise_not:       // ~
                    op = HlslOp.BitwiseNot;
                    break;
                default:
                    op = HlslOp.Invalid;
                    break;
            }

            return op;
        }

        internal int ParseMemberAccessRhs(ref TokenRunner runner, int lhsExp)
        {
            int ret = -1;

            // should be an identifier
            if (runner.Check(HlslToken._identifier))
            {
                // get the type of the parent expression
                HlslTree.Node lhsNode = runner.parser.tree.allNodes[lhsExp];

                HlslTree.FullTypeInfo fti = runner.parser.tree.fullTypeInfo[lhsExp];

                string rhsName = runner.TopData();

                if (fti.nativeType == HlslNativeType._struct)
                {
                    int structNodeId = runner.GetStructDefinitionFromString(fti.identifier);

                    // is this a struct member or function
                    int fieldIndex = -1;
                    int protoIndex = -1;
                    tree.DoesStructHaveDeclaration(out fieldIndex, out protoIndex, structNodeId, rhsName, unityReserved);

                    // we might find one or the other, but we should never find both
                    HlslUtil.ParserAssert(fieldIndex < 0 || protoIndex < 0);

                    HlslUtil.StructInfo structInfo = tree.GetStructInfoFromNodeId(structNodeId, unityReserved);

                    if (fieldIndex >= 0)
                    {
                        HlslTree.NodeMemberVariable nodeMemberVariable = new HlslTree.NodeMemberVariable();
                        nodeMemberVariable.structNodeId = structNodeId;
                        nodeMemberVariable.fieldIndex = fieldIndex;

                        int dstNodeId = tree.AddNode(nodeMemberVariable, this, runner.isValid);
                        ret = dstNodeId;

                        runner.AdvanceToken();
                    }
                    else if (protoIndex >= 0)
                    {
                        HlslTree.NodeMemberFunction nodeMemberFunc = new HlslTree.NodeMemberFunction();
                        nodeMemberFunc.structNodeId = structNodeId;
                        nodeMemberFunc.funcIndex = protoIndex;
                        runner.AdvanceToken();

                        // parse params

                        //dstExp.rhsNodeId = dstNodeId;

                        runner.Expect(HlslToken._parense_l);
                        List<int> parsedParams = ParseParamList(ref runner);
                        runner.Expect(HlslToken._parense_r);

                        nodeMemberFunc.funcParamNodeIds = parsedParams.ToArray();

                        ret = tree.AddNode(nodeMemberFunc, this, runner.isValid);

                    }
                    else
                    {
                        HlslUtil.StructInfo debugInfo = tree.GetStructInfoFromNodeId(structNodeId, unityReserved);

                        runner.Error("Failed to identify right side of member access operator.");
                    }
                }
                else
                {
                    bool isValidSwizzleType = HlslUtil.IsValidSwizzleType(fti.nativeType);
                    if (isValidSwizzleType)
                    {
                        int swizzleLength = HlslUtil.GetSwizzleLength(rhsName, fti.nativeType);
                        if (swizzleLength >= 1)
                        {
                            HlslTree.NodeSwizzle nodeSwizzle = new HlslTree.NodeSwizzle();
                            nodeSwizzle.swizzle = rhsName;
                            int dstNodeId = tree.AddNode(nodeSwizzle, this, runner.isValid);
                            ret = dstNodeId;

                            runner.AdvanceToken();
                        }
                        else
                        {
                            runner.Error("Invalid swizzle.");
                        }
                    }
                }
            }
            else
            {
                runner.Error("Invalid token for member access.");
            }

            return ret;
        }

        internal int ParseOperatorExpression(ref TokenRunner runner, int precedence)
        {
            // Making a copy for debugging. We only keep track of the current top token, but
            // store the token when entering the function to make it easier to debug callstacks.
            TokenRunner debugCopy = runner.MakeCopy();

            // First, check if this is a function call, which we can do by checking if the
            // the starting token is an identifier and it's a valid function identifier. Note
            // that we would have to modify this logic if we were to allow the scope resolution
            // operator as in:
            //     y = foo::bar(x);
            // In that case, we would need a scope resolution node that merges foo::bar.

            bool isFunctionCall = IsFunctionCall(runner);

            int fullExp = -1;
            if (isFunctionCall)
            {
                fullExp = ParseFunctionCall(ref runner);
            }
            else
            {
                // if it's not a function call, try to find the left side
                fullExp = ParseSingleExpression(ref runner, precedence);

                if (runner.Check(HlslToken._parense_l))
                {
                    // could be either a function or a function cast

                    // skip the '('
                    runner.Expect(HlslToken._parense_l);

                    // is it native? if so, then it's a function cast
                    bool isTopNative = HlslTokenizer.IsTokenNativeType(runner.TopToken());

                    // is it a struct? if os, then it's a function cast
                    int topTokenStruct = runner.GetTokenStructType();

                    // otherwise is an identifier? if so, then it's a function.
                    bool isTopIdentifier = runner.Check(HlslToken._identifier);

                    int opTokenId = runner.currToken;

                    if (isTopNative || (topTokenStruct >= 0))
                    {
                        runner.Error("Unimplemented");
                    }
                    else if (isTopIdentifier)
                    {
                        string identifier = runner.TopData();
                        bool isLegalFunctionCall = IsIdentifierFunctionCall(identifier);

                        runner.Error("Unimplemented");
                    }
                    else
                    {
                        // failed to parse
                        runner.Error("Failed to parse either c-style cast or function call.");
                    }

                    runner.Expect(HlslToken._parense_r);
                }
            }

            // try to parse any operators to the right of this expression
            {
                bool done = false;

                while (!done)
                {
                    HlslOp dstOp;
                    string dstOpCastName;
                    int dstOpTokenId;
                    int foundStructId;

                    TokenRunner rollbackRunner = runner.MakeCopy();

                    bool foundOp = TryParseNextOp(ref runner, out dstOp, out dstOpCastName, out dstOpTokenId, out foundStructId, precedence, true);
                    if (foundOp)
                    {
                        bool isBinary = GetOperatorIsBinary(dstOp);

                        if (dstOp == HlslOp.TernaryQuestion)
                        {
                            int lhsExp = fullExp;
                            int opPrecedence = GetOperatorPrcedence(dstOp);

                            int rhsExp = ParseOperatorExpression(ref runner, opPrecedence);
                            if (rhsExp < 0)
                            {
                                runner.Error("Could not parse middle of ternary expression.");
                            }
                            runner.Expect(HlslToken._colon);

                            int rhsRhsExp = ParseOperatorExpression(ref runner, opPrecedence);
                            if (rhsRhsExp < 0)
                            {
                                runner.Error("Could not parse right of ternary expression.");
                            }

                            HlslTree.NodeExpression dstExp = new HlslTree.NodeExpression();

                            // if it's a single operator then apply it
                            dstExp.lhsNodeId = lhsExp;
                            dstExp.rhsNodeId = rhsExp;
                            dstExp.rhsRhsNodeId = rhsRhsExp;
                            dstExp.op = dstOp;
                            dstExp.opTokenId = dstOpTokenId;

                            fullExp = runner.parser.tree.AddNode(dstExp, this, runner.isValid);


                        }
                        else if (dstOp == HlslOp.Subscript)
                        {
                            int lhsExp = fullExp;

                            // parse with reset precedence
                            int rhsExp = ParseExpression(ref runner);

                            runner.Expect(HlslToken._bracket_r);

                            HlslTree.NodeExpression dstExp = new HlslTree.NodeExpression();
                            dstExp.lhsNodeId = lhsExp;
                            dstExp.rhsNodeId = rhsExp;
                            dstExp.op = dstOp;
                            dstExp.opTokenId = dstOpTokenId;

                            fullExp = runner.parser.tree.AddNode(dstExp, this, runner.isValid);
                        }
                        else if (isBinary)
                        {
                            int opPrecedence = GetOperatorPrcedence(dstOp);

                            int rhsExp = ParseOperatorExpression(ref runner, opPrecedence);
                            if (rhsExp < 0)
                            {
                                runner.Error("Could not find right side expression.");
                            }
                            else
                            {
                                // if it's a single operator then apply it
                                int lhsExp = fullExp;
                                HlslTree.NodeExpression dstExp = new HlslTree.NodeExpression();
                                dstExp.lhsNodeId = lhsExp;
                                dstExp.rhsNodeId = rhsExp;
                                dstExp.op = dstOp;
                                dstExp.opTokenId = dstOpTokenId;

                                fullExp = runner.parser.tree.AddNode(dstExp, this, runner.isValid);
                            }
                        }
                        else
                        {
                            int lhsExp = fullExp;

                            // if it's a single operator then apply it
                            HlslTree.NodeExpression dstExp = new HlslTree.NodeExpression();
                            dstExp.lhsNodeId = lhsExp;
                            dstExp.op = dstOp;
                            dstExp.opTokenId = dstOpTokenId;


                            if (dstOp == HlslOp.MemberAccess)
                            {
                                HlslToken topToken = runner.TopToken();
                                TokenRunner copy = runner.MakeCopy();

                                dstExp.rhsNodeId = ParseMemberAccessRhs(ref runner, lhsExp);


                                if (runner.isValid && dstExp.rhsNodeId < 0)
                                {
                                    runner = copy;
                                    dstExp.rhsNodeId = ParseMemberAccessRhs(ref runner, lhsExp);

                                    HlslUtil.ParserAssert(false);
                                }
                            }
                            else
                            {
                            }

                            fullExp = runner.parser.tree.AddNode(dstExp, this, runner.isValid);
                        }
                    }
                    else
                    {
                        // if we didn't find a parens, and we didn't find an op, then we are done
                        done = true;
                    }
                }
            }

            return fullExp;
        }

        internal int ParseExpression(ref TokenRunner runner)
        {
            int ret = ParseOperatorExpression(ref runner, GetWeakestPrcedence());
            return ret;
        }

        // For a declaration, parse only the type and identifer. Ignore the trailing comma/semicolon for function params/variables
        // and any initializers. If skipAddNode is true, simply parse but don't actually create any nodes.
        internal void ParseDeclTypeAndIdentifier(ref HlslTree.NodeDeclaration nodeDecl, ref TokenRunner runner, bool skipAddNode)
        {
            string topTokenText = runner.TopData();
            bool isMacroTypeDecl = unityReserved.IsIdentifierMacroDecl(topTokenText);

            if (isMacroTypeDecl)
            {
                ParseMacroDeclaration(out nodeDecl, ref runner);
            }
            else
            {
                int topTokenStruct = runner.GetTokenStructType();

                if (skipAddNode)
                {
                    nodeDecl.typeNodeId = -1;
                }
                else
                {
                    nodeDecl.typeNodeId = CreateNodeForTopToken(ref runner);
                }

                if (HlslTokenizer.IsTokenNativeType(runner.TopToken()))
                {
                    nodeDecl.nativeType = tokenToNativeTable[runner.TopToken()];
                }
                else
                {
                    nodeDecl.structId = topTokenStruct;
                    nodeDecl.nativeType = HlslNativeType._struct;
                }
                nodeDecl.debugType = runner.TopData();

                runner.AdvanceToken();

                if (runner.Accept(HlslToken._less_than))
                {
                    if (skipAddNode)
                    {
                        // this should be a native type or identifier for struct
                        nodeDecl.subTypeNodeId = CreateNodeForTopToken(ref runner);
                    }
                    else
                    {
                        nodeDecl.subTypeNodeId = -1;
                    }

                    runner.Expect(HlslToken._greater_than);
                }

                // next token should be the name
                runner.Check(HlslToken._identifier);
                nodeDecl.nameTokenId = runner.currToken;
                nodeDecl.variableName = runner.TopData();

                runner.AdvanceToken();

                List<int> dims = new List<int>();

                // do we have a bracket? If so, it's an array.
                while (runner.Accept(HlslToken._bracket_l))
                {
                    // a ']' means implicit size based on the initialzier, otherwise
                    // we have an expression here
                    int currExp = -1;
                    if (!runner.Check(HlslToken._bracket_r))
                    {
                        currExp = ParseExpression(ref runner);
                    }

                    dims.Add(currExp);

                    // we should expect a ']' on the end
                    runner.Expect(HlslToken._bracket_r);

                    // if we have another '[' then the while loop will continue, otherwise we are done.
                }

                nodeDecl.arrayDims = dims.ToArray();
            }
        }

        internal int ParseBlockOrStatement(ref TokenRunner runner)
        {
            int ret = -1;
            if (runner.Check(HlslToken._brace_l))
            {
                List<HlslTree.VariableInfo> ignoredVariables = new List<HlslTree.VariableInfo>();
                ret = ParseBlock(ref runner, ignoredVariables);
            }
            else
            {
                ret = ParseStatement(ref runner);
            }
            return ret;
        }

        // Parse the initializer, such as:
        //    float3 val = { 0, foo*4, zelp.x };
        // These can also be nested. For simplicity, we are going to parse the types blind and
        // force them to FPD.
        internal int ParseBlockInitializer(ref TokenRunner runner)
        {
            int ret = -1;
            runner.Expect(HlslToken._brace_l);

            List<int> initList = new List<int>();

            bool done = false;
            while (!done)
            {
                if (initList.Count > 0)
                {
                    runner.Expect(HlslToken._comma);
                }

                int currChild = -1;
                if (runner.Check(HlslToken._brace_l))
                {
                    // parse child block
                    currChild = ParseBlockInitializer(ref runner);
                }
                else
                {
                    // parse an expression
                    currChild = ParseExpression(ref runner);
                }

                if (currChild < 0)
                {
                    done = true;
                }

                if (runner.IsEof() || runner.Check(HlslToken._brace_r))
                {
                    done = true;
                }

                initList.Add(currChild);
            }
            runner.Expect(HlslToken._brace_r);

            HlslTree.NodeBlockInitializer blockInit = new HlslTree.NodeBlockInitializer();
            blockInit.initNodeIds = initList.ToArray();

            ret = runner.parser.tree.AddNode(blockInit, this, runner.isValid);

            return ret;
        }

        internal int ParseStatement(ref TokenRunner runner)
        {
            int foundExp = -1;

            int topTokenStruct = runner.GetTokenStructType();

            if (topTokenStruct >= 0 || HlslTokenizer.IsTokenNativeType(runner.TopToken()))
            {
                HlslTree.NodeDeclaration nodeDecl = new HlslTree.NodeDeclaration();

                ParseDeclTypeAndIdentifier(ref nodeDecl, ref runner, false);

                // check if we have an initializer
                if (runner.Accept(HlslToken._assignment))
                {
                    int rhsExp = -1;
                    if (runner.Check(HlslToken._brace_l))
                    {
                        rhsExp = ParseBlockInitializer(ref runner);
                    }
                    else
                    {
                        rhsExp = ParseExpression(ref runner);
                    }

                    nodeDecl.initializerId = rhsExp;
                }

                runner.Expect(HlslToken._semicolon);

                foundExp = runner.parser.tree.AddNode(nodeDecl, this, runner.isValid);

                HlslTree.VariableInfo variableInfo = GetVariableInfoFromDecl(nodeDecl, foundExp);

                if (!variableInfo.IsValid())
                {
                    runner.Error("Invalid variable.");
                }
                else
                {
                    // add type for scope
                    tree.AddVariableIdentifier(variableInfo);
                }
            }
            else if (runner.Check(HlslToken._return))
            {
                // skip the return
                runner.Expect(HlslToken._return);

                int expression = ParseExpression(ref runner);

                runner.Expect(HlslToken._semicolon);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.expression = expression;
                node.type = HlslStatementType.Return;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._while))
            {
                // skip the while
                runner.Expect(HlslToken._while);

                // parse the expression in the while
                runner.Expect(HlslToken._parense_l);

                int whileExp = ParseExpression(ref runner);
                runner.Expect(HlslToken._parense_r);

                int childNode = ParseBlockOrStatement(ref runner);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.expression = whileExp;
                node.childBlockOrStatement = childNode;

                node.type = HlslStatementType.While;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._do))
            {
                // skip the do
                runner.Expect(HlslToken._do);

                int childNode = ParseBlockOrStatement(ref runner);

                runner.Expect(HlslToken._while);

                // parse the expression in the while
                runner.Expect(HlslToken._parense_l);

                int whileExp = ParseExpression(ref runner);
                runner.Expect(HlslToken._parense_r);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.expression = whileExp;
                node.childBlockOrStatement = childNode;

                node.type = HlslStatementType.Do;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._for))
            {
                // skip the do
                runner.Expect(HlslToken._for);
                runner.Expect(HlslToken._parense_l);

                int[] forExps = new int[3] { -1, -1, -1 };
                forExps[0] = ParseExpression(ref runner);
                runner.Expect(HlslToken._semicolon);
                forExps[1] = ParseExpression(ref runner);
                runner.Expect(HlslToken._semicolon);
                forExps[2] = ParseExpression(ref runner);
                runner.Expect(HlslToken._parense_r);

                // Quick note on scope. The declarations in forExps[0] will be put in the outer block scope
                // instead of just inside the for loop's block scope. Of course, that is wrong by c++
                // standard but correct (for better or for worse) for hlsl.
                int childNode = ParseBlockOrStatement(ref runner);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.expression = -1;
                node.forExpressions = forExps;
                node.childBlockOrStatement = childNode;
                node.type = HlslStatementType.For;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._if))
            {
                runner.Expect(HlslToken._if);
                runner.Expect(HlslToken._parense_l);
                int expression = ParseExpression(ref runner);
                runner.Expect(HlslToken._parense_r);
                int childNode = ParseBlockOrStatement(ref runner);

                int childElse = -1;
                if (runner.Accept(HlslToken._else))
                {
                    childElse = ParseBlockOrStatement(ref runner);
                }

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.expression = expression;
                node.elseBlockOrStatement = childElse;
                node.childBlockOrStatement = childNode;
                node.type = HlslStatementType.If;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._continue))
            {
                runner.Expect(HlslToken._continue);
                runner.Expect(HlslToken._semicolon);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.type = HlslStatementType.Continue;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._switch))
            {
                runner.Expect(HlslToken._switch);
                runner.Expect(HlslToken._parense_l);
                int expression = ParseExpression(ref runner);
                runner.Expect(HlslToken._parense_r);

                List<HlslTree.VariableInfo> ignoredParams = new List<HlslTree.VariableInfo>();
                int childBlock = ParseBlock(ref runner, ignoredParams);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.type = HlslStatementType.Switch;
                node.expression = expression;
                node.childBlockOrStatement = childBlock;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._break))
            {
                runner.Expect(HlslToken._break);
                runner.Expect(HlslToken._semicolon);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.type = HlslStatementType.Break;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._default))
            {
                runner.Expect(HlslToken._default);
                runner.Expect(HlslToken._colon);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.type = HlslStatementType.Default;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else if (runner.Check(HlslToken._goto))
            {
                runner.Expect(HlslToken._goto);

                int labelExpression = CreateNodeForTopToken(ref runner);
                runner.Expect(HlslToken._identifier);

                runner.Expect(HlslToken._semicolon);

                HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                node.type = HlslStatementType.Goto;
                node.expression = labelExpression;

                foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
            }
            else
            {
                // We are either an expression or a label. Check 1 token ahead to see.
                HlslToken currToken = runner.TopToken();
                HlslToken nextToken = runner.LookAheadToken(1);

                if (currToken == HlslToken._identifier && nextToken == HlslToken._colon)
                {
                    int labelExpression = CreateNodeForTopToken(ref runner);
                    runner.Expect(HlslToken._identifier);
                    runner.Expect(HlslToken._colon);

                    HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                    node.type = HlslStatementType.Label;
                    node.expression = labelExpression;

                    foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
                }
                else
                {
                    int expression = ParseExpression(ref runner);

                    runner.Expect(HlslToken._semicolon);

                    HlslTree.NodeStatement node = new HlslTree.NodeStatement();
                    node.expression = expression;
                    node.type = HlslStatementType.Expression;

                    foundExp = runner.parser.tree.AddNode(node, this, runner.isValid);
                }
            }

            return foundExp;
        }

        internal int ParseBlock(ref TokenRunner runner, List<HlslTree.VariableInfo> protoVariables)
        {
            bool preprocStatus = runner.allowPreprocessor;
            runner.allowPreprocessor = false;

            HlslTree.NodeBlock block = new HlslTree.NodeBlock();

            runner.Expect(HlslToken._brace_l);
            if (runner.isValid)
            {
                runner.parser.tree.PushScope();

                // add the prototype variables to the scope
                for (int i = 0; i < protoVariables.Count; i++)
                {
                    tree.AddVariableIdentifier(protoVariables[i]);
                }

                List<int> statements = new List<int>();
                while (runner.isValid && !runner.Check(HlslToken._brace_r))
                {
                    //int statement = ParseStatement(ref runner);
                    int statement = ParseBlockOrStatement(ref runner);

                    if (statement >= 0)
                    {
                        statements.Add(statement);
                    }
                    else
                    {
                        runner.Error("Failed to parse statement.");
                    }
                }

                block.statements = statements.ToArray();

                runner.parser.tree.PopScope();
            }

            // the status gets checked for the following token, so revert status before parsing the }
            runner.allowPreprocessor = preprocStatus;


            runner.Expect(HlslToken._brace_r);

            int addedBlock = -1;
            if (runner.isValid)
            {
                addedBlock = tree.AddNode(block, this, runner.isValid);
            }

            return addedBlock;
        }

        internal int ParseFunction(ref TokenRunner runner)
        {
            int foundId = -1;

            List<HlslTree.VariableInfo> foundVariables;
            int prototypeId = ParseFunctionPrototype(ref runner, out foundVariables);
            if (prototypeId >= 0)
            {
                HlslUtil.ParserAssert(tree.allNodes[prototypeId] is HlslTree.NodeFunctionPrototype);

                // is this one of our ignored funcs?
                bool isIgnored = false;
                string funcName;
                {
                    HlslTree.NodeFunctionPrototype nodeProto = (HlslTree.NodeFunctionPrototype)tree.allNodes[prototypeId];

                    funcName = tokenizer.GetTokenData(nodeProto.nameTokenId);
                    if (ignoredFuncs.Contains(funcName))
                    {
                        // trigger an error, which will cause the parsing of this function to faile which will
                        // cause it to instead match braces.
                        runner.Error("Ignoring function: " + funcName);
                        isIgnored = true;
                    }
                }

                if (!isIgnored)
                {
                    //bool preprocessorStatus = runner.allowPreprocessor;
                    //runner.allowPreprocessor = false; // preprocessor commands are not allowed inside functions

                    int blockId = ParseBlock(ref runner, foundVariables);
                    if (blockId >= 0)
                    {
                        HlslTree.NodeFunction node = new HlslTree.NodeFunction();

                        node.prototypeId = prototypeId;
                        node.blockId = blockId;

                        foundId = runner.parser.tree.AddNode(node, this, runner.isValid);
                    }

                    //runner.allowPreprocessor = preprocessorStatus; // revert to previous state
                }
            }

            return foundId;
        }

        internal int ParseFunctionMatchOnly(ref TokenRunner runner, string parseErr)
        {
            int foundId = -1;

            List<HlslTree.VariableInfo> foundVariables;
            int prototypeId = ParseFunctionPrototype(ref runner, out foundVariables);
            if (prototypeId >= 0)
            {

                int firstToken = runner.currToken;

                runner.Expect(HlslToken._brace_l);

                int stackSize = 1;
                while (runner.isValid && runner.TopToken() != HlslToken._eof && stackSize > 0)
                {
                    HlslToken currType = runner.TopToken();
                    if (currType == HlslToken._brace_l)
                    {
                        stackSize++;
                    }
                    if (currType == HlslToken._brace_r)
                    {
                        stackSize--;
                    }
                    runner.AdvanceToken();
                }

                if (stackSize > 0)
                {
                    runner.Error("Failed to find closing brace");
                }

                int lastToken = runner.currToken;

                // Note that we need to include everything in the range [firstToken,lastToken). The while loop
                // skips whitespace tokens but we need to include them here.
                int[] foundTokens = new int[lastToken - firstToken];
                for (int i = 0; i < foundTokens.Length; i++)
                {
                    foundTokens[i] = firstToken + i;
                }

                HlslTree.NodePassthrough nodePassthrough = new HlslTree.NodePassthrough();
                nodePassthrough.tokenIds = foundTokens;
                nodePassthrough.writeDirect = true;

                int blockId = tree.AddNode(nodePassthrough, this, runner.isValid);
                {
                    HlslTree.NodeFunction node = new HlslTree.NodeFunction();

                    node.prototypeId = prototypeId;
                    node.blockId = blockId;
                    node.parseErr = parseErr;

                    foundId = runner.parser.tree.AddNode(node, this, runner.isValid);
                }
            }

            return foundId;
        }

        internal int ParseFunctionWithFallback(ref TokenRunner outerRunner)
        {
            TokenRunner currRunner = outerRunner.MakeCopy();
            int funcId = ParseFunction(ref currRunner);
            if (currRunner.isValid)
            {
                outerRunner = currRunner;
            }
            else
            {
                string err = currRunner.lastErr;

                TokenRunner matchRunner = outerRunner.MakeCopy();
                funcId = ParseFunctionMatchOnly(ref matchRunner, err);

                outerRunner = matchRunner;

                // remove errors
                tree.ResetErrors();
            }

            return funcId;
        }

        internal bool IsTopTokenCustomGlobalMacro(TokenRunner runner)
        {
            // Is the current token a valid top-level macro in a shader, such as CBUFFER_START? These
            // have special parsing rules.

            string topTokenText = runner.TopData();

            bool ret = false;
            if (string.Equals(topTokenText, "CBUFFER_START") ||
                string.Equals(topTokenText, "CBUFFER_END"))
            {
                ret = true;
            }

            bool isMacroTypeDecl = unityReserved.IsIdentifierMacroDecl(topTokenText);
            if (isMacroTypeDecl)
            {
                ret = true;
            }

            return ret;
        }

        internal int CreatePassThroughForRunnerRange(TokenRunner startRunner, TokenRunner endRunner)
        {
            int firstToken = startRunner.currToken;
            int endToken = endRunner.currToken;

            List<int> tokenIds = new List<int>();
            for (int i = startRunner.currToken; i < endRunner.currToken; i++)
            {
                tokenIds.Add(i);
            }

            HlslTree.NodePassthrough passthrough = new HlslTree.NodePassthrough();
            passthrough.tokenIds = tokenIds.ToArray();

            int passthroughId = tree.AddNode(passthrough, this, endRunner.isValid);
            return passthroughId;
        }

        internal void ParseMacroDeclaration(out HlslTree.NodeDeclaration nodeDecl, ref TokenRunner runner)
        {
            nodeDecl = new HlslTree.NodeDeclaration();

            string topTokenText = runner.TopData();

            HlslParser.TypeInfo baseType;
            HlslParser.TypeInfo subType;
            unityReserved.GetMacroDeclBaseAndSubType(out baseType, out subType, topTokenText);

            // these are all native base types, so don't worry about handling structs
            HlslUtil.ParserAssert(baseType.nativeType != HlslNativeType._struct);

            {
                runner.AdvanceToken();
                runner.Expect(HlslToken._parense_l);

                // get the name
                nodeDecl.nameTokenId = runner.currToken; //CreateNodeForTopToken(ref runner);
                runner.Expect(HlslToken._identifier);

                // If subType is non-invalid, then the subtype is predefined by the macro. Otherwise, it's a user paramater
                // and we need a comma before the subType name. If the subtype is unknown, then we explicitly do not have
                // have a subtype
                if (subType.nativeType == HlslNativeType._invalid)
                {
                    runner.Expect(HlslToken._comma);

                    if (HlslTokenizer.IsTokenNativeType(runner.TopToken()))
                    {
                        subType.nativeType = tokenToNativeTable[runner.TopToken()];
                    }
                    else
                    {
                        runner.Error("Invalid subType.");
                    }
                    runner.AdvanceToken();
                }

                nodeDecl.nativeType = baseType.nativeType;
                nodeDecl.typeNodeId = CreateNodeForNativeType(ref runner, baseType.nativeType);

                nodeDecl.subTypeNodeId = -1;
                if (subType.nativeType != HlslNativeType._unknown)
                {
                    nodeDecl.subTypeNodeId = CreateNodeForNativeType(ref runner, subType.nativeType);
                }

                nodeDecl.variableName = tokenizer.GetTokenData(nodeDecl.nameTokenId);


                nodeDecl.isMacroDecl = true;
                nodeDecl.macroDeclString = topTokenText;

                {
                    List<int> dims = new List<int>();

                    // do we have a bracket? If so, it's an array.
                    while (runner.Accept(HlslToken._bracket_l))
                    {
                        // a ']' means implicit size based on the initialzier, otherwise
                        // we have an expression here
                        int currExp = -1;
                        if (!runner.Check(HlslToken._bracket_r))
                        {
                            currExp = ParseExpression(ref runner);
                        }

                        dims.Add(currExp);

                        // we should expect a ']' on the end
                        runner.Expect(HlslToken._bracket_r);

                        // if we have another '[' then the while loop will continue, otherwise we are done.
                    }

                    nodeDecl.arrayDims = dims.ToArray();
                }


                runner.Expect(HlslToken._parense_r);
            }
        }

        internal int ParseToplevelCustomGlobalMacro(ref TokenRunner runner)
        {
            string topTokenText = runner.TopData();

            int ret = -1;
            if (string.Equals(topTokenText, "CBUFFER_START"))
            {
                TokenRunner startRunner = runner.MakeCopy();

                runner.AdvanceToken();
                runner.Expect(HlslToken._parense_l);
                runner.Expect(HlslToken._identifier);
                runner.Expect(HlslToken._parense_r);

                int passthroughId = CreatePassThroughForRunnerRange(startRunner, runner);
                ret = passthroughId;
            }
            else if (string.Equals(topTokenText, "CBUFFER_END"))
            {
                TokenRunner startRunner = runner.MakeCopy();

                runner.AdvanceToken();
                int passthroughId = CreatePassThroughForRunnerRange(startRunner, runner);
                ret = passthroughId;
            }
            else
            {
                bool isMacroTypeDecl = unityReserved.IsIdentifierMacroDecl(topTokenText);

                if (isMacroTypeDecl)
                {
                    TokenRunner startRunner = runner.MakeCopy();

                    HlslTree.NodeDeclaration nodeDecl;
                    ParseMacroDeclaration(out nodeDecl, ref runner);

                    ret = tree.AddNode(nodeDecl, this, runner.isValid);

                    HlslTree.VariableInfo variableInfo = GetVariableInfoFromDecl(nodeDecl, ret);
                    tree.AddVariableIdentifier(variableInfo);

                }
                else
                {
                    runner.Error("Invalid top level declaration.");
                }
            }

            return ret;
        }

        internal int ParseTopLevel(TokenRunner runner)
        {
            HlslTree.NodeTopLevel topLevel = new HlslTree.NodeTopLevel();

            List<int> statements = new List<int>();
            List<HlslTokenizer.CodeSection> codeSections = new List<HlslTokenizer.CodeSection>();

            while (!runner.IsEof())
            {
                HlslTokenizer.CodeSection currCodeSection = runner.TopCodeSection();

                bool isMacroDecl = IsTopTokenCustomGlobalMacro(runner);
                if (isMacroDecl)
                {
                    int parsedMacroDecl = ParseToplevelCustomGlobalMacro(ref runner);
                    statements.Add(parsedMacroDecl);
                    codeSections.Add(currCodeSection);
                }
                else if (runner.Check(HlslToken._struct))
                {
                    // struct
                    int parsedStruct = ParseStruct(ref runner);
                    statements.Add(parsedStruct);
                    codeSections.Add(currCodeSection);
                }
                else if (runner.CheckType())
                {
                    // If we parse as:
                    //   <type> <identifier> <'('>
                    // then we are a function. But if we instead have
                    //   <type> <identifier> <anything other than a '('>
                    // then we are a variable declaration. So look ahead
                    bool isFunction = false;
                    {
                        HlslTree.NodeDeclaration nodeIgnored = new HlslTree.NodeDeclaration();

                        TokenRunner lookAheadRunner = runner.MakeCopy();

                        ParseDeclTypeAndIdentifier(ref nodeIgnored, ref lookAheadRunner, true);

                        bool isNextParens = lookAheadRunner.Check(HlslToken._parense_l);

                        if (lookAheadRunner.isValid && isNextParens)
                        {
                            isFunction = true;
                        }
                    }

                    if (isFunction)
                    {
                        int parsedFunc = ParseFunctionWithFallback(ref runner);
                        statements.Add(parsedFunc);
                        codeSections.Add(currCodeSection);
                    }
                    else
                    {
                        HlslTree.VariableInfo variableInfo;
                        int parsedDecl = ParseDeclaration(out variableInfo, ref runner, true, false);
                        statements.Add(parsedDecl);
                        codeSections.Add(currCodeSection);

                        // add this variable to the list for this scope
                        tree.AddVariableIdentifier(variableInfo);
                    }
                }
                else
                {
                    runner.AdvanceToken();
                }
            }

            HlslUtil.ParserAssert(statements.Count == codeSections.Count);

            topLevel.statements = statements.ToArray();
            topLevel.codeSections = codeSections.ToArray();
            int topLevelId = tree.AddNode(topLevel, this, runner.isValid);

            return topLevelId;
        }

        internal Dictionary<HlslToken, HlslNativeType> tokenToNativeTable;

        internal HlslTokenizer tokenizer;
        internal HlslUnityReserved unityReserved;

        internal HlslTree tree;
        List<string> ignoredFuncs;
    }
}
