using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes
{
    public class MatrixTexture : Texture
    {
        int width;
        int height = 1;

        public int Width { get { return width; } }
        public int Height { get { return height; } }

        Vector4[] data;

        public MatrixTexture(Matrix4[] array)
        {
            width = array.Length * 4;
            data = new Vector4[width * height];
            textureID = createTexture();
        }

        unsafe public void setTextureData(Matrix4[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                int start = i * 4;
                data[start] = array[i].Row0;
                data[start + 1] = array[i].Row1;
                data[start + 2] = array[i].Row2;
                data[start + 3] = array[i].Row3;
            }

            GL.BindTexture(TextureTarget.TextureRectangle, textureID);

            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgba32f, width, height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.Float, data);

            GL.BindTexture(TextureTarget.TextureRectangle, 0);
        }

        int createTexture()
        {
            int id = GL.GenTexture();
            GL.BindTexture(TextureTarget.TextureRectangle, id);

            // We will not upload mipmaps, so disable mipmapping (otherwise the texture will not appear).
            // We can use GL.GenerateMipmaps() or GL.Ext.GenerateMipmaps() to create
            // mipmaps automatically. In that case, use TextureMinFilter.LinearMipmapLinear to enable them.
            GL.TexParameter(TextureTarget.TextureRectangle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.TextureRectangle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.BindTexture(TextureTarget.TextureRectangle, 0);

            return id;
        }
    }
}
