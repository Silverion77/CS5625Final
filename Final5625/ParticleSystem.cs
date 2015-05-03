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
    ///    1 x int8: Reaction Coordinate ("time") 
    ///    1 x int8: Random Seed
    ///    1 x int8: Texture Layer
    ///    9 x int8: Padding
    ///
    /// </summary>
    class ParticleSystem
    {
        public struct Particle
        {
            public Vector3 position;
            public Vector3 velocity;

            public float radius;
            public float life;

            public float gravity;
        }

        const int MAX_PARTICLES = 100000;
        const int PARTICLE_DATA_SIZE = 32;

        static int VBO;
        static int VAO;

        static IntPtr VideoMemoryIntPtr;
        static uint ringbufferHead = 0;

        static IntPtr[] Fences = new IntPtr[3];

        static List<Particle> particles = new List<Particle>();

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
            GL.EnableVertexAttribArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, PARTICLE_DATA_SIZE, 0);
            GL.BindVertexArray(0);

            Fences[0] = Fences[1] = Fences[2] = IntPtr.Zero;
        }

        public static void SpawnParticle(Particle particle)
        {
            if(particles.Count < MAX_PARTICLES)
                particles.Add(particle);
        }

        public static void Update(FrameEventArgs e)
        {
            float dt = (float)e.Time;
            for (int i = 0; i < particles.Count(); i++)
            {
                Particle p = particles[i];
                p.velocity.Z -= p.gravity * dt;
                p.position += p.velocity * dt;
                p.life -= dt;

                if (p.life <= 0.0f)
                {
                    particles.RemoveAt(i);
                    --i;
                }
                else
                {
                    particles[i] = p;
                }
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
                for (int i = 0; i < particles.Count(); i++)
                {
                    *(float*)(ptr + 32 * i + 0) = particles[i].position.X;
                    *(float*)(ptr + 32 * i + 4) = particles[i].position.Y;
                    *(float*)(ptr + 32 * i + 8) = particles[i].position.Z;
                }
            }
            GL.Enable(EnableCap.ProgramPointSize);
            Shaders.ParticleShader.use();
            Shaders.ParticleShader.setUniformMatrix4("viewProjectionMatrix", camera.getViewMatrix() * camera.getProjectionMatrix());
            GL.BindVertexArray(VAO);
            GL.DrawArrays(PrimitiveType.Points, (int)ringbufferHead * MAX_PARTICLES, particles.Count());
            GL.BindVertexArray(0);
            Shaders.ParticleShader.unuse();

            Fences[ringbufferHead] = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            ringbufferHead = (ringbufferHead + 1) % 3;
        }
    }
}
