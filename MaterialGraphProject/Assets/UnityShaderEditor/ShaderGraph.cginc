// Gradient Type
struct Gradient
{
	int type;
	int colorsLength;
	int alphasLength;
	float4 colors[8];
	float2 alphas[8];
};

Gradient ShaderGraph_DefaultGradient()
{
	Gradient g;
	g.type = 0;
	g.colorsLength = 2;
	g.alphasLength = 2;
	g.colors[0] = float4(0, 0, 0, 0);
	g.colors[1] = float4(1, 1, 1, 1);
	g.colors[2] = float4(0, 0, 0, 0);
	g.colors[3] = float4(0, 0, 0, 0);
	g.colors[4] = float4(0, 0, 0, 0);
	g.colors[5] = float4(0, 0, 0, 0);
	g.colors[6] = float4(0, 0, 0, 0);
	g.colors[7] = float4(0, 0, 0, 0);
	g.alphas[0] = float2(1, 0);
	g.alphas[1] = float2(1, 1);
	g.alphas[2] = float2(0, 0);
	g.alphas[3] = float2(0, 0);
	g.alphas[4] = float2(0, 0);
	g.alphas[5] = float2(0, 0);
	g.alphas[6] = float2(0, 0);
	g.alphas[7] = float2(0, 0);
	return g;
}