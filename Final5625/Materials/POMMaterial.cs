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
        // Textures
        // For now, we will use 2 textures for Blinn Phong (subject to change)
        Texture diffuseTexture;
        Texture specularTexture;
        // Required for POM
        Texture heightMap;
        Texture normalMap;
        float parallaxScale = .1f;
        Vector2 scaleFactor = new Vector2(20, 20);
        float shininess;

        public POMMaterial(float shininess, string TextureDirectory)
        {
            this.shininess = shininess;
            String texturePath = "data/texture/" + TextureDirectory;
            diffuseTexture = TextureManager.getTexture(texturePath + "/diffuse.png");
            specularTexture = TextureManager.getTexture(texturePath + "/specular.png");
            heightMap = TextureManager.getTexture(texturePath + "/height.png");
            normalMap = TextureManager.getTexture(texturePath + "/normal.png");
        }

        public POMMaterial(string TextureDirectory) :
            this(32, TextureDirectory) { }

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
            program.bindTexture2D("diffuseTexture", startTexUnit, diffuseTexture);
            program.bindTexture2D("specularTexture", startTexUnit + 1, specularTexture);
            program.bindTexture2D("heightTexture", startTexUnit + 2, heightMap);
            program.bindTexture2D("normalTexture", startTexUnit + 3, normalMap);
            program.setUniformFloat1("parallaxScale", parallaxScale);
            program.setUniformFloat1("mat_shininess", shininess);
            program.setUniformFloat2("scale_factor", scaleFactor);

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
