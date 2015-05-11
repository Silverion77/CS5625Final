using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Meshes
{
    /// <summary>
    /// This class represents a 2D Array Texture which is an OpenGL
    /// object representing a set of 2D textures all of the 
    /// same size. It should not be confused with the FloatArrayTexture
    /// class which is used to represent a float array as a one 
    /// dimensional texture (actually 2D with height = 1).
    /// </summary>
    class ArrayTexture : Texture
    {
        public ArrayTexture(int width, int height, params string[] layers)
        {   
            textureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, textureID);

            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, width, height, layers.Length);

            for(int i = 0; i < layers.Length; i++){
                if (String.IsNullOrEmpty(layers[i]))
                    throw new ArgumentException(layers[i]);

                Bitmap bmp = new Bitmap(layers[i]);

                if (bmp.Width != width || bmp.Height != height)
                    throw new ArgumentException(layers[i] + " has incorrect size");

                BitmapData bmp_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, i, width, height, 1, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);
                bmp.UnlockBits(bmp_data);
            }

            //GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);        
        }
    }
}
