using Godot;

namespace PawStorm;

public partial class PlayerController : CharacterBody3D
{
	[Export] public PlayerStats Stats { get; set; }
	[Export] public Node3D Visual { get; set; }
	[Export] public float KillHeight { get; set; } = -40f;

	public StateMachine Machine { get; } = new();
	public Vector3 InputDir { get; private set; }
	public Vector3 ExternalAccel { get; set; }

	private Vector3 _spawn;
	private float _jumpBuffer;
	private const float JumpBufferTime = 0.12f;

	public float HorizontalSpeed => new Vector2(Velocity.X, Velocity.Z).Length();
	public float Speed => Velocity.Length();

	public override void _Ready()
	{
		AddToGroup("player");
		Stats ??= new PlayerStats();
		_spawn = GlobalPosition;
		FloorMaxAngle = Mathf.DegToRad(Stats.MaxFloorAngleDeg);
		FloorStopOnSlope = false;
		FloorSnapLength = 0.6f;
		FloorConstantSpeed = false;
		Machine.Register(PlayerStateId.Idle, new IdleState(this));
		Machine.Register(PlayerStateId.Run, new RunState(this));
		Machine.Register(PlayerStateId.Jump, new JumpState(this));
		Machine.Register(PlayerStateId.Fall, new FallState(this));
		Machine.Transition(PlayerStateId.Fall);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_jumpBuffer = Input.IsActionJustPressed("jump") ? JumpBufferTime : Mathf.Max(0f, _jumpBuffer - dt);
		InputDir = ReadCameraRelativeInput();
		Machine.Physics(dt);
		UpdateVisual(dt);
		if (GlobalPosition.Y < KillHeight) Respawn();
	}

	public bool JumpPressed() => _jumpBuffer > 0f;
	public void ConsumeJump() => _jumpBuffer = 0f;

	public void Land() => Machine.Transition(InputDir.LengthSquared() > 0.01f || HorizontalSpeed > 0.5f
		? PlayerStateId.Run : PlayerStateId.Idle);

	private Vector3 ReadCameraRelativeInput()
	{
		Vector2 raw = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		if (raw.LengthSquared() < 0.0001f) return Vector3.Zero;
		var cam = GetViewport().GetCamera3D();
		Basis b = cam != null ? cam.GlobalTransform.Basis : Basis.Identity;
		Vector3 fwd = -b.Z; fwd.Y = 0; fwd = fwd.Normalized();
		Vector3 right = b.X; right.Y = 0; right = right.Normalized();
		return (right * raw.X + fwd * -raw.Y).LimitLength(1f);
	}

	/// <summary>Momentum-based ground movement along the floor plane, with slope gravity.</summary>
	public void ApplyGroundMovement(float dt)
	{
		Vector3 n = IsOnFloor() ? GetFloorNormal() : Vector3.Up;
		Vector3 vel = Velocity.Slide(n);
		float speed;

		// Slope: gravity component along the surface speeds up downhill, slows uphill.
		Vector3 slope = (Vector3.Down * Stats.Gravity).Slide(n) * (Stats.SlopeForce / Stats.Gravity);
		vel += slope * dt;

		if (InputDir.LengthSquared() > 0.01f)
		{
			Vector3 wish = InputDir.Slide(n).Normalized();
			speed = vel.Length();
			Vector3 dir = speed > 0.05f ? vel / speed : wish;
			float dot = dir.Dot(wish);
			if (dot < -0.3f && speed > 1f)
			{
				// Reversing against momentum: heavy drag instead of an instant turn.
				speed = Mathf.Max(0f, speed - Stats.TurnDrag * dt);
			}
			else
			{
				dir = dir.Slerp(wish, Mathf.Clamp(Stats.TurnRate * dt, 0f, 1f));
				if (dir.LengthSquared() < 0.0001f) dir = wish;
				dir = dir.Normalized();
				if (speed < Stats.MaxSpeed)
					speed = Mathf.Min(Stats.MaxSpeed, speed + Stats.Acceleration * dt * InputDir.Length());
			}
			vel = dir * speed;
		}
		else
		{
			float len = vel.Length();
			vel = len > 0.001f ? vel / len * Mathf.Max(0f, len - Stats.Friction * dt) : Vector3.Zero;
		}

		vel += ExternalAccel.Slide(n) * Stats.WindForceMultiplier * dt;
		// Small downward push so floor snap holds on crests.
		Velocity = vel - n * 0.5f;
		TryStepUp(dt);
	}

	/// <summary>Lifts the body over low ledges (stairs) if the way is blocked at foot level but free above.</summary>
	private void TryStepUp(float dt)
	{
		Vector3 h = new Vector3(Velocity.X, 0, Velocity.Z);
		if (h.LengthSquared() < 0.25f) return;
		Vector3 motion = h * dt + h.Normalized() * 0.1f;
		Transform3D t = GlobalTransform;
		if (!TestMove(t, motion)) return;
		Vector3 up = Vector3.Up * Stats.StepHeight;
		if (TestMove(t, up)) return;
		if (TestMove(t.Translated(up), motion)) return;
		GlobalPosition += up;
	}

	public void ApplyAirMovement(float dt)
	{
		Vector3 v = Velocity;
		Vector3 horiz = new Vector3(v.X, 0, v.Z);
		if (InputDir.LengthSquared() > 0.01f)
		{
			Vector3 target = InputDir * Stats.MaxSpeed;
			horiz = horiz.MoveToward(target, Stats.AirAcceleration * Stats.AirControl * dt * InputDir.Length());
		}
		else
		{
			horiz = horiz.MoveToward(Vector3.Zero, Stats.AirDrag * dt);
		}
		horiz += ExternalAccel * Stats.WindForceMultiplier * dt;
		v.X = horiz.X; v.Z = horiz.Z;
		v.Y = Mathf.Max(v.Y - Stats.Gravity * dt, -Stats.MaxFallSpeed);
		Velocity = v;
	}

	private void UpdateVisual(float dt)
	{
		if (Visual == null) return;
		Vector3 h = new Vector3(Velocity.X, 0, Velocity.Z);
		if (h.LengthSquared() > 0.25f)
		{
			float targetYaw = Mathf.Atan2(-h.X, -h.Z);
			Vector3 r = Visual.Rotation;
			r.Y = Mathf.LerpAngle(r.Y, targetYaw, Mathf.Clamp(14f * dt, 0f, 1f));
			Visual.Rotation = r;
		}
	}

	public void Respawn()
	{
		GlobalPosition = _spawn;
		Velocity = Vector3.Zero;
		Machine.Transition(PlayerStateId.Fall);
	}
}
