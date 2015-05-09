using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Text;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.UI
{
    public class TextRenderer : IDisposable
    {
        Bitmap bmp;
        Graphics gfx;
        int textureID;
        Rectangle dirty_region;
        bool disposed;

        int pixelWidth;
        int pixelHeight;

        float normalizedScreenWidth;
        float normalizedScreenHeight;

        #region Constructors

        /// <summary>
        /// Constructs a new instance.
        /// </summary>
        /// <param name="width">The width of the backing store in pixels.</param>
        /// <param name="height">The height of the backing store in pixels.</param>
        public TextRenderer(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height ");
            if (GraphicsContext.CurrentContext == null)
                throw new InvalidOperationException("No GraphicsContext is current on the calling thread.");

            bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            gfx = Graphics.FromImage(bmp);
            gfx.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            textureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureID);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            pixelWidth = width;
            pixelHeight = height;

            normalizedScreenWidth = 2 * (float)pixelWidth / GameMain.ScreenWidth;
            // I want the texture to extend down from the top-left corner of the box,
            // rather than up from the bottom-left, so this is negated.
            normalizedScreenHeight = -2 * (float)pixelHeight / GameMain.ScreenHeight;

            // Start it off as transparency
            Clear();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Clears the backing store to the specified color.
        /// </summary>
        /// <param name="color">A <see cref="System.Drawing.Color"/>.</param>
        public void Clear(Color color)
        {
            gfx.Clear(color);
            dirty_region = new Rectangle(0, 0, bmp.Width, bmp.Height);
        }

        public void Clear()
        {
            Clear(Color.Transparent);
        }

        /// <summary>
        /// Draws the specified string to the backing store.
        /// </summary>
        /// <param name="text">The <see cref="System.String"/> to draw.</param>
        /// <param name="font">The <see cref="System.Drawing.Font"/> that will be used.</param>
        /// <param name="brush">The <see cref="System.Drawing.Brush"/> that will be used.</param>
        /// <param name="point">The location of the text on the backing store, in 2d pixel coordinates.
        /// The origin (0, 0) lies at the top-left corner of the backing store.</param>
        public void DrawString(string text, Font font, Brush brush, PointF point)
        {
            gfx.DrawString(text, font, brush, point);

            SizeF size = gfx.MeasureString(text, font);
            dirty_region = Rectangle.Round(RectangleF.Union(dirty_region, new RectangleF(point, size)));
            dirty_region = Rectangle.Intersect(dirty_region, new Rectangle(0, 0, bmp.Width, bmp.Height));
        }

        /// <summary>
        /// Gets a <see cref="System.Int32"/> that represents an OpenGL 2d texture handle.
        /// The texture contains a copy of the backing store. Bind this texture to TextureTarget.Texture2d
        /// in order to render the drawn text on screen.
        /// </summary>
        public int Texture
        {
            get
            {
                UploadBitmap();
                return textureID;
            }
        }

        #endregion

        #region Private Members

        // Uploads the dirty regions of the backing store to the OpenGL texture.
        void UploadBitmap()
        {
            if (dirty_region != RectangleF.Empty)
            {
                System.Drawing.Imaging.BitmapData data = bmp.LockBits(dirty_region,
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.BindTexture(TextureTarget.Texture2D, textureID);
                GL.TexSubImage2D(TextureTarget.Texture2D, 0,
                    dirty_region.X, dirty_region.Y, dirty_region.Width, dirty_region.Height,
                    PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

                bmp.UnlockBits(data);

                dirty_region = Rectangle.Empty;
            }
        }

        public void drawTextAtLoc(int x, int y)
        {
            Chireiden.ShaderProgram program = ShaderLibrary.TextShader;

            float normalizedScreenX = 2 * (float)x / GameMain.ScreenWidth - 1;
            // I want (x,y) to start at the top-left, rather than the bottom-left,
            // hence the negation of the Y coordinate here
            float normalizedScreenY = 1 - 2 * (float)y / GameMain.ScreenHeight;

            Vector4 locDims = new Vector4(normalizedScreenX, normalizedScreenY, normalizedScreenWidth, normalizedScreenHeight);

            Console.WriteLine("Drawing text at {0} with dimensions {1}", locDims.Xy, locDims.Zw);

            // Bind the stuff we need for this object (VAO, index buffer, program)
            GL.BindVertexArray(Utils.QuadVAO);

            program.use();
            program.bindTexture2D("texture", 0, (uint)Texture);
            program.setUniformFloat4("locationAndDimensions", locDims);

            GL.DrawElements(PrimitiveType.Triangles, Utils.QuadNumFaces,
                DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Clean up
            program.unbindTexture2D(0);
            program.unuse();
            GL.BindVertexArray(0);
        }

        #endregion

        #region IDisposable Members

        void Dispose(bool manual)
        {
            if (!disposed)
            {
                if (manual)
                {
                    bmp.Dispose();
                    gfx.Dispose();
                    if (GraphicsContext.CurrentContext != null)
                        GL.DeleteTexture(textureID);
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TextRenderer()
        {
            Console.WriteLine("[Warning] Resource leaked: {0}.", typeof(TextRenderer));
        }

        #endregion
    }
}
