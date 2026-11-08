using System;

namespace PawStorm;

/// <summary>Run-wide state (fish, 9 Lives, Storm Core shards, level timer). Static so scenes can reload freely.</summary>
public static class GameState
{
	public const int MaxLives = 9;

	public static int Fish { get; private set; }
	public static int Lives { get; private set; } = MaxLives;
	public static int CoreShards { get; private set; }
	public static float LevelTime { get; set; }
	public static bool LevelDone { get; private set; }
	/// <summary>Boss health for the HUD (0 max = no boss in this level).</summary>
	public static int BossHp { get; set; }
	public static int BossMaxHp { get; set; }
	public static string BossName { get; set; } = "";

	public static event EventHandler FishChanged;
	public static event EventHandler LivesChanged;
	public static event EventHandler LevelCompleted;

	public static void ResetAll()
	{
		Fish = 0; Lives = MaxLives; CoreShards = 0;
		IsGameOver = false;
		BeginLevel();
		FishChanged?.Invoke(null, EventArgs.Empty);
		LivesChanged?.Invoke(null, EventArgs.Empty);
	}

	public static void BeginLevel() { LevelTime = 0f; LevelDone = false; }

	public static void AddFish(int n = 1) { Fish += n; FishChanged?.Invoke(null, EventArgs.Empty); }

	/// <summary>Player was hit: drop up to <paramref name="max"/> fish. Returns how many were lost.</summary>
	public static int TakeFishHit(int max = 15)
	{
		int lost = Math.Min(Fish, max);
		Fish -= lost;
		if (lost > 0) FishChanged?.Invoke(null, EventArgs.Empty);
		return lost;
	}

	public static bool IsGameOver { get; private set; }
	public static event EventHandler GameOver;

	/// <summary>Lose one life. Returns true when none are left; the player must then pick an option on the Game Over screen.</summary>
	public static bool LoseLife()
	{
		Lives = Math.Max(0, Lives - 1);
		LivesChanged?.Invoke(null, EventArgs.Empty);
		return Lives <= 0;
	}

	/// <summary>Show the Game Over screen.</summary>
	public static void RaiseGameOver()
	{
		IsGameOver = true;
		GameOver?.Invoke(null, EventArgs.Empty);
	}

	/// <summary>Continue after Game Over: lives back to 9, fish lost.</summary>
	public static void Continue()
	{
		IsGameOver = false;
		Lives = MaxLives;
		Fish = 0;
		FishChanged?.Invoke(null, EventArgs.Empty);
		LivesChanged?.Invoke(null, EventArgs.Empty);
	}

	/// <param name="grantsShard">False for boss levels (they do not give a Storm Core shard).</param>
	public static void CompleteLevel(bool grantsShard = true)
	{
		if (LevelDone) return;
		LevelDone = true;
		if (grantsShard) CoreShards++;
		LevelCompleted?.Invoke(null, EventArgs.Empty);
	}
}
