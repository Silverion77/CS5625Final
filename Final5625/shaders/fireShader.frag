#version 140

in vec2 geom_texCoord;

// uniform
uniform sampler2D un_NoiseTexture;
uniform sampler2D un_FireTexture;
uniform float un_Time;
uniform vec3 un_ScrollSpeeds;

out vec4 out_frag_color;

void main()
{
	float y = mod(geom_texCoord.y + un_Time * un_ScrollSpeeds.x, 1);
	vec4 set1 = texture(un_NoiseTexture, vec2(geom_texCoord.x, y));
	y = mod(geom_texCoord.y * 2 + un_Time * un_ScrollSpeeds.y, 1);
	vec4 set2 = texture(un_NoiseTexture, vec2(mod(geom_texCoord.x * 2, 1), y ));
	y = mod(geom_texCoord.y * 3 + un_Time * un_ScrollSpeeds.z, 1);
	vec4 set3 = texture(un_NoiseTexture, vec2(mod(geom_texCoord.x * 3, 1), y));
	float avgX = (set1.x + set2.x + set3.x) / 3;
	float avgY = (set1.y + set2.y + set3.y) / 3;
	out_frag_color = texture(un_FireTexture, vec2(mod(avgX, 1), mod(avgY, 1)));
}
