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
        Texture additiveTexture;
        // Required for POM
        Texture heightMap;
        Texture normalMap;

        public POMMaterial(Vector4 diffuseColor, Vector3 specularColor,
                            Vector3 ambientColor, float shininess, string TextureDirectory)
        {
            this.diffuseColor = diffuseColor;
            this.specularColor = specularColor;
            this.ambientColor = ambientColor;
            this.shininess = shininess;
        }

        public int useMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            return 0;
        }

        public void unuseMaterialParameters(ShaderProgram program, int startTexUnit)
        {

        }
    }
}
