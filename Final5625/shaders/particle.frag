#version 450

uniform sampler2DArray tex;
uniform sampler2DArray reaction;

out vec4 FragColor;

in VertexData{
	float reactionCoord;
	vec2 texCoord;
	float colorScale;
	float alphaScale;
	float textureLayer;
}vertexIn;

void main()
{
	float transparency = texture(tex, vec3(vertexIn.texCoord, vertexIn.textureLayer)).r;
	vec4 rColor = texture(reaction, vec3(vertexIn.reactionCoord, 0.5, vertexIn.textureLayer));
	FragColor = transparency * rColor;

	FragColor.rgb *= vertexIn.colorScale;
	FragColor.a *= vertexIn.alphaScale;
}
