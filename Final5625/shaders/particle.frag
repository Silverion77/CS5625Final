#version 450

uniform sampler2D tex;
uniform sampler2D reaction;

out vec4 FragColor;

in VertexData{
	float reactionCoord;
	vec2 texCoord;
}vertexIn;

void main()
{
	FragColor = texture(tex, vertexIn.texCoord).r * vec4(texture(reaction, vec2(vertexIn.reactionCoord, 0.5)));
}
