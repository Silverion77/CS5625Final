﻿using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    public class PointLight : PlaceableObject
    {
        /// <summary>
        /// The brightness of the light.
        /// </summary>
        public float Energy { get; set; }
        /// <summary>
        /// The distance at which the light source's perceived intensity is half as much.
        /// </summary>
        public float FalloffDistance { get; set; }
        /// <summary>
        /// The color of the light.
        /// </summary>
        public Vector3 Color { get; set; }

        public PointLight()
            : base()
        {
            Energy = 1;
            FalloffDistance = 5;
            Color = new Vector3(1, 1, 1);
        }

        public PointLight(Vector3 loc)
            : base(loc)
        {
            Energy = 1;
            FalloffDistance = 5;
            Color = new Vector3(1, 1, 1);
        }

        public PointLight(Vector3 loc, float intensity, float falloff)
            : base(loc)
        {
            Energy = intensity;
            FalloffDistance = falloff;
            Color = new Vector3(1, 1, 1);
        }

        public PointLight(Vector3 loc, float intensity, float falloff, Vector3 color)
            : base(loc)
        {
            Energy = intensity;
            FalloffDistance = falloff;
            Color = color;
        }

        public override void render(Camera camera)
        {
            renderChildren(camera);
        }
    }
}