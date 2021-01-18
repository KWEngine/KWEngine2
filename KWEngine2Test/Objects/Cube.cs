using KWEngine2.GameObjects;
using KWEngine2.Helper;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects
{
    class Cube : GameObject
    {
        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
            
            AddRotationY(1 * deltaTimeFactor, true);

        }
    }
}
