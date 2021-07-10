using KWEngine2.GameObjects;
using OpenTK;
using System.Collections.Generic;

namespace KWEngine2.Helper
{
    /// <summary>
    /// Helferklasse für Vektoroperationen
    /// </summary>
    public static class HelperVector
    {
        /// <summary>
        /// Reflektiert den eingehenden Vektor 'directionIn' am Ebenenvektor 'surfaceNormal'
        /// </summary>
        /// <param name="directionIn">Eingehender Vektor</param>
        /// <param name="surfaceNormal">Ebenenvektor</param>
        /// <returns>Reflektierter Vektor</returns>
        public static Vector3 Reflect(Vector3 directionIn, Vector3 surfaceNormal)
        {
            Vector3 reflectedVector = directionIn - 2 * Vector3.Dot(directionIn, surfaceNormal) * surfaceNormal;
            return Vector3.NormalizeFast(reflectedVector);
        }

        /// <summary>
        /// Erzeugt einen Strahl von der angegebenen Position in die angegebene Richtung und gibt alle Objekte die innerhalb dieser Blickrichtung liegen als Liste zurück.
        /// </summary>
        /// <param name="position">Startposition</param>
        /// <param name="direction">Blickrichtung (relativ)</param>
        /// <param name="maxDistance">Maximale Suchdistanz [Standard: 0 (unendlich)]</param>
        /// <returns></returns>
        public static List<GameObject> PickGameObjectsFrom(Vector3 position, Vector3 direction, float maxDistance = 0)
        {
            List<GameObject> pickedObjects = new List<GameObject>();
            GLWindow w = GLWindow.CurrentWindow;
            if (w == null || w.CurrentWorld == null || !w.Focused)
            {
                return pickedObjects;
            }

            foreach (GameObject go in w.CurrentWorld.GetGameObjects())
            {
                if (go.IsPickable)
                {
                    if (GameObject.IntersectRaySphere(position, direction, go.GetCenterPointForAllHitboxes(), go.GetMaxDiameter() / 2))
                    {
                        if (maxDistance > 0)
                        {
                            if ((go.GetCenterPointForAllHitboxes() - position).LengthFast <= maxDistance)
                            {
                                pickedObjects.Add(go);
                            }
                        }
                        else
                        {
                            pickedObjects.Add(go);
                        }
                    }
                }
            }
            return pickedObjects;
        }
    }
}
