using System;
using System.Collections.Generic;

using OpenTK;

namespace Chireiden.Meshes.Animations
{
    public class BlendShape
    {
        int id;
        string name;
        Vector3[] displacements;

        public string ID { get { return ID; } }
        public string Name { get { return Name; } }
        public Vector3[] Displacements { get { return displacements; } }

        public BlendShape(int id, string name, Vector3[] displacements)
        {
            this.id = id;
            this.name = name;
            this.displacements = displacements;
        }
    }
}
