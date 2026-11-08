using Godot;

namespace PawStorm;

/// <summary>Pati Sprint (spin dash) wind-up: hold spin, aim with input, release to launch.</summary>
public partial class ChargeState : PlayerState
{
	private float _charge;
	private int _tier;
	private Vector3 _aim;

	public ChargeState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		_charge = 0f;
		_tier = 0;
		_aim = Player.Facing;
		Player.SetChargeSound(true);
	}

	public override void Exit()
	{
		Player.SetChargeSound(false);
		Player.ChargeRatio = 0f;
	}

	public override void Physics(float dt)
	{
		var p = Player;
		_charge += dt;
		float ratio = Mathf.Clamp(_charge / Stats.SpinChargeTime, 0f, 1f);
		p.ChargeRatio = Mathf.Max(ratio, 0.001f); // keeps the HUD bar visible from the first frame
		int tier = Mathf.FloorToInt(ratio * ChargeBar.Tiers + 0.0001f);
		if (tier > _tier)
		{
			_tier = tier;
			Sfx.Play(p, "fish", -6f, 0f, 1f + 0.25f * tier); // rising tick per filled segment
		}
		if (p.InputDir.LengthSquared() > 0.01f) _aim = p.InputDir.Normalized();
		p.FaceDirection(_aim, dt);

		Vector3 n = p.IsOnFloor() ? p.GetFloorNormal() : Vector3.Up;
		p.Velocity = -n * 0.5f; // stand still, keep floor contact
		p.MoveAndSlide();
		if (!p.IsOnFloor()) { Machine.Transition(PlayerStateId.Fall); return; }
		if (p.JumpPressed()) { Machine.Transition(PlayerStateId.Jump); return; }

		if (!Input.IsActionPressed("spin"))
		{
			// Three discrete power levels: 0, 1, 2 or 3 full bar segments.
			float speed = Mathf.Lerp(Stats.SpinDashMinSpeed, Stats.SpinDashMaxSpeed, _tier / (float)ChargeBar.Tiers);
			p.Velocity = _aim.Slide(n).Normalized() * speed;
			Sfx.Play(p, "spin");
			Machine.Transition(PlayerStateId.Spin);
		}
	}
}
