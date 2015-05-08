#version 450

layout(location = 0) in vec3 position;
layout(location = 1) in float rotation;
layout(location = 2) in float radius;
layout(location = 3) in float reactionCoord;

out VertexData{
	float reactionCoord;
	float radius;
	float angle;
	int instanceID;
}vertexOut;

void main(void)
{
	vertexOut.reactionCoord = reactionCoord;
	vertexOut.radius = radius;
	vertexOut.angle = rotation;
	vertexOut.instanceID = gl_InstanceID;
	gl_Position = vec4(position, 1);
}