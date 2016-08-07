using System;

using UnityEngine;

using Verse;

namespace Mending
{
	[StaticConstructorOnStartup]
	internal class Textures
	{
		public static readonly Texture2D Outside = ContentFinder<Texture2D>.Get ("UI/Designators/NoRoofAreaOn", true);
		public static readonly Texture2D Inside = ContentFinder<Texture2D>.Get ("UI/Designators/HomeAreaOn", true);
	}
}

