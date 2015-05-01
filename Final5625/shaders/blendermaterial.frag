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
		baseColor = 0.5 * textureColor.xyz;
		result.a = textureColor.a;
	}
	else {
		baseColor = 0.5 * mat_diffuse.xyz;
		result.a = mat_diffuse.a;
	}

	for (int i = 0; i < light_count; i++) {
		vec3 v = -normalize(geom_position);
		vec3 l = light_eyePosition[i] - geom_position;
		float r_squared = dot(l, l);
		vec3 h = normalize(v + l);

		// The attenuation model used here is from http://wiki.blender.org/index.php/Doc:2.6/Manual/Lighting/Lights/Light_Attenuation
		float d2 = light_falloffDistance[i] * light_falloffDistance[i];
		float intensity = light_energy[i] * (d2 / (d2 + r_squared));
		vec3 color = baseColor * intensity;

		float dotProd = dot(n, l);
		if (mat_hasTexture) {
			color += 0.7 * max(0,dotProd) * textureColor.xyz * intensity;
		}
		else {
			color += 0.7 * max(0,dotProd) * mat_diffuse.xyz * intensity;
		}

		float halfDot = dot(h, n);
		vec3 specular = pow(max(0.00001, halfDot), mat_shininess) * mat_specular * intensity;
		color += specular;
		result.rgb += color;
	}

	if (mat_hasAdditiveTexture)
	{
		vec2 t = normalize(mat3(transpose(viewMatrix)) * n).xy;
		t.x = t.x*0.5 + 0.5;
		t.y = t.y*0.5 + 0.5;
		vec4 sphereMapColor = texture2D(mat_additiveTexture, t);
		result.rgb += sphereMapColor.rgb;
	}

	out_frag_color = result;

}