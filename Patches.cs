using HarmonyLib;
using PolytopiaBackendBase.Game;

namespace PolyMod
{
	internal class Patches
	{

		[HarmonyPatch(typeof(Tile))]
		[HarmonyPatch("get_IsHidden")]
		class Tile_IsHidden
		{
			public static bool Postfix(Tile __instance, ref bool __result)
			{
				if (GameManager.Client != null && GameManager.Client.IsReplay && !SettingsUtils.ReplayEnableFog)
				{
					__result = false;
					return __result;
				}
				if (GameManager.Client != null && GameManager.Client.IsReplay && SettingsUtils.ReplaySharedFog)
				{
					__result = __instance.Data.explorers == null || __instance.Data.explorers.Count == 0;
					return __result;
				}
				if (Plugin.view_current)
				{
					__result = __instance.Data.GetExplored(GameManager.GameState.CurrentPlayer);
					return __result;
				}
				__result = !__instance.Data.GetExplored(GameManager.LocalPlayer.Id);
				return __result;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(ClientActionManager), nameof(ClientActionManager.Update))]
		static bool Update(ClientActionManager __instance)
		{
			if (Plugin.skip_recap)
			{
				__instance.LastSeenCommand = (ushort)__instance.gameState.CommandStack.Count;
			}
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(ClientActionManager), nameof(ClientActionManager.StartRecap))]
		private static void ClientActionManager_StartRecap_()
		{
			DebugConsole.Write("Recap started (shouldn't it be skipped?)");
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
		private static void GameManager_Update(GameManager __instance)
		{
			Plugin.Update();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		static void MapGenerator_Generate(ref GameState state, ref MapGeneratorSettings settings)
		{
			MapEditor.PreGenerate(ref state, ref settings);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		private static void MapGenerator_Generate_(ref GameState state)
		{
			MapEditor.PostGenerate(ref state);
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.ExecuteCommand))]
		private static bool DebugConsole_ExecuteCommand(ref string command)
		{
			return !Commands.Execute(command);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(DebugConsole), nameof(DebugConsole.CmdHelp))]
		private static void DebugConsole_CmdHelp()
		{
			Commands.Help();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SettingsScreen), nameof(SettingsScreen.CreateLanguageList))]
		private static bool SettingsScreen_CreateLanguageList(SettingsScreen __instance, UnityEngine.Transform parent)
		{
			List<string> list = new() { "Automatic", "English", "Français", "Deutsch", "Italiano", "Português", "Русский", "Español", "日本語", "한국어" };
			List<int> list2 = new() { 1, 3, 7, 9, 6, 4, 5, 8, 11, 12 };
			if (GameManager.GetPurchaseManager().IsTribeUnlocked(Polytopia.Data.TribeData.Type.Elyrion))
			{
				list.Add("∑∫ỹriȱŋ");
				list2.Add(10);
			}
			list.Add("Custom...");
			list2.Add(2);
			__instance.languageSelector = UnityEngine.Object.Instantiate(__instance.horizontalListPrefab, parent ?? __instance.container);
			__instance.languageSelector.UpdateScrollerOnHighlight = true;
			__instance.languageSelector.HeaderKey = "settings.language";
			__instance.languageSelector.SetIds(list2.ToArray());
			__instance.languageSelector.SetData(list.ToArray(), 0, false);
			__instance.languageSelector.SelectId(SettingsUtils.Language, true, -1f);
			__instance.languageSelector.IndexSelectedCallback = new Action<int>(__instance.LanguageChangedCallback);
			__instance.totalHeight += __instance.languageSelector.rectTransform.sizeDelta.y;

			return false;
		}
	}
}
