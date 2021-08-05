using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Settings to use when capturing a probe.</summary>
    [Serializable]
    public struct ProbeCapturePositionSettings
    {
        /// <summary>Default value.</summary>
        [Obsolete("Since 2019.3, use ProbeCapturePositionSettings.NewDefault() instead.")]
        public static readonly ProbeCapturePositionSettings @default = default;
        /// <summary>Default value.</summary>
        /// <returns>The default value.</returns>
        public static ProbeCapturePositionSettings NewDefault() => new ProbeCapturePositionSettings(
            Vector3.zero, Quaternion.identity,
            Vector3.zero, Quaternion.identity,
            Matrix4x4.identity
        );

        /// <summary>The proxy position.</summary>
        public Vector3 proxyPosition;
        /// <summary>The proxy rotation.</summary>
        public Quaternion proxyRotation;
        /// <summary>
        /// The reference position.
        ///
        /// This additional information is used to compute the actual capture position. (usually, the viewer position) (<see cref="ProbeSettings.ProbeType"/>)
        /// </summary>
        public Vector3 referencePosition;
        /// <summary>
        /// The reference rotation.
        ///
        /// This additional information is used to compute the actual capture position. (usually, the viewer rotation) (<see cref="ProbeSettings.ProbeType"/>)
        /// </summary>
        public Quaternion referenceRotation;

        /// <summary>
        /// The matrix for influence to world.
        /// </summary>
        public Matrix4x4 influenceToWorld;

        /// <summary>Create a new settings with only the probe transform.</summary>
        /// <param name="proxyPosition">The proxy position.</param>
        /// <param name="proxyRotation">The proxy rotation.</param>
        /// <param name="influenceToWorld">Influence to world matrix</param>
        public ProbeCapturePositionSettings(
            Vector3 proxyPosition,
            Quaternion proxyRotation,
            Matrix4x4 influenceToWorld
        )
        {
            this.proxyPosition = proxyPosition;
            this.proxyRotation = proxyRotation;
            referencePosition = Vector3.zero;
            referenceRotation = Quaternion.identity;
            this.influenceToWorld = influenceToWorld;
        }

        /// <summary>Create new settings.</summary>
        /// <param name="proxyPosition">The proxy position.</param>
        /// <param name="proxyRotation">The proxy rotation.</param>
        /// <param name="referencePosition">The reference position.</param>
        /// <param name="referenceRotation">The reference rotation.</param>
        /// <param name="influenceToWorld">Influence to world matrix</param>
        public ProbeCapturePositionSettings(
            Vector3 proxyPosition,
            Quaternion proxyRotation,
            Vector3 referencePosition,
            Quaternion referenceRotation,
            Matrix4x4 influenceToWorld
        )
        {
            this.proxyPosition = proxyPosition;
            this.proxyRotation = proxyRotation;
            this.referencePosition = referencePosition;
            this.referenceRotation = referenceRotation;
            this.influenceToWorld = influenceToWorld;
        }

        /// <summary>
        /// Compute the probe capture settings from an HDProbe and a reference transform.
        /// </summary>
        /// <param name="probe">The probe to extract settings from.</param>
        /// <param name="reference">The reference transform. Use <c>null</c> when no reference is available.</param>
        /// <returns>The probe capture position settings.</returns>
        public static ProbeCapturePositionSettings ComputeFrom(HDProbe probe, Transform reference)
        {
            var referencePosition = Vector3.zero;
            var referenceRotation = Quaternion.identity;
            if (reference != null)
            {
                referencePosition = reference.position;
                referenceRotation = reference.rotation;
            }
            else
            {
                if (probe.type == ProbeSettings.ProbeType.PlanarProbe)
                {
                    var planar = (PlanarReflectionProbe)probe;
                    return ComputeFromMirroredReference(planar, planar.referencePosition);
                }
            }

            var result = ComputeFrom(probe, referencePosition, referenceRotation);

            return result;
        }

        /// <summary>
        /// Compute the probe capture settings from an HDProbe and a reference position.
        /// The position will be mirrored based on the mirror position of the probe.
        /// </summary>
        /// <param name="probe">The probe to extract settings from.</param>
        /// <param name="referencePosition">The reference position to use.</param>
        /// <returns>The probe capture position setting.</returns>
        public static ProbeCapturePositionSettings ComputeFromMirroredReference(
            HDProbe probe, Vector3 referencePosition
        )
        {
            var positionSettings = ComputeFrom(
                probe,
                referencePosition, Quaternion.identity
            );
            // Set proper orientation for the reference rotation
            var proxyMatrix = Matrix4x4.TRS(
                positionSettings.proxyPosition,
                positionSettings.proxyRotation,
                Vector3.one
            );
            var mirrorPosition = proxyMatrix.MultiplyPoint(probe.settings.proxySettings.mirrorPositionProxySpace);
            positionSettings.referenceRotation = Quaternion.LookRotation(mirrorPosition - positionSettings.referencePosition);
            return positionSettings;
        }

        /// <summary>
        /// Compute a hash based on the settings' values
        /// </summary>
        /// <returns></returns>
        public Hash128 ComputeHash()
        {
            var h = new Hash128();
            var h2 = new Hash128();
            HashUtilities.QuantisedVectorHash(ref proxyPosition, ref h);
            HashUtilities.QuantisedVectorHash(ref referencePosition, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            var euler = proxyRotation.eulerAngles;
            HashUtilities.QuantisedVectorHash(ref euler, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            euler = referenceRotation.eulerAngles;
            HashUtilities.QuantisedVectorHash(ref euler, ref h2);
            HashUtilities.AppendHash(ref h2, ref h);
            return h;
        }

        static ProbeCapturePositionSettings ComputeFrom(
            HDProbe probe,
            Vector3 referencePosition, Quaternion referenceRotation
        )
        {
            var result = new ProbeCapturePositionSettings();
            var proxyToWorld = probe.proxyToWorld;
            result.proxyPosition = proxyToWorld.GetColumn(3);

            // If reference position and proxy position is exactly the same, we end up in some degeneracies triggered
            // by engine code when computing culling parameters. This is an extremely rare case, but can happen
            // in editor when focusing on the planar probe. So if that happens, we offset them 0.1 mm apart.
            if(Vector3.Distance(result.proxyPosition, referencePosition) < 1e-4f)
            {
                referencePosition += new Vector3(1e-4f, 1e-4f, 1e-4f);
            }

            result.proxyRotation = proxyToWorld.rotation;
            result.referencePosition = referencePosition;
            result.referenceRotation = referenceRotation;
            result.influenceToWorld = probe.influenceToWorld;
            return result;
        }
    }
}
