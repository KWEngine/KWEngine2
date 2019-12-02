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
            SetCameraPosition(0, 100, 100);
            SetCameraTarget(0, 0, 0);

            LoadModelFromFile("rect", @".\Models\Schoolpart\building.fbx");



            Building go = new Building();
            go.SetModel(GetModel("rect"));
            AddGameObject(go);
        }
    }
}
