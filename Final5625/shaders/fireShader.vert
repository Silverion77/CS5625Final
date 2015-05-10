#version 140

in vec3 vert_position;
in vec3 vert_normal;
in vec2 vert_texCoord;
in vec4 vert_tangent;

// uniforms
uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;

out vec2 geom_texCoord;

void main()
{
	geom_texCoord = vert_texCoord;
	gl_Position = projectionMatrix * modelViewMatrix * vec4(vert_position, 1);
}