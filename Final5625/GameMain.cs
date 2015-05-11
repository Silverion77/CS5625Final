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
using System.Drawing;
using System.Drawing.Text;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using Chireiden.Meshes;
using Chireiden.Scenes;
using Chireiden.UI;
using Chireiden.Scenes.Stages;

namespace Chireiden
{
    public class GameMain : GameWindow
    {
        World world;

        MouseState previous;

        UtsuhoReiuji okuu;
        TrackingCamera camera;
        Stage stage;

        List<ZombieFairy> zombies;

        Stopwatch stopwatch;

        float aspectRatio;

        bool paused = false;

        public const float RenderDistance = 500;

        // Temp used to retrieve new projectiles from Okuu
        FieryProjectile newProjectile = null;

        TextRenderer okuuHPText;
        TextRenderer fairyHPText;
        TextRenderer winText;
        TextRenderer loseText;

        bool gameCleared = false;

        public GameMain()
            : base(1600, 900,
            new GraphicsMode(), "Subterranean Arsonism", 0,
            DisplayDevice.Default, 4, 5,
            GraphicsContextFlags.ForwardCompatible | GraphicsContextFlags.Debug)
        {
            Framebuffer.Init(Width, Height);
            ParticleSystem.Init();
        }

        public static int ScreenWidth { get; set; }
        public static int ScreenHeight { get; set; }

        string levelFile = "data/stage/testlevel";

        protected override void OnLoad(System.EventArgs e)
        {
            VSync = VSyncMode.Off;

            // Load shaders here
            ShaderLibrary.loadShaders();
            // Load meshes
            MeshLibrary.loadMeshes();

            // Other state
            GL.Enable(EnableCap.DepthTest);
            WindowBorder = WindowBorder.Fixed;

            /*
            MeshNode happyNode = new MeshNode(MeshLibrary.HappySphere, new Vector3(0, 0, 5));
            world.addChild(happyNode);

            var emitter = new ParticleEmitter(new Vector3(-2,10,0), 100.0f);
            world.addChild(emitter);

            var emitter2 = new ParticleEmitter(new Vector3(4, 10, -2), 100.0f);
            world.addChild(emitter2);

            var tile = new SurfaceTile(new Vector3(1,1,1));
            world.addChild(tile);

            var light = new PointLight(new Vector3(0.5f, 1f, 4), 2, 5, new Vector3(1, 1, 1));
            world.addPointLight(light); */

            setUpScreen();

            loadStage(levelFile);

            previous = OpenTK.Input.Mouse.GetState();
            stopwatch = new Stopwatch();
        }

        bool firstTime = true;

        void setUpScreen()
        {
            ScreenWidth = ClientSize.Width;
            ScreenHeight = ClientSize.Height;
            aspectRatio = ClientSize.Width / (float)(ClientSize.Height);

            okuuHPText = new TextRenderer(400, 40, ClientSize.Width, ClientSize.Height);
            fairyHPText = new TextRenderer(400, 40, ClientSize.Width, ClientSize.Height);
            winText = new TextRenderer(600, 300, ClientSize.Width, ClientSize.Height);
            winText.DrawString("You win!", new Font(FontFamily.GenericSansSerif, 72), Brushes.White, PointF.Empty);
            loseText = new TextRenderer(600, 300, ClientSize.Width, ClientSize.Height);
            loseText.DrawString("Press Ctrl-R to try again", new Font(FontFamily.GenericSansSerif, 36), Brushes.White, PointF.Empty);

            Framebuffer.Init(ClientSize.Width, ClientSize.Height);
        }

        protected override void OnResize(EventArgs e)
        {
            if (firstTime)
            {
                firstTime = false;
                return;
            }
            setUpScreen();
            Console.WriteLine("width = {0}, height = {1}", ClientSize.Width, ClientSize.Height);
            TrackingCamera replacement = new TrackingCamera(okuu, (float)Math.PI / 4, aspectRatio, 0.1f, RenderDistance);
            replacement.setStage(stage);
            replacement.copyRotation(camera);
            camera = replacement;
        }

        void loadStage(string stageFile)
        {
            gameCleared = false;
            StageData stageData = StageImporter.importStageFromFile(stageFile);
            world = StageImporter.makeStageWorld(stageData, out stage, out okuu, out zombies);

            camera = new TrackingCamera(okuu, (float)Math.PI / 4, aspectRatio, 0.1f, RenderDistance);
            camera.setStage(stage);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    okuu.inputAttack();
                    break;
                case MouseButton.Right:
                    okuu.inputCannon();
                    break;
                default:
                    break;
            }
        }

        bool fullScreen = false;
        
        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (e.Keyboard.IsKeyDown(Key.AltRight))
                    {
                        if (!fullScreen)
                        {
                            WindowBorder = WindowBorder.Hidden;
                            WindowState = WindowState.Fullscreen;
                            fullScreen = true;
                        }
                        else
                        {
                            WindowBorder = WindowBorder.Fixed;
                            WindowState = WindowState.Normal;
                            fullScreen = false;
                        }
                    }
                    break;
                case Key.R:
                    if (e.Keyboard.IsKeyDown(Key.ControlLeft) || e.Keyboard.IsKeyDown(Key.ControlRight))
                    {
                        loadStage(levelFile);
                    }
                    break;
                case Key.F:
                    camera.toggleCameraFrozen();
                    break;
                case Key.Space:
                    paused = !paused;
                    break;
                case Key.C:
                    okuu.inputCheer();
                    break;
                case Key.Tab:
                    okuu.inputBackstep();
                    break;
                case Key.E:
                    okuu.toggleReadyIdle();
                    break;
                case Key.T:
                    okuu.getHurt(1);
                    break;
                case Key.K:
                    okuu.instantKnockout();
                    break;
                case Key.M:
                    okuu.medicalMiracle();
                    break;
                default:
                    break;
            }
        }

        float updateTime = 0;
        List<FieryProjectile> okuuProjectiles = new List<FieryProjectile>();
        List<ZombieFairy> KOedFairies = new List<ZombieFairy>();
        List<ZombieProjectile> zombieProjectiles = new List<ZombieProjectile>();

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

            camera.addRotation(mouseDX, mouseDY);

            // Because the viewpoint has changed, compute the new frame for the camera
            camera.computeFrame();

            if (paused) return;

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

            bool running = true;

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
            if (keyboard[OpenTK.Input.Key.LShift])
                running = false;

            if (moveX != 0 || moveY != 0)
            {
                Vector3 worldMovementVector = camera.getMovementVector(moveX, moveY);
                okuu.toggleRunWalk(running);
                okuu.inputMove(worldMovementVector);
            }

            okuu.setCameraForward(camera.cameraForwardDir());

            // Give every zombie fairy Okuu's location, so that their AI can use it
            foreach (ZombieFairy fairy in zombies)
            {
                fairy.updateOkuuLocation(okuu.worldPosition);
            }

            ZombieFairy damaged = okuu.checkAttackHit(zombies);
            if (damaged != null) lastDamaged = damaged;

            foreach (ZombieFairy fairy in zombies)
            {
                fairy.checkAttackHit(okuu);
            }

            // Update then adds velocity to position, and also updates modeling transformations.
            world.update(e);

            foreach (ZombieFairy fairy in zombies)
            {
                if (fairy.timeKOed() > 20) KOedFairies.Add(fairy);
            }
            foreach (ZombieFairy fairy in KOedFairies)
            {
                world.removeChild(fairy);
                zombies.Remove(fairy);
            }
            KOedFairies.Clear();

            handleOkuuProjectiles(e);
            handleZombieProjectiles(e);

            handleExplosions();

            if (okuu.getProjectile(out newProjectile))
            {
                newProjectile.setStage(stage);
                world.addChild(newProjectile);
                world.registerPointLight(newProjectile.light);
                okuuProjectiles.Add(newProjectile);
            }

            LightExplosion backstepExplode;
            if (okuu.getExplosion(out backstepExplode))
            {
                backstepExplode.addToWorld(world);
                explosions.Add(backstepExplode);

                foreach (ZombieFairy fairy in zombies)
                {
                    float dist = (fairy.worldPosition - backstepExplode.Location).Length;
                    if (dist < explosionDamageRadius)
                    {
                        fairy.registerHit(OkuuAttackType.Explosion);
                        lastDamaged = fairy;
                    }
                }
            }

            foreach (ZombieFairy fairy in zombies)
            {
                ZombieProjectile fairyProjectile;
                if (fairy.getProjectile(out fairyProjectile))
                {
                    fairyProjectile.setStage(stage);
                    world.addChild(fairyProjectile);
                    world.registerPointLight(fairyProjectile.light);
                    zombieProjectiles.Add(fairyProjectile);
                }
            }

            if (stage.atGoal(okuu.worldPosition) && !gameCleared)
            {
                okuu.winGame();
                foreach (ZombieFairy fairy in zombies) {
                    fairy.endGame();
                }
                gameCleared = true;
            }

            ParticleSystem.Update(e);

            // TODO: probably game logic goes here, e.g. hit detection, damage calculations
            updateTime = (1000.0f * stopwatch.ElapsedTicks) / Stopwatch.Frequency;
        }

        List<FieryProjectile> goneProjs = new List<FieryProjectile>();
        List<LightExplosion> explosions = new List<LightExplosion>();
        List<LightExplosion> endedExplosions = new List<LightExplosion>();

        float explosionDamageRadius = 5;

        void handleOkuuProjectiles(FrameEventArgs e)
        {
            foreach (FieryProjectile projectile in okuuProjectiles)
            {
                projectile.checkTargetHits(zombies);
                if (projectile.hitSomething())
                {
                    goneProjs.Add(projectile);
                }
            }

            foreach (FieryProjectile projectile in goneProjs)
            {
                okuuProjectiles.Remove(projectile);
                world.removeChild(projectile);
                world.unregisterPointLight(projectile.light);
                Vector3 explodeLoc = projectile.worldPosition;
                LightExplosion explode = new LightExplosion(explodeLoc, new Vector3(1, 0.5f, 0.2f), 2, 10000f, 5);
                explode.addToWorld(world);
                explosions.Add(explode);

                foreach (ZombieFairy fairy in zombies) {
                    float dist = (fairy.worldPosition - explodeLoc).Length;
                    if (dist < explosionDamageRadius)
                    {
                        fairy.registerHit(OkuuAttackType.Explosion);
                        lastDamaged = fairy;
                    }
                }
            }
            goneProjs.Clear();
        }

        void handleZombieProjectiles(FrameEventArgs e)
        {
            foreach (ZombieProjectile projectile in zombieProjectiles)
            {
                projectile.checkTargetHit(okuu);
                if (projectile.hitSomething())
                {
                    goneProjs.Add(projectile);
                }
            }

            foreach (ZombieProjectile projectile in goneProjs)
            {
                zombieProjectiles.Remove(projectile);
                world.removeChild(projectile);
                world.unregisterPointLight(projectile.light);
                Vector3 explodeLoc = projectile.worldPosition;
                LightExplosion explode = new LightExplosion(explodeLoc, new Vector3(0.2f, 0.5f, 1f), 2, 500f, 4);
                explode.addToWorld(world);
                explosions.Add(explode);

                float dist = (okuu.worldPosition - explodeLoc).Length;
                if (dist < explosionDamageRadius)
                    okuu.getHurt(10);
            }
            goneProjs.Clear();
        }

        void handleExplosions()
        {
            foreach (LightExplosion explode in explosions)
            {
                if (explode.isOver())
                    endedExplosions.Add(explode);
            }
            foreach (LightExplosion ended in endedExplosions)
            {
                ended.removeFromWorld(world);
                explosions.Remove(ended);
            }
            endedExplosions.Clear();
        }

        Font font = new Font(FontFamily.GenericSansSerif, 24);

        ZombieFairy lastDamaged = null;

        float renderTime = 0;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            stopwatch.Restart();

            GL.Viewport(0, 0, Width, Height);
            GL.ClearColor(System.Drawing.Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            camera.transformPointLights(world.getPointLights());

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            world.render(camera);

            Framebuffer.StartTransparency();
            ParticleSystem.Render(camera);
            Framebuffer.EndTransparency();

            string okuuHPstring = "Okuu's HP: " + okuu.HitPoints + " / 100";
            okuuHPText.DrawString(okuuHPstring, font, Brushes.White, PointF.Empty);
            okuuHPText.drawTextAtLoc(10, 10);

            if (lastDamaged != null)
            {
                string fairyHPstring = "Zombie fairy's HP: " + lastDamaged.HitPoints + " / 10";
                fairyHPText.DrawString(fairyHPstring, font, Brushes.White, PointF.Empty);
                fairyHPText.drawTextAtLoc(ScreenWidth - 400, ScreenHeight - 50);
            }

            if (gameCleared)
            {
                winText.drawTextAtLoc(ScreenWidth / 2 - 200, ScreenHeight / 2 - 36);
            }
            else if (okuu.isKOed())
            {
                loseText.drawTextAtLoc(ScreenWidth / 2 - 270, ScreenHeight / 2 - 18);
            }

            Framebuffer.BlitToScreen();

            SwapBuffers();

            renderTime = (1000.0f * stopwatch.ElapsedTicks) / Stopwatch.Frequency;
            //Console.Write("Update time: {0,2:F1} ms      Render time: {1,2:F1} ms   \r", updateTime, renderTime);
        }

        [STAThread]
        public static void Main()
        {
            using (GameMain example = new GameMain())
            {
                example.Run(60);
            }
        }
    }
}