#ifdef SAMPLE_TEXTURE2D
#undef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2)                               textureName.SampleLevel(samplerName, coord2, 0)
#endif
