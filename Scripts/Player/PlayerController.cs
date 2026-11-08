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
		_spawn = _firstSpawn = GlobalPosition;
		_anim = new ModelAnimator(Visual);
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
		Machine.Register(PlayerStateId.WallClimb, new WallClimbState(this));
		Machine.Register(PlayerStateId.Loop, new LoopState(this));
		Machine.Register(PlayerStateId.Hurt, new HurtState(this));
		Machine.Transition(PlayerStateId.Fall);
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_prevVelY = Velocity.Y;
		_grindCooldown -= dt;
		UpdateInvulnerability(dt);
		_jumpBuffer = Input.IsActionJustPressed("jump") ? JumpBufferTime : Mathf.Max(0f, _jumpBuffer - dt);
		InputDir = ReadCameraRelativeInput();
		Machine.Physics(dt);
		UpdateVisual(dt);
		UpdateFootsteps(dt);
		UpdatePounceIndicator();
		if (GlobalPosition.Y < KillHeight) Die();
	}

	// ---- Damage / lives ----
	public Vector3 HurtDirection { get; private set; }
	public bool IsInvulnerable => _invuln > 0f;
	private float _invuln;
	private Vector3 _firstSpawn;
	private PackedScene _fishScene;

	/// <summary>True while the player's body damages enemies (rolling, jumping or pouncing).</summary>
	public bool IsAttacking => Machine.CurrentId is PlayerStateId.Spin or PlayerStateId.Pounce or PlayerStateId.Jump
		|| (Machine.CurrentId == PlayerStateId.Fall && Velocity.Y < -1f);

	public void SetCheckpoint(Vector3 position) => _spawn = position;

	private void UpdateInvulnerability(float dt)
	{
		if (_invuln <= 0f) return;
		_invuln -= dt;
		if (Visual != null) Visual.Visible = _invuln <= 0f || ((int)(_invuln * 12f)) % 2 == 0;
	}

	/// <summary>Hit by an enemy or hazard: drop fish; with no fish left lose a life.</summary>
	public void TakeHit(Vector3 sourcePosition)
	{
		if (_invuln > 0f || Machine.CurrentId == PlayerStateId.Hurt || GameState.LevelDone) return;
		_invuln = Stats.InvulnTime;
		int lost = GameState.TakeFishHit();
		Sfx.Play(this, "hurt");
		if (lost == 0) { Die(); return; }
		ScatterFish(lost);
		Vector3 away = GlobalPosition - sourcePosition; away.Y = 0;
		HurtDirection = away.LengthSquared() > 0.01f ? away.Normalized() : -Facing;
		Machine.Transition(PlayerStateId.Hurt);
	}

	/// <summary>Lose a life and return to the last checkpoint (or the level start on game over).</summary>
	public void Die()
	{
		bool gameOver = GameState.LoseLife();
		if (gameOver) { _spawn = _firstSpawn; GameState.BeginLevel(); }
		_invuln = Stats.InvulnTime;
		Respawn();
	}

	private void ScatterFish(int count)
	{
		_fishScene ??= GD.Load<PackedScene>("res://Scenes/Props/Fish.tscn");
		int n = Mathf.Min(count, 12);
		for (int i = 0; i < n; i++)
		{
			float a = Mathf.Tau * i / n;
			var fish = _fishScene.Instantiate<FishPickup>();
			fish.Delay = 1.2f;
			fish.Position = GlobalPosition + new Vector3(Mathf.Cos(a), 0.6f + (i % 3) * 0.5f, Mathf.Sin(a)) * (1.5f + (i % 2));
			GetParent().CallDeferred(Node.MethodName.AddChild, fish);
		}
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

	public Vector3 WallNormal { get; private set; }
	public LoopTrack CurrentLoop { get; private set; }
	public bool AirTwistUsed { get; private set; }

	public void ResetAirActions() => AirTwistUsed = false;

	/// <summary>Second jump in mid-air (only once until landing).</summary>
	public bool TryAirTwist()
	{
		if (AirTwistUsed) return false;
		AirTwistUsed = true;
		ConsumeJump();
		var v = Velocity;
		v.Y = Stats.AirTwistVelocity;
		Velocity = v;
		return true;
	}

	/// <summary>Cling to a wall in the "climbable" group when pushing into it in mid-air.</summary>
	public bool TryWallClimb()
	{
		if (!IsOnWall() || IsOnFloor() || InputDir.LengthSquared() < 0.01f) return false;
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var c = GetSlideCollision(i);
			Vector3 n = c.GetNormal();
			if (Mathf.Abs(n.Y) > 0.3f) continue;
			if (c.GetCollider() is not Node node || !node.IsInGroup("climbable")) continue;
			if (InputDir.Normalized().Dot(-n) < 0.3f) continue;
			WallNormal = n;
			Machine.Transition(PlayerStateId.WallClimb);
			return true;
		}
		return false;
	}

	/// <summary>Enter a loop when running into its trigger fast enough in its direction.</summary>
	public void TryStartLoop(LoopTrack loop)
	{
		var id = Machine.CurrentId;
		if (id != PlayerStateId.Run && id != PlayerStateId.Spin) return;
		Vector3 h = new Vector3(Velocity.X, 0, Velocity.Z);
		if (h.Length() < Stats.LoopMinSpeed || h.Normalized().Dot(loop.Forward) < 0.8f) return;
		CurrentLoop = loop;
		Machine.Transition(PlayerStateId.Loop);
	}

	public void StartGrindCooldown() => _grindCooldown = Stats.GrindCooldown;

	/// <summary>Homing attack: lock the nearest pounce target in front and switch to the Pounce state.</summary>
	public bool TryPounce()
	{
		Node3D best = FindPounceTarget();
		if (best == null) return false;
		PounceTargetNode = best;
		ConsumeJump();
		Machine.Transition(PlayerStateId.Pounce);
		return true;
	}

	/// <summary>Nearest pounce target in range and roughly in the travel direction, or null.</summary>
	public Node3D FindPounceTarget()
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
		return best;
	}

	private MeshInstance3D _indicator;

	/// <summary>Yellow ring around the target the Pounce would lock onto (shown while airborne).</summary>
	private void UpdatePounceIndicator()
	{
		if (_indicator == null)
		{
			var mat = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = new Color(1f, 0.9f, 0.1f),
				NoDepthTest = true,
			};
			_indicator = new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.75f, OuterRadius = 0.9f, Material = mat },
				TopLevel = true,
				Visible = false,
			};
			AddChild(_indicator);
		}
		bool air = Machine.CurrentId is PlayerStateId.Jump or PlayerStateId.Fall;
		Node3D t = air ? FindPounceTarget() : (Machine.CurrentId == PlayerStateId.Pounce ? PounceTargetNode : null);
		if (t == null || !GodotObject.IsInstanceValid(t)) { _indicator.Visible = false; return; }
		float pulse = 1f + 0.15f * Mathf.Sin(Time.GetTicksMsec() * 0.012f);
		_indicator.Visible = true;
		_indicator.GlobalPosition = t.GlobalPosition;
		_indicator.Scale = Vector3.One * pulse * 1.2f;
		_indicator.Rotation = new Vector3(Mathf.Pi / 2f, 0, 0); // ring stands up, seen from the side
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
			ResetAirActions();
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
		Vector3 vel = KeepGroundSpeed(Velocity.Slide(n));
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
		_storedSpeed = vel.Length();
		Velocity = vel - n * 0.5f;
		TryStepUp(dt);
	}

	public bool JumpPressed() => _jumpBuffer > 0f;
	public void ConsumeJump() => _jumpBuffer = 0f;

	private float _prevVelY, _stepDistance;
	private AudioStreamPlayer _grindSfx, _chargeSfx;

	private void UpdateFootsteps(float dt)
	{
		if (Machine.CurrentId != PlayerStateId.Run || !IsOnFloor()) { _stepDistance = 0f; return; }
		_stepDistance += HorizontalSpeed * dt;
		if (_stepDistance < 2.2f) return;
		_stepDistance = 0f;
		Sfx.Play(this, "step", -13f, 0.12f);
	}

	/// <summary>Looping state sounds (rail grinding, spin charge); they stop when the state exits.</summary>
	public void SetGrindSound(bool on) => Sfx.SetLoop(this, ref _grindSfx, "grind_loop", on, -9f);
	public void SetChargeSound(bool on) => Sfx.SetLoop(this, ref _chargeSfx, "charge_loop", on, -8f);

	public void Land()
	{
		if (_prevVelY < -9f) Sfx.Play(this, "land", -8f, 0.08f);
		ResetAirActions();
		LandTransition();
	}

	private void LandTransition() => Machine.Transition(InputDir.LengthSquared() > 0.01f || HorizontalSpeed > 0.5f
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

	private ulong _lastGroundFrame;
	private float _storedSpeed;

	private bool HitWallLastMove()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
			if (Mathf.Abs(GetSlideCollision(i).GetNormal().Y) < 0.5f) return true;
		return false;
	}

	/// <summary>
	/// MoveAndSlide hands back only the horizontal part of a velocity that follows a tilted floor, so the speed
	/// would shrink by cos(slope) every frame. While we stay grounded (and did not hit a wall), restore the
	/// speed we set last frame.
	/// </summary>
	private Vector3 KeepGroundSpeed(Vector3 slid)
	{
		ulong frame = Engine.GetPhysicsFrames();
		bool contiguous = _lastGroundFrame + 1 == frame;
		_lastGroundFrame = frame;
		float len = slid.Length();
		if (contiguous && len > 0.01f && _storedSpeed > len && !HitWallLastMove())
			slid = slid / len * _storedSpeed;
		return slid;
	}
	/// <summary>Momentum-based ground movement along the floor plane, with slope gravity.</summary>
	public void ApplyGroundMovement(float dt)
	{
		Vector3 n = IsOnFloor() ? GetFloorNormal() : Vector3.Up;
		Vector3 vel = KeepGroundSpeed(Velocity.Slide(n));
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
				else if (n.Y > 0.95f)
					// Over top speed on flat ground (after a Spin Dash, boost...): bleed back to MaxSpeed.
					speed = Mathf.Max(Stats.MaxSpeed, speed - Stats.OverspeedFriction * dt);
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
		_storedSpeed = vel.Length();
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
		v.Y = Mathf.Max(v.Y - Stats.Gravity * dt + ExternalAccel.Y * Stats.WindForceMultiplier * dt, -Stats.MaxFallSpeed);
		Velocity = v;
	}

	private ModelAnimator _anim;

	/// <summary>Pick the baked clip that matches the current state.</summary>
	private void UpdateAnimation()
	{
		switch (Machine.CurrentId)
		{
			case PlayerStateId.Idle: _anim.Play("Idle"); break;
			case PlayerStateId.Run: _anim.Play("Run", Mathf.Clamp(HorizontalSpeed / 7f, 0.6f, 2.5f)); break;
			case PlayerStateId.Loop: _anim.Play("Run", 2.5f); break;
			case PlayerStateId.Jump: _anim.Play("Jump"); break;
			case PlayerStateId.Fall: _anim.Play("Fall"); break;
			case PlayerStateId.Charge: _anim.Play("Charge"); break;
			case PlayerStateId.Spin: case PlayerStateId.Pounce: _anim.Play("Ball"); break;
			case PlayerStateId.Hurt: _anim.Play("Hurt"); break;
			case PlayerStateId.Grind: _anim.Play("Grind"); break;
			case PlayerStateId.WallClimb:
				_anim.Play("Climb", Mathf.Abs(Input.GetAxis("move_back", "move_forward")) > 0.1f ? 1f : 0.05f);
				break;
		}
	}

	private void UpdateVisual(float dt)
	{
		if (Visual == null) return;
		UpdateAnimation();
		var id = Machine.CurrentId;
		bool ball = id == PlayerStateId.Charge || id == PlayerStateId.Spin || id == PlayerStateId.Pounce;
		Visual.Scale = Visual.Scale.Lerp(ball ? new Vector3(1, 0.8f, 1) : Vector3.One, Mathf.Clamp(15f * dt, 0f, 1f));
		// Pounce / Spin: roll forward like a ball; otherwise ease back upright.
		var rot = Visual.Rotation;
		rot.X = id == PlayerStateId.Pounce || id == PlayerStateId.Spin
			? Mathf.Wrap(rot.X - 22f * dt, -Mathf.Pi, Mathf.Pi)
			: Mathf.LerpAngle(rot.X, 0f, Mathf.Clamp(15f * dt, 0f, 1f));
		Visual.Rotation = rot;
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
