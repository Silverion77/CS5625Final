#version 450

layout(location = 0) in vec3 position;
layout(location = 1) in float rotation;
layout(location = 2) in float radius;
layout(location = 3) in float colorScale;
layout(location = 4) in float alphaScale;
layout(location = 5) in float reactionCoord;
layout(location = 6) in int textureLayer;

out VertexData{
	float reactionCoord;
	float radius;
	float angle;
	int instanceID;
	float colorScale;
	float alphaScale;
	flat int textureLayer;
}vertexOut;

void main(void)
{
	vertexOut.reactionCoord = reactionCoord;
	vertexOut.radius = radius;
	vertexOut.angle = rotation;
	vertexOut.instanceID = gl_InstanceID;
	vertexOut.textureLayer = textureLayer;
	vertexOut.colorScale = colorScale;
	vertexOut.alphaScale = alphaScale;
	gl_Position = vec4(position, 1);
}