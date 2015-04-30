using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    class Empty : MobileObject
    {
        public Empty() : base() { }

        public Empty(Vector3 loc) : base(loc) { }

        public override void render(Matrix4 viewMatrix, Matrix4 projectionMatrix)
        {
            renderChildren(viewMatrix, projectionMatrix);
        }
    }
}
