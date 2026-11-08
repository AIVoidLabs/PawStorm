using Godot;

namespace PawStorm;

/// <summary>Patrolling placeholder enemy. Hurts the player unless the player is attacking (Spin/Jump/Pounce).</summary>
public partial class Enemy : Area3D, IPounceable
{
	[Export] public Vector3 PatrolOffset { get; set; } = new Vector3(4, 0, 0);
	[Export] public float PatrolSpeed { get; set; } = 0.5f;
	[Export] public int FishReward { get; set; } = 2;

	private Vector3 _origin;
	private float _t;
	private bool _dead;

	public override void _Ready()
	{
		AddToGroup("pounce_target");
		new ModelAnimator(this).PlayRandomOffset("Walk", 1f);
		_origin = Position;
		_t = GD.Randf() * Mathf.Tau;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_dead) return;
		_t += (float)delta * PatrolSpeed * Mathf.Tau;
		Position = _origin + PatrolOffset * Mathf.Sin(_t);
		Rotation = new Vector3(0, Mathf.Atan2(-PatrolOffset.X * Mathf.Cos(_t), -PatrolOffset.Z * Mathf.Cos(_t)), 0);

		foreach (Node3D b in GetOverlappingBodies())
		{
			if (b is not PlayerController p) continue;
			if (p.IsAttacking)
			{
				if (!p.IsOnFloor() && p.Machine.CurrentId != PlayerStateId.Pounce)
				{
					var v = p.Velocity; v.Y = p.Stats.EnemyBounce; p.Velocity = v;
					p.ResetAirActions();
				}
				Hit();
				return;
			}
			p.TakeHit(GlobalPosition);
		}
	}

	public void Hit()
	{
		if (_dead) return;
		_dead = true;
		GameState.AddFish(FishReward);
		Sfx.Play(this, "hit");
		QueueFree();
	}
}
