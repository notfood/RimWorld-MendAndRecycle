using UnityEngine;

using Verse;

namespace MendAndRecycle
{
    [StaticConstructorOnStartup]
    class Textures
    {
        public static readonly Texture2D Outside = ContentFinder<Texture2D>.Get ("UI/Designators/NoRoofArea", true);
        public static readonly Texture2D Inside = ContentFinder<Texture2D>.Get ("UI/Designators/HomeAreaOn", true);
    }
}

