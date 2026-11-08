using Godot;

namespace PawStorm;

/// <summary>Pati Sprint (spin dash) wind-up: hold spin, aim with input, release to launch.</summary>
public partial class ChargeState : PlayerState
{
	private float _charge;
	private Vector3 _aim;

	public ChargeState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		_charge = 0f;
		_aim = Player.Facing;
		Player.SetChargeSound(true);
	}

	public override void Exit() => Player.SetChargeSound(false);

	public override void Physics(float dt)
	{
		var p = Player;
		_charge += dt;
		if (p.InputDir.LengthSquared() > 0.01f) _aim = p.InputDir.Normalized();
		p.FaceDirection(_aim, dt);

		Vector3 n = p.IsOnFloor() ? p.GetFloorNormal() : Vector3.Up;
		p.Velocity = -n * 0.5f; // stand still, keep floor contact
		p.MoveAndSlide();
		if (!p.IsOnFloor()) { Machine.Transition(PlayerStateId.Fall); return; }
		if (p.JumpPressed()) { Machine.Transition(PlayerStateId.Jump); return; }

		if (!Input.IsActionPressed("spin"))
		{
			float t = Mathf.Clamp(_charge / Stats.SpinChargeTime, 0f, 1f);
			float speed = Mathf.Lerp(Stats.SpinDashMinSpeed, Stats.SpinDashMaxSpeed, t);
			p.Velocity = _aim.Slide(n).Normalized() * speed;
			Sfx.Play(p, "spin");
			Machine.Transition(PlayerStateId.Spin);
		}
	}
}
