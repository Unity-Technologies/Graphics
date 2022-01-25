using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.UIGen;

namespace UnityEditor.Rendering.UIGen
{
    [InitializeOnLoad]
    class DebugMenuImporter
    {
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

        static BindableViewExtensions.DiskLocation k_DiskLocation;

        static bool ImportDebugMenu(
            [NotNullWhen(false)]out Exception error
        )
        {
            var dataSource = TypeCache.GetTypesWithAttribute<DeriveDebugMenuAttribute>();

            if (!ValidateDatasources(dataSource, out error))
                return false;

            if (!GenerateUIDefinition(dataSource, out var definition, out error))
                return false;

            // TODO: Compare to previously imported definition and continue only if required

            // Generate asset and C# library
            if (!definition.GenerateDebugMenuBindableView(
                    new DebugMenuUIGenerator.Parameters(),
                    out var view,
                    out error))
                return false;

            // Write assets and C# generated library
            if (!view.WriteToDisk(k_DiskLocation, out error))
                return false;

            // TODO: generate and write integration
            //   custom inspector for editor
            //   runtime display

            throw new NotImplementedException();
        }

        static bool ValidateDatasources<TList>(
            [DisallowNull] TList dataSource,
            [NotNullWhen(false)] out Exception error
        ) where TList : IList<System.Type>
        {
            // Find errors
            var misusedAttributes = dataSource.Where(t => !t.IsClass
                // Check for static class
                || !(t.IsAbstract && t.IsSealed))
                .Select(t => new Exception($"Attribute {nameof(DeriveDebugMenuAttribute)} is used on a non-static class {t.Name}"))
                .ToList();

            if (misusedAttributes.Count == 0)
            {
                error = null;
                return true;
            }

            error = new AggregateException(misusedAttributes);
            return false;
        }

        static bool GenerateUIDefinition<TList>(
            [DisallowNull] TList dataSource,
            [NotNullWhen(true)] out UIDefinition definition,
            [NotNullWhen(false)] out Exception error
        )
            where TList : IList<System.Type>
        {
            definition = default;

            if (!GenerateUIDefinitionPerType(dataSource, out var definitions, out error))
                return false;

            using (definitions)
            {
                return definitions.Aggregate(out definition, out error);
            }
        }

        static bool GenerateUIDefinitionPerType<TList>(
            [DisallowNull] TList dataSource,
            out PooledList<UIDefinition> definitions,
            [NotNullWhen(false)] out Exception error
        )
            where TList : IList<System.Type>
        {
            throw new NotImplementedException();
        }
    }
}
