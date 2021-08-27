using System;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    public static class VFXSortingUtility
    {
        public enum SortCriteria
        {
            DistanceToCamera,
            YoungestInFront,
            OldestInFront,
            CameraDepth,
            Custom,
        }

        public static IEnumerable<string> GetSortingAdditionalDefines(SortCriteria sortCriteria)
        {
            switch (sortCriteria)
            {
                case SortCriteria.Custom:
                    yield return "VFX_CUSTOM_SORT_KEY";
                    break;
                case SortCriteria.CameraDepth:
                    yield return "VFX_DEPTH_SORT_KEY";
                    break;
                case SortCriteria.DistanceToCamera:
                    yield return "VFX_DISTANCE_SORT_KEY";
                    break;
                case SortCriteria.YoungestInFront:
                    yield return "VFX_YOUNGEST_SORT_KEY";
                    break;
                case SortCriteria.OldestInFront:
                    yield return "VFX_OLDEST_SORT_KEY";
                    break;
                default:
                    throw new NotImplementedException("This Sorting criteria is missing an Additional Define");
            }
        }

        internal static IEnumerable<VFXAttribute> GetSortingDependantAttributes(SortCriteria sortCriteria)
        {
            switch (sortCriteria)
            {
                case SortCriteria.Custom:
                    break;
                case SortCriteria.CameraDepth:
                case SortCriteria.DistanceToCamera:
                    yield return VFXAttribute.Position;
                    break;
                case SortCriteria.YoungestInFront:
                case SortCriteria.OldestInFront:
                    yield return VFXAttribute.Age;
                    break;
                default:
                    throw new NotImplementedException("This Sorting criteria is missing an Additional Define");
            }
        }

        public static bool IsPerCamera(SortCriteria criteria)
        {
            return criteria is SortCriteria.CameraDepth or SortCriteria.DistanceToCamera;
        }
    }
}
