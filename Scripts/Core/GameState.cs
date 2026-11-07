using System;

namespace PawStorm;

/// <summary>Minimal run-wide state (fish count). Static so scenes can reload freely.</summary>
public static class GameState
{
	public static int Fish { get; private set; }
	public static event EventHandler FishChanged;

	public static void Reset() { Fish = 0; FishChanged?.Invoke(null, EventArgs.Empty); }
	public static void AddFish(int n = 1) { Fish += n; FishChanged?.Invoke(null, EventArgs.Empty); }
}
