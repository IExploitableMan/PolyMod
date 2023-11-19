﻿using Newtonsoft.Json.Linq;
using Polytopia.Data;

namespace PolyMod
{
	internal static class MapEditor
	{
		private const ushort MIN = 6;
		private const ushort MAX = 100;

		private static string _mapPath = BepInEx.Paths.BepInExRootPath + "/map.json"; //TODO: file open dialog
		private static JObject? _mapJson;

		private static JObject GetMapJson() 
		{
			return _mapJson ?? JObject.Parse(File.ReadAllText(_mapPath));
		}

		internal static void PreGenerate(ref GameState state)
		{
			JObject json = GetMapJson();
			ushort size = (ushort)json["size"];
			if(size < MIN || size > MAX) 
			{
				throw new Exception($"The map size must be between {MIN} and {MAX}");
			}
			state.Map = new(size,size);
		}

		internal static void PostGenerate(ref GameState state)
		{
			JObject json = GetMapJson();
			MapData map = state.Map;

			for (int i = 0; i < map.tiles.Length; i++)
			{
				TileData tile = map.tiles[i];
				JToken tileJson = json["map"][i];

				tile.climate = (tileJson["climate"] == null || (int)tileJson["climate"] < 0 || (int)tileJson["climate"] > 16) ? 0 : (int)tileJson["climate"];

				if (tile.rulingCityCoordinates != WorldCoordinates.NULL_COORDINATES) continue;

				tile.terrain = tileJson["terrain"] == null ? TerrainData.Type.None : EnumCache<TerrainData.Type>.GetType((string)tileJson["terrain"]);
				tile.skinType = tileJson["skinType"] == null ? SkinType.Default : EnumCache<SkinType>.GetType((string)tileJson["skinType"]);
				tile.improvement = tileJson["improvement"] == null ? null : new() { type = EnumCache<ImprovementData.Type>.GetType((string)tileJson["improvement"]) };
				if (tile.improvement != null && tile.improvement.type == ImprovementData.Type.City)
				{
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

				map.tiles[i] = tile;
			}

			_mapJson = null;
		}
	}
}
