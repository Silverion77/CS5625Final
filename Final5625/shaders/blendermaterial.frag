#version 140

#define MAX_LIGHTS 40	

in vec3 geom_position;
in vec3 geom_normal;
in vec2 geom_texCoord;
in vec3 geom_tangent;
in vec3 geom_bitangent;

uniform int light_count;
uniform vec3 light_eyePosition[MAX_LIGHTS];
uniform float light_falloffDistance[MAX_LIGHTS];
uniform float light_energy[MAX_LIGHTS];
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

	vec3 baseColor;
	result.rgb += mat_ambient;

	vec4 textureColor = vec4(1);	
	if (mat_hasTexture) {
		textureColor = texture2D(mat_texture, geom_texCoord);
		baseColor = textureColor.xyz;
		result.a = textureColor.a;
	}
	else {
		baseColor = mat_diffuse.xyz;
		result.a = mat_diffuse.a;
	}

	vec3 v = -normalize(geom_position);

	for (int i = 0; i < light_count; i++) {

		vec3 l = light_eyePosition[i] - geom_position;
		float r_squared = dot(l, l);
		l = normalize(l);

		// The attenuation model used here is from http://wiki.blender.org/index.php/Doc:2.6/Manual/Lighting/Lights/Light_Attenuation
		float d2 = light_falloffDistance[i] * light_falloffDistance[i];
		float intensity = light_energy[i] * (d2 / (d2 + r_squared));
		// Compute diffuse
		float diffuse_dot = max(dot(n,l), 0);

		// give some ambient lighting
		result.xyz += 0.1 * baseColor;
		// the rest depends on the light angle (diffuse_dot)
		result.xyz += 0.9 * baseColor * diffuse_dot * light_color[i] * intensity;

		// Compute specular, but only if the light source is not behind the surface
		if (dot(l, n) > 0.000001) {
			vec3 h = normalize(v + l);
			float specular_dot = pow(max(0, dot(n, h)), mat_shininess);
			result.xyz += mat_specular * specular_dot * light_color[i] * intensity;
		}
	}

	if (mat_hasAdditiveTexture)
	{
		vec2 t = normalize(mat3(transpose(viewMatrix)) * n).xy;
		t.x = t.x*0.5 + 0.5;
		t.y = t.y*0.5 + 0.5;
		vec4 sphereMapColor = texture2D(mat_additiveTexture, t);
		result.rgb += sphereMapColor.rgb;
	}

	vec3 norm = (n + 1) / 2;

	out_frag_color = result;

}