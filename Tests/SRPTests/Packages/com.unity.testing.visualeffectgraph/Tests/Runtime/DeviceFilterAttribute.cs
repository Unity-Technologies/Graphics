using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace UnityEngine.VFX.Test
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DeviceFilterAttribute : NUnitAttribute, IApplyToTest
    {
        /// <summary>
        /// A subset of platforms you need to have your tests run on.
        /// </summary>
        public GraphicsDeviceType[] include { get; set; }
        /// <summary>
        /// List the platforms you do not want to have your tests run on.
        /// </summary>
        public GraphicsDeviceType[] exclude { get; set; }

        private string m_skippedReason;

        /// <summary>
        /// Constructs a new instance of the <see cref="UnityPlatformAttribute"/> class.
        /// </summary>
        public DeviceFilterAttribute()
        {
            include = new List<GraphicsDeviceType>().ToArray();
            exclude = new List<GraphicsDeviceType>().ToArray();
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="UnityPlatformAttribute"/> class with a list of platforms to include.
        /// </summary>
        /// <param name="include">The different <see cref="RuntimePlatform"/> to run the test on.</param>
        public DeviceFilterAttribute(params GraphicsDeviceType[] include)
            : this()
        {
            this.include = include;
        }

        /// <summary>
        /// Modifies a test as defined for the specific attribute.
        /// </summary>
        /// <param name="test">The test to modify</param>
        public void ApplyToTest(NUnit.Framework.Internal.Test test)
        {
            if (test.RunState == RunState.NotRunnable || test.RunState == RunState.Ignored || IsDeviceSupported(SystemInfo.graphicsDeviceType))
            {
                return;
            }
            test.RunState = RunState.Skipped;
            test.Properties.Add(PropertyNames.SkipReason, m_skippedReason);
        }

        internal bool IsDeviceSupported(GraphicsDeviceType testTargetDeviceType)
        {
            if (include.Any() && !include.Any(x => x == testTargetDeviceType))
            {
                m_skippedReason = string.Format("Only supported on {0}", string.Join(", ", include.Select(x => x.ToString()).ToArray()));
                return false;
            }

            if (exclude.Any(x => x == testTargetDeviceType))
            {
                m_skippedReason = string.Format("Not supported on  {0}", string.Join(", ", exclude.Select(x => x.ToString()).ToArray()));
                return false;
            }
            return true;
        }
    }
}
