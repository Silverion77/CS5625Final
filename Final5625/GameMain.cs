﻿// Credit to the OpenGL 3.0 example in the OpenTK distribution,
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

        public const float RenderDistance = 100;

        // Temp used to retrieve new projectiles from Okuu
        FieryProjectile newProjectile = null;

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

            ScreenWidth = ClientSize.Width;
            ScreenHeight = ClientSize.Height;
            aspectRatio = ClientSize.Width / (float)(ClientSize.Height);

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

            loadStage("data/stage/testlevel");

            previous = OpenTK.Input.Mouse.GetState();
            stopwatch = new Stopwatch();

            Framebuffer.InitShadowMaps(world.getPointLights().Count, 1024, 1024);

        }

        void loadStage(string stageFile)
        {
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
                case Key.C:
                    okuu.inputCheer();
                    break;
                case Key.Tab:
                    okuu.inputBackstep();
                    break;
                case Key.R:
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

            okuu.checkAttackHit(zombies);

            foreach (ZombieFairy fairy in zombies)
            {
                fairy.checkAttackHit(okuu);
            }

            // TODO: handle enemy movement here, once enemies are implemented

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

            if (okuu.getProjectile(out newProjectile))
            {
                newProjectile.setStage(stage);
                world.addChild(newProjectile);
                world.registerPointLight(newProjectile.light);
                okuuProjectiles.Add(newProjectile);
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

            ParticleSystem.Update(e);

            // TODO: probably game logic goes here, e.g. hit detection, damage calculations
            updateTime = (1000.0f * stopwatch.ElapsedTicks) / Stopwatch.Frequency;
        }

        List<FieryProjectile> goneProjs = new List<FieryProjectile>();

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
                Console.WriteLine("TODO: Explode");
                okuuProjectiles.Remove(projectile);
                world.removeChild(projectile);
                world.unregisterPointLight(projectile.light);
                // TODO: create an explosion
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
                Console.WriteLine("TODO: Explode");
                zombieProjectiles.Remove(projectile);
                world.removeChild(projectile);
                world.unregisterPointLight(projectile.light);
                // TODO: create an explosion
            }
            goneProjs.Clear();
        }

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

            // render shadow cube maps for each light
            /*
            for (int j = 0; j < world.getPointLights().Count; j++)
            {
                Vector3[] dirs = new Vector3[] { new Vector3(1,0,0), new Vector3(-1,0,0), 
                                                 new Vector3(0,1,0), new Vector3(0,-1,0),
                                                 new Vector3(0,0,1), new Vector3(0,0,-1)};
                for (int i = 0; i < 6; i++)
                {
                    Framebuffer.StartShadowMap(j, i, 1024, 1024);
                    world.getPointLights()[j].setupCamera(dirs[i]);
                    world.render(world.getPointLights()[j].getCamera());
                }
            }
            Framebuffer.EndShadowMaps();
            */
              
            world.render(camera);

            Framebuffer.StartTransparency();
            ParticleSystem.Render(camera);
            Framebuffer.EndTransparency();

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
                example.Run(30);
            }
        }
    }
}