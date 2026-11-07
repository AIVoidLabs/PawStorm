using Godot;

namespace PawStorm;

public partial class FishPickup : Area3D
{
	[Export] public float SpinSpeed { get; set; } = 3f;
	[Export] public float BobHeight { get; set; } = 0.15f;

	private float _t;
	private Vector3 _basePos;

	public override void _Ready()
	{
		_basePos = Position;
		_t = GD.Randf() * Mathf.Tau;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		RotateY(SpinSpeed * (float)delta);
		Position = _basePos + Vector3.Up * Mathf.Sin(_t * 2f) * BobHeight;
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is not PlayerController) return;
		GameState.AddFish();
		QueueFree();
	}
}
