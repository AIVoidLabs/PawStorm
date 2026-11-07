using Godot;

namespace PawStorm;

/// <summary>Storm Core shard at the end of a level; collecting it completes the level.</summary>
public partial class StormCore : Area3D
{
	[Export] public float SpinSpeed { get; set; } = 1.5f;

	public override void _Ready() => BodyEntered += b =>
	{
		if (b is not PlayerController) return;
		Sfx.Play(this, "core");
		GameState.CompleteLevel();
		Visible = false;
		SetDeferred(Area3D.PropertyName.Monitoring, false);
	};

	public override void _Process(double delta) => RotateY(SpinSpeed * (float)delta);
}
