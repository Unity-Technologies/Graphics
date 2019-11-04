using System;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public class CSharpToHLSL
    {
        public static bool GenerateHLSL(System.Type type, GenerateHLSL attribute, out string shaderSource)
        {
            List<string> errors;
            return GenerateHLSL(type, attribute, out shaderSource, out errors);
        }

        public static bool GenerateHLSL(System.Type type, GenerateHLSL attribute, out string shaderSource, out List<string> errors)
        {
            ShaderTypeGenerator gen = new ShaderTypeGenerator(type, attribute);
            bool success = gen.Generate();

            if (success)
            {
                shaderSource = gen.Emit();
            }
            else
            {
                shaderSource = null;
            }

            errors = gen.errors;
            return success;
        }

        public static void GenerateAll()
        {
            s_TypeName = new Dictionary<string, ShaderTypeGenerator>();

            // Iterate over assemblyList, discover all applicable types with fully qualified names
            var assemblyList = AppDomain.CurrentDomain.GetAssemblies()
                // We need to exclude dynamic assemblies (their type can't be queried, throwing an exception below)
                .Where(ass => !(ass.ManifestModule is System.Reflection.Emit.ModuleBuilder))
                .ToList();

            foreach (var type in TypeCache.GetTypesWithAttribute<GenerateHLSL>())
            {
                var attr = type.GetCustomAttributes(typeof(GenerateHLSL), false).First() as GenerateHLSL;
                ShaderTypeGenerator gen;
                if (s_TypeName.TryGetValue(type.FullName, out gen))
                {
                    Debug.LogError("Duplicate typename with the GenerateHLSL attribute detected: " + type.FullName +
                        " declared in both " + gen.type.Assembly.FullName + " and " + type.Assembly.FullName + ".  Skipping the second instance.");
                }
                s_TypeName[type.FullName] = new ShaderTypeGenerator(type, attr);
            }


            // Now that we have extracted all the typenames that we care about, parse all .cs files in all asset
            // paths and figure out in which files those types are actually declared.
            s_SourceGenerators = new Dictionary<string, List<ShaderTypeGenerator>>();

            var assetPaths = AssetDatabase.GetAllAssetPaths().Where(s => s.EndsWith(".cs")).ToList();
            foreach (var assetPath in assetPaths)
            {
                LoadTypes(assetPath);
            }

            // Finally, write out the generated code
            foreach (var it in s_SourceGenerators)
            {
                string fileName = it.Key + ".hlsl";
                bool skipFile = false;
                foreach (var gen in it.Value)
                {
                    if (!gen.Generate())
                    {
                        // Error reporting will be done by the generator.  Skip this file.
                        gen.PrintErrors();
                        skipFile = true;
                        break;
                    }
                }

                if (!skipFile && File.Exists(fileName))
                {
                    FileInfo info = null;
                    try
                    {
                        info = new FileInfo(fileName);
                    }

                    catch (UnauthorizedAccessException )

                    {
                        Debug.Log("Access to " + fileName + " is denied. Skipping it.");
                        skipFile = true;
                    }

                    catch (System.Security.SecurityException )

                    {
                        Debug.Log("You do not have permission to access " + fileName + ". Skipping it.");
                        skipFile = true;
                    }

                    if (info?.IsReadOnly ?? false)
                    {
                        Debug.Log(fileName + " is ReadOnly. Skipping it.");
                        skipFile = true;
                    }
                }

                if (skipFile)
                    continue;

                using (var writer = File.CreateText(fileName))
                {
                    writer.NewLine = Environment.NewLine;

                    var guard = Path.GetFileName(fileName).Replace(".", "_").ToUpper();
                    if (!char.IsLetter(guard[0]))
                        guard = "_" + guard;

                    writer.WriteLine("//");
                    writer.WriteLine("// This file was automatically generated. Please don't edit by hand.");
                    writer.WriteLine("//");
                    writer.WriteLine();
                    writer.WriteLine("#ifndef " + guard);
                    writer.WriteLine("#define " + guard);

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasStatics)
                        {
                            writer.WriteLine(gen.EmitDefines().Replace("\n", writer.NewLine));
                        }
                    }

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasFields)
                        {
                            writer.WriteLine(gen.EmitTypeDecl().Replace("\n", writer.NewLine));
                        }
                    }

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasFields && gen.needAccessors && !gen.hasPackedInfo)
                        {
                            writer.Write(gen.EmitAccessors().Replace("\n", writer.NewLine));
                            writer.Write(gen.EmitSetters().Replace("\n", writer.NewLine));
                            const bool emitInitters = true;
                            writer.Write(gen.EmitSetters(emitInitters).Replace("\n", writer.NewLine));
                        }
                    }

                    foreach (var gen in it.Value)
                    {
                        if (gen.hasStatics && gen.hasFields && gen.needParamDebug && !gen.hasPackedInfo)
                        {
                            writer.WriteLine(gen.EmitFunctions().Replace("\n", writer.NewLine));
                        }
                    }

                    foreach (var gen in it.Value)
                    {
                        if(gen.hasPackedInfo)
                        {
                            writer.WriteLine(gen.EmitPackedInfo().Replace("\n", writer.NewLine));
                        }
                    }

                    writer.WriteLine();

                    writer.WriteLine("#endif");

                    var customFile = it.Key + ".custom.hlsl";
                    if (File.Exists(customFile))
                        writer.Write("#include \"{0}\"", Path.GetFileName(customFile));
                }
            }
        }

        static Dictionary<string, ShaderTypeGenerator> s_TypeName;

        static void LoadTypes(string fileName)
        {
            using (var parser = ParserFactory.CreateParser(fileName))
            {
                // @TODO any standard preprocessor symbols we need?

                /*var uniqueSymbols = new HashSet<string>(definedSymbols.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                foreach (var symbol in uniqueSymbols)
                {
                    parser.Lexer.ConditionalCompilationSymbols.Add(symbol, string.Empty);
                }*/
                parser.Lexer.EvaluateConditionalCompilation = true;

                parser.Parse();
                try
                {
                    var visitor = new NamespaceVisitor();
                    var data = new VisitorData { typeName = s_TypeName };
                    parser.CompilationUnit.AcceptVisitor(visitor, data);

                    if (data.generators.Count > 0)
                        s_SourceGenerators[fileName] = data.generators;
                }
                catch
                {
                    // does NRefactory throw anything we can handle here?
                    throw;
                }
            }
        }

        static Dictionary<string, List<ShaderTypeGenerator>> s_SourceGenerators;

        class VisitorData
        {
            public VisitorData()
            {
                currentNamespaces = new Stack<string>();
                currentClasses = new Stack<string>();
                generators = new List<ShaderTypeGenerator>();
            }

            public string GetTypePrefix()
            {
                var fullNamespace = string.Empty;

                var separator = "";

                fullNamespace = currentClasses.Aggregate(fullNamespace, (current, ns) => ns + "+" + current);
                foreach (var ns in currentNamespaces)
                {
                    if (fullNamespace == string.Empty)
                    {
                        separator = ".";
                        fullNamespace = ns;
                    }
                    else
                        fullNamespace = ns + "." + fullNamespace;
                }

                var name = "";
                if (fullNamespace != string.Empty)
                {
                    name = fullNamespace + separator + name;
                }
                return name;
            }

            public readonly Stack<string> currentNamespaces;
            public readonly Stack<string> currentClasses;
            public readonly List<ShaderTypeGenerator> generators;
            public Dictionary<string, ShaderTypeGenerator> typeName;
        }

        class NamespaceVisitor : AbstractAstVisitor
        {
            public override object VisitNamespaceDeclaration(ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
            {
                var visitorData = (VisitorData)data;
                visitorData.currentNamespaces.Push(namespaceDeclaration.Name);
                namespaceDeclaration.AcceptChildren(this, visitorData);
                visitorData.currentNamespaces.Pop();

                return null;
            }

            public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data)
            {
                // Structured types only
                if (typeDeclaration.Type == ClassType.Class || typeDeclaration.Type == ClassType.Struct || typeDeclaration.Type == ClassType.Enum)
                {
                    var visitorData = (VisitorData)data;

                    var name = visitorData.GetTypePrefix() + typeDeclaration.Name;

                    ShaderTypeGenerator gen;
                    if (visitorData.typeName.TryGetValue(name, out gen))
                    {
                        visitorData.generators.Add(gen);
                    }

                    visitorData.currentClasses.Push(typeDeclaration.Name);
                    typeDeclaration.AcceptChildren(this, visitorData);
                    visitorData.currentClasses.Pop();
                }

                return null;
            }
        }
    }
}
