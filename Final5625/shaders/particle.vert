#version 140
uniform mat4 viewProjectionMatrix;

in vec3 in_position;

void main(void)
{
	gl_Position = viewProjectionMatrix * vec4(in_position, 1);
	gl_PointSize = 16.0;
}