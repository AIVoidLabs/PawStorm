using Godot;

namespace PawStorm;

/// <summary>Audio buses (Music / SFX / Ambient) created at runtime plus the player's volume settings (saved to user://settings.cfg).</summary>
public static class AudioSettings
{
	public const string MusicBus = "Music";
	public const string SfxBus = "SFX";
	public const string AmbientBus = "Ambient";
	private const string ConfigPath = "user://settings.cfg";

	private static bool _initialized;

	public static float MusicVolume { get; private set; } = 0.8f;
	public static float SfxVolume { get; private set; } = 1.0f;

	public static void Init()
	{
		if (_initialized) return;
		_initialized = true;
		Ensure(MusicBus);
		Ensure(SfxBus);
		Ensure(AmbientBus);
		var cfg = new ConfigFile();
		if (cfg.Load(ConfigPath) == Error.Ok)
		{
			MusicVolume = (float)cfg.GetValue("audio", "music", MusicVolume);
			SfxVolume = (float)cfg.GetValue("audio", "sfx", SfxVolume);
		}
		Apply();
	}

	public static void SetMusicVolume(float v) { Init(); MusicVolume = Mathf.Clamp(v, 0f, 1f); Apply(); Save(); }
	public static void SetSfxVolume(float v) { Init(); SfxVolume = Mathf.Clamp(v, 0f, 1f); Apply(); Save(); }

	private static void Ensure(string name)
	{
		if (AudioServer.GetBusIndex(name) >= 0) return;
		AudioServer.AddBus();
		int i = AudioServer.BusCount - 1;
		AudioServer.SetBusName(i, name);
		AudioServer.SetBusSend(i, "Master");
	}

	private static void Apply()
	{
		// Music and ambience are mixed lower than effects so they sit under the action.
		SetBus(MusicBus, MusicVolume, -6f);
		SetBus(SfxBus, SfxVolume, 0f);
		SetBus(AmbientBus, SfxVolume, -9f);
	}

	private static void SetBus(string bus, float linear, float offsetDb)
	{
		int i = AudioServer.GetBusIndex(bus);
		if (i < 0) return;
		AudioServer.SetBusMute(i, linear <= 0.001f);
		AudioServer.SetBusVolumeDb(i, Mathf.LinearToDb(Mathf.Max(linear, 0.001f)) + offsetDb);
	}

	private static void Save()
	{
		var cfg = new ConfigFile();
		cfg.Load(ConfigPath);
		cfg.SetValue("audio", "music", MusicVolume);
		cfg.SetValue("audio", "sfx", SfxVolume);
		cfg.Save(ConfigPath);
	}
}
