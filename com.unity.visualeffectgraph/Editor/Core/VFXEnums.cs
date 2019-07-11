namespace UnityEditor.VFX
{
    // TODO Tmp Just map the task types from bindings so that the enum is accessible from outside the package (For HDRP)
    enum VFXTaskType
    {
        None                        = UnityEngine.VFX.VFXTaskType.None,

        Spawner                     = UnityEngine.VFX.VFXTaskType.Spawner,
        Initialize                  = UnityEngine.VFX.VFXTaskType.Initialize,
        Update                      = UnityEngine.VFX.VFXTaskType.Update,
        Output                      = UnityEngine.VFX.VFXTaskType.Output,

        // updates
        CameraSort                  = UnityEngine.VFX.VFXTaskType.CameraSort,

        // outputs
        ParticlePointOutput         = UnityEngine.VFX.VFXTaskType.ParticlePointOutput,
        ParticleLineOutput          = UnityEngine.VFX.VFXTaskType.ParticleLineOutput,
        ParticleQuadOutput          = UnityEngine.VFX.VFXTaskType.ParticleQuadOutput,
        ParticleHexahedronOutput    = UnityEngine.VFX.VFXTaskType.ParticleHexahedronOutput,
        ParticleMeshOutput          = UnityEngine.VFX.VFXTaskType.ParticleMeshOutput,
        ParticleTriangleOutput      = UnityEngine.VFX.VFXTaskType.ParticleTriangleOutput,
        ParticleOctagonOutput       = UnityEngine.VFX.VFXTaskType.ParticleOctagonOutput,

        // spawners
        ConstantRateSpawner         = UnityEngine.VFX.VFXTaskType.ConstantRateSpawner,
        BurstSpawner                = UnityEngine.VFX.VFXTaskType.BurstSpawner,
        PeriodicBurstSpawner        = UnityEngine.VFX.VFXTaskType.PeriodicBurstSpawner,
        VariableRateSpawner         = UnityEngine.VFX.VFXTaskType.VariableRateSpawner,
        CustomCallbackSpawner       = UnityEngine.VFX.VFXTaskType.CustomCallbackSpawner,
        SetAttributeSpawner         = UnityEngine.VFX.VFXTaskType.SetAttributeSpawner,
    }
}

/*namespace UnityEngine.Experimental.VFX
{
    class VFXTaskTypeExtension
    {
        public static implicit operator VFXTaskType(UnityEditor.VFX.VFXTaskType taskType) => (VFXTaskType)taskType;
    }
}*/
