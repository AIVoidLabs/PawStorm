using Godot;

namespace PawStorm;

public partial class FishPickup : Area3D
{
	[Export] public float SpinSpeed { get; set; } = 3f;
	[Export] public float BobHeight { get; set; } = 0.15f;
	/// <summary>Seconds before the pickup can be collected (used for fish dropped on a hit).</summary>
	[Export] public float Delay { get; set; }

	private float _t;
	private float _age;
	private Vector3 _basePos;
	private bool _collected;

	public override void _Ready()
	{
		_basePos = Position;
		_t = GD.Randf() * Mathf.Tau;
		new ModelAnimator(this).PlayRandomOffset("Swim", 1.2f);
		BodyEntered += b => { if (_age >= Delay) Collect(b); };
	}

	public override void _Process(double delta)
	{
		_age += (float)delta;
		_t += (float)delta;
		RotateY(SpinSpeed * (float)delta);
		Position = _basePos + Vector3.Up * Mathf.Sin(_t * 2f) * BobHeight;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Dropped fish may already overlap the player when the delay ends.
		if (Delay <= 0f || _age < Delay || _collected) return;
		foreach (Node3D b in GetOverlappingBodies()) Collect(b);
	}

	private void Collect(Node3D body)
	{
		if (_collected || body is not PlayerController) return;
		_collected = true;
		GameState.AddFish();
		Sfx.Play(this, "fish");
		QueueFree();
	}
}
