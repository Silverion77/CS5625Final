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
    /// <summary>
    /// A type of material for all the stuff I'm exporting from blender.
    /// It's basically Blinn-Phong.
    /// </summary>
    public class BlenderMaterial : Material
    {
        // Standard parameters for Blinn-Phong.
        Vector4 diffuseColor;
        Vector3 specularColor;
        Vector3 ambientColor;
        float shininess;

        // I think there will only ever be two textures: the first one for the
        // actual image, and the second one that just gets added to produce
        // some extra highlights.
        Texture diffuseTexture;
        Texture additiveTexture;

        public BlenderMaterial(Assimp.Material mat, string textureDirectory)
        {
            var amb = mat.ColorAmbient;
            ambientColor = new Vector3(amb.R, amb.G, amb.B);
            var dif = mat.ColorDiffuse;
            diffuseColor = new Vector4(dif.R, dif.G, dif.B, dif.A);
            var spc = mat.ColorSpecular;
            specularColor = new Vector3(spc.R, spc.G, spc.B);
            shininess = mat.Shininess;

            int numTextures = mat.GetMaterialTextureCount(TextureType.Diffuse);
            var textures = mat.GetMaterialTextures(TextureType.Diffuse);
            diffuseTexture = null;
            additiveTexture = null;
            if (numTextures >= 1)
            {
                TextureSlot ts = textures[0];
                string texFile = System.IO.Path.Combine(textureDirectory, ts.FilePath);
                //Console.WriteLine("Diffuse texture is at {0}", texFile);
                Texture t = TextureManager.getTexture(texFile);
                diffuseTexture = t;
            }
            if (numTextures >= 2)
            {
                TextureSlot ts = textures[1];
                string texFile = System.IO.Path.Combine(textureDirectory, ts.FilePath);
                //Console.WriteLine("Additive texture is at {0}", texFile);
                Texture t = TextureManager.getTexture(texFile);
                additiveTexture = t;
            }
            //Console.WriteLine("Material has texture with ID {0}", diffuseTexture.getTextureID());
        }

        public bool hasDiffuseTexture()
        {
            return diffuseTexture != null;
        }

        public bool hasAdditiveTexture()
        {
            return additiveTexture != null;
        }

        public int useMaterialParameters(ShaderProgram program)
        {
            program.setUniformFloat3("mat_ambient", ambientColor);
            program.setUniformFloat4("mat_diffuse", diffuseColor);
            program.setUniformFloat3("mat_specular", specularColor);
            program.setUniformFloat1("mat_shininess", shininess);

            if (hasDiffuseTexture())
            {
                program.setUniformBool("mat_hasTexture", true);
                program.bindTexture2D("mat_texture", 0, diffuseTexture);
            }
            else program.setUniformBool("mat_hasTexture", false);
            if (hasAdditiveTexture())
            {
                program.setUniformBool("mat_hasAdditiveTexture", true);
                program.bindTexture2D("mat_additiveTexture", 1, additiveTexture);
            }
            else program.setUniformBool("mat_hasAdditiveTexture", false);

            // We bound 2 textures, so the next available texture unit is 2.
            return 2;
        }

        public void unuseMaterialParameters(ShaderProgram program)
        {
            program.unbindTexture2D(0);
            program.unbindTexture2D(1);
        }
    }
}
