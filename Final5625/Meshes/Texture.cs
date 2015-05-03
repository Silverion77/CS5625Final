using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes
{
    public class Texture
    {
        /// <summary>
        /// Internal texture ID that is known to OpenGL.
        /// </summary>
        int textureID;

        public Texture(string filename)
        {
            int id = LoadTexture(filename);
            textureID = id;
        }

        /// <summary>
        /// Returns the location of this texture, as used internally by OpenGL.
        /// Use this value when binding the texture for shaders.
        /// </summary>
        /// <returns></returns>
        public int getTextureID()
        {
            return textureID;
        }

        // This function courtesy of http://www.opentk.com/doc/graphics/textures/loading
        static int LoadTexture(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException(filename);

            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, id);

            // We will not upload mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // We can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            Bitmap bmp = new Bitmap(filename);
            BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            bmp.UnlockBits(bmp_data);

            return id;
        }
    }
}
