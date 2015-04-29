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
        Vector4 specularColor;

        // I think there will only ever be two textures: the first one for the
        // actual image, and the second one that just gets added to produce
        // some extra highlights.
        Texture diffuseTexture;
        Texture additiveTexture;

        public BlenderMaterial(Assimp.Material mat, string textureDirectory)
        {
            if (mat.HasColorDiffuse)
            {
                var c = mat.ColorDiffuse;
                diffuseColor = new Vector4(c.R, c.G, c.B, c.A);
            }
            else diffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1);

            if (mat.HasColorSpecular)
            {
                var c = mat.ColorSpecular;
                specularColor = new Vector4(c.R, c.G, c.B, c.A);
            }
            else specularColor = new Vector4(0.5f, 0.5f, 0.5f, 1f);

            int numTextures = mat.GetMaterialTextureCount(TextureType.Diffuse);
            var textures = mat.GetMaterialTextures(TextureType.Diffuse);
            diffuseTexture = null;
            additiveTexture = null;
            if (numTextures >= 1)
            {
                TextureSlot ts = textures[0];
                string texFile = System.IO.Path.Combine(textureDirectory, ts.FilePath);
                Console.WriteLine("Texture is at {0}", texFile);
                Texture t = TextureManager.getTexture(texFile);
                diffuseTexture = t;
            }
            if (numTextures >= 2)
            {
                TextureSlot ts = textures[1];
                string texFile = System.IO.Path.Combine(textureDirectory, ts.FilePath);
                Console.WriteLine("Texture is at {0}", texFile);
                Texture t = TextureManager.getTexture(texFile);
                additiveTexture = t;
            }
            Console.WriteLine("Material has texture with ID {0}", diffuseTexture.getID());
        }

        public bool hasDiffuseTexture()
        {
            return diffuseTexture == null;
        }

        public bool hasAdditiveTexture()
        {
            return additiveTexture == null;
        }

        public void useMaterialParameters()
        {
            throw new NotImplementedException();
        }

        public void unuseMaterialParameters()
        {
            throw new NotImplementedException();
        }
    }
}
