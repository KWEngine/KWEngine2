using KWEngine2.GameObjects;
using KWEngine2.Helper;
using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test.Objects.TestAll
{
    class PointLight : LightObject
    {
        float currentIntensity = 0;
        bool up = true;
        float degrees = 0;
        public override void Act(KeyboardState ks, MouseState ms, float deltaTimeFactor)
        {
            //Vector3 newPos = HelperRotation.CalculateRotationAroundPointOnAxis(new OpenTK.Vector3(0, 2, 0), 2, degrees);
            //degrees = (degrees + 0.5f) % 360;
            //SetPosition(newPos);

            if (up)
            {
                currentIntensity += 0.01f;
                if(currentIntensity >= 1.5f)
                {
                    up = false;
                }
            }
            else
            {
                currentIntensity -= 0.01f;
                if (currentIntensity <= 0.5f)
                    up = true;
            }
            SetColor(Color.X, Color.Y, Color.Z, currentIntensity);
        }
    }
}
