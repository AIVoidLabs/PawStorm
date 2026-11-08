using Godot;

namespace PawStorm;

[GlobalClass]
public partial class PlayerStats : Resource
{
	[ExportGroup("Run")]
	[Export] public float MaxSpeed { get; set; } = 24f;
	[Export] public float Acceleration { get; set; } = 9f;
	[Export] public float OverspeedFriction { get; set; } = 8f;
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

	[ExportGroup("Spin")]
	[Export] public float ChargeMaxEntrySpeed { get; set; } = 3f;
	[Export] public float SpinChargeTime { get; set; } = 0.8f;
	[Export] public float SpinDashMinSpeed { get; set; } = 14f;
	[Export] public float SpinDashMaxSpeed { get; set; } = 36f;
	[Export] public float RollFriction { get; set; } = 12f;
	[Export] public float RollSlopeMultiplier { get; set; } = 1.5f;
	[Export] public float RollTurnRate { get; set; } = 2.5f;
	[Export] public float RollMinSpeed { get; set; } = 4f;

	[ExportGroup("Pounce")]
	[Export] public float PounceRange { get; set; } = 14f;
	[Export] public float PounceSpeed { get; set; } = 55f;
	[Export] public float PounceBounce { get; set; } = 11f;
	[Export] public float PounceMaxTime { get; set; } = 0.6f;

	[ExportGroup("Grind")]
	[Export] public float GrindMinSpeed { get; set; } = 14f;
	[Export] public float GrindGravityFactor { get; set; } = 0.8f;
	[Export] public float GrindSnapDistance { get; set; } = 1.0f;
	[Export] public float GrindCooldown { get; set; } = 0.3f;

	[ExportGroup("Wall / Air Twist / Loop")]
	[Export] public float AirTwistVelocity { get; set; } = 11f;
	[Export] public float WallClimbSpeed { get; set; } = 6f;
	[Export] public float WallClimbTime { get; set; } = 2.5f;
	[Export] public float WallSlideSpeed { get; set; } = 3f;
	[Export] public float WallJumpPush { get; set; } = 9f;
	[Export] public float LoopMinSpeed { get; set; } = 20f;

	[ExportGroup("Damage")]
	[Export] public float HurtKnockback { get; set; } = 9f;
	[Export] public float HurtUpSpeed { get; set; } = 8f;
	[Export] public float HurtMinTime { get; set; } = 0.4f;
	[Export] public float InvulnTime { get; set; } = 2f;
	[Export] public float EnemyBounce { get; set; } = 10f;

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
