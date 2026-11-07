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

	public Node3D PounceTargetNode { get; private set; }
	public GrindRail CurrentRail { get; private set; }
	public float RailOffset { get; private set; }

	private float _grindCooldown;
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
		Machine.Register(PlayerStateId.Charge, new ChargeState(this));
		Machine.Register(PlayerStateId.Spin, new SpinState(this));
		Machine.Register(PlayerStateId.Pounce, new PounceState(this));
		Machine.Register(PlayerStateId.Grind, new GrindState(this));
		Machine.Transition(PlayerStateId.Fall);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_grindCooldown -= dt;
		_jumpBuffer = Input.IsActionJustPressed("jump") ? JumpBufferTime : Mathf.Max(0f, _jumpBuffer - dt);
		InputDir = ReadCameraRelativeInput();
		Machine.Physics(dt);
		UpdateVisual(dt);
		if (GlobalPosition.Y < KillHeight) Respawn();
	}

	/// <summary>Current state name (debug / tests).</summary>
	public string GetStateName() => Machine.CurrentId.ToString();

	public bool SpinPressed() => Input.IsActionJustPressed("spin");

	/// <summary>Horizontal direction the cat is currently facing.</summary>
	public Vector3 Facing
	{
		get
		{
			Vector3 f = Visual != null ? -Visual.GlobalTransform.Basis.Z : -GlobalTransform.Basis.Z;
			f.Y = 0;
			return f.LengthSquared() > 0.0001f ? f.Normalized() : Vector3.Forward;
		}
	}

	public void FaceDirection(Vector3 dir, float dt)
	{
		if (Visual == null || dir.LengthSquared() < 0.0001f) return;
		Vector3 r = Visual.Rotation;
		r.Y = Mathf.LerpAngle(r.Y, Mathf.Atan2(-dir.X, -dir.Z), Mathf.Clamp(20f * dt, 0f, 1f));
		Visual.Rotation = r;
	}

	public void StartGrindCooldown() => _grindCooldown = Stats.GrindCooldown;

	/// <summary>Homing attack: lock the nearest pounce target in front and switch to the Pounce state.</summary>
	public bool TryPounce()
	{
		Vector3 fwd = InputDir.LengthSquared() > 0.01f ? InputDir.Normalized()
			: HorizontalSpeed > 1f ? new Vector3(Velocity.X, 0, Velocity.Z).Normalized() : Facing;
		Node3D best = null;
		float bestDist = float.MaxValue;
		foreach (Node n in GetTree().GetNodesInGroup("pounce_target"))
		{
			if (n is not Node3D t) continue;
			Vector3 to = t.GlobalPosition - GlobalPosition;
			float d = to.Length();
			if (d > Stats.PounceRange || d < 0.5f) continue;
			Vector3 h = new Vector3(to.X, 0, to.Z);
			if (h.LengthSquared() > 0.01f && h.Normalized().Dot(fwd) < 0.2f) continue;
			if (d < bestDist) { best = t; bestDist = d; }
		}
		if (best == null) return false;
		PounceTargetNode = best;
		ConsumeJump();
		Machine.Transition(PlayerStateId.Pounce);
		return true;
	}

	/// <summary>Grab a rail when falling close to it.</summary>
	public bool TryStartGrind()
	{
		if (_grindCooldown > 0f || Velocity.Y > 0f) return false;
		Vector3 center = GlobalPosition + Vector3.Up * 0.2f;
		foreach (Node n in GetTree().GetNodesInGroup("grind_rail"))
		{
			if (n is not GrindRail rail || rail.Curve == null) continue;
			float off = rail.ClosestOffset(center);
			if (rail.WorldPointAt(off).DistanceTo(center) > Stats.GrindSnapDistance) continue;
			CurrentRail = rail;
			RailOffset = off;
			Machine.Transition(PlayerStateId.Grind);
			return true;
		}
		return false;
	}

	/// <summary>Rolling movement: low friction, boosted slope gain, weak steering.</summary>
	public void ApplyRollMovement(float dt)
	{
		Vector3 n = IsOnFloor() ? GetFloorNormal() : Vector3.Up;
		Vector3 vel = Velocity.Slide(n);
		vel += (Vector3.Down * Stats.Gravity).Slide(n) * (Stats.SlopeForce / Stats.Gravity) * Stats.RollSlopeMultiplier * dt;
		float speed = vel.Length();
		if (speed > 0.05f)
		{
			Vector3 dir = vel / speed;
			if (InputDir.LengthSquared() > 0.01f)
			{
				Vector3 wish = InputDir.Slide(n).Normalized();
				if (dir.Dot(wish) < -0.3f) speed -= Stats.TurnDrag * 0.25f * dt;
				else dir = dir.Slerp(wish, Mathf.Clamp(Stats.RollTurnRate * dt, 0f, 1f)).Normalized();
			}
			speed = Mathf.Max(0f, speed - Stats.RollFriction * dt);
			vel = dir * speed;
		}
		vel += ExternalAccel.Slide(n) * Stats.WindForceMultiplier * dt;
		Velocity = vel - n * 0.5f;
		TryStepUp(dt);
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
		bool ball = Machine.CurrentId == PlayerStateId.Charge || Machine.CurrentId == PlayerStateId.Spin;
		Visual.Scale = Visual.Scale.Lerp(ball ? new Vector3(1, 0.6f, 1) : Vector3.One, Mathf.Clamp(15f * dt, 0f, 1f));
		if (Machine.CurrentId == PlayerStateId.Charge) return;
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
