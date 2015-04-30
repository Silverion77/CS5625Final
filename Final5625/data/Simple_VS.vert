#version 140

precision highp float;

uniform mat4 projectionMatrix;
uniform mat4 modelViewMatrix;

in vec3 in_position;
in vec3 in_normal;

out vec3 normal;

void main(void)
{
  //works only for orthogonal modelview
  normal = (modelViewMatrix * vec4(in_normal, 0)).xyz;
  
  gl_Position = projectionMatrix * modelViewMatrix * vec4(in_position, 1);
}