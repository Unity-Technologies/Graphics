using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ForceShaderKeyword : MonoBehaviour
{
    void Start()
    {
        
    }

    public enum Test
    {
        Red,
        Green,
        Blue
    }

    public Test test;

    void Update()
    {
        Shader.DisableKeyword("_VFX_TEST_MULTI_RED");
        Shader.DisableKeyword("_VFX_TEST_MULTI_GREEN");
        Shader.DisableKeyword("_VFX_TEST_MULTI_BLUE");

        switch (test)
        {
            case Test.Red: Shader.EnableKeyword("_VFX_TEST_MULTI_RED"); break;
            case Test.Green: Shader.EnableKeyword("_VFX_TEST_MULTI_GREEN"); break;
            case Test.Blue: Shader.EnableKeyword("_VFX_TEST_MULTI_BLUE"); break;

        }

    }
}
