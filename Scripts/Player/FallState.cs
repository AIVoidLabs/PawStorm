namespace PawStorm;

public partial class FallState : PlayerState
{
	private float _coyote;

	public FallState(PlayerController p) : base(p) { }

	public override void Enter() => _coyote = Machine.PreviousWasGround ? Stats.CoyoteTime : 0f;

	public override void Physics(float dt)
	{
		var p = Player;
		_coyote -= dt;
		if (_coyote > 0f && p.JumpPressed()) { Machine.Transition(PlayerStateId.Jump); return; }
		p.ApplyAirMovement(dt);
		p.MoveAndSlide();
		if (p.IsOnFloor()) p.Land();
	}
}
