﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Chireiden.Meshes;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden
{
    /// <summary>
    /// All particle effects in the game will be managed by a single particle system. While
    /// this may not be ideal in a theoretical view, it allows us to considerably improve 
    /// performance and simplify the code.
    /// 
    /// The techniques applied here are based off of those presented in the GDC 2014 talk
    /// Approaching Zero Driver Overhead in OpenGL (Presented by NVIDIA). Specifically, we
    /// use a presistant coherent memory mapped ring buffer to 
    /// 
    /// In GPU memory, the layout for particle data is:
    /// 
    ///    3 x float: Position
    ///    1 x float: Rotation
    ///    1 x float: Radius
    ///    1 x int16: Reaction Coordinate ("time") 
    ///    1 x int8: Texture Layer
    ///    9 x int8: Padding
    ///
    /// </summary>
    class ParticleSystem
    {
        public class Particle
        {
            public Vector3 position;
            public Vector3 velocity;

            public float rotation;
            public float angularVelocity;

            public float radius;

            public float life;
            public float invTotalLife;

            public byte texture;

            public float gravity;
        }

        const int MAX_PARTICLES = 100000;
        const int PARTICLE_DATA_SIZE = 32;

        static int VBO;
        static int VAO;

        static IntPtr VideoMemoryIntPtr;
        static uint ringbufferHead = 0;

        static IntPtr[] Fences = new IntPtr[3];

        static LinkedList<Particle> particles = new LinkedList<Particle>();

        public static void Init()
        {
            BufferStorageFlags createFlags = BufferStorageFlags.MapWriteBit
                                              | BufferStorageFlags.MapPersistentBit
                                              | BufferStorageFlags.MapCoherentBit
                                              | BufferStorageFlags.DynamicStorageBit;

            BufferAccessMask mapFlags = BufferAccessMask.MapWriteBit
                                         | BufferAccessMask.MapPersistentBit
                                         | BufferAccessMask.MapCoherentBit;

            IntPtr length = (IntPtr)(3 * MAX_PARTICLES * PARTICLE_DATA_SIZE);

            GL.GenBuffers(1, out VBO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferStorage(BufferTarget.ArrayBuffer, length, IntPtr.Zero, createFlags);
            VideoMemoryIntPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, length, mapFlags);

            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO); 
            //position
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, PARTICLE_DATA_SIZE, 0);
            //rotation
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, PARTICLE_DATA_SIZE, 12);
            //radius
            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, PARTICLE_DATA_SIZE, 16);
            //reaction coordinate
            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.UnsignedShort, true, PARTICLE_DATA_SIZE, 20);
            //texture layer
            GL.EnableVertexAttribArray(4);
            GL.VertexAttribPointer(4, 1, VertexAttribPointerType.UnsignedByte, false, PARTICLE_DATA_SIZE, 22);
            GL.BindVertexArray(0);

            Fences[0] = Fences[1] = Fences[2] = IntPtr.Zero;
        }

        public static void SpawnParticle(Particle particle)
        {
            if(particles.Count < MAX_PARTICLES)
                particles.AddFirst(new LinkedListNode<Particle>(particle));
        }

        public static void Update(FrameEventArgs e)
        {
            float dt = (float)e.Time;
            LinkedListNode<Particle> p = particles.First;
            while(p != null)
            {
                p.Value.velocity.Z -= p.Value.gravity * dt;
                p.Value.position += p.Value.velocity * dt;
                p.Value.rotation += p.Value.angularVelocity * dt;
                p.Value.life -= dt;

                var nextNode = p.Next; 
                if (p.Value.life <= 0.0f)
                {
                    particles.Remove(p);
                }
                p = nextNode;
            }
        }

        public static void Render(Camera camera)
        {
            if (Fences[ringbufferHead] != IntPtr.Zero)
            {
                // If we have previously written to this same location in the ring buffer, then make sure that 
                // the associated render has completed. This loop will cause execution to block until it does.
                // Because we are using triple buffering, this should not happen very often (ever?)
                WaitSyncStatus status = GL.ClientWaitSync(Fences[ringbufferHead], ClientWaitSyncFlags.SyncFlushCommandsBit, 100000);
                while(status == WaitSyncStatus.TimeoutExpired){
                    System.Console.WriteLine("WARNING: waiting on fence sync object in ParticleSystem...");
                    status = GL.ClientWaitSync(Fences[ringbufferHead], ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000);
                }
            }

            unsafe
            {
                byte* ptr = (byte*)VideoMemoryIntPtr.ToPointer() + ringbufferHead * PARTICLE_DATA_SIZE * MAX_PARTICLES;
                int i = 0;
                foreach(Particle p in particles)
                {
                    *(float*)(ptr + 32 * i + 0) = p.position.X;
                    *(float*)(ptr + 32 * i + 4) = p.position.Y;
                    *(float*)(ptr + 32 * i + 8) = p.position.Z;
                    *(float*)(ptr + 32 * i + 12) = p.rotation;
                    *(float*)(ptr + 32 * i + 16) = p.radius;
                    *(ushort*)(ptr + 32 * i + 20) = (ushort)(1.0f - p.life * p.invTotalLife * ushort.MaxValue);
                    *(byte*)(ptr + 32 * i + 22) = p.texture;
                    i++;
                }
            }

            var viewProjectionMatrix = camera.getViewMatrix() * camera.getProjectionMatrix();
            var invViewProjectionMatrix = Matrix4.Invert(viewProjectionMatrix);
            Vector4 up4 = Vector4.Transform(new Vector4(0,1,0,0), invViewProjectionMatrix);
            Vector4 right4 = Vector4.Transform(new Vector4(1,0,0,0), invViewProjectionMatrix);
            Vector3 up = new Vector3(up4.X, up4.Y, up4.Z);
            Vector3 right = new Vector3(right4.X, right4.Y, right4.Z);

            GL.DepthMask(false);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
            Shaders.ParticleShader.use();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TextureManager.getTexture("data/texture/particle/reaction.png").getTextureID());
            Shaders.ParticleShader.setUniformInt1("reaction", 0);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, TextureManager.getTexture("data/texture/particle/0.png").getTextureID());
            Shaders.ParticleShader.setUniformInt1("tex", 1);

            Shaders.ParticleShader.setUniformMatrix4("viewProjectionMatrix", viewProjectionMatrix);
            Shaders.ParticleShader.setUniformFloat3("up", up / up.Length);
            Shaders.ParticleShader.setUniformFloat3("right", right / right.Length); 
            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Points, (int)ringbufferHead * MAX_PARTICLES, particles.Count());
            GL.BindVertexArray(0);
            Shaders.ParticleShader.unuse();
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.DepthMask(true);

            Fences[ringbufferHead] = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            ringbufferHead = (ringbufferHead + 1) % 3;
        }
    }
}