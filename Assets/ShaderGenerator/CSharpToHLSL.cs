using System;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.ScriptableRenderLoop
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
            m_typeName = new Dictionary<string, ShaderTypeGenerator>();

            // Iterate over assemblyList, discover all applicable types with fully qualified names
            var assemblyList = AssemblyEnumerator.EnumerateReferencedAssemblies(Assembly.GetCallingAssembly());

            foreach (var assembly in assemblyList)
            {
                Type[] types = assembly.GetExportedTypes();

                foreach (var type in types)
                {
                    object[] attributes = type.GetCustomAttributes(true);

                    foreach (var attr in attributes)
                    {
                        if (attr is GenerateHLSL)
                        {
                            Type parent = type.DeclaringType;
                            if (parent != null)
                            {
                                Debug.LogError("The GenerateHLSL attribute not supported on nested classes (" + type.FullName + "), skipping.");
                            }
                            else
                            {
                                ShaderTypeGenerator gen;
                                if (m_typeName.TryGetValue(type.FullName, out gen))
                                {
                                    Debug.LogError("Duplicate typename with the GenerateHLSL attribute detected: " + type.FullName +
                                        " declared in both " + gen.type.Assembly.FullName + " and " + type.Assembly.FullName + ".  Skipping the second instance.");
                                }
                                m_typeName[type.FullName] = new ShaderTypeGenerator(type, attr as GenerateHLSL);
                            }
                        }
                    }
                }
            }


            // Now that we have extracted all the typenames that we care about, parse all .cs files in all asset 
            // paths and figure out in which files those types are actually declared.
            m_sourceGenerators = new Dictionary<string, List<ShaderTypeGenerator>>();

            var assetPaths = AssetDatabase.GetAllAssetPaths().Where(s => s.EndsWith(".cs")).ToList();
            foreach (var assetPath in assetPaths)
            {
                LoadTypes(assetPath);
            }

            // Finally, write out the generated code
            foreach (var it in m_sourceGenerators)
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
                        break; ;
                    }
                }

                if (!skipFile)
                {
                    using (System.IO.StreamWriter writer = File.CreateText(fileName))
                    {
                        writer.Write("//\n");
                        writer.Write("// This file was automatically generated from " + it.Key + ".  Please don't edit by hand.\n");
                        writer.Write("//\n\n");

                        foreach (var gen in it.Value)
                        {
                            if (gen.hasStatics)
                            {
                                writer.Write(gen.EmitDefines() + "\n");
                            }
                        }

                        foreach (var gen in it.Value)
                        {
                            if (gen.hasFields)
                            {
                                writer.Write(gen.EmitTypeDecl() + "\n");
                            }
                        }

                        foreach (var gen in it.Value)
                        {
                            if (gen.hasFields && !gen.IsSimple())
                            {
                                writer.Write(gen.EmitAccessors() + "\n");
                            }
                        }

                        writer.Write("\n");
                    }
                }
            }
        }

        static Dictionary<string, ShaderTypeGenerator> m_typeName;

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
                    VisitorData data = new VisitorData();
                    data.m_typeName = m_typeName;
                    parser.CompilationUnit.AcceptVisitor(visitor, data);

                    if (data.generators.Count > 0)
                        m_sourceGenerators[fileName] = data.generators;

                }
                catch
                {
                    // does NRefactory throw anything we can handle here?
                    throw;
                }
            }
        }

        static Dictionary<string, List<ShaderTypeGenerator>> m_sourceGenerators;

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
                string fullNamespace = string.Empty;

                string separator = "";
                foreach (string ns in currentClasses)
                {
                    fullNamespace = ns + "+" + fullNamespace;
                }

                foreach (string ns in currentNamespaces)
                {
                    if (fullNamespace == string.Empty)
                    {
                        separator = ".";
                        fullNamespace = ns;
                    }
                    else
                        fullNamespace = ns + "." + fullNamespace;
                }

                string name = "";
                if (fullNamespace != string.Empty)
                {
                    name = fullNamespace + separator + name;
                }
                return name;
            }

            public Stack<string> currentNamespaces;
            public Stack<string> currentClasses;
            public List<ShaderTypeGenerator> generators;
            public Dictionary<string, ShaderTypeGenerator> m_typeName;
        }

        class NamespaceVisitor : AbstractAstVisitor
        {
            public override object VisitNamespaceDeclaration(ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
            {
                VisitorData visitorData = (VisitorData)data;
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
                    VisitorData visitorData = (VisitorData)data;

                    string name = visitorData.GetTypePrefix() + typeDeclaration.Name;

                    ShaderTypeGenerator gen;
                    if (visitorData.m_typeName.TryGetValue(name, out gen))
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

    // Helper class to recursively enumerate assemblies referenced by the calling assembly, including unloaded ones
    static class AssemblyEnumerator
    {
        public static List<Assembly> EnumerateReferencedAssemblies(Assembly assembly)
        {
            Dictionary<string, Assembly> assemblies = assembly.GetReferencedAssembliesRecursive();
            assemblies[GetName(assembly.FullName)] = assembly;
            return assemblies.Values.ToList();
        }

        public static Dictionary<string, Assembly> GetReferencedAssembliesRecursive(this Assembly assembly)
        {
            assemblies = new Dictionary<string, Assembly>();
            InternalGetDependentAssembliesRecursive(assembly);

            // Skip assemblies from GAC (@TODO:  any reason we'd want to include them?)
            var keysToRemove = assemblies.Values.Where(
                o => o.GlobalAssemblyCache == true).ToList();

            foreach (var k in keysToRemove)
            {
                assemblies.Remove(GetName(k.FullName));
            }

            return assemblies;
        }

        private static void InternalGetDependentAssembliesRecursive(Assembly assembly)
        {
            // Load assemblies with newest versions first.
            var referencedAssemblies = assembly.GetReferencedAssemblies()
                .OrderByDescending(o => o.Version);

            foreach (var r in referencedAssemblies)
            {
                if (String.IsNullOrEmpty(assembly.FullName))
                {
                    continue;
                }

                if (assemblies.ContainsKey(GetName(r.FullName)) == false)
                {
                    try
                    {
                        // Ensure that the assembly is loaded
                        var a = Assembly.Load(r.FullName);
                        assemblies[GetName(a.FullName)] = a;
                        InternalGetDependentAssembliesRecursive(a);
                    }
                    catch
                    {
                        // Missing dll, ignore.
                    }
                }
            }
        }

        static string GetName(string name)
        {
            return name.Split(',')[0];
        }

        static Dictionary<string, Assembly> assemblies;
    }
};