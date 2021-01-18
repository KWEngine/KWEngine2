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
    class Sphere : GameObject
    {
        private float metalness = 0;
        private float roughness = 1;

        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
            AddRotationY(0.2f, true);

            if (ks[Key.Q])
            {
                metalness = HelperGL.Clamp(metalness - 0.005f, 0, 1);
                Console.WriteLine("Metalness: "+ metalness);
                SetMetalness(metalness);
            }

            if (ks[Key.E])
            {
                metalness = HelperGL.Clamp(metalness + 0.005f, 0, 1);
                Console.WriteLine("Metalness: " + metalness);
                SetMetalness(metalness);
            }

            if (ks[Key.A])
            {
                roughness = HelperGL.Clamp(roughness - 0.005f, 0, 1);
                Console.WriteLine("Roughness: " + roughness);
                SetRoughness(roughness);
            }

            if (ks[Key.D])
            {
                roughness = HelperGL.Clamp(roughness + 0.005f, 0, 1);
                Console.WriteLine("Roughness: " + roughness);
                SetRoughness(roughness);
            }
        }
    }
}
