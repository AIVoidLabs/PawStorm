using System;
using System.Linq;

namespace PawStorm;

/// <summary>Level order of the campaign plus per-level title, music and ambience. The last level is followed by the main menu.</summary>
public static class Levels
{
	public const string MainMenu = "res://Scenes/UI/MainMenu.tscn";

	/// <param name="Victory">Text shown on the completion screen of a boss level that is not the last level.</param>
	public sealed record Info(string Path, string Title, string Music, string Ambient, string Preview, bool IsBoss = false, string Victory = null);

	public static readonly Info[] All =
	{
		new("res://Scenes/Levels/Level1_Rooftops.tscn", "Çatılar Şehri", "rooftops", "amb_rooftops", "rooftops"),
		new("res://Scenes/Levels/Level2_Harbor.tscn", "Liman", "harbor", "amb_harbor", "harbor"),
		new("res://Scenes/Levels/Level3_Forest.tscn", "Orman", "forest", "amb_forest", "forest"),
		new("res://Scenes/Levels/Level4_Factory.tscn", "Fabrika", "factory", "amb_factory", "factory"),
		new("res://Scenes/Levels/Level5_SkyIsland.tscn", "Gökyüzü Adası", "sky", "amb_sky", "sky"),
		new("res://Scenes/Levels/Level6_Boss.tscn", "Dr. Kennel", "boss", "amb_boss", "boss", true,
			"Dr. Kennel yenildi!\nAma fırtınanın asıl sahibi hâlâ ortada...\nKayıp kediler Kristal Mağaralar'ın ötesinde."),
		new("res://Scenes/Levels/Level7_CrystalCaves.tscn", "Kristal Mağaralar", "crystal", "amb_crystal", "crystal"),
		new("res://Scenes/Levels/Level8_Temple.tscn", "Antik Tapınak", "temple", "amb_temple", "temple"),
		new("res://Scenes/Levels/Level9_StormFortress.tscn", "General Growl", "boss2", "amb_storm", "storm", true),
	};

	public static string First => All[0].Path;

	/// <summary>Regular (non-boss) levels, each of which ends with a Storm Core shard.</summary>
	public static int ShardCount => All.Count(l => !l.IsBoss);

	public static Info For(string scenePath) => Array.Find(All, l => l.Path == scenePath);

	public static string Next(string currentScenePath)
	{
		int i = Array.FindIndex(All, l => l.Path == currentScenePath);
		return i >= 0 && i + 1 < All.Length ? All[i + 1].Path : MainMenu;
	}
}
