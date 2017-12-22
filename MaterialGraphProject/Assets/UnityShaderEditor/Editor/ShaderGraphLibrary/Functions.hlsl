// UNITY_SHADER_NO_UPGRADE
#ifndef UNITY_SHADER_GRAPH_INCLUDED
#define UNITY_SHADER_GRAPH_INCLUDED

bool IsGammaSpace()
{
    #ifdef UNITY_COLORSPACE_GAMMA
        return true;
    #else
        return false;
    #endif
}

#endif // UNITY_SHADER_GRAPH_INCLUDED
