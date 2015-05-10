#version 140

in vec3 vert_position;
in vec3 vert_normal;
in vec2 vert_texCoord;
in vec4 vert_tangent;

// uniforms
uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;

smooth out vec2 out_texCoord;

void main()
{
	out_texCoord = vert_texCoord;
	gl_Position = projectionMatrix * modelViewMatrix * vec4(vert_position, 1);
}