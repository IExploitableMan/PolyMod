using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Newtonsoft.Json.Linq;
using Polytopia.Data;

namespace PolyMod
{
	internal class Patches
	{
		[HarmonyPostfix]
		[HarmonyPatch(typeof(TileData), nameof(TileData.GetExplored))]
		private static void TileData_GetExplored(ref bool __result)
		{
			if (Plugin.foghack)
			{
				__result = true;
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
		private static void GameManager_Awake(GameManager __instance)
		{
			ClassInjector.RegisterTypeInIl2Cpp(typeof(Behaviour));
			__instance.gameObject.AddComponent<Behaviour>();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateInternal))]
		private static void MapGenerator_GenerateInternal(ref MapData __result)
		{
			//TODO: any size

			JObject json = JObject.Parse(File.ReadAllText(BepInEx.Paths.BepInExRootPath + "/map.json"));
			for (int i = 0; i < __result.tiles.Length; i++)
			{
				TileData tile = __result.tiles[i];
				JToken tileJson = json["map"][i];

				if (tile.rulingCityCoordinates != new WorldCoordinates(-1, -1)) continue;

				tile.climate = (tileJson["climate"] == null || (int)tileJson["climate"] < 0 || (int)tileJson["climate"] > 16) ? 0 : (int)tileJson["climate"];
				tile.terrain = tileJson["terrain"] == null ? TerrainData.Type.None : EnumCache<TerrainData.Type>.GetType((string)tileJson["terrain"]);
				tile.improvement = tileJson["improvement"] == null ? null : new() { type = EnumCache<ImprovementData.Type>.GetType((string)tileJson["improvement"]) };
				if (tile.improvement != null && tile.improvement.type == ImprovementData.Type.City) {
					tile.improvement = new ImprovementState
					{
						type = ImprovementData.Type.City,
						founded = 0,
						level = 1,
						borderSize = 1,
						production = 1
					};
				}
				tile.resource = tileJson["resource"] == null ? null : new() { type = EnumCache<ResourceData.Type>.GetType((string)tileJson["resource"]) };

				__result.tiles[i] = tile;
			}
		}
	}
}
