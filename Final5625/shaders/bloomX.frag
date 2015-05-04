#version 140

uniform sampler2D tex;
uniform int mipmapLevel;
uniform float threshold;

out vec4 FragColor;

//Radius of the gaussian filter. Standard deviation will be half this value.
uniform int width = 15;

void main()
{
	float totalWeight = 0;
	FragColor = vec4(0,0,0,1);
	for(int i = -width; i <= width; i++){
		float weight = exp(-1.0 * i*i / (0.5 * width * width)); 
		vec3 texel = texelFetch(tex, ivec2(gl_FragCoord.xy + vec2(i, 0)), mipmapLevel).rgb;
		FragColor.rgb += max(texel - threshold, 0.0) * weight;
		totalWeight += weight;
	}
	FragColor.rgb /= totalWeight;
}