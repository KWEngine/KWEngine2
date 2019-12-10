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
            SetCameraPosition(0,15,15);
            SetCameraTarget(0, 0, 0);

            LoadModelFromFile("rect", @".\Models\cubetest2.fbx");

            Building go = new Building();
            go.SetModel(GetModel("KWCube"));
            go.Scale = new OpenTK.Vector3(1,1,1);
            go.AddRotationY(0);
            go.IsCollisionObject = true;
            AddGameObject(go);

            Block block = new Block();
            block.SetModel(GetModel("KWCube6"));
            block.SetPosition(5, 0, 3);
            block.SetScale(0.5f);
            block.IsCollisionObject = true;
            AddGameObject(block);
            block.SetTexture(".\\textures\\holland.jpg", KWEngine.CubeSide.All, KWEngine.TextureType.Diffuse);
        }
    }
}
