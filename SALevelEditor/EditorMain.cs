using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using Chireiden;
using Chireiden.Scenes;
using Chireiden.Scenes.Stages;

namespace SALevelEditor
{
    class EditorMain : GameWindow
    {
        public EditorMain()
            : base(1600, 900,
            new GraphicsMode(), "Level Editor", 0,
            DisplayDevice.Default, 4, 5,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        {

        }

        OrthoCamera mapCamera;
        LevelEditor levelEditor;
        MouseState previous;

        TrackingCamera trackingCamera;
        World world;
        Empty camTarget;
        PointLight light;
        Stage stage;

        bool viewingMap = true;

        protected override void OnLoad(EventArgs e)
        {
            VSync = VSyncMode.Off;
            ShaderLibrary.LoadShaders();
            Chireiden.ShaderLibrary.loadShaders();
            Chireiden.MeshLibrary.loadMeshes();
            levelEditor = new LevelEditor();
            float aspectRatio = (float)ClientSize.Width / (float)ClientSize.Height;
            Console.WriteLine("Aspect ratio = {0}", aspectRatio);
            mapCamera = new OrthoCamera(new Vector3(0.5f, 0.5f, 1), aspectRatio, 0.1f, 100);
            previous = OpenTK.Input.Mouse.GetState();

            world = new World();
            light = new PointLight(new Vector3(0.5f, 0.5f, 5), 2, 5, new Vector3(1, 1, 1));
            camTarget = new Empty();

            world.addChild(camTarget);
            camTarget.addChild(light);

            world.registerPointLight(light);

            SkeletalMeshNode cube = new SkeletalMeshNode(MeshLibrary.TextCube, new Vector3(0.5f, 0.5f, 0));
            MeshNode happyNode = new MeshNode(MeshLibrary.HappySphere, new Vector3(0.5f, 0.5f, 5));
            world.addChild(happyNode);

            world.addChild(cube);

            trackingCamera = new TrackingCamera(camTarget, (float)Math.PI / 4, aspectRatio, 0.1f, 100);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (viewingMap)
            {
                double normalizedX = (double)e.X / ClientSize.Width;
                double normalizedY = 1 - (double)e.Y / ClientSize.Height;
                normalizedX = 2 * normalizedX - 1;
                normalizedY = 2 * normalizedY - 1;
                Vector3 clicked = mapCamera.getClickedLoc(normalizedX, normalizedY);
                int tileX = (int)Math.Floor(clicked.X);
                int tileY = (int)Math.Floor(clicked.Y);
                Console.WriteLine("Clicked square ({0}, {1})", tileX, tileY);
                int newValue;
                if (e.Button == MouseButton.Left)
                {
                    newValue = 1;
                }
                else
                {
                    newValue = 0;
                }
                levelEditor.editSquare(tileX, tileY, newValue);
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            var key = e.Key;
            if (key == OpenTK.Input.Key.Tab)
            {
                switchModes();
            }
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Change camera's orientation based on mouse movement
            MouseState current = OpenTK.Input.Mouse.GetState();

            float currWheel = current.WheelPrecise;
            float prevWheel = previous.WheelPrecise;
            float wheelDiff = currWheel - prevWheel;

            float mouseDX = current.X - previous.X;
            float mouseDY = current.Y - previous.Y;

            previous = current;

            var keyboard = OpenTK.Input.Keyboard.GetState();
            float moveX = 0;
            float moveY = 0;
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

            if (viewingMap)
            {
                mapCamera.elevate(wheelDiff, e.Time);
                mapCamera.move(moveX, moveY, e.Time);
                mapCamera.computeFrame();
            }
            else
            {
                Vector3 worldMovementVector = trackingCamera.getMovementVector(moveX, moveY);
                camTarget.setVelocity(worldMovementVector);

                trackingCamera.zoom(-wheelDiff);
                trackingCamera.addRotation(mouseDX, mouseDY);
                trackingCamera.computeFrame();
                world.update(e);
            }
        }

        void switchModes()
        {
            if (viewingMap)
            {
                // If we were previously editing the map, we now need to construct the stage
                // Delete the old version, since it may have changed
                world.removeChild(stage);
                stage = levelEditor.constructStage();
                world.addChild(stage);
            }
            // Swap modes
            viewingMap = !viewingMap;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            GL.ClearColor(System.Drawing.Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            if (viewingMap)
            {
                levelEditor.render(mapCamera);
            }
            else
            {
                trackingCamera.transformPointLights(world.getPointLights());
                world.render(trackingCamera);
            }

            SwapBuffers();
        }


        [STAThread]
        public static void Main()
        {
            Console.WriteLine("Hello world");
            
            using (EditorMain example = new EditorMain())
            {
                example.Run(30);
            }
        }
    }
}
