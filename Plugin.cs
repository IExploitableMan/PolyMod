using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace PolyMod
{
	[BepInPlugin("com.polymod", "PolyMod", "1.0.0.0")]
	public class Plugin : BasePlugin
	{
		internal static bool foghack = false;

		public override void Load() => Load_();

		private static void Load_()
		{
			Harmony.CreateAndPatchAll(typeof(Patches));
		}

		internal static void Update()
		{
			if (Input.GetKeyDown(KeyCode.F1))
			{
				foghack = !foghack;
				ShowStatusPopup(nameof(foghack), foghack);
			}
			if (Input.GetKeyDown(KeyCode.F2))
			{
				GameManager.LocalPlayer.Currency += 100;
				ShowPopup("<color=\"yellow\">+100 stars");
			}
		}

		private static void ShowPopup(string value)
		{
			Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PopupBase.PopupButtonData> buttons = new(new[] { new PopupBase.PopupButtonData("buttons.ok") });
			PopupManager.GetBasicPopup(new PopupManager.BasicPopupData("PolyMod", value, buttons)).Show();
		}

		private static void ShowStatusPopup(string name, bool value)
		{
			string status = "<color=\"red\">DISABLED";
			if (value)
			{
				status = "<color=\"green\">ENABLED";
			}
			ShowPopup($"Module {name} is now {status}");
		}
	}
}
