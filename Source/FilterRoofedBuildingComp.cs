using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using RimWorld;
using Verse;
using Verse.AI;

namespace Mending
{
	public class FilterRoofedBuildingComp : ThingComp
	{
		public bool restrictInside;

		public override void PostExposeData ()
		{
			Scribe_Values.LookValue<bool> (ref this.restrictInside, "outsideItems", false, true);
		}

		public override IEnumerable<Command> CompGetGizmosExtra ()
		{
			foreach (var current in base.CompGetGizmosExtra())
				yield return current;

			var command = new Command_Action
			{
				defaultLabel  = restrictInside ? "mending.roofed".Translate() : "mending.everywhere".Translate(),
				defaultDesc   = "mending.limitation".Translate(),
				icon          = restrictInside ? Textures.Inside : Textures.Outside,
				activateSound = SoundDef.Named( "DesignateMine" ),
				hotKey        = KeyBindingDefOf.CommandColonistDraft,
				action        = () =>
				{
					restrictInside = !restrictInside;
				}
			};

			yield return command;
		}
	}
}
