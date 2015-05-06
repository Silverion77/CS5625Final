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
        static uint DepthRenderbuffer;

        // The first is the main renderbuffer, while the second is used for
        // doing transparency and blurring. We use the third to do the bloom 
        // effect.
        static uint[] ColorBuffers = new uint[3];

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
            GL.GenTextures(3, ColorBuffers);
            for (int i = 0; i < 3; i++)
            {
                GL.BindTexture(TextureTarget.Texture2D, ColorBuffers[i]);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapNearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            }
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
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorBuffers[0], 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, DepthRenderbuffer);

            CheckFrameBufferStatus();

            // since there's only 1 Color buffer attached this is not explicitly required
            GL.DrawBuffer((DrawBufferMode)FramebufferAttachment.ColorAttachment0);

            GL.Viewport(0, 0, width, height);

            generateFullscreenQuad();
        }

        /// <summary>
        /// Transparent particles are rendered using premultiplied alpha, which allows for additive blending.
        /// To properly compose them into the scene, it is necessary to render them to a seperate buffer
        /// and then later blend them with the rest of the scene.
        /// </summary>
        public static void StartTransparency()
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorBuffers[1], 0);

            GL.Viewport(0, 0, width, height);

            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            CheckFrameBufferStatus();
        }
        public static void EndTransparency()
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorBuffers[0], 0);
            GL.Viewport(0, 0, width, height);

            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Disable(EnableCap.DepthTest);
            ShaderLibrary.CopyShader.use();
            ShaderLibrary.CopyShader.bindTexture2D("tex", 0, ColorBuffers[1]);
            RenderFullscreenQuad();
            ShaderLibrary.CopyShader.unuse();
            GL.Enable(EnableCap.DepthTest);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            CheckFrameBufferStatus();
        }
        private static void CheckFrameBufferStatus()
        {
            FramebufferErrorCode e = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (e != FramebufferErrorCode.FramebufferComplete)
                throw new Exception(e.ToString());
        }
        public static void BlitToScreen()
        {
            CheckFrameBufferStatus();

            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, 0);

            GL.BindTexture(TextureTarget.Texture2D, ColorBuffers[0]);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

            CheckFrameBufferStatus();

            GL.Disable(EnableCap.DepthTest);

            //Compute bloom  
            ShaderLibrary.BloomXShader.use();
            ShaderLibrary.BloomXShader.bindTexture2D("tex", 0, ColorBuffers[0]);
            ShaderLibrary.BloomXShader.setUniformFloat1("threshold", 1.0f);
            for (int i = 0; i <= 6; i += 3)
            {
                GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorBuffers[1], i);
                CheckFrameBufferStatus();
                GL.Viewport(0, 0, width >> i, height >> i);
                
                ShaderLibrary.BloomXShader.setUniformInt1("mipmapLevel", i);
                RenderFullscreenQuad();
            }
            ShaderLibrary.BloomXShader.unuse();
            GL.Viewport(0, 0, width, height);

            ShaderLibrary.BloomYShader.use();
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorBuffers[0], 0);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            ShaderLibrary.BloomYShader.bindTexture2D("tex", 0, ColorBuffers[1]);
            for (int i = 0; i <= 6; i += 3)
            {
                ShaderLibrary.BloomYShader.setUniformFloat1("bloomWeight", 0.2f);
                ShaderLibrary.BloomYShader.setUniformInt1("mipmapLevel", i);
                RenderFullscreenQuad();
            }
            ShaderLibrary.BloomYShader.unuse();
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            //Compute log luminance
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, DownsampleTexture, 0);

            GL.Viewport(0, 0, width, height);
            ShaderLibrary.LogLuminanceShader.use();
            ShaderLibrary.LogLuminanceShader.setUniformFloat1("alpha", 0.015f);
            ShaderLibrary.LogLuminanceShader.bindTexture2D("colorBuffer", 0, ColorBuffers[0]);

            RenderFullscreenQuad();
            ShaderLibrary.LogLuminanceShader.unuse();
            GL.BindTexture(TextureTarget.Texture2D, DownsampleTexture);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DrawBuffer(DrawBufferMode.Back);

            ShaderLibrary.TonemapShader.use();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, ColorBuffers[0]);
            ShaderLibrary.TonemapShader.setUniformInt1("colorBuffer", 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, DownsampleTexture);
            ShaderLibrary.TonemapShader.setUniformInt1("logLuminance", 1);
            RenderFullscreenQuad();
            ShaderLibrary.TonemapShader.unuse();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, FboHandle);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ColorBuffers[0], 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, DepthRenderbuffer);
            GL.DrawBuffer((DrawBufferMode)FramebufferAttachment.ColorAttachment0);
            GL.Enable(EnableCap.DepthTest);
        }
    }
}
