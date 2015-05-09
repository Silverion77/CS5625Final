using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chireiden.Materials
{
    class NullMaterial : Material
    {
        public NullMaterial() { }

        public int useMaterialParameters(ShaderProgram program, int startTexUnit)
        {
            return startTexUnit;
        }

        public void unuseMaterialParameters(ShaderProgram program, int startTexUnit) { }
    }
}
