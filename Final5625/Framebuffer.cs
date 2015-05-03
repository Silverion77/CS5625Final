using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden
{
    class Framebuffer
    {
        static int width;
        static int height;

        static uint FboHandle;
        static uint ColorTexture;
        static uint DepthRenderbuffer;

        static uint DownsampleTexture;

        static int FullscreenQuadVbo;
        static int FullscreenQuadVao;

        private static void RenderFullscreenQuad()
        {
            GL.BindVertexArray(FullscreenQuadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.BindVertexArray(0);
        }

        private static void generateFullscreenQuad()
        {
            Vector2[] PositionVboData = new Vector2[]{
            new Vector2(-1.0f, -1.0f),
            new Vector2( 1.0f, -1.0f),
            new Vector2( 1.0f,  1.0f),
            new Vector2( 1.0f,  1.0f),
            new Vector2(-1.0f,  1.0f),
            new Vector2(-1.0f, -1.0f) };

            FullscreenQuadVbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, FullscreenQuadVbo);
            GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
                new IntPtr(PositionVboData.Length * Vector2.SizeInBytes),
                PositionVboData, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            FullscreenQuadVao = GL.GenVertexArray();
            GL.BindVertexArray(FullscreenQuadVao);
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, FullscreenQuadVbo);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
            GL.BindVertexArray(0);
        }

        public static void Init(int fboWidth, int fboHeight)
        {
            width = fboWidth;
            height = fboHeight;

            // Based on: http://www.opentk.com/doc/graphics/frame-buffer-objects
            
            // Create Color Texture
            GL.GenTextures(1, out ColorTexture);
            GL.BindTexture(TextureTarget.Texture2D, ColorTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb16f, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Create Downsample Texture
            GL.GenTextures(1, out DownsampleTexture);
            GL.BindTexture(TextureTarget.Texture2D, DownsampleTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, width, height, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            // Create Depth Renderbuffer
            GL.GenRenderbuffers(1, out DepthRenderbuffer);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthRenderbuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent32, width, height);

            // TODO: test for GL Error here (might be unsupported format)

            // Create a FBO and attach the textures
            GL.GenFramebuffers(1, out FboHandle);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FboHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTexture, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, DepthRenderbuffer);

            // TODO: now GL.Ext.CheckFramebufferStatus( FramebufferTarget.FramebufferExt ) can be called, check the end of this page for a snippet.

            // since there's only 1 Color buffer attached this is not explicitly required
            GL.DrawBuffer((DrawBufferMode)FramebufferAttachment.ColorAttachment0);

            GL.Viewport(0, 0, width, height);
            
            generateFullscreenQuad();
        }

        public static void AttachDepthBuffer()
        {
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, DepthRenderbuffer);
        }
        public static void DetachDepthBuffer()
        {
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);
        }

        public static void BlitToScreen()
        {
            GL.Disable(EnableCap.DepthTest);

            //Compute log luminance
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, DownsampleTexture, 0);
            Shaders.LogLuminanceShader.use();
            Shaders.LogLuminanceShader.bindTexture2D("colorBuffer", 0, ColorTexture);
            RenderFullscreenQuad();
            Shaders.LogLuminanceShader.unuse();
            GL.BindTexture(TextureTarget.Texture2D, DownsampleTexture);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
            GL.DrawBuffer(DrawBufferMode.Back);
            Shaders.TonemapShader.use();
            Shaders.TonemapShader.bindTexture2D("colorBuffer", 0, ColorTexture);
            Shaders.TonemapShader.bindTexture2D("logLuminance", 1, DownsampleTexture);
            RenderFullscreenQuad();
            Shaders.TonemapShader.unuse();

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, FboHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorTexture, 0);
            GL.DrawBuffer((DrawBufferMode)FramebufferAttachment.ColorAttachment0);
            GL.Enable(EnableCap.DepthTest);
        }
    }
}
