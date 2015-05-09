using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    public class Empty : MobileObject
    {
        public Empty() : base() { }

        public Empty(Vector3 loc) : base(loc) { }

        public override void render(Camera camera)
        {
            renderChildren(camera);
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            base.update(e, parentToWorldMatrix);
        }
    }
}
