using Godot;

namespace PawStorm;

[GlobalClass]
public partial class PlayerStats : Resource
{
	[ExportGroup("Run")]
	[Export] public float MaxSpeed { get; set; } = 40f;
	[Export] public float Acceleration { get; set; } = 18f;
	[Export] public float Deceleration { get; set; } = 30f;
	[Export] public float Friction { get; set; } = 10f;
	[Export] public float TurnDrag { get; set; } = 40f;
	[Export] public float TurnRate { get; set; } = 8f;

	[ExportGroup("Jump")]
	[Export] public float JumpVelocity { get; set; } = 13f;
	[Export] public float JumpCutMultiplier { get; set; } = 0.4f;
	[Export] public float Gravity { get; set; } = 36f;
	[Export] public float MaxFallSpeed { get; set; } = 60f;
	[Export] public float CoyoteTime { get; set; } = 0.1f;

	[ExportGroup("Air")]
	[Export] public float AirAcceleration { get; set; } = 12f;
	[Export] public float AirControl { get; set; } = 0.6f;
	[Export] public float AirDrag { get; set; } = 0.5f;

	[ExportGroup("Slope")]
	[Export] public float SlopeForce { get; set; } = 30f;
	[Export] public float MaxFloorAngleDeg { get; set; } = 60f;
	[Export] public float StepHeight { get; set; } = 0.5f;

	[ExportGroup("Wind")]
	[Export] public float WindForceMultiplier { get; set; } = 1f;

	[ExportGroup("Camera")]
	[Export] public float FovMin { get; set; } = 70f;
	[Export] public float FovMax { get; set; } = 100f;
	[Export] public float CameraDistance { get; set; } = 6f;
	[Export] public float CameraFollowSpeed { get; set; } = 8f;
	[Export] public float MouseSensitivity { get; set; } = 0.003f;
	[Export] public float StickSensitivity { get; set; } = 2.5f;
}
