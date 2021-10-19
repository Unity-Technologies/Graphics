using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Extract volume components paths.
    ///
    /// Discards components that have:
    /// * <see cref="HideInInspector"/> attribute
    /// * <see cref="ObsoleteAttribute"/> attribute
    /// </summary>
    class VolumeComponentTypeSetPathAndType : VolumeComponentTypeSetExtension
    {
        public struct Factory : IVolumeComponentTypeSetExtensionFactory<VolumeComponentTypeSetPathAndType>
        {
            struct BuiltinRenderPipeline { }

            public VolumeComponentTypeSetPathAndType Create(VolumeComponentTypeSet volumeComponentTypeSet)
            {
                var currentPipeline = RenderPipelineManager.currentPipeline?.GetType() ?? typeof(BuiltinRenderPipeline);
                var types = volumeComponentTypeSet.AsArray();

                var componentPathAndTypes = new List<(string path, Type type)>();

                // filter types based on attributes
                foreach (var type in types)
                {
                    var path = string.Empty;
                    var attrs = type.GetCustomAttributes(false);
                    var skipComponent = !IsSupportedOn.IsSupportedBy(type, currentPipeline);
                    if (skipComponent)
                        continue;

                    // Look for the attributes of this volume component and decide how is added and if it needs to be skipped
                    foreach (var attr in attrs)
                    {
                        switch (attr)
                        {
                            case VolumeComponentMenu attrMenu:
                            {
                                path = attrMenu.menu;
                                break;
                            }
                            case HideInInspector:
                            case ObsoleteAttribute:
                                continue;
                        }
                    }

#if UNITY_EDITOR

                    // If no attribute or in case something went wrong when grabbing it, fallback to a
                    // beautified class name
                    if (string.IsNullOrEmpty(path))
                        path = UnityEditor.ObjectNames.NicifyVariableName(type.Name);
#endif

                    componentPathAndTypes.Add((path, type));
                }

                return new VolumeComponentTypeSetPathAndType(componentPathAndTypes);
            }
        }

        VolumeComponentTypeSetPathAndType(List<(string path, Type type)> componentPathAndTypes)
        {
            volumeComponentPathAndTypes = componentPathAndTypes;
        }

        [NotNull]
        public IReadOnlyList<(string path, Type type)> volumeComponentPathAndTypes { get; }
    }

    static class VolumeComponentTypeSetPathAndTypeExtension
    {
        public static bool GetOrAddPathAndType(
            [DisallowNull] this VolumeComponentTypeSet typeSet,
            [NotNullWhen(true)] out VolumeComponentTypeSetPathAndType extension)
            => typeSet.GetOrAddExtension<VolumeComponentTypeSetPathAndType, VolumeComponentTypeSetPathAndType.Factory>(out extension);
    }
}
