using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ProjectAuditor
{
    class SpotCookieAnalyzer : GameObjectModuleAnalyzer
    {
        internal const string URP0403 = nameof(URP0403);

        internal static readonly Descriptor k_Descriptor = new Descriptor(
            URP0403,
            "URP: Spot Light needs a cookie to match Built-in cone falloff",
            Areas.Quality,
            "This project uses the Quadratic light falloff mode, which reproduces the distance attenuation of the Built-in Render Pipeline but not its spot cone falloff. Spot Lights render a different cone shape than Built-in unless Light Cookies are enabled in every URP Asset and the light has a matching cookie.",
            "Enable Light Cookies in each URP Asset in Graphics and Quality Settings, assign the SpotConeFalloff texture from Packages > Universal Render Pipeline > Textures to the Spot Light's Cookie slot, and set the Inner / Outer Spot Angle values to match."
        )
        {
            MessageFormat = "URP: Spot Light '{0}' needs a cookie to match the Built-in cone falloff",
        };

        bool m_QuadraticFalloffMode;
        bool m_LightCookiesDisabled;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_Descriptor);
        }

        public override void OnAnalysisStarted()
        {
            m_QuadraticFalloffMode = URPProjectAuditorUtilities.UsesQuadraticFalloff(out m_LightCookiesDisabled);
        }

        public override IEnumerable<ReportItemBuilder> Analyze(GameObjectAnalysisContext context)
        {
            if (!m_QuadraticFalloffMode)
                yield break;

            var light = context.GameObject.GetComponent<Light>();
            if (NeedsConeFalloffCookie(light, m_LightCookiesDisabled))
            {
                yield return context.CreateIssue(IssueCategory.GameObject, k_Descriptor.Id, light.name);
            }
        }

        internal static bool NeedsConeFalloffCookie(Light light, bool lightCookiesDisabled)
        {
            return light != null && light.type == LightType.Spot && (light.cookie == null || lightCookiesDisabled);
        }
    }
}
