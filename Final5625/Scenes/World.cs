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
    class World : SceneTreeNode
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

        public override void render(Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            renderChildren(viewMatrix, projectionMatrix);
        }

        public void registerPointLight(PointLight p)
        {
            pointLights.Add(p);
        }

    }
}
