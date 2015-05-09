using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chireiden.Meshes;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace SALevelEditor
{
    class MapTexture : Texture
    {
        int Width;
        int Height;
        Vector4[,] data;

        public MapTexture(int[,] tiles)
        {
            Width = tiles.GetUpperBound(0) + 1;
            Height = tiles.GetUpperBound(1) + 1;
            data = new Vector4[Height, Width];
        }

        void setData(int[,] tiles)
        {
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    // Rectangular arrays address things as (row, column), but we want our
                    // 2D map to be addressed as (x, y) = (column, row).
                    data[j, i] = new Vector4(tiles[i, j] * 0.2f, tiles[i, j] * 0.9f, 0, 1);
                }
            }
        }

        public void setTextureData(int[,] tiles)
        {
            setData(tiles);
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
