#version 450
 
layout(points) in;
layout(triangle_strip, max_vertices=4) out;

in VertexData{
	float reactionCoord;
	float radius;
	float angle;
}vertexIn[1];
 
out VertexData{
	float reactionCoord;
	vec2 texCoord;
}vertexOut;

uniform vec3 up;
uniform vec3 right;
uniform mat4 viewProjectionMatrix;

void main()
{
	vertexOut.reactionCoord = vertexIn[0].reactionCoord;

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
