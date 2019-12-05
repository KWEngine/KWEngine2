﻿using KWEngine2;
using KWEngine2.GameObjects;
using KWEngine2Test.GameObjects;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2Test
{
    class GameWorld : World
    {
        public override void Act(KeyboardState kbs, MouseState ms)
        {
            
        }

        public override void Prepare()
        {
            FOV = 90;
            SetCameraPosition(10, 10, 10);
            SetCameraTarget(0, 0, 0);

            LoadModelFromFile("rect", @".\Models\Schoolpart\cubetest.fbx");



            Building go = new Building();
            go.SetModel(GetModel("rect"));
            go.Scale = new OpenTK.Vector3(1,1,1);
            AddGameObject(go);
        }
    }
}
