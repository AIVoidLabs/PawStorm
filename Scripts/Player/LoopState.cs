using Godot;

namespace PawStorm;

/// <summary>Runs the player around a vertical loop at constant speed (arcade-style, forgiving).</summary>
public partial class LoopState : PlayerState
{
	private LoopTrack _track;
	private float _theta;
	private float _speed;

	public LoopState(PlayerController p) : base(p) { }

	public override void Enter()
	{
		_track = Player.CurrentLoop;
		_theta = 0f;
		_speed = Mathf.Max(Player.HorizontalSpeed, Stats.LoopMinSpeed);
	}

	public override void Physics(float dt)
	{
		var p = Player;
		_theta += _speed * dt / _track.Radius;
		if (_theta >= Mathf.Tau)
		{
			p.Velocity = _track.TangentAt(Mathf.Tau) * _speed;
			p.GlobalPosition = _track.PointAt(Mathf.Tau);
			Machine.Transition(PlayerStateId.Fall);
			return;
		}
		p.GlobalPosition = _track.PointAt(_theta);
		p.Velocity = _track.TangentAt(_theta) * _speed;
	}
}
