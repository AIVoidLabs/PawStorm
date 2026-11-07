using Godot;

namespace PawStorm;

/// <summary>Wind-blown prop placeholder: bobs and drifts on a sine path (kinematic).</summary>
public partial class FloatingProp : AnimatableBody3D
{
	[Export] public Vector3 Amplitude { get; set; } = new Vector3(0, 1.5f, 0);
	[Export] public float Frequency { get; set; } = 0.6f;
	[Export] public float SpinSpeed { get; set; } = 0.5f;

	private Vector3 _origin;
	private float _t;

	public override void _Ready()
	{
		_origin = Position;
		_t = GD.Randf() * Mathf.Tau;
	}

	public override void _PhysicsProcess(double delta)
	{
		_t += (float)delta * Frequency * Mathf.Tau;
		Position = _origin + Amplitude * Mathf.Sin(_t);
		RotateY(SpinSpeed * (float)delta);
	}
}
