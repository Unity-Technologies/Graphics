using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class HlslProcessor
    {
        internal HlslProcessor(int numUvs)
        {
            adjustedUvDerivs = new bool[numUvs];

            debugTokenizerInfo = "";
            debugDirectReconstruction = "";
            debugParserTree = "";
            debugNodeInfo = "";
            debugTextInput = "";
            debugTextOutput = "";
            debugLog = "";

            dstGraphFunctions = "";
            dstGraphPixel = "";
            isValid = false;

            debugNodeStack = new List<int>();
        }

        internal void ProcessFunctions(string propStr, string surfaceDescInputStr, string graphFuncStr, string graphPixelStr, bool applyEmulatedDerivatives, List<string> ignoredFuncs, string shaderName)
        {
            isValid = false;

            List<string> allLines = new List<string>();
            allLines.Add(propStr);
            allLines.Add(surfaceDescInputStr);

            allLines.Add("#section unity-derivative-graph-function begin");
            allLines.Add(graphFuncStr);
            allLines.Add("#section unity-derivative-graph-function end");
            allLines.Add("#section unity-derivative-graph-pixel begin");
            allLines.Add(graphPixelStr);
            allLines.Add("#section unity-derivative-graph-pixel end");

            string[] srcLines;
            {
                List<string> splitLines = new List<string>();
                char[] splitChars = new char[2] { '\r', '\n' };

                for (int lineIter = 0; lineIter < allLines.Count; lineIter++)
                {
                    string[] currLines = allLines[lineIter].Split(splitChars);
                    for (int i = 0; i < currLines.Length; i++)
                    {
                        splitLines.Add(currLines[i]);
                    }
                }

                srcLines = splitLines.ToArray();
            }

            {

                debugTextInput = HlslGenerator.FlattenStringVec(srcLines);

                HlslTokenizer tokenizer = new HlslTokenizer();
                tokenizer.Init(srcLines);

                HlslUnityReserved unityReserved = new HlslUnityReserved();

                HlslParser parser = new HlslParser(tokenizer, unityReserved, ignoredFuncs);

                string[] debugLines = tokenizer.CalcDebugTokenLines(false);
                debugTokenizerInfo = HlslGenerator.FlattenStringVec(debugLines);


                HlslTree tree = new HlslTree(unityReserved, tokenizer);
                parser.ParseTokens(tree);

                string[] dstLines = tokenizer.RebuildTextFromTokens();
                debugDirectReconstruction = HlslGenerator.FlattenStringVec(dstLines);

                string[] parsedLines = tree.CalcDebugLines(tokenizer, unityReserved);
                debugParserTree = HlslGenerator.FlattenStringVec(parsedLines);

                for (int i = 0; i < tree.errList.Count; i++)
                {
                    debugLog += "Error at %d:" + tree.errList[i].errText + "\n";
                }


                if (tree.errList.Count == 0)
                {
                    HlslGenerator generator = new HlslGenerator();
                    generator.Init(tokenizer, unityReserved, tree, applyEmulatedDerivatives);

                    string[] genLines = generator.GenerateLines(debugNodeStack,shaderName);
                    debugTextOutput = HlslGenerator.FlattenStringVec(genLines);
                    debugLog += generator.debugLog;

                    string[] nodeLines = generator.debugNodeLines;
                    debugNodeInfo = HlslGenerator.FlattenStringVec(nodeLines);

                    string customFuncLines = HlslGenerator.FlattenStringVec(tokenizer.allCustomLines);

                    dstGraphFunctions = generator.dstApdFuncs + generator.dstGraphFunction + customFuncLines;
                    dstGraphPixel = generator.dstGraphPixel;

                    if (generator.isValid)
                    {
                        if (!applyEmulatedDerivatives)
                        {
                            for (int uvIter = 0; uvIter < generator.uvDerivatives.Count; uvIter++)
                            {
                                UVChannel chan = generator.uvDerivatives[uvIter];
                                int chanIdx = (int)chan;
                                if (chanIdx >= 0 && chanIdx < adjustedUvDerivs.Length)
                                {
                                    adjustedUvDerivs[chanIdx] = true;
                                }
                            }
                        }

                        isValid = true;
                    }
                }

            }

        }

        internal bool isValid;

        internal bool[] adjustedUvDerivs;

        internal string debugTokenizerInfo;
        internal string debugDirectReconstruction;
        internal string debugParserTree;
        internal string debugNodeInfo;
        internal string debugTextInput;
        internal string debugTextOutput;
        internal string debugLog;

        internal string dstGraphFunctions;
        internal string dstGraphPixel;
        internal List<int> debugNodeStack;
    }
}
