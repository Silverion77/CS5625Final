using System;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Assimp;

using Chireiden.Meshes;

namespace Chireiden.Materials
{
    public class POMMaterial : Material
    {
        // Standard parameters for Blinn-Phong
        Vector4 diffuseColor;
        Vector3 specularColor;
        Vector3 ambientColor;
        float shininess;

        // Textures
        // For now, we will use 2 textures for Blinn Phong (subject to change)
        Texture diffuseTexture;
        Texture specularTexture;
        // Required for POM
        Texture heightMap;
        Texture normalMap;
        float parallaxScale;

        public POMMaterial()
        { }

        public POMMaterial(Vector4 diffuseColor, Vector3 specularColor,
                            Vector3 ambientColor, float shininess, string TextureDirectory)
        {
            this.diffuseColor = diffuseColor;
            this.specularColor = specularColor;
            this.ambientColor = ambientColor;
            this.shininess = shininess;
        }

        public bool hasDiffuseTexture()
        {
            return diffuseTexture != null;
        }

        public bool hasAdditiveTexture()
        {
            return specularTexture != null;
        }

        public int useMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            program.setUniformFloat3("mat_ambient", ambientColor);
            program.setUniformFloat4("mat_diffuse", diffuseColor);
            program.setUniformFloat3("mat_specular", specularColor);
            program.setUniformFloat1("mat_shininess", shininess);

            //program.bindTexture2D("diffuseTexture", startTexUnit, diffuseTexture);
            //program.bindTexture2D("specularTexture", startTexUnit + 1, specularTexture);
            //program.bindTexture2D("heightTexture", startTexUnit + 2, heightMap);
            //program.bindTexture2D("normalTexture", startTexUnit + 3, normalMap);
            program.setUniformFloat1("parallaxScale", parallaxScale);

            return startTexUnit + 4;
        }

        public void unuseMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            program.unbindTexture2D(startTexUnit);
            program.unbindTexture2D(startTexUnit + 1);
            program.unbindTexture2D(startTexUnit + 2);
            program.unbindTexture2D(startTexUnit + 3);
        }
    }
}
