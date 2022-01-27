using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
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
                throw new NotImplementedException();
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

            if (!definition.ComputeHash(out var hash, out error))
                return false;

            if (!LoadLastImportReport(out var lastReport, out error))
                return false;

            // Early exit when already computed
            if (lastReport?.uiDefinitionHash == hash)
                return true;

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
            if (!ImportReport.From(out var report, out error))
                return false;

            if (!SaveImportReport(report, out error))
                return false;

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
                .Select(t => new Exception($"Attribute {nameof(DeriveDebugMenuAttribute)} must be used on a non-static class {t.Name}"))
                .ToList();

            if (misusedAttributes.Count == 0)
            {
                error = null;
                return true;
            }

            error = new AggregateException(misusedAttributes);
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

            if (!GenerateUIDefinitionPerType(
                    dataSource,
                    out var uiDefinitions,
                    out var uiContextDefinitions,
                    out error
                ))
                return false;

            using (uiDefinitions)
            using (uiContextDefinitions)
            {
                if (!uiDefinitions.listUnsafe.Aggregate(out uiDefinition, out error))
                    return false;

                if (!uiContextDefinitions.listUnsafe.Aggregate(out uiContextDefinition, out error))
                    return false;
            }

            return true;
        }

        [MustUseReturnValue]
        static bool GenerateUIDefinitionPerType<TList>(
            [DisallowNull] TList dataSource,
            out PooledList<UIDefinition> uiDefinitions,
            out PooledList<UIContextDefinition> uiContextDefinitions,
            [NotNullWhen(false)] out Exception error
        )
            where TList : IList<System.Type>
        {
            throw new NotImplementedException();
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
