using Godot;

namespace PawStorm;

/// <summary>Claws on a "climbable" wall: climb with forward/back, wall-jump with Space, drop by pressing away.</summary>
public partial class WallClimbState : PlayerState
{
	private float _stamina;
	private Vector3 _n;

	public WallClimbState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		Player.ResetAirActions();
		_stamina = Stats.WallClimbTime;
		_n = Player.WallNormal;
		Player.Velocity = Vector3.Zero;
	}

	public override void Physics(float dt)
	{
		var p = Player;
		float axis = Input.GetAxis("move_back", "move_forward"); // +1 = up
		float climb;
		if (_stamina <= 0f) climb = -Stats.WallSlideSpeed;
		else
		{
			climb = axis * Stats.WallClimbSpeed;
			if (axis > 0.1f) _stamina -= dt;
		}
		p.FaceDirection(-_n, dt);

		if (p.JumpPressed())
		{
			p.Velocity = _n * Stats.WallJumpPush;
			Machine.Transition(PlayerStateId.Jump); // Jump.Enter adds the upward speed
			return;
		}
		if (p.InputDir.LengthSquared() > 0.01f && p.InputDir.Normalized().Dot(_n) > 0.5f)
		{
			p.Velocity = _n * 3f;
			Machine.Transition(PlayerStateId.Fall);
			return;
		}

		p.Velocity = -_n * 3f + Vector3.Up * climb;
		p.MoveAndSlide();

		bool touching = false;
		for (int i = 0; i < p.GetSlideCollisionCount(); i++)
		{
			Vector3 n = p.GetSlideCollision(i).GetNormal();
			if (Mathf.Abs(n.Y) < 0.3f) { touching = true; _n = n; }
		}
		if (!touching)
		{
			// Top of the wall: pop over the edge.
			p.Velocity = climb > 0.1f ? -_n * 5f + Vector3.Up * 5f : Vector3.Zero;
			Machine.Transition(PlayerStateId.Fall);
			return;
		}
		if (p.IsOnFloor() && climb <= 0f) p.Land();
	}
}
