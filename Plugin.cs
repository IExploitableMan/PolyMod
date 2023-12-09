using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace PolyMod
{
	[BepInPlugin("com.polymod", "PolyMod", "1.0.0.0")]
	public class Plugin : BepInEx.Unity.IL2CPP.BasePlugin
	{
		internal const uint MAP_MIN_SIZE = 6;
		internal const uint MAP_MAX_SIZE = 100;
		internal const uint CAMERA_CONSTANT = 1000;

		internal static bool start = false;

		internal static bool bots_only = false;
		internal static bool unview = false;
		internal static LocalClient? localClient = null;

		public override void Load()
		{
			Harmony.CreateAndPatchAll(typeof(Patches));
		}

		internal static void Start()
		{
			AddCommand("bots", "", (args) =>
			{
				bots_only = !bots_only;

				DebugConsole.Write($"Bots only: {bots_only}");
			});

			DevConsole.Init();
		}

		internal static void Update()
		{
			if (!start)
			{
				Start();
				start = true;
			}

			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab))
			{
				DevConsole.Toggle();
			}
		}
	}
}
