﻿using KWEngine2;
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
    class Sphere : GameObject
    {
        public override void Act(KeyboardState ks, MouseState ms)
        {
            AddRotationY(1 * KWEngine.DeltaTimeFactor, true);
        }
    }
}
