namespace UnityEditor.VFX
{
    // TODO Tmp Just map the task types from bindings so that the enum is accessible from outside the package (For HDRP)
    enum VFXTaskType
    {
        None                        = UnityEngine.Experimental.VFX.VFXTaskType.None,

        Spawner                     = UnityEngine.Experimental.VFX.VFXTaskType.Spawner,
        Initialize                  = UnityEngine.Experimental.VFX.VFXTaskType.Initialize,
        Update                      = UnityEngine.Experimental.VFX.VFXTaskType.Update,
        Output                      = UnityEngine.Experimental.VFX.VFXTaskType.Output,

        // updates
        CameraSort                  = UnityEngine.Experimental.VFX.VFXTaskType.CameraSort,
        StripSort                   = UnityEngine.Experimental.VFX.VFXTaskType.StripSort,
        StripUpdatePerParticle      = UnityEngine.Experimental.VFX.VFXTaskType.StripUpdatePerParticle,
        StripUpdatePerStrip         = UnityEngine.Experimental.VFX.VFXTaskType.StripUpdatePerStrip,

        // outputs
        ParticlePointOutput         = UnityEngine.Experimental.VFX.VFXTaskType.ParticlePointOutput,
        ParticleLineOutput          = UnityEngine.Experimental.VFX.VFXTaskType.ParticleLineOutput,
        ParticleQuadOutput          = UnityEngine.Experimental.VFX.VFXTaskType.ParticleQuadOutput,
        ParticleHexahedronOutput    = UnityEngine.Experimental.VFX.VFXTaskType.ParticleHexahedronOutput,
        ParticleMeshOutput          = UnityEngine.Experimental.VFX.VFXTaskType.ParticleMeshOutput,
        ParticleTriangleOutput      = UnityEngine.Experimental.VFX.VFXTaskType.ParticleTriangleOutput,
        ParticleOctagonOutput       = UnityEngine.Experimental.VFX.VFXTaskType.ParticleOctagonOutput,

        // spawners
        ConstantRateSpawner         = UnityEngine.Experimental.VFX.VFXTaskType.ConstantRateSpawner,
        BurstSpawner                = UnityEngine.Experimental.VFX.VFXTaskType.BurstSpawner,
        PeriodicBurstSpawner        = UnityEngine.Experimental.VFX.VFXTaskType.PeriodicBurstSpawner,
        VariableRateSpawner         = UnityEngine.Experimental.VFX.VFXTaskType.VariableRateSpawner,
        CustomCallbackSpawner       = UnityEngine.Experimental.VFX.VFXTaskType.CustomCallbackSpawner,
        SetAttributeSpawner         = UnityEngine.Experimental.VFX.VFXTaskType.SetAttributeSpawner,
    }
}

/*namespace UnityEngine.Experimental.VFX
{
    class VFXTaskTypeExtension
    {
        public static implicit operator VFXTaskType(UnityEditor.VFX.VFXTaskType taskType) => (VFXTaskType)taskType;
    }
}*/
