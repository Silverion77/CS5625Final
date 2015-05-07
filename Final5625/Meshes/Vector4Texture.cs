using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes
{
    public class Vector4Texture : Texture
    {
        Vector4[] data;

        public int Width { get { return data.Length; } }
        public int Height { get { return 1; } }

        public Vector4Texture(Vector4[] array)
        {
            data = array;
            textureID = createTexture();
            setTextureData();
        }

        public void setTextureData()
        {
            GL.BindTexture(TextureTarget.TextureRectangle, textureID);

            GL.TexImage2D(TextureTarget.TextureRectangle, 0, PixelInternalFormat.Rgba32f, Width, Height, 0,
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
