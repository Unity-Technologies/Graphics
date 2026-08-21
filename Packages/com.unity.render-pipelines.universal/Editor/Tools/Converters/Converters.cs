using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor.Rendering.Converter;
using UnityEngine;

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
        const string k_ScanToFileCommand = "-scanToFile";
        const string k_InclusiveFlag = "--inclusive";
        const string k_ExclusiveFlag = "--exclusive";

        // Json layout written by the -scanToFile command
        [Serializable]
        class ScanResultItem
        {
            public string name;
            public string info;
            public string type;
        }

        [Serializable]
        class ScanResultConverter
        {
            public string container;
            public string converterType;
            public List<ScanResultItem> items = new();
        }

        [Serializable]
        class ScanResult
        {
            // One of the k_ScanStatus values
            public string status;
            public int convertersCompleted;
            public int convertersFailed;
            public List<ScanResultConverter> converters = new();
        }

        // The scan started by ScanToFile, until it completes or is cancelled. Converters report the items they found
        // through a callback, so a scan outlives the ScanToFile call that started it.
        class ScanOperation
        {
            public readonly string filePath;
            public readonly int converterCount;
            public readonly Action<string> onScanFinished;
            public readonly ScanResult result = new();

            public int pendingScans;

            public ScanOperation(string filePath, int converterCount, Action<string> onScanFinished)
            {
                this.filePath = filePath;
                this.converterCount = converterCount;
                this.onScanFinished = onScanFinished;
                pendingScans = converterCount;
            }
        }

        static ScanOperation s_CurrentScan;

        /// <summary>
        /// True while a scan started by ScanToFile is still waiting for converters to report. Converters can scan
        /// asynchronously, so this can remain true after ScanToFile has returned.
        /// </summary>
        internal static bool IsScanInProgress => s_CurrentScan != null;

        // List all available containers
        static void ListAvailableConverters()
        {
            // Get all converters and group them by container
            var containersDict = UnityEngine.Pool.DictionaryPool<string,List<string>>.Get();
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
                                   $"\t \t[{k_ContainerCommand} <name of container>] [{k_TypesFilterCommand} <types to include or exclude>] [{k_InclusiveFlag}|{k_ExclusiveFlag}]\n" +
                                   $"\t \t[{k_ScanToFileCommand} <file name>]");
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
            helpMessage.AppendLine($"\t{k_ScanToFileCommand} <file name> \t \t \t Only scan the selected converters and write the results as json to <file name> in the project's temporary folder. Nothing is converted.");
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
            convertedTypesFilter ??= new List<string>();

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

            var parsedArgs = UnityEngine.Pool.DictionaryPool<string,List<string>>.Get();
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

                // ScanToFile ------
                if (args.TryGetValue(k_ScanToFileCommand, out var scanFileName))
                {
                    if (scanFileName.Count != 1 || string.IsNullOrEmpty(scanFileName[0]))
                        throw new ArgumentException($"Please specify a single file name: {k_ScanToFileCommand} <file name>.");

                    string scanStatus = null;
                    var scanFilePath = ScanToFile(FilterConverters(converter[0], filteredTypes, hasInclusive),
                        scanFileName[0], status => scanStatus = status);

                    // Batch mode exits as soon as this method returns, so a scan which didn't finish, or which couldn't write its results, is an error
                    if (scanStatus != "Completed")
                        throw new InvalidOperationException($"The converters did not complete their scan (status: {scanStatus ?? "still scanning"}), {scanFilePath} does not contain usable results.");
                }
                else
                {
                    RunInBatchMode(converter[0], filteredTypes, hasInclusive);
                }
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

            var convertersToExecute = CreateConverters(converterTypes, out var errors);

            BatchConverters(convertersToExecute);

            return !errors;
        }

        // Instantiate the given converter types, reporting the ones we could not create
        static List<IRenderPipelineConverter> CreateConverters(List<Type> converterTypes, out bool errors)
        {
            List<IRenderPipelineConverter> converters = new();

            errors = false;
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
                        converters.Add(instance);
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

            return converters;
        }

        // Scan the given converters and write the results to json, without converting anything.
        // Some converters scan asynchronously, so the scan may still be running when this returns. onScanFinished is
        // invoked with "Completed", "Failed" if nothing could be scanned or the results could not be written, or
        // "Cancelled" if CancelScan was called before the converters finished.
        internal static string ScanToFile(List<Type> converterTypes, string fileName, Action<string> onScanFinished = null)
        {
            if (s_CurrentScan != null)
                throw new InvalidOperationException("A converter scan is already in progress.");

            var filePath = Path.Combine(Path.GetDirectoryName(FileUtil.GetUniqueTempPathInProject()), fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath)));

            // Remove any previous results, so that they can't be mistaken for the results of this scan
            if (File.Exists(filePath))
                File.Delete(filePath);

            var converters = CreateConverters(converterTypes, out var hasCreationErrors);
            var scan = new ScanOperation(filePath, converters.Count, onScanFinished);

            // A converter could not be created
            if (hasCreationErrors)
                scan.result.convertersFailed = converterTypes.Count - converters.Count;

            s_CurrentScan = scan;

            foreach (var converter in converters)
            {
                var converterInfo = converter.GetType().GetCustomAttribute<BatchModeConverterClassInfo>();
                var converterResult = new ScanResultConverter
                {
                    container = converterInfo != null ? converterInfo.containerName : string.Empty,
                    converterType = converterInfo != null ? converterInfo.converterType : converter.GetType().Name
                };
                scan.result.converters.Add(converterResult);

                var converterFinished = false;

                try
                {
                    converter.Scan(OnConverterCompleteDataCollection);
                }
                catch (Exception e)
                {
                    // A converter that throws must not leave the scan hanging
                    Debug.LogError($"Converter {converterResult.converterType} failed to scan: {e.Message}\n{e}");
                    OnConverterScanFinished(succeeded: false);
                }

                void OnConverterCompleteDataCollection(List<IRenderPipelineConverterItem> items)
                {
                    // Flatten folders to leaf items, those are the ones that would be converted
                    var leafItems = new List<IRenderPipelineConverterItem>();
                    RenderPipelineConverterUtility.CollectLeafItems(items, leafItems);

                    foreach (var item in leafItems)
                    {
                        converterResult.items.Add(new ScanResultItem
                        {
                            name = item.name,
                            info = item.info,
                            type = item.GetType().FullName
                        });
                    }

                    OnConverterScanFinished(succeeded: true);
                }

                void OnConverterScanFinished(bool succeeded)
                {
                    // A cancelled scan ignores the converters which report after it was abandoned
                    if (converterFinished || s_CurrentScan != scan)
                        return;
                    converterFinished = true;

                    if (succeeded)
                        scan.result.convertersCompleted++;
                    else
                        scan.result.convertersFailed++;

                    if (--scan.pendingScans == 0)
                        CompleteScan(scan);
                }
            }

            if (converters.Count == 0)
                CompleteScan(scan);

            return filePath;
        }

        internal static void CancelScan()
        {
            var scan = s_CurrentScan;
            if (scan == null)
                return;

            // Converters have no way of being interrupted, so cancelling detaches from the scan instead: the
            // converters which are still scanning are left to finish, and whatever they report is discarded.
            s_CurrentScan = null;

            Debug.Log($"Converter scan cancelled, {scan.result.convertersCompleted} of {scan.converterCount} converters had finished scanning.");

            scan.onScanFinished?.Invoke("Cancelled");
        }

        static void CompleteScan(ScanOperation scan)
        {
            // Individual converters may have failed and still leave usable results.
            var status = scan.result.convertersCompleted == 0 && scan.result.convertersFailed > 0
                ? "Failed"
                : "Completed";

            scan.result.status = status;

            try
            {
                File.WriteAllText(scan.filePath, JsonUtility.ToJson(scan.result, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to write the scan results to {Path.GetFullPath(scan.filePath)}: {e.Message}\n{e}");
                status = "Failed";
            }
            finally
            {
                s_CurrentScan = null;
            }

            scan.onScanFinished?.Invoke(status);
        }

        static void BatchConverters(List<IRenderPipelineConverter> converters)
        {
            foreach (var converter in converters)
            {
                var sb = StringBuilderPool.Get();

                converter.Scan(OnConverterCompleteDataCollection);

                void OnConverterCompleteDataCollection(List<IRenderPipelineConverterItem> items)
                {
                    // Flatten folders to leaf items for batch conversion
                    var leafItems = new List<IRenderPipelineConverterItem>();
                    RenderPipelineConverterUtility.CollectLeafItems(items, leafItems);

                    converter.BeforeConvert();
                    foreach (var item in leafItems)
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
