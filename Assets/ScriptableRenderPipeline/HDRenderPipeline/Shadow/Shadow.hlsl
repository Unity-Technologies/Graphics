// TODO: 
// - How to support a Gather sampling with such abstraction ?
// - What's belong to shadow and what's belong to renderloop ? (shadowmap size depends on the usage of atlas or not)
// - Is PunctualShadowData fixed or customizable ? Who is the owner ? Should it be pass to GetPunctualShadowAttenuation ? Sure it should...
// - Could be return by GetShadowTextureCoordinate() and pass to GetPunctualShadowAttenuation(). But in this case, who control the atlas application ? 
// TODO: 
// Caution: formula doesn't work as we are texture atlas...
// if (max3(abs(NDC.x), abs(NDC.y), 1.0 - texCoordXYZ.z) <= 1.0) return 1.0;

