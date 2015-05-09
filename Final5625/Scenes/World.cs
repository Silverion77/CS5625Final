using System;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    public class World : SceneTreeNode
    {
        List<PointLight> pointLights;

        public World() : base()
        {
            pointLights = new List<PointLight>();
        }

        public void update(FrameEventArgs e)
        {
            updateChildren(e, Matrix4.Identity);
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            update(e);
        }

        public override void render(Camera camera)
        {
            renderChildren(camera);
        }

        public void addPointLight(PointLight p)
        {
            pointLights.Add(p);
            addChild(p);
        }

        public void registerPointLight(PointLight p)
        {
            pointLights.Add(p);
        }

        public void removePointLight(PointLight p)
        {
            pointLights.Remove(p);
            removeChild(p);
        }

        public void unregisterPointLight(PointLight p)
        {
            pointLights.Remove(p);
        }

        public List<PointLight> getPointLights()
        {
            return pointLights;
        }

    }
}
