#version 140

uniform sampler2D colorBuffer;

out vec4 FragColor;

void main()
{
	//see: https://en.wikipedia.org/wiki/Relative_luminance

	vec3 color = texelFetch(colorBuffer, ivec2(gl_FragCoord.xy), 0).rgb;
	float luminance = dot(vec3(0.2126, 0.7152, 0.0722), max(color,0));

	FragColor = vec4(log(luminance + 0.001), 0,0,1);
}