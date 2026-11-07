using Godot;

namespace PawStorm;

/// <summary>Area that pushes the player with a constant acceleration (rooftop gusts).</summary>
public partial class WindZone : Area3D
{
	[Export] public Vector3 Acceleration { get; set; } = new Vector3(8, 0, 0);

	public override void _Ready()
	{
		BodyEntered += b => { if (b is PlayerController p) p.ExternalAccel += Acceleration; };
		BodyExited += b => { if (b is PlayerController p) p.ExternalAccel -= Acceleration; };
	}
}
