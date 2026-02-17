using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor.Rendering.Converter;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Universal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class BatchModeConverterClassInfo : Attribute
    {
        public string converterType { get; }
        public string containerName { get; }

        public BatchModeConverterClassInfo(string containerName, string converterType)
        {
            this.converterType = converterType;
            this.containerName = containerName;
        }
    }

    /// <summary>
    /// Class for the converter framework.
    /// </summary>
    public static partial class Converters
    {
        // Commands
        const string k_BatchmodeCommand = "-batchmode";
        const string k_HelpCommand = "--help";
        const string k_ListCommand = "--list";
        const string k_ContainerCommand = "-container";
        const string k_TypesFilterCommand = "-typesFilter";
        const string k_InclusiveFlag = "--inclusive";
        const string k_ExclusiveFlag = "--exclusive";

        // List all available containers
        static void ListAvailableConverters()
        {
            // Get all converters and group them by container
            var containersDict = DictionaryPool<string,List<string>>.Get();
            foreach (var container in TypeCache.GetTypesWithAttribute<BatchModeConverterClassInfo>())
            {
                if (container.IsAbstract || container.IsInterface)
                    continue;

                var info = container.GetCustomAttribute<BatchModeConverterClassInfo>();
                if (!containersDict.ContainsKey(info.containerName))
                {
                    containersDict[info.containerName] = new List<string>();
                }

                containersDict[info.containerName].Add(info.converterType);
            }

            // Organize the information
            StringBuilder convertersMessage = StringBuilderPool.Get();
            foreach (var converter in containersDict)
            {
                convertersMessage.AppendLine($"Container: {converter.Key}");
                convertersMessage.AppendLine("Available converter types:");
                convertersMessage.AppendLine(String.Join($"\n\t- ", converter.Value));
                convertersMessage.AppendLine("\n");
            }

            Debug.Log($"Available containers and their converter types\n{convertersMessage}");
        }

        static void LogHelp()
        {
            StringBuilder helpMessage = StringBuilderPool.Get();

            // Description
            helpMessage.AppendLine("\n");
            helpMessage.AppendLine( "The batchmode converter is a tool to help you upgrade your projects from one scriptable render pipeline\n" +
                                   "to another. Using this API can lead to incomplete or unpredictable conversion outcomes.\n" +
                                   "For reliable results, please perform the conversion via the dedicated window: Window > Rendering > Render Pipeline Converter.");
            helpMessage.AppendLine("\n");

            // Usage
            helpMessage.AppendLine($"usage: \t<path to Unity executable> -projectPath <project path> {k_BatchmodeCommand} -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine\n" +
                                   $"\t \t[{k_HelpCommand}] [{k_ListCommand}]\n" +
                                   $"\t \t[{k_ContainerCommand} <name of container>] [{k_TypesFilterCommand} <types to include or exclude>] [{k_InclusiveFlag}|{k_ExclusiveFlag}]");
            helpMessage.AppendLine("\n");

            // Commands
            helpMessage.AppendLine("Commands");
            helpMessage.AppendLine($"\t{k_HelpCommand} \t \t Show this help and exit.");
            helpMessage.AppendLine($"\t{k_ListCommand} \t \t List all available converters and exit.");
            helpMessage.AppendLine("\n");

            // Options
            helpMessage.AppendLine("Options");
            helpMessage.AppendLine($"\t{k_ContainerCommand} <name of container> \t \t \t The name of the container which will be batched (required).");
            helpMessage.AppendLine($"\t{k_TypesFilterCommand} <types to include or exclude> \t The list of converters types that will be either included or excluded from batching. These converters need to be part of the passed in container for them to run.");
            helpMessage.AppendLine($"\t{k_InclusiveFlag}|{k_ExclusiveFlag} \t \t \t Whether the list of converters specified with {k_TypesFilterCommand} will be included or excluded when batching.");
            helpMessage.AppendLine("\n");

            helpMessage.AppendLine("Notes");
            helpMessage.AppendLine($"\t Use either {k_InclusiveFlag} or {k_ExclusiveFlag}, not both.");
            helpMessage.AppendLine($"\t When using {k_InclusiveFlag}, you must specify values for {k_TypesFilterCommand}.");
            helpMessage.AppendLine($"\t Values for {k_TypesFilterCommand} must be provided as a space-separated list: {k_TypesFilterCommand} typeA typeB typeC");
            helpMessage.AppendLine("\nOnline documentation: https://docs.unity3d.com/6000.5/Documentation/Manual/urp/convert-assets-to-urp.html\n");

            Debug.Log(helpMessage);
        }

        internal static void SuggestUpdatedCommand(string container, List<string> converters, bool isInclusive)
        {
            var containerParameter = $" {k_ContainerCommand} {container}";
            var converterParameter = converters.Count == 0 ? "" : $" {k_TypesFilterCommand} {string.Join(" ", converters)}";
            var filterModeFlag = isInclusive ? $" {k_InclusiveFlag}" : $" {k_ExclusiveFlag}";
            Debug.Log("The method you're trying to use is deprecated. Try running the following command in the command line:\n" +
                      $"<path to Unity> -projectPath <path to project> {k_BatchmodeCommand} -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine{filterModeFlag}{containerParameter}{converterParameter}");
        }

        // Return all converters we will be running
        internal static List<Type> FilterConverters(string containerName, List<string> convertedTypesFilter, bool isInclusive = false)
        {
            var allConverters = TypeCache.GetTypesWithAttribute<BatchModeConverterClassInfo>();
            var filteredList = new List<Type>(allConverters.Count);

            // early return
            if (isInclusive && convertedTypesFilter.Count == 0)
                return filteredList; // nothing included in the list

            foreach (var converterType in allConverters)
            {
                var converterInfo = converterType.GetCustomAttribute<BatchModeConverterClassInfo>();
                if (containerName != converterInfo.containerName)
                    continue;

                var isTypeInFilteredList = convertedTypesFilter.Contains(converterInfo.converterType);

                // add if inclusive and in the included list, add if exclusive and not in the excluded list
                if (isInclusive == isTypeInFilteredList)
                    filteredList.Add(converterType);
            }

            return filteredList;
        }

        internal static Dictionary<string, List<string>> ParseArgs(string[] rawArgs)
        {
            int batchmodeArgIndex = Array.FindIndex(rawArgs, arg => arg == k_BatchmodeCommand);
            if (batchmodeArgIndex == -1)
                throw new ArgumentException($"No {k_BatchmodeCommand} argument found. Exiting.");

            var parsedArgs = DictionaryPool<string,List<string>>.Get();
            parsedArgs["Flags"] = new List<string>();

            string currentKey = null; // are we collecting values for a key?

            for(int i = batchmodeArgIndex + 1; i < rawArgs.Length; i++)
            {
                if (rawArgs[i].StartsWith("--")) // new flag
                {
                    parsedArgs["Flags"].Add(rawArgs[i]);
                    currentKey = "";
                }
                else if (rawArgs[i].StartsWith("-")) // new argument
                {
                    parsedArgs.Add(rawArgs[i], new List<string>());
                    currentKey = rawArgs[i];
                }
                else // adding to the last argument
                {
                    if (String.IsNullOrEmpty(currentKey))
                    {
                        throw new ArgumentException($"Unrecognized argument: {rawArgs[i]}");
                    }

                    parsedArgs[currentKey].Add(rawArgs[i]);
                }
            }

            return parsedArgs;
        }

        /// <summary>
        /// Call this method to run all the converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched. All Converters in this Container will run if prerequisites are met.</param>
        public static void RunInBatchMode(string containerName)
        {
             RunInBatchMode(containerName, null, isInclusive: false);
        }

        /// <summary>
        /// Call this method to run a specific list of converters in a specific container in batch mode.
        /// </summary>
        /// <param name="containerName">The name of the container which will be batched.</param>
        /// <param name="convertedTypes">The list of converters that will be either included or excluded from batching. These converters need to be part of the passed in container for them to run.</param>
        /// <param name="isInclusive">Whether the list of converters will be included or excluded when batching.</param>
        public static void RunInBatchMode(string containerName, List<string> convertedTypes, bool isInclusive)
        {
            var types = FilterConverters(containerName, convertedTypes, isInclusive);
            RunInBatchMode(types);
        }

        /// <summary>
        /// Call this method to run a specific list of converters in a specific container in batch mode.
        /// Entry point for: -executeMethod UnityEditor.Rendering.Universal.Converters.RunInBatchModeCmdLine
        /// </summary>
        public static void RunInBatchModeCmdLine()
        {
            Debug.Log("BATCH MODE COMMAND LINE\n");
            var exitCode = 0;
            try
            {
                var args = ParseArgs(Environment.GetCommandLineArgs());
                // If help requested, print and exit
                if (args["Flags"].Contains(k_HelpCommand))
                {
                    LogHelp();
                    EditorApplication.Exit(0);
                    return;
                }

                if (args["Flags"].Contains(k_ListCommand))
                {
                    ListAvailableConverters();
                    EditorApplication.Exit(0);
                    return;
                }

                // ContainerType -----
                if (!args.TryGetValue(k_ContainerCommand, out var converter))
                    throw new ArgumentException($"Missing required {k_ContainerCommand} <name of container>. Use {k_ListCommand} to see available converter.");
                if(converter.Count != 1)
                    throw new ArgumentException($"Please specify only one container. Use {k_ListCommand} to see available converters.");

                // Filter + Include/Exclude ------
                var hasInclusive = args["Flags"].Contains(k_InclusiveFlag);
                var hasExclusive = args["Flags"].Contains(k_ExclusiveFlag);
                var hasTypesFilter = args.TryGetValue(k_TypesFilterCommand, out var filteredTypes);

                if (hasTypesFilter && hasExclusive == hasInclusive)
                {
                    throw new ArgumentException($"When using {k_TypesFilterCommand}, please specify exactly one of {k_InclusiveFlag} or {k_ExclusiveFlag}. Use {k_HelpCommand} for usage.");
                }

                if (hasInclusive && !hasTypesFilter)
                    throw new ArgumentException($"When using {k_InclusiveFlag} mode, please specify types to include using {k_TypesFilterCommand} otherwise nothing will be converted. " +
                                                $"Use {k_ListCommand} to see available types.");

                RunInBatchMode(converter[0], filteredTypes, hasInclusive);
            }
            catch (Exception ex)
            {
                Debug.LogError($"ConverterCli failed: {ex.Message}\n{ex}");
                exitCode = 1;
            }
            finally
            {
                EditorApplication.Exit(exitCode);
            }
        }

         /// <summary>
        /// Call this method to run a specific list of converters in batch mode.
        /// </summary>
        /// <param name="converterTypes">The list of converters to run</param>
        /// <returns>False if there were errors.</returns>
        internal static bool RunInBatchMode(List<Type> converterTypes)
        {
            Debug.LogWarning($"Using this API can lead to incomplete or unpredictable conversion outcomes. For reliable results, please perform the conversion via the dedicated window: Window > Rendering > Render Pipeline Converter.");

            List<IRenderPipelineConverter> convertersToExecute = new();

            bool errors = false;
            foreach (var type in converterTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as IRenderPipelineConverter;
                    if (instance == null)
                    {
                        Debug.LogWarning($"{type} is not a converter type.");
                        errors = true;
                    }
                    else
                        convertersToExecute.Add(instance);
                }
                catch
                {
                    Debug.LogWarning($"Unable to create instance of type {type}.");
                    errors = true;
                }
            }

            if (errors)
            {
                Debug.LogWarning($"Please use any of the given Converter Types.");
                ListAvailableConverters();
            }

            BatchConverters(convertersToExecute);

            return !errors;
        }

        static void BatchConverters(List<IRenderPipelineConverter> converters)
        {
            foreach (var converter in converters)
            {
                var sb = StringBuilderPool.Get();

                converter.Scan(OnConverterCompleteDataCollection);

                void OnConverterCompleteDataCollection(List<IRenderPipelineConverterItem> items)
                {
                    converter.BeforeConvert();
                    foreach (var item in items)
                    {
                        var status = converter.Convert(item, out var message);
                        switch (status)
                        {
                            case Status.Pending:
                                throw new InvalidOperationException("Converter returned a pending status when converting. This is not supported.");
                            case Status.Error:
                            case Status.Warning:
                                sb.AppendLine($"- {item.name} ({status}) ({message})");
                                break;
                            case Status.Success:
                            {
                                sb.AppendLine($"- {item.name} ({status})");
                            }
                            break;
                        }
                    }
                    converter.AfterConvert();

                    var conversionResult = sb.ToString();
                    if (!string.IsNullOrEmpty(conversionResult))
                        Debug.Log(sb.ToString());
                }
            }

            AssetDatabase.SaveAssets();
        }
    }
}
