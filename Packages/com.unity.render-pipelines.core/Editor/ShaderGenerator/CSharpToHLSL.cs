using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    internal class CSharpToHLSL
    {
        /// <summary>
        ///     Generate all shader code from <see cref="GenerateHLSL" /> attribute.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public static async Task GenerateAll()
        {
            Dictionary<string, List<ShaderTypeGenerator>> sourceGenerators = null;
            try
            {
                // Store per source file path the generator definitions
                sourceGenerators = DictionaryPool<string, List<ShaderTypeGenerator>>.Get();

                // Extract all types with the GenerateHLSL tag
                foreach (var type in TypeCache.GetTypesWithAttribute<GenerateHLSL>())
                {
                    var attr = type.GetCustomAttributes(typeof(GenerateHLSL), false).First() as GenerateHLSL;
                    if (!sourceGenerators.TryGetValue(attr.sourcePath, out var generators))
                    {
                        generators = ListPool<ShaderTypeGenerator>.Get();
                        sourceGenerators.Add(attr.sourcePath, generators);
                    }

                    generators.Add(new ShaderTypeGenerator(type, attr));
                }

                // Generate all files
                await Task.WhenAll(sourceGenerators.Select(async it =>
                    await GenerateAsync($"{it.Key}.hlsl", $"{Path.ChangeExtension(it.Key, "custom")}.hlsl", it.Value)));
            }
            finally
            {
                // Make sure we always release pooled resources
                if (sourceGenerators != null)
                {
                    foreach (var pair in sourceGenerators)
                        ListPool<ShaderTypeGenerator>.Release(pair.Value);
                    DictionaryPool<string, List<ShaderTypeGenerator>>.Release(sourceGenerators);
                }
            }
        }

        /// <summary>
        ///     Generate all shader code from <paramref name="generators" /> into <paramref name="targetFilename" />.
        /// </summary>
        /// <param name="targetFilename">Path of the file to generate.</param>
        /// <param name="targetCustomFilename">Path of the custom file to include. (If it exists)</param>
        /// <param name="generators">Generators to execute.</param>
        /// <returns>Awaitable task.</returns>
        private static async Task GenerateAsync(string targetFilename, string targetCustomFilename,
            List<ShaderTypeGenerator> generators)
        {
            var skipFile = false;

            // Sort elements to have consistent result
            generators.Sort();

            // Emit atomic element for all generators
            foreach (var gen in generators.Where(gen => !gen.Generate()))
            {
                // Error reporting will be done by the generator.  Skip this file.
                gen.PrintErrors();
                skipFile = true;
                break;
            }

            // If an error occured during generation, we abort this file
            if (skipFile)
                return;

            // Check access to the file
            if (File.Exists(targetFilename))
            {
                FileInfo info = null;
                try
                {
                    info = new FileInfo(targetFilename);
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.Log("Access to " + targetFilename + " is denied. Skipping it.");
                    return;
                }
                catch (SecurityException)
                {
                    Debug.Log("You do not have permission to access " + targetFilename + ". Skipping it.");
                    return;
                }

                if (info?.IsReadOnly ?? false)
                {
                    Debug.Log(targetFilename + " is ReadOnly. Skipping it.");
                    return;
                }
            }

            // Generate content
            using var writer = File.CreateText(targetFilename);
            writer.NewLine = "\n";

            // Include guard name
            var guard = Path.GetFileName(targetFilename).Replace(".", "_").ToUpper();
            if (!char.IsLetter(guard[0]))
                guard = "_" + guard;

            await writer.WriteLineAsync("//");
            await writer.WriteLineAsync("// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead");
            await writer.WriteLineAsync("//");
            await writer.WriteLineAsync();
            await writer.WriteLineAsync("#ifndef " + guard);
            await writer.WriteLineAsync("#define " + guard);

            foreach (var gen in generators.Where(gen => gen.hasStatics))
                await writer.WriteLineAsync(gen.EmitDefines().Replace("\n", writer.NewLine));

            foreach (var gen in generators.Where(gen => gen.hasFields))
                await writer.WriteLineAsync(gen.EmitTypeDecl().Replace("\n", writer.NewLine));

            foreach (var gen in generators.Where(gen => gen.hasFields && gen.needAccessors && !gen.hasPackedInfo))
            {
                await writer.WriteAsync(gen.EmitAccessors().Replace("\n", writer.NewLine));
                await writer.WriteAsync(gen.EmitSetters().Replace("\n", writer.NewLine));
                const bool emitInitters = true;
                await writer.WriteAsync(gen.EmitSetters(emitInitters).Replace("\n", writer.NewLine));
            }

            foreach (var gen in generators.Where(gen =>
                gen.hasStatics && gen.hasFields && gen.needParamDebug && !gen.hasPackedInfo))
                await writer.WriteLineAsync(gen.EmitFunctions().Replace("\n", writer.NewLine));

            foreach (var gen in generators.Where(gen => gen.hasPackedInfo))
                await writer.WriteLineAsync(gen.EmitPackedInfo().Replace("\n", writer.NewLine));

            await writer.WriteLineAsync();

            await writer.WriteLineAsync("#endif");

            if (File.Exists(targetCustomFilename))
                await writer.WriteAsync($"#include \"{Path.GetFileName(targetCustomFilename)}\"");
        }
    }
}
