float length2(float3 v)
{
	return dot(v, v);
}

float sqr(float x)
{
	return x * x;
}

float average(float3 v)
{
	return (v.x + v.y + v.z) / 3.0;
}

float luminance(float3 v)
{ 
	return 0.2126 * v.x + 0.7152 * v.y + 0.0722 * v.z;
}

float min(float3 v)
{ 
	return min(v.x, min(v.y, v.z)); 
}

float max(float3 v)
{ 
	return max(v.x, max(v.y, v.z)); 
}

float min(float x, float y, float z)
{ 
	return min(x, min(y, z)); 
}

float max(float x, float y, float z)
{ 
	return max(x, max(y, z)); 
}
