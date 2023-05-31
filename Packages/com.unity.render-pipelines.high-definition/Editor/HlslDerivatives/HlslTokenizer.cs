using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering.HighDefinition
{
    internal enum HlslToken
    {
        // we can't use just "int" because that's reserved in both hlsl and c#, so add a _ to everthing.

        // default is unknown
        _unknown, // what we found didn't match any expected pattern

        _invalid,
        _comment_single, // single line comment
        _comment_multi, // multiline comment
        _preprocessor, // preprocessor command (#)
        _whitespace,

        // these tokens directly match to their strings
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

        _Texture1D,
        _Texture1DArray,
        _Texture2D,
        _Texture2DArray,
        _Texture3D,
        _TextureCube,
        _TextureCubeArray,
        _SamplerState,
        _SamplerComparisonState,

        _void,
        _struct,

        _if,
        _else,
        _for,
        _while,
        _do,
        _switch,
        _case,
        _break,
        _continue,
        _default,
        _return,
        _goto,

        // modifiers
        _in,
        _out,
        _inout,

        _true,
        _false,

        // operators
        _colon_colon,       // ::
        _plus_plus,         // ++
        _minus_minus,       // --
        _parense_l,         // (
        _parense_r,         // )
        _bracket_l,         // [
        _bracket_r,         // ]
        _period,            // .
        _plus,              // +
        _minus,             // -
        _logical_not,       // !
        _bitwise_not,       // ~
        _mul,               // *
        _div,               // /
        _modulo,            // %
        _shift_l,           // <<
        _shift_r,           // >>
        _less_than,         // <
        _greater_than,      // >
        _less_equal,        // <=
        _greater_equal,     // >=
        _equivalent,        // ==
        _not_equal,         // !=
        _bitwise_and,       // &
        _bitwise_xor,       // ^
        _bitwise_or,        // |
        _logical_and,       // &&
        _logical_or,        // ||
        _question,          // ?
        _colon,             // :
        _assignment,        // =
        _add_equals,        // +=
        _sub_equals,        // -=
        _mul_equals,        // *=
        _div_equals,        // /=
        _mod_equals,        // %=
        _shift_l_equals,    // <<=
        _shift_r_equals,    // >>=
        _and_equals,        // &=
        _or_equals,         // |=
        _xor_equals,        // ^=
        _comma,             // ,
        _brace_l,           // {
        _brace_r,           // }
        _semicolon,         // ;

        // these requires the full name/value to convert to a string
        _literal_float,
        _literal_half,
        _literal_int,
        _literal_uint,
        _literal_double,

        // an identifier
        _identifier,

        // special case to denote end of file
        _eof,
    }

    internal class HlslTokenizer
    {
        static internal string AsReservedString(HlslToken nameId)
        {
            HlslUtil.ParserAssert(IsTokenReserved(nameId));
            string val = nameId.ToString().Substring(1);
            return val;
        }

        static internal bool IsTokenReserved(HlslToken tokenId)
        {
            bool isReserved = (HlslToken._float <= tokenId && tokenId <= HlslToken._false);
            return isReserved;
        }

        static internal string AsOperatorString(HlslToken tokenId)
        {
            string ret = "<error>";
            switch (tokenId)
            {
                case HlslToken._colon_colon: ret = "::"; break;
                case HlslToken._plus_plus: ret = "++"; break;
                case HlslToken._minus_minus: ret = "--"; break;
                case HlslToken._parense_l: ret = "("; break;
                case HlslToken._parense_r: ret = ")"; break;
                case HlslToken._bracket_l: ret = "["; break;
                case HlslToken._bracket_r: ret = "]"; break;
                case HlslToken._period: ret = "."; break;
                case HlslToken._plus: ret = "+"; break;
                case HlslToken._minus: ret = "-"; break;
                case HlslToken._logical_not: ret = "!"; break;
                case HlslToken._bitwise_not: ret = "~"; break;
                case HlslToken._mul: ret = "*"; break;
                case HlslToken._div: ret = "/"; break;
                case HlslToken._modulo: ret = "%"; break;
                case HlslToken._shift_l: ret = "<<"; break;
                case HlslToken._shift_r: ret = ">>"; break;
                case HlslToken._less_than: ret = "<"; break;
                case HlslToken._greater_than: ret = ">"; break;
                case HlslToken._less_equal: ret = "<="; break;
                case HlslToken._greater_equal: ret = ">="; break;
                case HlslToken._equivalent: ret = "=="; break;
                case HlslToken._not_equal: ret = "!="; break;
                case HlslToken._bitwise_and: ret = "&"; break;
                case HlslToken._bitwise_xor: ret = "^"; break;
                case HlslToken._bitwise_or: ret = "|"; break;
                case HlslToken._logical_and: ret = "&&"; break;
                case HlslToken._logical_or: ret = "||"; break;
                case HlslToken._question: ret = "?"; break;
                case HlslToken._colon: ret = ":"; break;
                case HlslToken._assignment: ret = "="; break;
                case HlslToken._add_equals: ret = "+="; break;
                case HlslToken._sub_equals: ret = "-="; break;
                case HlslToken._mul_equals: ret = "*="; break;
                case HlslToken._div_equals: ret = "/="; break;
                case HlslToken._mod_equals: ret = "%="; break;
                case HlslToken._shift_l_equals: ret = "<<="; break;
                case HlslToken._shift_r_equals: ret = ">>="; break;
                case HlslToken._and_equals: ret = "&="; break;
                case HlslToken._or_equals: ret = "|="; break;
                case HlslToken._xor_equals: ret = "^="; break;
                case HlslToken._comma: ret = ","; break;
                case HlslToken._brace_l: ret = "{"; break;
                case HlslToken._brace_r: ret = "}"; break;
                case HlslToken._semicolon: ret = ";"; break;
                default: break;
            }

            return ret;
        }

        static internal bool IsTokenOperator(HlslToken tokenId)
        {
            bool isOperator = (HlslToken._colon_colon <= tokenId && tokenId <= HlslToken._semicolon);
            return isOperator;
        }

        static internal bool IsTokenLiteral(HlslToken tokenId)
        {
            bool isLiteral = (HlslToken._literal_float <= tokenId && tokenId <= HlslToken._literal_uint);
            return isLiteral;
        }

        static internal bool IsTokenLiteralOrBool(HlslToken tokenId)
        {
            bool isLiteral = (HlslToken._literal_float <= tokenId && tokenId <= HlslToken._literal_uint);
            bool isBool = (tokenId == HlslToken._false || tokenId == HlslToken._true);

            return isLiteral || isBool;
        }

        static internal bool IsTokenIdentifier(HlslToken tokenId)
        {
            bool isIdentifier = (tokenId == HlslToken._identifier);
            return isIdentifier;
        }

        static internal bool IsTokenNativeType(HlslToken tokenId)
        {
            bool isNative = (HlslToken._float <= tokenId && tokenId <= HlslToken._void);
            return isNative;
        }

        internal struct TokenMarker
        {
            internal int indexLine;
            internal int indexChar;

            internal static bool IsEqual(TokenMarker lhs, TokenMarker rhs)
            {
                return lhs.indexLine == rhs.indexLine && lhs.indexChar == rhs.indexChar;
            }
        }

        internal enum CodeSection
        {
            Unknown,
            GraphFunction,
            GraphPixel,
        }

        internal struct SingleToken
        {
            internal static SingleToken MakeDirect(HlslToken tokenType, string data, TokenMarker marker, CodeSection codeSection)
            {
                SingleToken ret = new SingleToken();
                ret.tokenType = tokenType;
                ret.data = data;
                ret.marker = marker;
                ret.codeSection = codeSection;
                return ret;
            }

            internal HlslToken tokenType;
            internal string data;
            internal TokenMarker marker;
            internal float literalFloat;
            internal double literalDouble;
            internal int literalInt;
            internal uint literalUint;
            internal CodeSection codeSection;
        }

        internal static Dictionary<string, HlslToken> BuildOperatorDictionaryOfLength(int len)
        {
            Dictionary<string, HlslToken> dict = new Dictionary<string, HlslToken>();

            for (int i = (int)HlslToken._colon_colon; i <= (int)HlslToken._semicolon; i++)
            {
                HlslToken token = (HlslToken)i;
                string name = AsOperatorString(token);
                if (name.Length == len)
                {
                    dict.Add(name, token);
                }
            }

            return dict;
        }

        internal static Dictionary<string, HlslToken> BuildTokenDictionary()
        {
            Dictionary<string, HlslToken> dict = new Dictionary<string, HlslToken>();

            for (int i = (int)HlslToken._float; i <= (int)HlslToken._false; i++)
            {
                HlslToken token = (HlslToken)i;
                string name = AsReservedString(token);
                dict.Add(name, token);
            }

            for (int i = (int)HlslToken._colon_colon; i <= (int)HlslToken._semicolon; i++)
            {
                HlslToken token = (HlslToken)i;
                string name = AsOperatorString(token);
                dict.Add(name, token);
            }

            return dict;
        }

        bool IsEof(TokenMarker marker)
        {
            return marker.indexLine >= allSrcLines.Length;
        }

        internal void AdvanceMarker(ref TokenMarker marker, int num)
        {
            for (int i = 0; i < num; i++)
            {
                NextCharacter(ref marker);
            }
        }

        // get character and increment
        internal char NextCharacter(ref TokenMarker marker)
        {
            char ch = PeekCharacter(marker);
            if (marker.indexChar + 1 <= allSrcLines[marker.indexLine].Length)
            {
                marker.indexChar++;
            }
            else
            {
                marker.indexLine++;
                marker.indexChar = 0;
            }

            return ch;
        }

        internal char PeekCharacter(TokenMarker marker)
        {
            char ch = '\0';
            if (marker.indexChar == allSrcLines[marker.indexLine].Length)
            {
                ch = '\n';
            }
            else if (marker.indexChar < allSrcLines[marker.indexLine].Length)
            {
                ch = allSrcLines[marker.indexLine][marker.indexChar];
            }
            else
            {
                HlslUtil.ParserAssert(false);
            }

            return ch;
        }

        internal char PeekNextCharacter(TokenMarker marker, string[] allSrcLines)
        {
            // advance
            NextCharacter(ref marker);

            char ch = PeekCharacter(marker);
            return ch;
        }

        internal bool IsNumCharactersLeft(TokenMarker marker, int num)
        {
            for (int i = 0; i < num; i++)
            {
                bool isEof = IsEof(marker);
                if (isEof)
                {
                    return false;
                }
                // advance
                NextCharacter(ref marker);
            }

            return true;
        }
        internal string GetCharactersAsString(TokenMarker marker, int num)
        {
            char[] data = new char[num];
            for (int i = 0; i < num; i++)
            {
                data[i] = NextCharacter(ref marker);
            }

            return new string(data);
        }

        internal char SkipPeekCharacter(TokenMarker marker, int toSkip)
        {
            for (int i = 0; i < toSkip; i++)
            {
                // advance
                NextCharacter(ref marker);
            }

            char ch = PeekCharacter(marker);
            return ch;
        }

        static internal bool IsWhitespaceCharacter(char ch)
        {
            bool isWhitespace = false;
            switch (ch)
            {
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    isWhitespace = true;
                    break;
                default:
                    isWhitespace = false;
                    break;
            }

            return isWhitespace;
        }

        internal bool IsWhitespaceMarker(TokenMarker marker)
        {
            char ch = PeekCharacter(marker);
            bool ret = IsWhitespaceCharacter(ch);
            return ret;
        }

        internal void SkipWhitespace(ref TokenMarker marker)
        {
            bool done = false;
            do
            {
                bool isEof = IsEof(marker);
                if (isEof)
                {
                    done = true;
                }
                else
                {
                    char ch = PeekCharacter(marker);
                    if (IsWhitespaceCharacter(ch))
                    {
                        NextCharacter(ref marker);
                    }
                    else
                    {
                        done = true;
                    }
                }
            } while (!done);
        }

        static internal bool IsValid(HlslToken token)
        {
            return token != HlslToken._invalid;
        }

        internal HlslToken GetTokenType(int tokenId)
        {
            HlslToken ret = new HlslToken();
            if (tokenId >= 0 && tokenId < foundTokens.Count)
            {
                ret = foundTokens[tokenId].tokenType;
            }
            else if (tokenId >= foundTokens.Count)
            {
                ret = HlslToken._eof;
            }
            return ret;
        }

        internal string GetTokenData(int tokenId)
        {
            string ret = "<invalid>";
            if (tokenId >= 0 && tokenId < foundTokens.Count)
            {
                ret = foundTokens[tokenId].data;
            }
            else if (tokenId >= foundTokens.Count)
            {
                ret = "<eof>";
            }
            return ret;
        }

        internal HlslToken GetOperatorToken(TokenMarker marker)
        {
            HlslToken token = HlslToken._invalid;
            if (IsNumCharactersLeft(marker, 3))
            {
                string val = GetCharactersAsString(marker, 3);
                if (op3Tokens.ContainsKey(val))
                {
                    token = op3Tokens[val];
                }
            }

            if (!IsValid(token) && IsNumCharactersLeft(marker, 2))
            {
                string val = GetCharactersAsString(marker, 2);
                if (op2Tokens.ContainsKey(val))
                {
                    token = op2Tokens[val];
                }
            }

            if (!IsValid(token) && IsNumCharactersLeft(marker, 1))
            {
                string val = GetCharactersAsString(marker, 1);
                if (op1Tokens.ContainsKey(val))
                {
                    token = op1Tokens[val];
                }
            }

            return token;
        }

        static bool StringEqual(string lhs, string rhs)
        {
            return string.Compare(lhs, rhs) == 0;
        }

        internal string FetchStringBetweenMarkers(TokenMarker lhs, TokenMarker rhs)
        {
            TokenMarker marker = lhs;
            List<char> data = new List<char>();
            while (marker.indexLine < rhs.indexLine || (marker.indexLine == rhs.indexLine && marker.indexChar < rhs.indexChar))
            {
                char ch = NextCharacter(ref marker);
                data.Add(ch);
            }

            return new string(data.ToArray());
        }

        internal TokenMarker AdvanceWhitespace(TokenMarker srcMarker)
        {
            TokenMarker currMarker = srcMarker;

            while (!IsEof(currMarker) && IsWhitespaceMarker(currMarker))
            {
                AdvanceMarker(ref currMarker, 1);
            }
            return currMarker;
        }

        internal TokenMarker AdvanceNonWhitespace(TokenMarker srcMarker)
        {
            TokenMarker currMarker = srcMarker;

            while (!IsEof(currMarker) && !IsWhitespaceMarker(currMarker))
            {
                AdvanceMarker(ref currMarker, 1);
            }
            return currMarker;
        }

        internal bool IsNumeric(TokenMarker marker)
        {
            char ch = PeekCharacter(marker);
            if ('0' <= ch && ch <= '9')
                return true;

            return false;
        }

        internal bool IsPeriod(TokenMarker marker)
        {
            char ch = PeekCharacter(marker);
            return ch == '.';
        }

        internal bool IsValidMarkerForNumericLiteral(TokenMarker marker)
        {
            char ch = PeekCharacter(marker);

            if ('0' <= ch && ch <= '9')
                return true;

            switch (ch)
            {
                case '.':
                case 'x':
                case '+':
                case '-':
                case 'l':
                case 'L':
                case 'u':
                case 'U':
                case 'h':
                case 'H':
                case 'f':
                case 'F':
                case 'e':
                case 'E':
                    return true;
                default:
                    break;
            }

            return false;
        }


        internal bool IsAlpha(TokenMarker marker)
        {
            char ch = PeekCharacter(marker);
            if (('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z'))
                return true;

            return false;
        }

        internal bool IsUnderscore(TokenMarker marker)
        {
            char ch = PeekCharacter(marker);
            return ch == '_';
        }

        internal TokenMarker ParseNumericLiteral(out SingleToken foundToken, TokenMarker startMarker, CodeSection codeSection)
        {
            TokenMarker marker = startMarker;

            foundToken = new SingleToken();

            bool isNumeric = IsNumeric(startMarker);
            bool isPeriod = IsPeriod(startMarker);

            bool isPeriodLeadingFloat = false;
            if (isPeriod && IsNumCharactersLeft(startMarker, 2))
            {
                TokenMarker nextMarker = startMarker;
                AdvanceMarker(ref nextMarker, 1);

                bool isValidNumber = IsNumeric(nextMarker);

                isPeriodLeadingFloat = isValidNumber;
            }

            // find the end marker, assuming this literal is valid
            bool stillValid = true;
            bool foundSuffix = false;
            bool firstIsZero = false;
            int currIndex = 0;
            bool foundPeriod = false;
            int foundEIndex = -1;
            bool isHex = false;
            bool foundDigitBeforeE = false;
            bool foundDigitAfterE = false;
            bool leadingPosOrNeg = false;
            HlslToken foundType = HlslToken._unknown;

            string debugToken = "";

            while (!IsEof(marker) && stillValid && IsValidMarkerForNumericLiteral(marker))
            {
                char ch = NextCharacter(ref marker);
                debugToken += ch;

                if ('0' <= ch && ch <= '9')
                {
                    // just a digit
                    if (foundEIndex < 0)
                    {
                        foundDigitBeforeE = true;
                    }
                    else
                    {
                        foundDigitAfterE = true;
                    }
                }
                else
                {
                    switch (ch)
                    {
                        case '.':
                            // if we already found a period, this is a second one which is a parse failure
                            if (foundPeriod || isHex)
                            {
                                stillValid = false;
                            }
                            foundPeriod = true;
                            break;
                        case 'x':
                            // x is only allowed in the second index, and if it's a zero
                            if (!firstIsZero || currIndex != 1)
                            {
                                stillValid = false;
                            }
                            isHex = true;
                            break;
                        case '+':
                        case '-':
                            // + and - are only allowed if they are the first character or if they are directly following the e
                            if (currIndex == 0)
                            {
                                leadingPosOrNeg = true;
                            }
                            else
                            {
                                // if not in first position, it must be right after an e
                                if (foundEIndex < 0 || foundEIndex != currIndex - 1)
                                {
                                    stillValid = false;
                                }
                                else
                                {
                                    // no op, seems fine
                                }
                            }
                            break;
                        case 'l':
                        case 'L':
                            // invalid if we already found a suffix
                            if (foundSuffix)
                            {
                                stillValid = false;
                            }

                            if (foundPeriod || foundEIndex >= 0 || leadingPosOrNeg)
                            {
                                foundType = HlslToken._literal_double;
                            }
                            else
                            {
                                foundType = HlslToken._literal_int;
                            }
                            foundSuffix = true;
                            break;
                        case 'u':
                        case 'U':
                            // invalid if we already found a suffix
                            if (foundSuffix)
                            {
                                stillValid = false;
                            }

                            // not allowed to be a float
                            if (foundPeriod || foundEIndex >= 0 || leadingPosOrNeg)
                            {
                                stillValid = false;
                            }
                            else
                            {
                                foundType = HlslToken._literal_uint;
                            }
                            foundSuffix = true;
                            break;
                        case 'h':
                        case 'H':
                            // we must not have found a suffix, and we must have found a period or an E
                            if (foundSuffix || (!foundPeriod && foundEIndex < 0))
                            {
                                stillValid = false;
                            }
                            foundType = HlslToken._literal_half;
                            foundSuffix = true;
                            break;
                        case 'f':
                        case 'F':
                            // we must not have found a suffix, and we must have found a period or and E
                            if (foundSuffix || (!foundPeriod && foundEIndex < 0))
                            {
                                stillValid = false;
                            }
                            foundType = HlslToken._literal_float;
                            foundSuffix = true;
                            break;
                        case 'e':
                        case 'E':
                            // if we already found an E, then it's invalid to find a second one
                            if (foundEIndex >= 0)
                            {
                                stillValid = false;
                            }
                            else
                            {
                                foundEIndex = currIndex;
                            }
                            break;
                        default:
                            break;
                    }
                }
                currIndex++;
            }

            if (foundPeriod && (!foundDigitBeforeE || (foundEIndex >= 0 && !foundDigitAfterE)))
            {
                stillValid = false;
            }

            // if we still don't know the type, decide now
            if (foundType == HlslToken._unknown)
            {
                if (foundPeriod || foundEIndex >= 0)
                {
                    foundType = HlslToken._literal_float;
                }
                else
                {
                    foundType = HlslToken._literal_int;
                }
            }

            foundToken.tokenType = foundType;
            foundToken.marker = startMarker;
            foundToken.codeSection = codeSection;

            // try to actually parse it
            if (stillValid)
            {
                string fullStr = FetchStringBetweenMarkers(startMarker, marker);

                string trimStr = fullStr;
                if (foundSuffix)
                {
                    trimStr = fullStr.Substring(0, fullStr.Length - 1);
                }

                foundToken.data = fullStr;
                switch (foundType)
                {
                    case HlslToken._literal_float:
                    case HlslToken._literal_half:
                        stillValid = float.TryParse(trimStr, out foundToken.literalFloat);
                        break;

                    case HlslToken._literal_uint:
                        stillValid = uint.TryParse(trimStr, out foundToken.literalUint);
                        break;
                    case HlslToken._literal_int:
                        stillValid = int.TryParse(trimStr, out foundToken.literalInt);
                        break;
                    case HlslToken._literal_double:
                        stillValid = double.TryParse(trimStr, out foundToken.literalDouble);
                        break;
                    default:
                        // should never get here
                        HlslUtil.ParserAssert(false);
                        stillValid = false;
                        break;
                }

            }

            // if at any point we are no longer valid, return the original marker to signal
            // that parsing this literal failed
            if (!stillValid)
            {
                marker = startMarker;
                foundToken = new SingleToken();
            }

            return marker;
        }

        internal TokenMarker ParseIdentifierOrReserved(out SingleToken foundToken, TokenMarker startMarker, CodeSection codeSection)
        {
            TokenMarker marker = startMarker;
            bool isAlpha = IsAlpha(marker);
            bool isUnderscore = IsUnderscore(marker);
            if (isAlpha || isUnderscore)
            {
                AdvanceMarker(ref marker, 1);
                while (!IsEof(marker) &&
                    (IsAlpha(marker) || IsUnderscore(marker) || IsNumeric(marker)))
                {
                    AdvanceMarker(ref marker, 1);
                }
            }

            foundToken = new SingleToken();
            if (!TokenMarker.IsEqual(marker, startMarker))
            {
                foundToken.marker = marker;
                foundToken.data = FetchStringBetweenMarkers(startMarker, marker);
                foundToken.codeSection = codeSection;

                bool isFound = rawTokens.ContainsKey(foundToken.data);
                if (isFound)
                {
                    foundToken.tokenType = rawTokens[foundToken.data];
                }
                else
                {
                    foundToken.tokenType = HlslToken._identifier;
                }
            }

            return marker;
        }

        internal struct TextureParamOrArgInfo
        {
            internal static TextureParamOrArgInfo Make(string lhsFunc, string rhsFunc, bool lhsParens, bool rhsParens)
            {
                TextureParamOrArgInfo ret = new TextureParamOrArgInfo();
                ret.lhsFunc = lhsFunc;
                ret.rhsFunc = rhsFunc;
                ret.lhsParens = lhsParens;
                ret.rhsParens = rhsParens;
                return ret;
            }

            internal string lhsFunc;
            internal string rhsFunc;
            internal bool lhsParens;
            internal bool rhsParens;
        }


        // For the platforms that are relevant for analytic partial derivatives (Dx11/Dx12/Vulkan/Metal), they
        // all have the same definitions for these macros. So we will expand these specific macros while tokenizing. This isn't
        // a 100% robust parsing algorithm for macros, but it should be good enough for the use cases that shadergraph handles.
        // Essentially the algorithm is:
        // 1. Add lhs prefix.
        // 2. Find the comma.
        // 3. Add lhs suffix, comma, and rhs prefix
        // 4. Find trailing r parense.
        // 5. Add rhs suffix.
        internal static Dictionary<string, TextureParamOrArgInfo> BuildTexParamOrArgDictionary()
        {
            Dictionary<string, TextureParamOrArgInfo> dict = new Dictionary<string, TextureParamOrArgInfo>();

            dict.Add("TEXTURE2D_PARAM", TextureParamOrArgInfo.Make("TEXTURE2D", "SAMPLER", true, true));
            dict.Add("TEXTURE2D_ARRAY_PARAM", TextureParamOrArgInfo.Make("TEXTURE2D_ARRAY", "SAMPLER", true, true));
            dict.Add("TEXTURECUBE_PARAM", TextureParamOrArgInfo.Make("TEXTURECUBE", "SAMPLER", true, true));
            dict.Add("TEXTURECUBE_ARRAY_PARAM", TextureParamOrArgInfo.Make("TEXTURECUBE_ARRAY", "SAMPLER", true, true));
            dict.Add("TEXTURE3D_PARAM", TextureParamOrArgInfo.Make("TEXTURE3D", "SAMPLER", true, true));
            dict.Add("TEXTURE2D_SHADOW_PARAM", TextureParamOrArgInfo.Make("TEXTURE2D", "SAMPLER", true, true));
            dict.Add("TEXTURE2D_ARRAY_SHADOW_PARAM", TextureParamOrArgInfo.Make("TEXTURE2D_ARRAY", "SAMPLER", true, true));
            dict.Add("TEXTURECUBE_SHADOW_PARAM", TextureParamOrArgInfo.Make("TEXTURECUBE", "SAMPLER", true, true));
            dict.Add("TEXTURECUBE_ARRAY_SHADOW_PARAM", TextureParamOrArgInfo.Make("TEXTURECUBE_ARRAY", "SAMPLER", true, true));
            dict.Add("TEXTURE2D_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURE2D_ARRAY_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURECUBE_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURECUBE_ARRAY_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURE3D_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURE2D_SHADOW_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURE2D_ARRAY_SHADOW_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURECUBE_SHADOW_ARGS", TextureParamOrArgInfo.Make("", "", false, false));
            dict.Add("TEXTURECUBE_ARRAY_SHADOW_ARGS", TextureParamOrArgInfo.Make("", "", false, false));

            return dict;
        }

        static int FindMatchingTokenOfType(List<SingleToken> allTokens, int startIndex, HlslToken type)
        {
            // simplistic algorithm but should be enough for what we need it to do.
            int parenseCount = 0;
            int bracketCount = 0;
            int braceCount = 0;

            int currIndex = startIndex;
            bool done = false;
            while (currIndex < allTokens.Count && !done)
            {
                SingleToken currToken = allTokens[currIndex];
                if (parenseCount == 0 && bracketCount == 0 && braceCount == 0 && currToken.tokenType == type)
                {
                    done = true;
                }
                else
                {
                    switch (currToken.tokenType)
                    {
                        case HlslToken._parense_l:
                            parenseCount++;
                            break;
                        case HlslToken._parense_r:
                            parenseCount--;
                            break;
                        case HlslToken._bracket_l:
                            bracketCount++;
                            break;
                        case HlslToken._bracket_r:
                            bracketCount--;
                            break;
                        case HlslToken._brace_l:
                            braceCount++;
                            break;
                        case HlslToken._brace_r:
                            braceCount--;
                            break;
                        default:
                            // no op;
                            break;
                    }

                    currIndex++;
                }


                if (parenseCount < 0 || bracketCount < 0 || braceCount < 0)
                {
                    // invalid, signal an error
                    currIndex = -1;
                    done = true;
                }

            }

            return currIndex;
        }

        static List<SingleToken> ApplyTextureMacrosToTokenList(List<SingleToken> srcTokens)
        {
            Dictionary<string, TextureParamOrArgInfo> dict = BuildTexParamOrArgDictionary();

            List<SingleToken> dstTokens = new List<SingleToken>();

            int currIndex = 0;
            while (currIndex < srcTokens.Count)
            {
                SingleToken token = srcTokens[currIndex];

                if (token.tokenType == HlslToken._identifier && dict.ContainsKey(token.data))
                {
                    TextureParamOrArgInfo info = dict[token.data];

                    int startIndex = currIndex;

                    // we could have tokens in between due to whitespace or commments
                    int startParense = FindMatchingTokenOfType(srcTokens, startIndex, HlslToken._parense_l);

                    int commaIndex = -1;
                    if (startParense >= 0)
                    {
                        commaIndex = FindMatchingTokenOfType(srcTokens, startParense + 1, HlslToken._comma);
                    }

                    int closingParensIndex = -1;
                    if (commaIndex >= 0)
                    {
                        closingParensIndex = FindMatchingTokenOfType(srcTokens, commaIndex + 1, HlslToken._parense_r);
                    }

                    if (closingParensIndex >= 0 && commaIndex >= 0)
                    {
                        // splice in the new tokens
                        if (info.lhsFunc.Length > 0)
                        {
                            // add the function token
                            dstTokens.Add(SingleToken.MakeDirect(HlslToken._identifier, info.lhsFunc, srcTokens[startIndex].marker, token.codeSection));

                            // add a parens
                            dstTokens.Add(SingleToken.MakeDirect(HlslToken._parense_l, "(", srcTokens[startIndex].marker, token.codeSection));
                        }

                        // add the tokens for the lhs
                        for (int i = startParense + 1; i < commaIndex; i++)
                        {
                            string currName = srcTokens[i].data;
                            dstTokens.Add(srcTokens[i]);
                        }

                        // if we have a func, add a closing parense
                        if (info.lhsFunc.Length > 0)
                        {
                            dstTokens.Add(SingleToken.MakeDirect(HlslToken._parense_r, ")", srcTokens[commaIndex].marker, token.codeSection));
                        }

                        // add a comma
                        dstTokens.Add(SingleToken.MakeDirect(HlslToken._comma, ",", srcTokens[commaIndex].marker, token.codeSection));

                        // and again for the rhs side
                        if (info.rhsFunc.Length > 0)
                        {
                            // add the function token
                            dstTokens.Add(SingleToken.MakeDirect(HlslToken._identifier, info.rhsFunc, srcTokens[commaIndex].marker, token.codeSection));

                            // add a parens
                            dstTokens.Add(SingleToken.MakeDirect(HlslToken._parense_l, "(", srcTokens[commaIndex].marker, token.codeSection));
                        }

                        // add the tokens for the lhs
                        for (int i = commaIndex + 1; i < closingParensIndex; i++)
                        {
                            dstTokens.Add(srcTokens[i]);
                        }

                        // if we have a func, add a closing parense
                        if (info.rhsFunc.Length > 0)
                        {
                            dstTokens.Add(SingleToken.MakeDirect(HlslToken._parense_r, ")", srcTokens[commaIndex].marker, token.codeSection));
                        }

                        currIndex = closingParensIndex + 1;

                    }
                    else
                    {
                        // we had an error, so best plan is to fail silently and output the existing code without
                        // modification so the user can debug where the matching failed.
                        dstTokens.Add(token);
                        currIndex++;
                    }
                }
                else
                {
                    dstTokens.Add(token);
                    currIndex++;
                }
            }

            return dstTokens;
        }

        // if the last character is a new line, remove it
        static internal string StripLastNewline(string val)
        {
            if (val.Length >= 1 && val[val.Length - 1] == '\n')
            {
                return val.Substring(0, val.Length - 1);
            }

            return val;
        }

        internal string[] CalcDebugTokenLines(bool showWhitespace)
        {
            List<string> debugLines = new List<string>();
            for (int i = 0; i < foundTokens.Count; i++)
            {
                HlslTokenizer.SingleToken token = foundTokens[i];

                bool showToken = true;
                if (token.tokenType == HlslToken._whitespace && !showWhitespace)
                {
                    showToken = false;
                }

                if (showToken)
                {
                    // don't need to write whitespace in the debug output
                    string tokenData = token.tokenType != HlslToken._whitespace ? token.data : "";

                    string tokenSection = token.codeSection.ToString();

                    string line = string.Format("{0,-5} - {5,15} - {4,-16},({1}, {2,-3}): {3}", i, token.marker.indexLine, token.marker.indexChar, tokenData, token.tokenType, tokenSection);
                    debugLines.Add(line);
                }
            }

            return debugLines.ToArray();
        }

        internal string[] RebuildTextFromTokens()
        {
            List<string> allLines = new List<string>();

            string currLine = "";
            for (int tokenIter = 0; tokenIter < foundTokens.Count; tokenIter++)
            {
                SingleToken token = foundTokens[tokenIter];

                currLine += token.data;

                if (tokenIter < foundTokens.Count - 1)
                {
                    SingleToken nextToken = foundTokens[tokenIter + 1];
                    if (!token.data.Contains('\n') &&
                        token.marker.indexLine < nextToken.marker.indexLine)
                    {
                        currLine += "\n";
                    }
                }

            }

            allLines.Add(currLine);

            return allLines.ToArray();
        }

        void LogError(TokenMarker marker, string error)
        {
            // todo
        }

        internal void Init(string[] fileAllSrcLines)
        {
            allSrcLines = fileAllSrcLines;

            rawTokens = BuildTokenDictionary();

            op1Tokens = BuildOperatorDictionaryOfLength(1);
            op2Tokens = BuildOperatorDictionaryOfLength(2);
            op3Tokens = BuildOperatorDictionaryOfLength(3);


            TokenMarker tokenizerMarker = new TokenMarker();
            tokenizerMarker.indexLine = 0;
            tokenizerMarker.indexChar = 0;

            List<SingleToken> allTokens = new List<SingleToken>();

            List<string> customLines = new List<string>();

            // The last end marker for a valid, non-comment token. Necessary for determining if a # is
            // a preprocessor command, or if a # is something else. Although for now we aren't parsing ## and #@ operators.
            TokenMarker lastTokenMarker = tokenizerMarker;

            CodeSection codeSection = CodeSection.Unknown;

            Dictionary<string, CodeSection> codeSectionNames = new Dictionary<string, CodeSection>();
            codeSectionNames.Add("unity-derivative-graph-function", CodeSection.GraphFunction);
            codeSectionNames.Add("unity-derivative-graph-pixel", CodeSection.GraphPixel);

            Dictionary<string, bool> codeSectionOps = new Dictionary<string, bool>();
            codeSectionOps.Add("begin", true);
            codeSectionOps.Add("end", false);

            bool isInsideCustomFunc = false;

            bool done = IsEof(tokenizerMarker);
            while (!done)
            {
                // try to fetch the data in priority order:
                // 1. Multiline comment. I.e. /*
                // 2. Single line comment. I.e. //
                // 3. Preprocessor. I.e. #, but only if it's the first character on this line
                // 4. Numeric literal. (0x32, 1.4f). Needs to be before symbol because of . in front of floates. (i.e. .352f);
                // 5. Symbol (3, 2, 1 character)
                // 6. Whitespace
                // 7. AlphaNumericUnderscore token.
                //    7a: Reserved keyword. (for, if, etc)
                //    7b: Identifier.
                // 7. Unknown
                //
                // Also, note that literal strings (i.e. "hello world") are invalid. In the case of a tokenizer failure,
                // simply put the data as Unknown and do our best to continue so that we can dump the entire list and
                // debug the issue. Even if we fail to tokenize (for whatever reason) we may still want to dump the original
                // text.

                bool somethingFound = false;

                // 1. Multiline comment
                if (!somethingFound)
                {
                    if (IsNumCharactersLeft(tokenizerMarker, 2))
                    {
                        string nextTwo = GetCharactersAsString(tokenizerMarker, 2);
                        if (StringEqual(nextTwo, "/*"))
                        {
                            TokenMarker commentMarker = tokenizerMarker;
                            AdvanceMarker(ref commentMarker, 2);

                            // advance the marker
                            bool commentDone = false;
                            while (!commentDone)
                            {
                                if (IsNumCharactersLeft(commentMarker, 2))
                                {
                                    string commentTwo = GetCharactersAsString(commentMarker, 2);
                                    if (StringEqual(commentTwo, "*/"))
                                    {
                                        // found end comment, so we're done
                                        AdvanceMarker(ref commentMarker, 2);

                                        commentDone = true;
                                    }
                                    else
                                    {
                                        // not an end of comment, so skip a character and continue
                                        AdvanceMarker(ref commentMarker, 1);
                                    }
                                }
                                else
                                {
                                    // if there are not two characters left, then we are beyond the file, and we are done so simply advance to EOF.
                                    commentDone = true;

                                    commentMarker.indexLine = allSrcLines.Length;
                                    commentMarker.indexChar = 0;
                                }
                            }

                            string commentData = FetchStringBetweenMarkers(tokenizerMarker, commentMarker);

                            SingleToken singleToken = new SingleToken();
                            singleToken.tokenType = HlslToken._comment_multi;
                            singleToken.data = StripLastNewline(commentData);
                            singleToken.marker = tokenizerMarker;
                            singleToken.codeSection = codeSection;

                            allTokens.Add(singleToken);

                            // note: explicitly not advancing lastTokenMarker
                            tokenizerMarker = commentMarker;
                            somethingFound = true;
                        }
                    }

                }

                // 2. Single line comment
                if (!somethingFound)
                {
                    if (IsNumCharactersLeft(tokenizerMarker, 2))
                    {
                        string nextTwo = GetCharactersAsString(tokenizerMarker, 2);
                        if (StringEqual(nextTwo, "//"))
                        {
                            TokenMarker endMarker;
                            endMarker.indexLine = tokenizerMarker.indexLine + 1;
                            endMarker.indexChar = 0;

                            string commentData = FetchStringBetweenMarkers(tokenizerMarker, endMarker);

                            SingleToken singleToken = new SingleToken();
                            singleToken.tokenType = HlslToken._comment_single;
                            singleToken.data = StripLastNewline(commentData);
                            singleToken.marker = tokenizerMarker;
                            singleToken.codeSection = codeSection;

                            if (singleToken.data == "// unity-custom-func-begin")
                            {
                                isInsideCustomFunc = true;
                            }

                            if (singleToken.data == "// unity-custom-func-end")
                            {
                                isInsideCustomFunc = false;
                            }



                            allTokens.Add(singleToken);

                            // note: explicitly not advancing lastTokenMarker
                            tokenizerMarker = endMarker;
                            somethingFound = true;
                        }
                    }
                }

                // if we are inside a custom function, then skip to next character
                if (isInsideCustomFunc && !somethingFound)
                {
                    TokenMarker endMarker;
                    endMarker.indexLine = tokenizerMarker.indexLine + 1;
                    endMarker.indexChar = 0;

                    string lineData = FetchStringBetweenMarkers(tokenizerMarker, endMarker);
                    customLines.Add(StripLastNewline(lineData));

                    // note: explicitly not advancing lastTokenMarker
                    tokenizerMarker = endMarker;
                    somethingFound = true;
                }

                bool wasLastTokenOnPreviousLine = (lastTokenMarker.indexLine < tokenizerMarker.indexLine || (lastTokenMarker.indexLine == tokenizerMarker.indexLine && lastTokenMarker.indexChar == 0));

                // 3. Preprocessor, only valid if there was no token on this line
                if (!somethingFound && wasLastTokenOnPreviousLine)
                {
                    if (IsNumCharactersLeft(tokenizerMarker, 1))
                    {
                        string nextOne = GetCharactersAsString(tokenizerMarker, 1);
                        if (StringEqual(nextOne, "#"))
                        {
                            TokenMarker endMarker;
                            endMarker.indexLine = tokenizerMarker.indexLine + 1;
                            endMarker.indexChar = 0;

                            string commandData = FetchStringBetweenMarkers(tokenizerMarker, endMarker);

                            // check if it's a special section command pragma
                            {
                                string[] commandSplit = commandData.Split(new char[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

                                if (commandSplit.Length == 3)
                                {
                                    string sectionName = commandSplit[1];
                                    string sectionOp = commandSplit[2];
                                    if (codeSectionNames.ContainsKey(sectionName))
                                    {
                                        if (codeSectionOps.ContainsKey(sectionOp))
                                        {
                                            CodeSection adjustedSection = codeSectionNames[sectionName];
                                            bool adjustedOp = codeSectionOps[sectionOp];

                                            if (codeSection == CodeSection.Unknown)
                                            {
                                                if (adjustedOp == true)
                                                {
                                                    codeSection = adjustedSection;
                                                }
                                                else
                                                {
                                                    LogError(tokenizerMarker, "Invalid section pragma: Unable to end without matching begin.");
                                                }
                                            }
                                            else
                                            {
                                                if (adjustedOp == true || codeSection != adjustedSection)
                                                {
                                                    LogError(tokenizerMarker, "Invalid section pragma: Unable to match closing end");
                                                }
                                                else
                                                {
                                                    codeSection = CodeSection.Unknown;
                                                }
                                            }


                                        }
                                        else
                                        {
                                            LogError(tokenizerMarker, "Invalid section pragma: Unknown op - " + sectionOp);
                                        }
                                    }
                                }
                            }


                            SingleToken singleToken = new SingleToken();
                            singleToken.tokenType = HlslToken._preprocessor;
                            singleToken.data = StripLastNewline(commandData);
                            singleToken.marker = tokenizerMarker;
                            singleToken.codeSection = codeSection;

                            allTokens.Add(singleToken);

                            tokenizerMarker = endMarker;
                            lastTokenMarker = endMarker;
                            somethingFound = true;
                        }
                    }
                }

                // 4. Numeric literal
                if (!somethingFound)
                {
                    SingleToken singleToken;

                    TokenMarker foundMarker = ParseNumericLiteral(out singleToken, tokenizerMarker, codeSection);

                    // did we find something?
                    if (!TokenMarker.IsEqual(foundMarker, tokenizerMarker))
                    {
                        allTokens.Add(singleToken);

                        tokenizerMarker = foundMarker;
                        lastTokenMarker = foundMarker;
                        somethingFound = true;
                    }
                }

                // 5. Symbol operators
                if (!somethingFound)
                {
                    HlslToken opToken = GetOperatorToken(tokenizerMarker);
                    if (IsValid(opToken))
                    {
                        string val = AsOperatorString(opToken);

                        SingleToken singleToken = new SingleToken();
                        singleToken.tokenType = opToken;
                        singleToken.data = StripLastNewline(val);
                        singleToken.marker = tokenizerMarker;
                        singleToken.codeSection = codeSection;

                        AdvanceMarker(ref tokenizerMarker, val.Length);

                        allTokens.Add(singleToken);
                        somethingFound = true;
                    }
                }

                // 6. Whitespace
                if (!somethingFound)
                {
                    TokenMarker endMarker = AdvanceWhitespace(tokenizerMarker);
                    if (!TokenMarker.IsEqual(tokenizerMarker, endMarker))
                    {
                        string whitespaceData = FetchStringBetweenMarkers(tokenizerMarker, endMarker);

                        SingleToken singleToken = new SingleToken();
                        singleToken.tokenType = HlslToken._whitespace;
                        singleToken.data = StripLastNewline(whitespaceData);
                        singleToken.marker = tokenizerMarker;
                        singleToken.codeSection = codeSection;

                        allTokens.Add(singleToken);

                        // don't set lastTokenMarker
                        tokenizerMarker = endMarker;
                        somethingFound = true;
                    }
                }

                // 7. Identifier or Reserved
                if (!somethingFound)
                {
                    SingleToken singleToken;

                    TokenMarker foundMarker = ParseIdentifierOrReserved(out singleToken, tokenizerMarker, codeSection);

                    // did we find something?
                    if (!TokenMarker.IsEqual(foundMarker, tokenizerMarker))
                    {
                        allTokens.Add(singleToken);

                        tokenizerMarker = foundMarker;
                        lastTokenMarker = foundMarker;
                        somethingFound = true;
                    }
                }

                // 8. Unknown
                if (!somethingFound)
                {
                    // should never be whitespace because that was already checked
                    HlslUtil.ParserAssert(!IsWhitespaceMarker(tokenizerMarker));

                    // should never be eof
                    HlslUtil.ParserAssert(!IsEof(tokenizerMarker));

                    TokenMarker endMarker = AdvanceNonWhitespace(tokenizerMarker);
                    HlslUtil.ParserAssert(!TokenMarker.IsEqual(tokenizerMarker, endMarker));

                    string tokenData = FetchStringBetweenMarkers(tokenizerMarker, endMarker);

                    SingleToken singleToken = new SingleToken();
                    singleToken.tokenType = HlslToken._unknown;
                    singleToken.data = StripLastNewline(tokenData);
                    singleToken.marker = tokenizerMarker;
                    singleToken.codeSection = codeSection;

                    allTokens.Add(singleToken);

                    lastTokenMarker = endMarker;
                    tokenizerMarker = endMarker;
                    somethingFound = true;
                }

                done = IsEof(tokenizerMarker);
            }

            foundTokens = ApplyTextureMacrosToTokenList(allTokens);

            allCustomLines = customLines.ToArray();
        }

        // helpers for easy lookups
        Dictionary<string, HlslToken> rawTokens;

        Dictionary<string, HlslToken> op1Tokens;
        Dictionary<string, HlslToken> op2Tokens;
        Dictionary<string, HlslToken> op3Tokens;

        Dictionary<string, TextureParamOrArgInfo> texParamOrArgMacros;

        internal string[] allCustomLines;
        string[] allSrcLines;

        internal List<SingleToken> foundTokens;
    }
}
