using OpenTK;

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
    }
}
