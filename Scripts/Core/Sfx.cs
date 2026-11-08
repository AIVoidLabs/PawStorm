using System.Collections.Generic;
using Godot;

namespace PawStorm;

/// <summary>Sound effects from res://Audio/&lt;name&gt;.wav (one-shots) and looping streams. Missing files are silently ignored.</summary>
public static class Sfx
{
	private static readonly Dictionary<string, AudioStream> Cache = new();

	/// <summary>Load a wav; <paramref name="loop"/> turns it into a seamless forward loop.</summary>
	public static AudioStream Load(string path, bool loop = false)
	{
		string key = path + (loop ? "#loop" : "");
		if (Cache.TryGetValue(key, out var s)) return s;
		s = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
		if (loop && s is AudioStreamWav wav)
		{
			wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
			wav.LoopBegin = 0;
			wav.LoopEnd = wav.Data.Length / (wav.Stereo ? 4 : 2); // 16-bit samples
		}
		Cache[key] = s;
		return s;
	}

	public static void Play(Node context, string name, float volumeDb = 0f, float pitchJitter = 0f, float pitch = 1f)
	{
		if (context == null || !context.IsInsideTree()) return;
		AudioSettings.Init();
		var stream = Load($"res://Audio/{name}.wav");
		if (stream == null) return;
		var player = new AudioStreamPlayer
		{
			Stream = stream, VolumeDb = volumeDb, Bus = AudioSettings.SfxBus, ProcessMode = Node.ProcessModeEnum.Always,
			PitchScale = pitch + (pitchJitter > 0f ? (GD.Randf() * 2f - 1f) * pitchJitter : 0f),
		};
		player.Finished += player.QueueFree;
		var root = context.GetTree().Root;
		// Deferred: the root is "busy" while a scene is being set up (e.g. a button grabbing focus in _Ready).
		Callable.From(() => { root.AddChild(player); player.Play(); }).CallDeferred();
	}

	/// <summary>Start/stop a looping sound owned by <paramref name="owner"/> (the player node is created on first use).</summary>
	public static void SetLoop(Node owner, ref AudioStreamPlayer player, string name, bool on, float volumeDb = 0f, string bus = null)
	{
		if (player == null)
		{
			if (!on) return;
			AudioSettings.Init();
			var stream = Load($"res://Audio/{name}.wav", loop: true);
			if (stream == null) return;
			player = new AudioStreamPlayer { Stream = stream, VolumeDb = volumeDb, Bus = bus ?? AudioSettings.SfxBus };
			owner.AddChild(player);
		}
		if (on && !player.Playing) player.Play();
		else if (!on && player.Playing) player.Stop();
	}
}
