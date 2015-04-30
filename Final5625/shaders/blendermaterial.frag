#version 140

#define MAX_LIGHTS 40	

in vec3 geom_position;
in vec3 geom_normal;
in vec2 geom_texCoord;
in vec3 geom_tangent;
in vec3 geom_bitangent;

uniform int light_count;
uniform vec3 light_eyePosition[MAX_LIGHTS];
uniform vec3 light_attenuation[MAX_LIGHTS];
uniform vec3 light_color[MAX_LIGHTS];

uniform vec3 mat_ambient;
uniform vec4 mat_diffuse;
uniform vec3 mat_specular;
uniform float mat_shininess;

uniform bool mat_hasTexture;
uniform sampler2D mat_texture;

uniform bool mat_hasAdditiveTexture;
uniform sampler2D mat_additiveTexture;

uniform mat4 viewMatrix;

out vec4 out_frag_color;

void main()
{
	vec4 result = vec4(0, 0, 0, mat_diffuse.a);
	vec3 n = normalize(geom_normal);	

	vec3 baseColor = 0.5 * mat_ambient;
	vec4 textureColor = vec4(1);	
	if (mat_hasTexture) {
		textureColor = texture2D(mat_texture, vec2(geom_texCoord.x, 1 - geom_texCoord.y));
	}

	out_frag_color = vec4(textureColor.xyz, 1);

}