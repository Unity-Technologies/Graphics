using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering.UIGen;

namespace UnityEditor.Rendering.UIGen
{
    [InitializeOnLoad]
    class DebugMenuImporter
    {
        class ImportReport
        {
            public Hash128 uiDefinitionHash { get; }
            string fileLocations;

            public static bool From(out ImportReport report, out Exception error)
            {
                report = new ImportReport();
                error = default;
                return true;
            }
        }

        static DebugMenuImporter()
        {
            AssemblyReloadEvents.afterAssemblyReload += ImportDebugMenuCallback;
            ImportDebugMenuCallback();
        }

        static void ImportDebugMenuCallback()
        {
            if (!ImportDebugMenu(out var error))
                Debug.LogException(error);
        }

        static GenerationTargetLocations s_GenerationTargetLocations;

        static bool ImportDebugMenu(
            [NotNullWhen(false)]out Exception error
        )
        {
            var dataSource = TypeCache.GetTypesWithAttribute<DeriveDebugMenuAttribute>();

            if (!ValidateDataSources(dataSource, out error))
                return false;

            if (!GenerateDefinitions(dataSource, out var definition, out var context, out error))
                return false;

            // TODO: [Fred] Compute hash later
            // if (!definition.ComputeHash(out var hash, out error))
            //     return false;
            //
            // if (!LoadLastImportReport(out var lastReport, out error))
            //     return false;
            //
            // // Early exit when already computed
            // if (lastReport?.uiDefinitionHash == hash)
            //     return true;

            // Generate asset and C# library

            if (!DebugMenuUIGenerator.GenerateDebugMenuBindableView(
                    definition,
                    context,
                    new DebugMenuUIGenerator.Parameters(),
                    out var view,
                    out error))
                return false;

            // Write assets and C# generated library
            if (!view.WriteToDisk(s_GenerationTargetLocations, out error))
                return false;

            // TODO: may require additional arguments
            if (!DebugMenuIntegration.GenerateIntegration(default, out var integrationDocuments, out error))
                return false;

            if (!integrationDocuments.WriteToDisk(s_GenerationTargetLocations, out error))
                return false;

            // Save import report
            // if (!ImportReport.From(out var report, out error))
            //     return false;

            // if (!SaveImportReport(report, out error))
            //     return false;

            return true;
        }

        [MustUseReturnValue]
        static bool ValidateDataSources<TList>(
            [DisallowNull] TList dataSource,
            [NotNullWhen(false)] out Exception error
        ) where TList : IList<System.Type>
        {
            // Find errors
            var misusedAttributes = dataSource.Where(t => !t.IsClass
                // Check for static class
                || (t.IsAbstract && t.IsSealed))
                .Select(t => new Exception($"Attribute {nameof(DeriveDebugMenuAttribute)} " +
                    $"must be used on a non-static class {t.Name}").WithStackTrace())
                .ToList();

            if (misusedAttributes.Count == 0)
            {
                error = null;
                return true;
            }

            error = new AggregateException(misusedAttributes).WithStackTrace();
            return false;
        }

        [MustUseReturnValue]
        static bool GenerateDefinitions<TList>(
            [DisallowNull] TList dataSource,
            [NotNullWhen(true)] out UIDefinition uiDefinition,
            [NotNullWhen(true)] out UIContextDefinition uiContextDefinition,
            [NotNullWhen(false)] out Exception error
        )
            where TList : IList<System.Type>
        {
            uiDefinition = default;
            uiContextDefinition = default;

            if (!GenerateUIDefinitionForAllTypes(
                    dataSource,
                    out var uiDefinitions,
                    out var uiContextDefinitions,
                    out error
                ))
                return false;

            using (uiDefinitions)
            using (uiContextDefinitions)
            {
                uiDefinition = new UIDefinition();
                if (!uiDefinitions.listUnsafe.AggregateInto(uiDefinition, out error))
                    return false;

                uiContextDefinition = new UIContextDefinition();
                if (!uiContextDefinitions.listUnsafe.AggregateInto(uiContextDefinition, out error))
                    return false;
            }

            return true;
        }

        [MustUseReturnValue]
        static bool GenerateUIDefinitionForAllTypes<TList>(
            [DisallowNull] TList dataSource,
            out PooledList<UIDefinition> uiDefinitions,
            out PooledList<UIContextDefinition> uiContextDefinitions,
            [NotNullWhen(false)] out Exception error
        )
            where TList : IList<Type>
        {
            uiDefinitions = default;
            uiContextDefinitions = default;
            error = default;

            using (ListPool<UIDefinition>.Get(out var uiDefinitionsTmp))
            using (ListPool<UIContextDefinition>.Get(out var uiContextDefinitionsTmp))
            {
                foreach (var type in dataSource)
                {
                    if (!GenerateUIDefinitionForType(type, out var uiDefinition, out var uiContextDefinition, out error))
                        return false;

                    uiDefinitionsTmp.Add(uiDefinition);
                    uiContextDefinitionsTmp.Add(uiContextDefinition);
                }

                uiDefinitions = PooledList<UIDefinition>.New();
                uiContextDefinitions = PooledList<UIContextDefinition>.New();
                uiDefinitions.listUnsafe.AddRange(uiDefinitionsTmp);
                uiContextDefinitions.listUnsafe.AddRange(uiContextDefinitionsTmp);
            }

            return true;
        }

        [MustUseReturnValue]
        static bool GenerateUIDefinitionForType(
            [DisallowNull] Type type,
            [NotNullWhen(true)] out UIDefinition uiDefinition,
            [NotNullWhen(true)] out UIContextDefinition uiContextDefinition,
            [NotNullWhen(false)] out Exception error
        )
        {
            [MustUseReturnValue]
            bool GenerateUIDefinitionRecursive(
                [DisallowNull] Type typeWalk,
                [DisallowNull] UIDefinition uiDefinitionWalk,
                [DisallowNull] string pathWalk,
                [NotNullWhen(false)] out Exception errorWalk
            )
            {
                // Find leaves
                foreach (var (propertyPath, propertyType, propertyName, tooltip, primary, secondary)
                         in typeWalk.GetFields(BindingFlags.Instance | BindingFlags.Public)
                             .Select(info => (type: info.FieldType, info: (MemberInfo)info))
                             .Union(typeWalk.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                 .Select(info => (info.PropertyType, (MemberInfo)info)))
                             .Where(info => info.Item2.GetCustomAttribute(typeof(DebugMenuPropertyAttribute)) != null)
                             .Select(info =>
                             {
                                 var attribute = (DebugMenuPropertyAttribute)info.Item2.GetCustomAttribute(typeof(DebugMenuPropertyAttribute));
                                 return (
                                     propertyPath: UIDefinition.PropertyPath.FromUnsafe($"{pathWalk}.{info.Item2.Name}"),
                                     type: info.Item1,
                                     propertyName: UIDefinition.PropertyName.FromUnsafe(info.Item2.Name),
                                     tooltip: UIDefinition.PropertyTooltip.FromUnsafe(attribute.tooltip),
                                     primaryCategory: UIDefinition.CategoryId.FromUnsafe(attribute.primaryCategory),
                                     secondaryCategory: UIDefinition.CategoryId.FromUnsafe(attribute.secondaryCategory)
                                );
                             }))
                {
                    if (!uiDefinitionWalk.AddCategorizedProperty(
                            propertyPath,
                            propertyType,
                            propertyName,
                            tooltip,
                            primary,
                            secondary,
                            out _,
                            out errorWalk
                        ))
                        return false;
                }

                // Find non-leaves types and recurse
                foreach (var (childType, childPath)
                         in typeWalk.GetFields(BindingFlags.Instance | BindingFlags.Public)
                             .Select(info => (type: info.FieldType, info: (MemberInfo)info))
                             .Union(typeWalk.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                 .Select(info => (info.PropertyType, (MemberInfo)info)))
                             .Where(info => info.Item2.GetCustomAttribute(typeof(DebugMenuPropertyAttribute)) == null)
                             .Select(info => (
                                 childType: info.Item1,
                                 childPath: $"{pathWalk}.{info.Item2.Name}"
                             )))
                {
                    if (!GenerateUIDefinitionRecursive(childType, uiDefinitionWalk, childPath, out errorWalk))
                        return false;
                }

                errorWalk = default;
                return true;
            }

            uiContextDefinition = default;
            uiDefinition = new UIDefinition();
            // Note: the initial path must be the same as the context paths, see GenerateContextDefinition
            var path = type.Name;
            if (!GenerateUIDefinitionRecursive(type, uiDefinition, path, out error))
                return false;

            uiContextDefinition = new UIContextDefinition();
            if (!uiContextDefinition.AddMember(type, path, out error))
                return false;

            return true;
        }

        [MustUseReturnValue]
        static bool LoadLastImportReport(
            out ImportReport report, // can be null
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }

        [MustUseReturnValue]
        static bool SaveImportReport(
            [DisallowNull] ImportReport report,
            [NotNullWhen(false)] out Exception error
        )
        {
            throw new NotImplementedException();
        }
    }
}
