using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiMaterialPlacer : MonoBehaviour
{
    public Renderer prefabObject;
    [Tooltip("Optional")]
    public Material material;

    public MaterialParameterVariation[] commonParameters;

    public bool is2D = false;

    public MaterialParameterVariation[] instanceParameters;

    public float offset = 1.5f;
}
