using System;
using System.Collections.Generic;
using System.Linq;

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
        internal class SortKeySlotComparer : IEqualityComparer<VFXSlot>
        {
            public bool Equals(VFXSlot x, VFXSlot y)
            {
                return x.GetExpression().Equals(y.GetExpression());
            }

            public int GetHashCode(VFXSlot obj)
            {
                return obj.GetExpression().GetHashCode();
            }
        }

        internal static TResult MajorityVote<TResult, TVoter>(IEnumerable<TVoter> voterContainer, Func<TVoter, TResult> getVoteFunc)
        {
            Dictionary<TResult, int> voteCounts = new Dictionary<TResult, int>();
            foreach (var voter in voterContainer)
            {
                TResult vote = getVoteFunc(voter);
                voteCounts.TryGetValue(vote, out var currentCount);
                voteCounts[vote] = currentCount + 1;
            }
            //Return result with the most votes
            return voteCounts.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
        }
    }
}
