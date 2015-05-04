// Credit to the OpenGL 3.0 example in the OpenTK distribution,
// for providing us with a template to start with.

/* COMMENT BLOCK FOR VARIOUS IMPORTANT THINGS
 * 
 * Anything important that we ought to remember about the entire program,
 * I'm going to write here. Feel free to add your own notes as well.
 *
 *      - If you are missing AssImp, go to Tools > [NuGet|Library] Package Manager
 *          > Package Manager Console and type:
 *              Install-Package AssimpNet 
 * 
 *      - We are using OpenGL 3.1.
 *      
 *      - The Z-axis is hereby defined to be up, and the negative Y-axis is
 *          hereby defined to be forward. Why? Because Blender uses that, and
 *          trying to get it to export otherwise is more trouble than it's worth...
 *          
 *      - The order of matrix multiplication in OpenTK isn't what any mathematician
 *          would expect -- Matrix4.Mult(A, B) actually computes C = BA, i.e.
 *          the composition applies A first, instead of B.
 *          
 *      - When exporting from Blender, please DO NOT check "Deform bones only" or 
 *          an equivalent setting -- it will mess up the bone hierarchy for the importer
 */

using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using Chireiden.Meshes;
using Chireiden.Scenes;

namespace Chireiden
{
    public class GameMain : GameWindow
    {

        World world;

        MouseState previous;

        MobileObject camTarget;
        TrackingCamera camera;

        Stopwatch stopwatch;

        bool paused = false;

        public GameMain()
            : base(1200, 900,
            new GraphicsMode(), "The Great Game", 0,
            DisplayDevice.Default, 4, 5,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        {
            Framebuffer.Init(Width, Height);
            ParticleSystem.Init();
        }

        SkeletalMeshNode meshOrig;
        SkeletalMeshNode meshCopy;
        SkeletalMeshNode okuu;

        protected override void OnLoad(System.EventArgs e)
        {
            VSync = VSyncMode.Off;

            // Load shaders here
            ShaderLibrary.loadShaders();
            // Load meshes
            MeshLibrary.loadMeshes();

            // Other state
            GL.Enable(EnableCap.DepthTest);

            // Make the world
            world = new World();

            // For now, our camera is going to focus on an empty, so that we can see things clearly
            Empty empty = new Empty();
            world.addChild(empty);
            camTarget = empty;

            float aspectRatio = ClientSize.Width / (float)(ClientSize.Height);

            meshOrig = new SkeletalMeshNode(MeshLibrary.TextCube);
            world.addChild(meshOrig);

            MeshNode happyNode = new MeshNode(MeshLibrary.HappySphere, new Vector3(0, 0, 5));
            world.addChild(happyNode);

            meshCopy = new SkeletalMeshNode(MeshLibrary.TextCube, new Vector3(4, 0, 0));
            world.addChild(meshCopy);

            okuu = new SkeletalMeshNode(MeshLibrary.Okuu, new Vector3(2, 2, 0));
            world.addChild(okuu);

            var emitter = new ParticleEmitter(new Vector3(-2,10,0), 100.0f);
            world.addChild(emitter);

            var light = new PointLight(new Vector3(0.5f, 1f, 4), 1, 3, new Vector3(1, 1, 1));
            world.addPointLight(light);

            camera = new TrackingCamera(camTarget, (float)Math.PI / 4, aspectRatio, 1, 100);
            previous = OpenTK.Input.Mouse.GetState();
            stopwatch = new Stopwatch();

            meshOrig.switchAnimation("twist");
            meshCopy.switchAnimation("gyrate");
            okuu.switchAnimation("cheer");
        }
        
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F:
                    camera.toggleCameraFrozen();
                    break;
                case Key.Space:
                    paused = !paused;
                    break;
                default:
                    break;
            }
        }

        float updateTime = 0;

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            stopwatch.Restart();

            // Change camera's orientation based on mouse movement
            MouseState current = OpenTK.Input.Mouse.GetState();

            float currWheel = current.WheelPrecise;
            float prevWheel = previous.WheelPrecise;
            float wheelDiff = currWheel - prevWheel;

            camera.zoom(-wheelDiff);

            float mouseDX = current.X - previous.X;
            float mouseDY = current.Y - previous.Y;
            previous = current;

            if (paused) return;

            camera.addRotation(mouseDX, mouseDY);

            // Because the viewpoint has changed, compute the new frame for the camera
            camera.computeFrame();

            // VELOCITY MODIFICATIONS HERE

            // Here we should set the velocities of every object.

            // This firstly includes physics objects -- we just modify their velocity
            // by adding gravity, and whatever other damping effects we deem necessary.

            // This also includes characters. Because there's just a simple
            // direction they want to move in, we can just set their velocity to be in
            // that direction, and scale by their move speed.

            // Handle player's movement here
            float moveX = 0;
            float moveY = 0;

            var keyboard = OpenTK.Input.Keyboard.GetState();
            if (keyboard[OpenTK.Input.Key.Escape])
                Exit();
            if (keyboard[OpenTK.Input.Key.W])
                moveY += 1;
            if (keyboard[OpenTK.Input.Key.A])
                moveX -= 1;
            if (keyboard[OpenTK.Input.Key.S])
                moveY -= 1;
            if (keyboard[OpenTK.Input.Key.D])
                moveX += 1;

            if (moveX != 0 || moveY != 0)
            {
                Vector3 worldMovementVector = camera.getMovementVector(moveX, moveY);
                camTarget.setVelocity(worldMovementVector);
            }
            else
            {
                camTarget.setVelocity(Vector3.Zero);
            }

            meshCopy.advanceAnimation(e.Time);
            meshOrig.advanceAnimation(e.Time);
            okuu.advanceAnimation(e.Time);

            // TODO: handle enemy movement here, once enemies are implemented

            // Update then adds velocity to position, and also updates modeling transformations.
            world.update(e);

            ParticleSystem.Update(e);

            // TODO: probably game logic goes here, e.g. hit detection, damage calculations
            updateTime = stopwatch.ElapsedMilliseconds;
        }

        float renderTime = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            stopwatch.Restart();
            if (paused) return;

            GL.Viewport(0, 0, Width, Height);
            GL.ClearColor(System.Drawing.Color.MidnightBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            camera.transformPointLights(world.getPointLights());

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            world.render(camera);

            Framebuffer.StartTransparency();
            ParticleSystem.Render(camera);
            Framebuffer.EndTransparency();

            Framebuffer.BlitToScreen();

            SwapBuffers();

            renderTime = stopwatch.ElapsedMilliseconds;
            Console.Write("Update time: {0} ms               Render time: {1} ms   \r", updateTime, renderTime);
        }

        [STAThread]
        public static void Main()
        {
            using (GameMain example = new GameMain())
            {
                example.Run(30);
            }
        }
    }
}