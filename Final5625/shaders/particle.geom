#version 450
 
layout(points) in;
layout(triangle_strip, max_vertices=4) out;

in VertexData{
	float reactionCoord;
	float radius;
	float angle;
	int instanceID;
	float colorScale;
	float alphaScale;
	float textureLayer;
}vertexIn[1];
 
out VertexData{
	float reactionCoord;
	vec2 texCoord;
	float colorScale;
	float alphaScale;
	float textureLayer;
}vertexOut;

uniform vec3 up;
uniform vec3 right;
uniform vec3 eye;
uniform vec3 forward;
uniform float zNear;
uniform float zFar;
uniform float invInstances;
uniform mat4 viewProjectionMatrix;

void main()
{
	float depth = dot(gl_in[0].gl_Position.xyz - eye, forward);
	float sliceNear = zNear + (zFar - zNear) * (1.0 - (vertexIn[0].instanceID+1) * invInstances);
	float sliceFar =  zNear + (zFar - zNear) * (1.0 - vertexIn[0].instanceID * invInstances);
	if(sliceFar < depth || sliceNear > depth)
		return;

	vertexOut.reactionCoord = vertexIn[0].reactionCoord;
	vertexOut.textureLayer = vertexIn[0].textureLayer;
	vertexOut.colorScale = vertexIn[0].colorScale;
	vertexOut.alphaScale = vertexIn[0].alphaScale;

	float sAng = sin(vertexIn[0].angle);
	float cAng = cos(vertexIn[0].angle);

	vec4 U = vec4((-right * sAng + up * cAng) * vertexIn[0].radius, 0.0);
	vec4 R = vec4(( right * cAng + up * sAng) * vertexIn[0].radius, 0.0);
	
	gl_Position = viewProjectionMatrix * (gl_in[0].gl_Position + U + R);
	vertexOut.texCoord  = vec2(0,0);
	EmitVertex();
	
	gl_Position = viewProjectionMatrix * (gl_in[0].gl_Position + U - R);
	vertexOut.texCoord  = vec2(0,1);
	EmitVertex();

	gl_Position = viewProjectionMatrix * (gl_in[0].gl_Position - U + R);
	vertexOut.texCoord = vec2(1,0);
	EmitVertex();

	gl_Position = viewProjectionMatrix * (gl_in[0].gl_Position - U - R);
	vertexOut.texCoord  = vec2(1,1);
	EmitVertex();
}
