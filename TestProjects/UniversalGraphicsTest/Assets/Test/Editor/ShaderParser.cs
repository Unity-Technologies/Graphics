using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


/* JSON SETUP
    version
    date
    files
    [
        {
            name
            functions
            [
                {
                    functionName
                    variations
                    [
                        {
                            returnType
                            functionAPI
                            parameters
                            [
                                {
                                    name
                                    type
                                    functionArgs
                                    hasFunctionArgs
                                    isOptional
                                },
                            ]
                        },
                    ]
                },
            ]
        },
    ]
*/

[Serializable]
public struct PackageFunctionsSaveData
{
    public string version;
    public string date;
    public FileFunctionsSaveData[] files;
}

[Serializable]
public struct FileFunctionsSaveData
{
    public string name;
    public ShaderFunction[] functions;
}

[Serializable]
public struct ShaderFunction
{
    public string functionName;
    public ShaderFunctionVariation[] variations;
}

[Serializable]
public struct ShaderFunctionVariation
{
    public string returnType;
    public string functionAPI;
    public ShaderParameter[] parameters;
}

[Serializable]
public struct ShaderParameter
{
    public string name;
    public string type;
    public string functionArg;
    public bool hasFunctionArgs;
    public bool isOptional;
}



public static class ShaderParser
{
    private const string k_Inline = "inline";
    private const string kDeprecatedFilePath = "com.unity.render-pipelines.universal/ShaderLibrary/Deprecated.hlsl";

    private enum TokenState
    {
        TypeOrParamArgs,
        Name,
        OptionalType
    }

    private static HashSet<string> s_ShaderTypes = new HashSet<string>() {
        "void",
        "bool",
        "int",
        "int2",
        "int3",
        "int4",
        "uint",
        "uint2",
        "uint3",
        "uint4",
        "float",
        "float2",
        "float3",
        "float4",
        "half",
        "half2",
        "half3",
        "half4",
        "real",
        "real2",
        "real3",
        "real4",
    };

    private static HashSet<string> s_FunctionArguments = new HashSet<string>() {
        "in",
        "inout",
        "out",
        "uniform",
    };

    private static Dictionary<string, HashSet<string>> typeToAllowedChanges = new Dictionary<string, HashSet<string>>()
    {
        {
            "void",
            new HashSet<string> {
                "int",
                "real",
                "half",
                "float"
            }
        }, {
            "bool",
            new HashSet<string>()
        }, {
            "int",
            new HashSet<string> {
                "real",
                "half",
                "float"
            }
        },{
            "int2",
            new HashSet<string> {
                "real2",
                "float2"
            }
        }, {
            "int3",
            new HashSet<string> {
                "real3",
                "half3",
                "float3"
            }
        }, {
            "int4",
            new HashSet<string> {
                "real4",
                "half4",
                "float4"
            }
        }, {
            "real",
            new HashSet<string> {
                "int",
                "half",
                "float",
            }
        }, {
            "real2",
            new HashSet<string> {
                "int2",
                "half2",
                "float2",
            }
        }, {
            "real3",
            new HashSet<string> {
                "int3",
                "half3",
                "float3",
            }
        }, {
            "real4",
            new HashSet<string> {
                "int4",
                "half4",
                "float4",
            }
        }, {
            "half",
            new HashSet<string> {
                "int",
                "real",
                "float"
            }
        },{
            "half2",
            new HashSet<string> {
                "int2",
                "real2",
                "float2"
            }
        }, {
            "half3",
            new HashSet<string> {
                "int3",
                "real3",
                "float3"
            }
        }, {
            "half4",
            new HashSet<string> {
                "int4",
                "real4",
                "float4"
            }
        }, {
            "float",
            new HashSet<string> {
                "int",
                "real",
                "half",
            }
        }, {
            "float2",
            new HashSet<string> {
                "int2",
                "real2",
                "half2"
            }
        }, {
            "float3",
            new HashSet<string> {
                "int3",
                "real3",
                "half3"
            }
        }, {
            "float4",
            new HashSet<string> {
                "int4",
                "real4",
                "half4"
            }
        }
    };

    /*******************************
     * Public functions...
     *******************************/

    public static bool SplitVersionInfo(string versionString, out int majorVersion, out int minorVersion, out int patchVersion)
    {
        majorVersion = 0;
        minorVersion = 0;
        patchVersion = 0;

        string[] versions = versionString.Split('.');
        if (!int.TryParse(versions[0], out majorVersion)) return false;
        if (!int.TryParse(versions[1], out minorVersion)) return false;
        if (!int.TryParse(versions[2], out patchVersion)) return false;

        return true;
    }

    public static PackageFunctionsSaveData CreateShaderAPIList(string version, string rootDirectory, string[] directories, string[] searchPatterns = null)
    {
        if (searchPatterns == null)
            searchPatterns = new[] {""};

        PackageFunctionsSaveData packageFunctions = new PackageFunctionsSaveData();
        packageFunctions.version = version;
        packageFunctions.date = DateTime.Now.ToString();

        List<FileFunctionsSaveData> filesData = new List<FileFunctionsSaveData>();
        // Traverse the folders + files and parse the shaders
        for (int i = 0; i < directories.Length; i++)
        {
            string directory = directories[i];
            if (!Directory.Exists(directory))
            {
                Debug.LogError("ShaderParser.RetrieveAll(): Path \"" + directory + "\" does not exists!");
                continue;
            }

            for (int j = 0; j < searchPatterns.Length; j++)
            {
                string[] files = Directory.GetFiles(directory, searchPatterns[j]);
                for (int k = 0; k < files.Length; k++)
                {
                    string filePath = files[k];
                    int index = filePath.IndexOf(rootDirectory);

                    // Extract the filename...
                    int rootDirLength = rootDirectory.Length + 1;
                    string filePathShort;
                    filePathShort = index < 0 ? filePath : filePath.Substring(rootDirLength, filePath.Length - rootDirLength);

                    // Parse the file and extract all functions and their variations...
                    FileFunctionsSaveData fileData = new FileFunctionsSaveData();
                    fileData.name = filePathShort;
                    fileData.functions = ParseFile(filePath);
                    filesData.Add(fileData);
                }
            }
        }

        packageFunctions.files = filesData.ToArray();
        return packageFunctions;
    }

    public static bool LoadShaderAPIList(string fileName, out PackageFunctionsSaveData packageFunctionsSaveData)
    {
        packageFunctionsSaveData = new PackageFunctionsSaveData();
        if (!File.Exists(fileName))
        {
            return false;
        }

        using (StreamReader sr = File.OpenText(fileName))
        {
            string s = sr.ReadToEnd();
            packageFunctionsSaveData = JsonUtility.FromJson<PackageFunctionsSaveData>(s);
        }

        return true;
    }

    public static void SavePackageFileToDisk(PackageFunctionsSaveData packageFunctions, string graphicsDirectory, string saveFilePath)
    {
        // Get the package version
        Debug.Log("Saving to \"" + saveFilePath + "\"");
        try
        {
            // Check if file already exists. If yes, delete it.
            if (File.Exists(saveFilePath))
            {
                File.Delete(saveFilePath);
            }

            // Save the file
            using (StreamWriter sw = File.CreateText(saveFilePath))
            {
                sw.WriteLine(JsonUtility.ToJson(packageFunctions, true));
            }
        }
        catch (Exception Ex)
        {
            Debug.Log(Ex.ToString());
        }
    }

    public static bool ComparePackageFunctions(bool isAMajorVersionChange, PackageFunctionsSaveData prevPackage, PackageFunctionsSaveData currentPackage, out string log)
    {
        bool passedTest = true;
        StringBuilder allLogSB = new StringBuilder();
        StringBuilder changedLogSB = new StringBuilder();
        StringBuilder errorLogSB = new StringBuilder();

        // Find deprecated functions in the current package...
        bool foundDeprecatedFile = FindFile(kDeprecatedFilePath, currentPackage, out FileFunctionsSaveData deprecatedFile);
        Dictionary<string, ShaderFunction> deprecatedFunctionsDict = (foundDeprecatedFile) ? CreateDictFromFile(deprecatedFile) : new Dictionary<string, ShaderFunction>();

        // For each file...
        for (int i = 0; i < prevPackage.files.Length; i++)
        {
            StringBuilder allSB = new StringBuilder();
            StringBuilder changedSB = new StringBuilder();
            StringBuilder errorSB = new StringBuilder();

            FileFunctionsSaveData prevFile = prevPackage.files[i];
            bool isDeprecatedFile = prevFile.name == deprecatedFile.name;
            string fileName = prevFile.name;
            int numOfFunctions = prevFile.functions.Length;

            if (numOfFunctions == 0)
            {
                allLogSB.AppendLine(fileName + "\n\tNo Functions.\n");
                continue;
            }

            // Find the same file and functions in the current package
            // If we didn't find it, we must check the deprecated file for the functions...
            if (!FindFile(fileName, currentPackage, out FileFunctionsSaveData curFile))
            {
                if (!foundDeprecatedFile)
                {
                    string error = fileName + "\n\tUnable to find the current functions in this file or in deprectated.hlsl.\n";
                    allLogSB.AppendLine(error);
                    errorLogSB.AppendLine(error);
                    passedTest = false;
                    continue;
                }
            }

            // Compare the functions in the previous file to the current one + deprecated...
            passedTest &= CompareFunctions(isAMajorVersionChange, isDeprecatedFile, prevFile, curFile, deprecatedFunctionsDict, ref allSB, ref changedSB, ref errorSB);

            allLogSB.AppendLine(fileName + "\n" + allSB);
            if (changedSB.Length > 0)
                changedLogSB.AppendLine(fileName + "\n" + changedSB);
            if (errorSB.Length > 0)
                errorLogSB.AppendLine(fileName + "\n" + errorSB);
        }

        if (passedTest)
        {
            log = "========================================\n"
                  + "\t\t\tFull Log\n"
                  +"========================================\n\n"
                  + allLogSB;
        }
        else
        {
            log = "\n\n========================================\n"
                + "\t\t\tErrors\n"
                + "========================================\n\n"
                + errorLogSB
                + "\n========================================\n"
                + "\t\t\tFull Log\n"
                +"========================================\n\n"
                + allLogSB;
        }
        return passedTest;
    }

    /*******************************
     * Private functions...
     *******************************/

    private static ShaderFunction[] ParseFile(string fileName)
    {
        List<ShaderFunction> allFunctions = new List<ShaderFunction>();
        Dictionary<string, int> allFunctionsMap = new Dictionary<string, int>();

        string[] fileLines = File.ReadAllLines(fileName);
        for (int lineIndex = 0; lineIndex < fileLines.Length;)
        {
            string line = fileLines[lineIndex];
            if (line.Length == 0)
            {
                lineIndex++;
                continue;
            }

            if (ExtractFunction(ref lineIndex, ref fileLines, out var extractedFunction))
            {
                bool hasFunctionName = allFunctionsMap.TryGetValue(extractedFunction.functionName, out var functionIndex);
                if (hasFunctionName)
                {
                    ShaderFunction sf = allFunctions[functionIndex];
                    ShaderFunctionVariation[] sfs = allFunctions[functionIndex].variations;
                    ShaderFunctionVariation[] sfsNew = new ShaderFunctionVariation[sfs.Length+1];
                    for (int i = 0; i < sfs.Length; i++)
                    {
                        sfsNew[i] = sfs[i];
                    }
                    sfsNew[sfs.Length] = extractedFunction.variations[0];
                    sf.variations = sfsNew;
                    allFunctions[functionIndex] = sf;
                }
                else
                {
                    allFunctionsMap.Add(extractedFunction.functionName, allFunctions.Count);
                    allFunctions.Add(extractedFunction);
                }
            }

            lineIndex++;
        }

        return allFunctions.ToArray();
    }

    private static bool ExtractFunction(ref int lineIndex, ref string[] fileLines, out ShaderFunction sf)
    {
        sf = new ShaderFunction();
        ShaderFunctionVariation variation = new ShaderFunctionVariation();

        string line = fileLines[lineIndex];
        string[] lineWords = line.Trim().Split(' ');

        // ignore inline
        int functionReturnTypeIndex = lineWords[0].ToLower().Equals(k_Inline) ? 1 : 0;
        variation.returnType = lineWords[functionReturnTypeIndex];


        // Check if the line starts with any of the allowed values
        if (!s_ShaderTypes.Contains(variation.returnType))
            return false;

        // It's not a function declaration if it contains => \\
        int forward_slash = SearchWordArrayContains(lineWords, "\\");
        if (forward_slash != -1)
            return false;

        // It's not a function declaration if it contains => ;
        int semicolon = SearchWordArrayContains(lineWords, ";");
        if (semicolon != -1)
            return false;

        // It's not a function declaration if we don't have open parentheses,
        int openParenthesesIndex = SearchWordArrayContains(lineWords, "(");
        if (openParenthesesIndex == -1)
            return false;

        // It's not a function declaration if equals sign is before the open parentheses,
        int equalsIndex = SearchWordArrayContains(lineWords, "=");
        if (equalsIndex != -1 && equalsIndex < openParenthesesIndex)
            return false;

        //
        // We've now concluded that this is a function
        //
        int openBracketsIndex = SearchWordArrayContains(lineWords, "{");
        if (openBracketsIndex != -1)
        {
            line = line.Substring(0, line.Length - openBracketsIndex);
        }

        variation.functionAPI = line;
        sf.functionName = lineWords[functionReturnTypeIndex + 1];
        sf.functionName = sf.functionName.Substring(0, sf.functionName.IndexOf('(')).Trim();

        // Sometimes function declarations are split into in several lines
        int closeParenthesesIndex = SearchWordArrayContains(lineWords, ")");
        if (closeParenthesesIndex == -1)
        {
            while (closeParenthesesIndex == -1 && lineIndex < fileLines.Length)
            {
                lineIndex++;
                string next_line = fileLines[lineIndex];
                variation.functionAPI += next_line.Trim();
                closeParenthesesIndex = next_line.IndexOf(')');
            }
        }

        // split the parameters
        int ooo = variation.functionAPI.IndexOf("(");
        int ccc = variation.functionAPI.LastIndexOf(")");
        string parametersString = variation.functionAPI.Substring(ooo + 1, ccc - ooo - 1);
        variation.parameters = ExtractParameters(parametersString).ToArray();

        sf.variations = new []{variation};

        return true;
    }

    private static List<ShaderParameter> ExtractParameters(string parameters)
    {
        int length = parameters.Length;
        List<ShaderParameter> tokenizedParams = new List<ShaderParameter>();
        StringBuilder sb = new StringBuilder();

        TokenState tokenState = TokenState.TypeOrParamArgs;

        bool isADefinitionParam = false;
        int openParenthesisCount = 0;
        string type = "";
        string functionArgs = "";
        for (int index = 0; index < length; index++)
        {
            char c = parameters[index];
            sb.Append(c);

            switch (tokenState)
            {
                // in, out, float, half....
                case TokenState.TypeOrParamArgs:
                    HandleTypeOrArgsToken(index, length, c, ref sb, ref tokenizedParams, ref openParenthesisCount, ref isADefinitionParam, ref type, ref functionArgs, ref tokenState);
                    break;

                // Parameter names
                case TokenState.Name:
                    HandleNameToken(index, length, c, ref sb, ref tokenizedParams, ref openParenthesisCount, ref isADefinitionParam, ref type, ref functionArgs, ref tokenState);
                    break;

                // Check if optional and move on to the other params...
                case TokenState.OptionalType:
                    HandleOptionalToken(index, length, c, ref sb, ref tokenizedParams, ref openParenthesisCount, ref isADefinitionParam, ref type, ref functionArgs, ref tokenState);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return tokenizedParams;
    }

    private static void HandleTypeOrArgsToken(int index, int length, char c, ref StringBuilder sb, ref List<ShaderParameter> tokenizedParams, ref int openParenthesisCount, ref bool isADefinitionParam, ref string type, ref string functionArgs, ref TokenState state)
    {
        switch (c)
        {
            case ',':
                if (openParenthesisCount <= 0 && isADefinitionParam)
                {
                    type = sb.ToString().Trim();
                    type = type.Substring(0, type.Length - 1);
                    ShaderParameter sp = new ShaderParameter()
                    {
                        name = "None",
                        type = type,
                        functionArg = functionArgs,
                        hasFunctionArgs = false,
                        isOptional = false,
                    };
                    tokenizedParams.Add(sp);

                    sb.Clear();

                    state = TokenState.TypeOrParamArgs;
                }

                break;
            case '(':
                openParenthesisCount++;
                isADefinitionParam = true;
                break;
            case ')':
                openParenthesisCount--;
                break;
            case ' ':
                if (openParenthesisCount <= 0)
                {
                    string sss = sb.ToString().Trim();
                    if (sss.Length == 0)
                    {
                        sb.Clear();
                        return;
                    }

                    if (sss.Equals("in")
                        || sss.Equals("inout")
                        || sss.Equals("out"))
                    {
                        functionArgs = sss;
                        sb.Clear();
                        return;
                    }

                    type = sss;
                    state = TokenState.Name;
                    openParenthesisCount = 0;
                    sb.Clear();
                }

                break;
        }
    }

    private static void HandleNameToken(int index, int length, char c, ref StringBuilder sb, ref List<ShaderParameter> tokenizedParams, ref int openParenthesisCount, ref bool isADefinitionParam, ref string type, ref string functionArgs, ref TokenState state)
    {
        switch (c)
        {
            case '=':
                sb.Length = sb.Length - 1;
                tokenizedParams.Add(new ShaderParameter() {
                    name = sb.ToString().Trim(),
                    type = type,
                    functionArg = functionArgs,
                    hasFunctionArgs = functionArgs.Length > 0,
                    isOptional = true,
                });

                type = "";
                functionArgs = "";
                sb.Clear();

                state = TokenState.OptionalType;
                break;
            case ',':
                sb.Length = sb.Length - 1;
                tokenizedParams.Add(new ShaderParameter() {
                    name = sb.ToString().Trim(),
                    type = type,
                    functionArg = functionArgs,
                    hasFunctionArgs = functionArgs.Length > 0,
                    isOptional = false,
                });

                type = "";
                functionArgs = "";
                sb.Clear();

                state = TokenState.TypeOrParamArgs;
                break;
            default:
                if (index == length - 1)
                {
                    tokenizedParams.Add(new ShaderParameter() {
                        name = sb.ToString().Trim(),
                        type = type,
                        functionArg = functionArgs,
                        hasFunctionArgs = functionArgs.Length > 0,
                        isOptional = false,
                    });

                    type = "";
                    functionArgs = "";
                    sb.Clear();

                    state = TokenState.TypeOrParamArgs;
                }

                break;
        }
    }

    private static void HandleOptionalToken(int index, int length, char c, ref StringBuilder sb, ref List<ShaderParameter> tokenizedParams, ref int openParenthesisCount, ref bool isADefinitionParam, ref string type, ref string functionArgs, ref TokenState state)
    {
        switch (c)
        {
            case '(':
                openParenthesisCount++;
                break;

            case ')':
                openParenthesisCount--;

                if (openParenthesisCount <= 0)
                {
                    state = TokenState.TypeOrParamArgs;
                    sb.Clear();
                }
                break;

            case ',':
                if (openParenthesisCount <= 0)
                {
                    state = TokenState.TypeOrParamArgs;
                    sb.Clear();
                }
                break;
        }
    }

    private static bool CompareFunctions(bool isAMajorVersionChange, bool isDeprecatedFile, FileFunctionsSaveData prevFile, FileFunctionsSaveData curFile, Dictionary<string, ShaderFunction> deprecatedFunctionsDict, ref StringBuilder allLogSB, ref StringBuilder changedLogSB, ref StringBuilder errorLogSB)
    {
        bool foundAllFunctionsAndVariations = true;

        // Create a dictionary of functions in current packages
        Dictionary<string, ShaderFunction> curFunctionsDict = CreateDictFromFile(curFile);

        ShaderFunction[] prevFunctions = prevFile.functions;
        for (int p = 0; p < prevFunctions.Length; p++)
        {
            ShaderFunction prevFunction = prevFunctions[p];
            string prevFunctionName = prevFunction.functionName;
            curFunctionsDict.TryGetValue(prevFunctionName, out var curFunction);

            // Make sure we have all the variations of that function in the same file or deprecated...
            foundAllFunctionsAndVariations &= CompareFunctionVariations(isAMajorVersionChange, isDeprecatedFile, prevFunction, curFunction, deprecatedFunctionsDict, ref allLogSB, ref changedLogSB, ref errorLogSB);
        }

        return foundAllFunctionsAndVariations;
    }

    private static bool CompareFunctionVariations(bool isAMajorVersionChange, bool isDeprecatedFile, ShaderFunction prevFunction, ShaderFunction curFunction, Dictionary<string, ShaderFunction> deprecatedFunctionsDict, ref StringBuilder allLogSB, ref StringBuilder changedLogSB, ref StringBuilder errorLogSB)
    {
        bool foundAllVariations = true;

        ShaderFunctionVariation[] prevVariations = prevFunction.variations;
        for (int i = 0; i < prevVariations.Length; i++)
        {
            ShaderFunctionVariation prevVariation = prevFunction.variations[i];

            // Compare against the functions in the file, if available...
            bool foundVariation = false;
            if (curFunction.variations != null)
            {
                for (int j = 0; j < curFunction.variations.Length; j++)
                {
                    ShaderFunctionVariation curVariation = curFunction.variations[j];
                    foundVariation |= CompareFunctionVariation(prevVariation, curVariation);

                    if (foundVariation)
                        break;
                }
            }

            if (foundVariation)
            {
                LogPassed(prevVariation.functionAPI, ref allLogSB);
                continue;
            }

            // Check Deprecated
            bool foundFunctionInDeprecated = deprecatedFunctionsDict.TryGetValue(prevFunction.functionName, out ShaderFunction deprecatedFunction);
            if (foundFunctionInDeprecated)
            {
                for (int t = 0; t < deprecatedFunction.variations.Length; t++)
                {
                    ShaderFunctionVariation deprecatedVariation = deprecatedFunction.variations[t];
                    foundVariation |= CompareFunctionVariation(prevVariation, deprecatedVariation);

                    if (foundVariation)
                        break;
                }

                if (foundVariation)
                {
                    LogMoved(prevVariation.functionAPI, ref allLogSB);
                    LogMoved(prevVariation.functionAPI, ref changedLogSB);
                    continue;
                }
            }

            if (isDeprecatedFile && isAMajorVersionChange)
            {
                LogRemoved(prevVariation.functionAPI, ref allLogSB);
                continue;
            }

            LogFailed(prevVariation.functionAPI, ref allLogSB);
            LogFailed(prevVariation.functionAPI, ref errorLogSB);
            foundAllVariations = false;
        }

        return foundAllVariations;
    }

    private static void LogPassed(string functionAPI, ref StringBuilder sb)
    {
        sb.AppendLine("\tPassed -> " + functionAPI);
    }

    private static void LogMoved(string functionAPI, ref StringBuilder sb)
    {
        sb.AppendLine("\tMoved -> " + functionAPI + " -> \"" + kDeprecatedFilePath + "\"");
    }

    private static void LogRemoved(string functionAPI, ref StringBuilder sb)
    {
        sb.AppendLine("\tRemoved -> " + functionAPI);
    }

    private static void LogFailed(string functionAPI, ref StringBuilder sb)
    {
        sb.AppendLine("\tFailed -> " + functionAPI + ": Not found");
    }

    private static bool CompareFunctionVariation(ShaderFunctionVariation prevFunction, ShaderFunctionVariation curFunction)
    {
        // Skip if the return value isn't the same
        if (prevFunction.returnType != curFunction.returnType)
        {
            if (!typeToAllowedChanges[prevFunction.returnType].Contains(curFunction.returnType))
            {
                return false;
            }
        }

        // Found the function if both functions do not contain parameters...
        if (prevFunction.parameters.Length == 0 && curFunction.parameters.Length == 0)
            return true;

        int prevParametersLength = prevFunction.parameters.Length;
        int curParametersLength = curFunction.parameters.Length;

        // Skip if the previous function has more params
        if (prevParametersLength > curParametersLength)
            return false;

        // Skip if the extra parameters are not all optional...
        if (prevParametersLength < curParametersLength)
        {
            bool everyExtraParamIsOptional = true;
            for (int k = prevParametersLength; k < curParametersLength; k++)
            {
                ShaderParameter curParameter = curFunction.parameters[k];
                everyExtraParamIsOptional &= curParameter.isOptional;
            }

            if (!everyExtraParamIsOptional)
            {
                return false;
            }
        }

        // Check each parameter....
        for (int k = 0; k < prevFunction.parameters.Length; k++)
        {
            if (k >= curFunction.parameters.Length)
            {
                break;
            }

            ShaderParameter prevParameter = prevFunction.parameters[k];
            ShaderParameter curParameter = curFunction.parameters[k];

            // in, out, inout... don't match
            if (prevParameter.hasFunctionArgs != curParameter.hasFunctionArgs)
            {
                return false;
            }

            // float, half, void... don't match
            if (prevParameter.type != curParameter.type)
            {
                return false;
            }

            // previous is optional but the current isn't
            if (prevParameter.isOptional && !curParameter.isOptional)
            {
                return false;
            }
        }

        return true;
    }


    private static bool FindFile(string fileName, PackageFunctionsSaveData package, out FileFunctionsSaveData file)
    {
        file = new FileFunctionsSaveData();
        for (int j = 0; j < package.files.Length; j++)
        {
            file = package.files[j];
            if (file.name.Equals(fileName))
                return true;
        }

        return false;
    }

    private static int SearchWordArrayContains(string[] array, string searchWord)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Contains(searchWord))
                return i;
        }

        return -1;
    }

    private static Dictionary<string, ShaderFunction> CreateDictFromFile(FileFunctionsSaveData file)
    {
        Dictionary<string, ShaderFunction> dict = new Dictionary<string, ShaderFunction>(file.functions.Length);
        for (int m = 0; m < file.functions.Length; m++)
        {
            dict.Add(file.functions[m].functionName, file.functions[m]);
        }
        return dict;
    }
}
