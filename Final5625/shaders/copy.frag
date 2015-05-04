#version 140

uniform sampler2D tex;

out vec4 FragColor;

void main()
{
	FragColor = texelFetch(tex, ivec2(gl_FragCoord.xy), 0);
}