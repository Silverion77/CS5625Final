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

        TransformableObject camTarget;
        TrackingCamera camera;

        public GameMain()
            : base(1200, 900,
            new GraphicsMode(), "OpenGL 3 Example", 0,
            DisplayDevice.Default, 3, 2,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        {}

        World createWorld()
        {
            World w = new World();
            Cube cube1 = new Cube();
            w.addChild(cube1);
            EmptyObject empty = new EmptyObject();
            w.addChild(empty);
            camTarget = empty;
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
            // Matrix4 rotation = Matrix4.CreateRotationY((float)e.Time);
            // TODO: rotate the cube
            world.update(e);

            MouseState current = OpenTK.Input.Mouse.GetState();
            float mouseDX = current.X - previous.X;
            float mouseDY = current.Y - previous.Y;
            camera.AddRotation(-mouseDX, -mouseDY);
            previous = current;

            var keyboard = OpenTK.Input.Keyboard.GetState();
            if (keyboard[OpenTK.Input.Key.Escape])
                Exit();
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