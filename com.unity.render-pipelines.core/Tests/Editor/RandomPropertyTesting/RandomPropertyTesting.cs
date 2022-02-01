using System;
using System.Diagnostics.CodeAnalysis;
using FsCheck;
using JetBrains.Annotations;

namespace UnityEngine.Tests
{
    public static class UnityFsCheckExtensions
    {
        public static FsCheck.Random.StdGen ReplayConsistent
            => FsCheck.Random.StdGen.NewStdGen(1145655947,296144285);

        [CanBeNull]
        public static FsCheck.Random.StdGen ReplayOverride = null;

        [CanBeNull]
        public static FsCheck.Random.StdGen ReplayOverrideFromEnv
        {
            get
            {
                var seedString = Environment.GetEnvironmentVariable("FSCHECK_SEED");
                if (!ExtractSeedFromString(seedString, out var stdGen, out var error))
                {
                    if (error is FormatException)
                    {
                        Debug.LogException(error);
                    }
                }

                return stdGen;
            }
        }

        [MustUseReturnValue]
        static bool ExtractSeedFromString(
            [AllowNull] string seedAsString,
            [NotNullWhen(true)] out FsCheck.Random.StdGen stdGen,
            [NotNullWhen(false)] out Exception error
            )
        {
            error = default;
            stdGen = default;

            if (string.IsNullOrEmpty(seedAsString))
            {
                error = new ArgumentException("Seed string is null or empty");
                return false;
            }

            var ints = seedAsString.Split('|');
            if (ints.Length != 2 || !int.TryParse(ints[0], out var s0) || !int.TryParse(ints[1], out var s1))
            {
                error = new FormatException("Seed must be formatted as `{int}|{int}`");
                return false;
            }

            stdGen = FsCheck.Random.StdGen.NewStdGen(s0, s1);
            return true;
        }


        public static Configuration ConsistentQuickThrowOnFailure
        {
            get
            {
                var configuration = Configuration.QuickThrowOnFailure;
                configuration.Replay = ReplayConsistent;
                return configuration;
            }
        }

        // Replay depends on context settings
        public static Configuration ContextualQuickThrowOnFailure
        {
            get
            {
                var configuration = Configuration.QuickThrowOnFailure;
                configuration.Replay = ReplayOverrideFromEnv ?? (ReplayOverride ?? ReplayConsistent);
                return configuration;
            }
        }

        public static void ConsistentQuickCheckThrowOnFailure(this Property property)
            => property.Check(ConsistentQuickThrowOnFailure);

        // Replay depends on context settings
        public static void ContextualQuickCheckThrowOnFailure(this Property property)
            => property.Check(ContextualQuickThrowOnFailure);
    }
}
