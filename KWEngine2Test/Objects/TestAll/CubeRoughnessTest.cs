using KWEngine2.GameObjects;
using KWEngine2.Helper;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.TestAll
{
    class CubeRoughnessTest : GameObject
    {
        private float r = 1;

        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
            if (ks[Key.Left] && r > 0)
            {
                r = r - 0.01f * deltaTimeFactor;
                SetRoughness(r);
                Console.WriteLine("Roughness: " + r);
            }
            else if (ks[Key.Right] && r < 1)
            {
                r = r + 0.01f * deltaTimeFactor;
                SetRoughness(r);
                Console.WriteLine("Roughness: " + r);
            }

        }
    }
}
