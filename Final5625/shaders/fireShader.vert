#version 330

in vec3 in_Position;
in vec3 in_Normal;
in vec2 in_TexCoord;

// uniforms
uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;

out vec2 out_texCoord;

void main()
{
	out_texCoord = in_TexCoord;
	gl_Position = projectionMatrix * modelViewMatrix * vec4(in_Position, 1);
}