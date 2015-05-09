using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using Chireiden;
using Chireiden.Scenes;
using Chireiden.Scenes.Stages;
using Chireiden.UI;

namespace SALevelEditor
{

    public enum ObjectType
    {
        Material,
        Okuu,
        ZombieFairy,
        Finish,
        None
    }

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

        ObjectType objectMode;
        int materialID;
        TextRenderer statusText;
        Font font = new Font(FontFamily.GenericSansSerif, 24);

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
            light = new PointLight(new Vector3(0, 0, 4), 2, 5, new Vector3(1, 1, 1));
            camTarget = new Empty(new Vector3(5, 5, 0));

            world.addChild(camTarget);
            camTarget.addChild(light);

            world.registerPointLight(light);

            MeshNode mn = new MeshNode(Chireiden.MeshLibrary.HappySphere, new Vector3(0, 0, 3));
            camTarget.addChild(mn);

            trackingCamera = new TrackingCamera(camTarget, (float)Math.PI / 4, aspectRatio, 0.1f, 100);

            objectMode = ObjectType.Material;
            materialID = 1;
            Console.WriteLine("Text is {0} {1}", (int)ClientSize.Width, 100);
            statusText = new TextRenderer((int)ClientSize.Width, 100, ClientSize.Width, ClientSize.Height);
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

                int mat;
                if (e.Button == MouseButton.Right) mat = 0;
                else mat = materialID;

                levelEditor.makeEdit(clicked.X, clicked.Y, objectMode, mat);
            }
        }

        void switchMaterial(Key key)
        {
            switch (key)
            {
                case OpenTK.Input.Key.Number1:
                    materialID = 1;
                    break;
                case OpenTK.Input.Key.Number2:
                    materialID = 2;
                    break;
                case OpenTK.Input.Key.Number3:
                    materialID = 3;
                    break;
                case OpenTK.Input.Key.Number4:
                    materialID = 4;
                    break;
                case OpenTK.Input.Key.Number5:
                    materialID = 5;
                    break;
                case OpenTK.Input.Key.Number6:
                    materialID = 6;
                    break;
                case OpenTK.Input.Key.Number7:
                    materialID = 7;
                    break;
                case OpenTK.Input.Key.Number8:
                    materialID = 8;
                    break;
                case OpenTK.Input.Key.Number9:
                    materialID = 9;
                    break;
                case OpenTK.Input.Key.Number0:
                    materialID = 10;
                    break;
            }
        }

        void export()
        {
            string path;
            System.Windows.Forms.SaveFileDialog file = new System.Windows.Forms.SaveFileDialog();
            if (file.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                path = file.FileName;
                Console.WriteLine("export to {0}", path);
            }
            else
            {
                Console.WriteLine("cancelled");
                return;
            }
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            var key = e.Key;
            if (key == OpenTK.Input.Key.Tab) {
                switchModes();
            }
            else if (viewingMap)
            {
                switch (key)
                {
                    case OpenTK.Input.Key.M:
                        objectMode = ObjectType.Material;
                        break;
                    case OpenTK.Input.Key.O:
                        objectMode = ObjectType.Okuu;
                        break;
                    case OpenTK.Input.Key.Z:
                        objectMode = ObjectType.ZombieFairy;
                        break;
                    case OpenTK.Input.Key.F:
                        objectMode = ObjectType.Finish;
                        break;
                    case OpenTK.Input.Key.E:
                        export();
                        break;
                    default:
                        if (objectMode == ObjectType.Material) switchMaterial(key);
                        break;
                }
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
                camTarget.setStage(stage);
            }
            else
            {
                camTarget.setStage(null);
            }
            // Swap modes
            viewingMap = !viewingMap;
        }

        string statusString()
        {
            string placing;
            switch (objectMode)
            {
                case ObjectType.Material:
                    placing = "material " + materialID;
                    break;
                case ObjectType.Okuu:
                    placing = "Okuu start position";
                    break;
                case ObjectType.ZombieFairy:
                    placing = "zombie fairy";
                    break;
                case ObjectType.Finish:
                    placing = "finish line";
                    break;
                default:
                    placing = "???";
                    break;
            }
            return "Currently placing: " + placing;
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

            statusText.DrawString(statusString(), font, Brushes.White, PointF.Empty);
            statusText.drawTextAtLoc(10, 10);

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
