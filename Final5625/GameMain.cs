// Credit to the OpenGL 3.0 example in the OpenTK distribution,
// for providing us with a template to start with.

/* COMMENT BLOCK FOR VARIOUS IMPORTANT THINGS
 * 
 * Anything important that we ought to remember about the entire program,
 * I'm going to write here. Feel free to add your own notes as well.
 *
 *      - We are using OpenGL 3.1.
 *      - The Y-axis is hereby defined to be up.
 */

using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Chireiden
{
    public class GameMain : GameWindow
    {

        World world;

        Vector3 eyePos = new Vector3(5, 5, 5);
        Vector3 lookAt = new Vector3(0, 0, 0);
        Vector3 up = new Vector3(0, 1, 0);

        MouseState previous;

        MobileObject camTarget;
        TrackingCamera camera;

        public GameMain()
            : base(1200, 900,
            new GraphicsMode(), "The Great Game", 0,
            DisplayDevice.Default, 3, 2,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        {}

        World createWorld()
        {
            World w = new World();
            Cube cube1 = new Cube();
            w.addChild(cube1);
            Cube cube2 = new Cube(new Vector3(2,1,0));
            w.addChild(cube2);
            camTarget = cube2;
            return w;
        }

        protected override void OnLoad(System.EventArgs e)
        {
            VSync = VSyncMode.On;

            // Other state
            GL.Enable(EnableCap.DepthTest);
            GL.ClearColor(System.Drawing.Color.MidnightBlue);

            // Make the world
            world = createWorld();
            float aspectRatio = ClientSize.Width / (float)(ClientSize.Height);

            camera = new TrackingCamera(camTarget, (float)Math.PI / 4, aspectRatio, 1, 100);
            previous = OpenTK.Input.Mouse.GetState();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Change camera's orientation based on mouse movement
            MouseState current = OpenTK.Input.Mouse.GetState();

            float currWheel = current.WheelPrecise;
            float prevWheel = previous.WheelPrecise;
            float wheelDiff = currWheel - prevWheel;

            camera.zoom(-wheelDiff);

            float mouseDX = current.X - previous.X;
            float mouseDY = current.Y - previous.Y;
            camera.addRotation(mouseDX, mouseDY);
            previous = current;

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

            // TODO: handle enemy movement here, once enemies are implemented

            // Update then adds velocity to position, and also updates modeling transformations.
            world.update(e);

            // TODO: probably game logic goes here, e.g. hit detection, damage calculations
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            world.render(viewMatrix, projectionMatrix);

            SwapBuffers();
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