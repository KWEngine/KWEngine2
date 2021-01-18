using KWEngine2.GameObjects;
using KWEngine2.Helper;
using System;
using System.Reflection;
using static KWEngine2.GameObjects.GameObject;
using static KWEngine2.KWEngine;

namespace KWEngine2.Model
{
    /*
    internal class GeoModelCube
    {

        public float SpecularPower = 0;
        public float SpecularArea = 512;

        public GeoTexture GeoTextureFront = new GeoTexture("");
        public GeoTexture GeoTextureBack = new GeoTexture("");
        public GeoTexture GeoTextureLeft = new GeoTexture("");
        public GeoTexture GeoTextureRight = new GeoTexture("");
        public GeoTexture GeoTextureTop = new GeoTexture("");
        public GeoTexture GeoTextureBottom = new GeoTexture("");

        public GeoTexture GeoTextureFrontNormal = new GeoTexture("");
        public GeoTexture GeoTextureBackNormal = new GeoTexture("");
        public GeoTexture GeoTextureLeftNormal = new GeoTexture("");
        public GeoTexture GeoTextureRightNormal = new GeoTexture("");
        public GeoTexture GeoTextureTopNormal = new GeoTexture("");
        public GeoTexture GeoTextureBottomNormal = new GeoTexture("");

        public GeoTexture GeoTextureFrontSpecular = new GeoTexture("");
        public GeoTexture GeoTextureBackSpecular = new GeoTexture("");
        public GeoTexture GeoTextureLeftSpecular = new GeoTexture("");
        public GeoTexture GeoTextureRightSpecular = new GeoTexture("");
        public GeoTexture GeoTextureTopSpecular = new GeoTexture("");
        public GeoTexture GeoTextureBottomSpecular = new GeoTexture("");

        public GameObject Owner = null;

        public void SetTextureRepeat(float x, float y, CubeSide side)
        {
            if(side == CubeSide.All)
            {
                EditTextureObject(ref GeoTextureFront, x, y);
                EditTextureObject(ref GeoTextureBack, x, y);
                EditTextureObject(ref GeoTextureLeft, x, y);
                EditTextureObject(ref GeoTextureRight, x, y);
                EditTextureObject(ref GeoTextureTop, x, y);
                EditTextureObject(ref GeoTextureBottom, x, y);
            }
            else if(side == CubeSide.Front)
            {
                EditTextureObject(ref GeoTextureFront, x, y);
            }
            else if (side == CubeSide.Back)
            {
                EditTextureObject(ref GeoTextureBack, x, y);
            }
            else if (side == CubeSide.Left)
            {
                EditTextureObject(ref GeoTextureLeft, x, y);
            }
            else if (side == CubeSide.Right)
            {
                EditTextureObject(ref GeoTextureRight, x, y);
            }
            else if (side == CubeSide.Top)
            {
                EditTextureObject(ref GeoTextureTop, x, y);
            }
            else if (side == CubeSide.Bottom)
            {
                EditTextureObject(ref GeoTextureBottom, x, y);
            }
        }

        internal void SetTextureInternal(string texture, CubeSide side, TextureType type)
        {
            if (Owner == null || GLWindow.CurrentWindow.CurrentWorld == null)
            {
                throw new Exception("Cube texture owner is not set or is not member of a world.");
            }
            if (this is GeoModelCube1)
            {
                SetTextureAll(texture, type);
            }
            else if (side == CubeSide.All)
            {

                SetTextureFront(texture, type);
                SetTextureBack(texture, type);
                SetTextureLeft(texture, type);
                SetTextureRight(texture, type);
                SetTextureTop(texture, type);
                SetTextureBottom(texture, type);
            }
            else if (side == CubeSide.Front)
            {
                SetTextureFront(texture, type, isFile);
            }
            else if (side == CubeSide.Back)
            {
                SetTextureBack(texture, type, isFile);
            }
            else if (side == CubeSide.Left)
            {
                SetTextureLeft(texture, type, isFile);
            }
            else if (side == CubeSide.Right)
            {
                SetTextureRight(texture, type, isFile);
            }
            else if (side == CubeSide.Top)
            {
                SetTextureTop(texture, type, isFile);
            }
            else if (side == CubeSide.Bottom)
            {
                SetTextureBottom(texture, type, isFile);
            }
        }

        public void SetTexture(string texture, CubeSide side, TextureType type)
        {
            SetTextureInternal(texture, side, type);
        }

        private void SetTextureAll(string texture, TextureType type)
        {
            int texAll;
           
            if (KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].ContainsKey(texture))
            {
                texAll = KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld][texture];
            }
            else
            {
                Assembly a = Assembly.GetEntryAssembly();
                texAll = HelperTexture.LoadTextureForModelExternal(texture);
                KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].Add(texture, texAll);
            }

            if (type == TextureType.Albedo)
            {   
                EditTextureObject(ref GeoTextureFront, texAll, type, texture);
                EditTextureObject(ref GeoTextureBack, texAll, type, texture);
                EditTextureObject(ref GeoTextureLeft, texAll, type, texture);
                EditTextureObject(ref GeoTextureRight, texAll, type, texture);
                EditTextureObject(ref GeoTextureTop, texAll, type, texture);
                EditTextureObject(ref GeoTextureBottom, texAll, type, texture);
            }
            else if (type == TextureType.Normal)
            {
                EditTextureObject(ref GeoTextureFrontNormal, texAll, type, texture);
                EditTextureObject(ref GeoTextureBackNormal, texAll, type, texture);
                EditTextureObject(ref GeoTextureLeftNormal, texAll, type, texture);
                EditTextureObject(ref GeoTextureRightNormal, texAll, type, texture);
                EditTextureObject(ref GeoTextureTopNormal, texAll, type, texture);
                EditTextureObject(ref GeoTextureBottomNormal, texAll, type, texture);
            }
            else if (type == TextureType.Specular)
            {
                EditTextureObject(ref GeoTextureFrontSpecular, texAll, type, texture);
                EditTextureObject(ref GeoTextureBackSpecular, texAll, type, texture);
                EditTextureObject(ref GeoTextureLeftSpecular, texAll, type, texture);
                EditTextureObject(ref GeoTextureRightSpecular, texAll, type, texture);
                EditTextureObject(ref GeoTextureTopSpecular, texAll, type, texture);
                EditTextureObject(ref GeoTextureBottomSpecular, texAll, type, texture);
            }

        }

        private void SetTextureFront(string texture, TextureType type, bool isFile)
        {
            int texId = -1;
            if (CustomTextures[GLWindow.CurrentWindow.CurrentWorld].ContainsKey(texture))
            {
                texId = KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld][texture];
            }
            else
            {
                texId = isFile ? HelperTexture.LoadTextureForModelExternal(texture) : HelperTexture.LoadTextureForModelInternal(texture);
                KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].Add(texture, texId);
            }

            if(type == TextureType.Albedo)
                EditTextureObject(ref GeoTextureFront, texId, type, texture);
            else if(type == TextureType.Normal)
                EditTextureObject(ref GeoTextureFrontNormal, texId, type, texture);
            else if (type == TextureType.Specular)
                EditTextureObject(ref GeoTextureFrontSpecular, texId, type, texture);

        }

       

        private void SetTextureBack(string texture, TextureType type, bool isFile)
        {
            int texId = -1;
            if (KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].ContainsKey(texture))
            {
               
                    texId = KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld][texture];
            }
            else
            {

                texId = isFile ? HelperTexture.LoadTextureForModelExternal(texture) : HelperTexture.LoadTextureForModelInternal(texture);

                KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].Add(texture, texId);
            }

            if (type == TextureType.Albedo)
                EditTextureObject(ref GeoTextureBack, texId, type, texture);
            else if (type == TextureType.Normal)
                EditTextureObject(ref GeoTextureBackNormal, texId, type, texture);
            else if (type == TextureType.Specular)
                EditTextureObject(ref GeoTextureBackSpecular, texId, type, texture);
        }

        private void SetTextureLeft(string texture, TextureType type, bool isFile)
        {
            int texId = -1;
            if (KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].ContainsKey(texture))
            {
               
                    texId = KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld][texture];
            }
            else
            {

                texId = isFile ? HelperTexture.LoadTextureForModelExternal(texture) : HelperTexture.LoadTextureForModelInternal(texture);

                KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].Add(texture, texId);
            }

            if (type == TextureType.Albedo)
                EditTextureObject(ref GeoTextureLeft, texId, type, texture);
            else if (type == TextureType.Normal)
                EditTextureObject(ref GeoTextureLeftNormal, texId, type, texture);
            else if (type == TextureType.Specular)
                EditTextureObject(ref GeoTextureLeftSpecular, texId, type, texture);
        }

        private void SetTextureRight(string texture, TextureType type, bool isFile)
        {
            int texId = -1;
            if (KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].ContainsKey(texture))
            {
                
                    texId = KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld][texture];
            }
            else
            {

                texId = isFile ? HelperTexture.LoadTextureForModelExternal(texture) : HelperTexture.LoadTextureForModelInternal(texture);

                KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].Add(texture, texId);
            }

            if (type == TextureType.Albedo)
                EditTextureObject(ref GeoTextureRight, texId, type, texture);
            else if (type == TextureType.Normal)
                EditTextureObject(ref GeoTextureRightNormal, texId, type, texture);
            else if (type == TextureType.Specular)
                EditTextureObject(ref GeoTextureRightSpecular, texId, type, texture);
        }

        private void SetTextureTop(string texture, TextureType type, bool isFile)
        {
            int texId = -1;
            if (KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].ContainsKey(texture))
            {
                texId = KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld][texture];
            }
            else
            {
                texId = isFile ? HelperTexture.LoadTextureForModelExternal(texture) : HelperTexture.LoadTextureForModelInternal(texture);
                KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].Add(texture, texId);
            }

            if (type == TextureType.Albedo)
                EditTextureObject(ref GeoTextureTop, texId, type, texture);
            else if (type == TextureType.Normal)
                EditTextureObject(ref GeoTextureTopNormal, texId, type, texture);
            else if (type == TextureType.Specular)
                EditTextureObject(ref GeoTextureTopSpecular, texId, type, texture);
        }

        private void SetTextureBottom(string texture, TextureType type, bool isFile)
        {
            int texId = -1;
            if (KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld].ContainsKey(texture))
            {
                texId = KWEngine.CustomTextures[GLWindow.CurrentWindow.CurrentWorld][texture];
            }
            else
            {
                texId = isFile ? HelperTexture.LoadTextureForModelExternal(texture) : HelperTexture.LoadTextureForModelInternal(texture);
            }

            if (type == TextureType.Albedo)
                EditTextureObject(ref GeoTextureBottom, texId, type, texture);
            else if (type == TextureType.Normal)
                EditTextureObject(ref GeoTextureBottomNormal, texId, type, texture);
            else if (type == TextureType.Specular)
                EditTextureObject(ref GeoTextureBottomSpecular, texId, type, texture);
        }

        private void EditTextureObject(ref GeoTexture tex, int texId, TextureType type, string name)
        {
            if (type == TextureType.Albedo)
            {
                
                tex.Filename = name;
                tex.Type = GeoTexture.TexType.Diffuse;
                tex.OpenGLID = texId;
            }
            else if (type == TextureType.Normal)
            {
               
                tex.Filename = name;
                tex.Type = GeoTexture.TexType.Normal;
                tex.OpenGLID = texId;
            }
            else
            {
                tex.Filename = name;
                tex.Type = GeoTexture.TexType.Specular;
                tex.OpenGLID = texId;
            }
        }

        private void EditTextureObject(ref GeoTexture tex, float x, float y)
        {
            tex.UVTransform = new OpenTK.Vector2(x, y);
        }

    }
    */
}
