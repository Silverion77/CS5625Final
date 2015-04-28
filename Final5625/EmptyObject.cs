using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
namespace Chireiden
{
    class EmptyCamTarget : MobileObject
    {
        public EmptyCamTarget() : base() { }

        public override void render(OpenTK.Matrix4 viewMatrix, OpenTK.Matrix4 projectionMatrix)
        {
            foreach (SceneTreeNode c in children)
            {
                c.render(viewMatrix, projectionMatrix);
            }
        }
    }
}
