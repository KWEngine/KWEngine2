﻿using KWEngine2.GameObjects;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2.Collision
{
    /// <summary>
    /// Kollisionsklasse
    /// </summary>
    public class Intersection
    {
        /// <summary>
        /// Das Objekt, mit dem kollidiert wurde
        /// </summary>
        public GameObject Object { get; private set; } = null;
        /// <summary>
        /// Der Name der Hitbox, mit der kollidiert wurde
        /// </summary>
        public string MeshName { get; private set; } = "";
        
        private Vector3 mMTV = Vector3.Zero;
        private Vector3 mMTVUp = Vector3.Zero;

        /// <summary>
        /// Minimal-Translation-Vector (für Kollisionskorrektur)
        /// </summary>
        public Vector3 MTV
        {
            get
            {
                return mMTV;
            }
        }
        
        /// <summary>
        /// Minimal-Translation-Vector für die Y-Achse
        /// </summary>
        public Vector3 MTVUp
        {
            get
            {
                return mMTVUp;
            }
        }

        /// <summary>
        /// Kollisionspunkt (für Terrains)
        /// </summary>
        public float HeightOnTerrain { get; internal set; } = 0;

        /// <summary>
        /// Kollisionspunkt (für Terrains), der die Höhe des aufrufenden Objekts berücksichtigt
        /// </summary>
        public float HeightOnTerrainSuggested { get; internal set; } = 0;

        /// <summary>
        /// Gibt an, ob es sich bei dem Kollisionsobjekt um Terrain handelt
        /// </summary>
        public bool IsTerrain { get; internal set; } = false;

        /// <summary>
        /// Konstruktormethode
        /// </summary>
        /// <param name="collider">Kollisionsobjekt</param>
        /// <param name="mtv">Korrektur-MTV</param>
        /// <param name="mtvUp">Korrektur-MTV (Y-Achse)</param>
        /// <param name="mName">Mesh-Name</param>
        /// <param name="suggestedHeightOnTerrain">Höhe auf Terrain (inkl. Hitbox)</param>
        /// <param name="heightOnTerrain">Absolute Höhe auf dem Terrain</param>
        /// <param name="isTerrain">true, wenn es ein Terrain-Objekt ist</param>
        public Intersection(GameObject collider, Vector3 mtv, Vector3 mtvUp, string mName, float suggestedHeightOnTerrain = 0, float heightOnTerrain = 0, bool isTerrain = false)
        {
            Object = collider;
            MeshName = mName;
            mMTV = mtv;
            mMTVUp = mtvUp;
            HeightOnTerrainSuggested = suggestedHeightOnTerrain;
            HeightOnTerrain = heightOnTerrain;
            IsTerrain = isTerrain;
        }
    }
}
