using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes
{
    public class FloatArrayTexture : Texture
    {
        Vector4[] data;

        public int Width { get { return data.Length; } }
        public int Height { get { return 1; } }

        public FloatArrayTexture(float[] array)
        {
            data = new Vector4[array.Length];
            textureID = createTexture();
            setTextureData(array);
        }

        public void setTextureData(float[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                data[i].X = array[i];
                data[i].Y = array[i];
                data[i].Z = array[i];
                data[i].W = array[i];
            }
            if (data.Length > 1)
            {
                Console.WriteLine("Data in texture {0}:", textureID);
                foreach (Vector4 v in data)
                {
                    Console.WriteLine(v);
                }
            }

            GL.BindTexture(TextureTarget.TextureRectangle, textureID);

            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgba32f, Width, Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.Float, data);

            GL.BindTexture(TextureTarget.TextureRectangle, 0);
            Console.WriteLine("Texture {0} has size {1} x {2}", textureID, Width, Height);
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
