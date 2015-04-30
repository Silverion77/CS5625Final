using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden
{
    class Empty : MobileObject
    {
        public Empty() : base() { }

        public override void render(Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            foreach (SceneTreeNode c in children)
            {
                c.render(viewMatrix, projectionMatrix);
            }
        }
    }
}
