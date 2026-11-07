using Godot;

namespace PawStorm;

/// <summary>Placeholder homing-attack target (future enemies derive from the same idea).</summary>
/// <summary>Anything the Pounce can lock onto and hit.</summary>
public interface IPounceable
{
	void Hit();
}

public partial class PounceTarget : Node3D, IPounceable
{
	[Export] public int FishReward { get; set; } = 3;

	public override void _Ready() => AddToGroup("pounce_target");

	public void Hit()
	{
		GameState.AddFish(FishReward);
		QueueFree();
	}
}
