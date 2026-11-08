using Godot;

namespace PawStorm;

/// <summary>
/// Plasma ball fired by General Growl. It hurts the player, unless the player is attacking (spin / jump / pounce):
/// then it is REFLECTED back at the boss and hurts him instead. It is also a Pounce target, so Pounce sends it back.
/// </summary>
public partial class LaserBall : Area3D, IPounceable
{
	public Vector3 Velocity { get; set; }
	public GeneralGrowl Boss { get; set; }

	private bool _reflected;
	private bool _dead;
	private float _life = 8f;
	private float _age;
	private StandardMaterial3D _mat;
	private OmniLight3D _light;
	private MeshInstance3D _mesh;

	private static readonly Color Hostile = new(1f, 0.4f, 0.1f);
	private static readonly Color Friendly = new(0.3f, 0.95f, 1f);

	public static LaserBall Spawn(Node context, GeneralGrowl boss, Vector3 position, Vector3 velocity)
	{
		var ball = new LaserBall { Boss = boss, Velocity = velocity, Position = position };
		(context.GetTree().CurrentScene ?? context.GetTree().Root).AddChild(ball);
		ball.GlobalPosition = position;
		return ball;
	}

	public override void _Ready()
	{
		AddToGroup("pounce_target");
		AddChild(new CollisionShape3D { Shape = new SphereShape3D { Radius = 0.75f } });
		_mat = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = Hostile,
		};
		_mesh = new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.5f, Height = 1f, RadialSegments = 14, Rings = 8, Material = _mat } };
		AddChild(_mesh);
		// white-hot core
		AddChild(new MeshInstance3D
		{
			Mesh = new SphereMesh { Radius = 0.26f, Height = 0.52f, RadialSegments = 10, Rings = 6, Material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = Colors.White } },
		});
		_light = new OmniLight3D { LightColor = Hostile, OmniRange = 6f, LightEnergy = 2.5f };
		AddChild(_light);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_dead) return;
		float dt = (float)delta;
		_age += dt;
		GlobalPosition += Velocity * dt;
		float pulse = 1f + 0.12f * Mathf.Sin(_age * 30f);
		_mesh.Scale = Vector3.One * pulse;
		if (_age > _life) { Fizzle(); return; }

		if (!_reflected)
		{
			foreach (Node3D b in GetOverlappingBodies())
			{
				if (b is not PlayerController p) continue;
				if (p.IsAttacking) { Reflect(p); return; }
				p.TakeHit(GlobalPosition);
				Explode();
				return;
			}
		}
		else if (GodotObject.IsInstanceValid(Boss) && GlobalPosition.DistanceTo(Boss.ChestPosition) < 2.6f)
		{
			Boss.OnReflectedHit();
			Explode();
		}
	}

	private void Reflect(PlayerController player)
	{
		_reflected = true;
		RemoveFromGroup("pounce_target");
		float speed = Mathf.Max(Velocity.Length() * 1.6f, 22f);
		Vector3 target = GodotObject.IsInstanceValid(Boss) ? Boss.ChestPosition : GlobalPosition - Velocity;
		Velocity = (target - GlobalPosition).Normalized() * speed;
		_mat.AlbedoColor = Friendly;
		_light.LightColor = Friendly;
		_age = 0f;
		Sfx.Play(this, "reflect", -2f);
		Vfx.Flash(this, GlobalPosition, Friendly, 0.8f, 0.2f);
		Vfx.Popup(this, GlobalPosition + Vector3.Up * 1.2f, "REFLECT!", Friendly);
		Vfx.Shake(this, 0.08f, 0.15f);
		if (player != null && !player.IsOnFloor() && player.Machine.CurrentId != PlayerStateId.Pounce)
		{
			var v = player.Velocity; v.Y = player.Stats.EnemyBounce; player.Velocity = v;
			player.ResetAirActions();
		}
	}

	/// <summary>Called by the Pounce state when it reaches the ball.</summary>
	public void Hit()
	{
		if (!_reflected && !_dead) Reflect(null);
	}

	private void Explode()
	{
		if (_dead) return;
		_dead = true;
		Sfx.Play(this, "laser_hit", -3f, 0.1f);
		Vfx.Flash(this, GlobalPosition, _reflected ? Friendly : Hostile, 1.1f, 0.25f);
		Vfx.Ring(this, GlobalPosition, _reflected ? Friendly : Hostile, 3.5f, 0.35f);
		Vfx.Burst(this, GlobalPosition, 18, 0.5f, 3f, 8f, 0.1f, _reflected ? Friendly : Hostile, Colors.White, -3f);
		QueueFree();
	}

	private void Fizzle()
	{
		_dead = true;
		Vfx.Burst(this, GlobalPosition, 8, 0.4f, 1f, 3f, 0.08f, Hostile, Colors.White, 0f);
		QueueFree();
	}
}
