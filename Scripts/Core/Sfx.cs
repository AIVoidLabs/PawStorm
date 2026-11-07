using System.Collections.Generic;
using Godot;

namespace PawStorm;

/// <summary>One-shot sound effects from res://Audio/&lt;name&gt;.wav. Missing files are silently ignored.</summary>
public static class Sfx
{
	private static readonly Dictionary<string, AudioStream> Cache = new();

	public static void Play(Node context, string name, float volumeDb = 0f)
	{
		if (context == null || !context.IsInsideTree()) return;
		if (!Cache.TryGetValue(name, out var stream))
		{
			string path = $"res://Audio/{name}.wav";
			stream = ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
			Cache[name] = stream;
		}
		if (stream == null) return;
		var player = new AudioStreamPlayer { Stream = stream, VolumeDb = volumeDb, ProcessMode = Node.ProcessModeEnum.Always };
		player.Finished += player.QueueFree;
		context.GetTree().Root.AddChild(player);
		player.Play();
	}
}
