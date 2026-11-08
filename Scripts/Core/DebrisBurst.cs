using Godot;

namespace PawStorm;

/// <summary>
/// One-shot burst of small meshes flying out on ballistic arcs, shrinking and fading.
/// Simple and deterministic (no GPU/CPU particle system involved).
/// </summary>
public partial class DebrisBurst : Node3D
{
	private MeshInstance3D[] _pieces;
	private Vector3[] _velocity;
	private Vector3[] _spin;
	private float[] _baseScale;
	private StandardMaterial3D _material;
	private Color _from, _to;
	private float _life, _age, _gravity;

	public static DebrisBurst Create(int count, float life, float speedMin, float speedMax, float size, Color from, Color to, float gravityY, bool cube)
	{
		var b = new DebrisBurst { _life = life, _from = from, _to = to, _gravity = gravityY };
		b._material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded, AlbedoColor = from,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
		};
		Mesh mesh = cube
			? new BoxMesh { Size = Vector3.One * size * 2f, Material = b._material }
			: new SphereMesh { Radius = size, Height = size * 2f, RadialSegments = 8, Rings = 4, Material = b._material };
		b._pieces = new MeshInstance3D[count];
		b._velocity = new Vector3[count];
		b._spin = new Vector3[count];
		b._baseScale = new float[count];
		for (int i = 0; i < count; i++)
		{
			var dir = new Vector3(GD.Randf() * 2f - 1f, GD.Randf() * 1.2f - 0.1f, GD.Randf() * 2f - 1f).Normalized();
			b._velocity[i] = dir * (speedMin + GD.Randf() * (speedMax - speedMin));
			b._spin[i] = new Vector3(GD.Randf() - 0.5f, GD.Randf() - 0.5f, GD.Randf() - 0.5f) * 14f;
			b._baseScale[i] = 0.6f + GD.Randf() * 0.8f;
			var mi = new MeshInstance3D { Mesh = mesh, Position = dir * 0.15f, Scale = Vector3.One * b._baseScale[i] };
			b._pieces[i] = mi;
			b.AddChild(mi);
		}
		return b;
	}

	public override void _Process(double delta)
	{
		_age += (float)delta;
		float t = Mathf.Clamp(_age / _life, 0f, 1f);
		for (int i = 0; i < _pieces.Length; i++)
		{
			var p = _pieces[i];
			p.Position += (_velocity[i] + new Vector3(0, _gravity * _age * 0.5f, 0)) * (float)delta;
			p.Rotation += _spin[i] * (float)delta;
			p.Scale = Vector3.One * _baseScale[i] * (1f - t * t);
		}
		var c = _from.Lerp(_to, t);
		c.A = 1f - Mathf.Clamp((t - 0.55f) / 0.45f, 0f, 1f);
		_material.AlbedoColor = c;
		if (t >= 1f) QueueFree();
	}
}
