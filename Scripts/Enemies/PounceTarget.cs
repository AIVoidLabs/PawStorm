using Godot;

namespace PawStorm;

/// <summary>Placeholder homing-attack target (future enemies derive from the same idea).</summary>
public partial class PounceTarget : Node3D
{
	[Export] public int FishReward { get; set; } = 3;

	public override void _Ready() => AddToGroup("pounce_target");

	public void Hit()
	{
		GameState.AddFish(FishReward);
		QueueFree();
	}
}
