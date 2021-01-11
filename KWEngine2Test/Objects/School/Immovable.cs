using KWEngine2.GameObjects;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.School
{
    class Immovable : GameObject
    {

        public bool IsStair { get; set; } = false;

        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {

        }
    
    }
}
