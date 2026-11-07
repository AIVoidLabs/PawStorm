using Godot;

namespace PawStorm;

/// <summary>Rail/wire the player can grind on. Straight segments between <see cref="Points"/> (local space).</summary>
public partial class GrindRail : Path3D
{
	[Export] public Vector3[] Points { get; set; } = System.Array.Empty<Vector3>();
	[Export] public float Radius { get; set; } = 0.08f;

	public float Length => Curve.GetBakedLength();

	public override void _Ready()
	{
		AddToGroup("grind_rail");
		var c = new Curve3D();
		foreach (var p in Points) c.AddPoint(p);
		Curve = c;
		BuildVisual();
	}

	public Vector3 WorldPointAt(float offset) => ToGlobal(Curve.SampleBaked(Mathf.Clamp(offset, 0f, Length)));

	public Vector3 WorldTangentAt(float offset)
		=> (WorldPointAt(offset + 0.25f) - WorldPointAt(offset - 0.25f)).Normalized();

	public float ClosestOffset(Vector3 world) => Curve.GetClosestOffset(ToLocal(world));

	private void BuildVisual()
	{
		var mat = new StandardMaterial3D { DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon, SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled, AlbedoColor = new Color(0.75f, 0.78f, 0.85f), Metallic = 0.6f };
		for (int i = 0; i < Points.Length - 1; i++)
		{
			Vector3 a = Points[i], b = Points[i + 1];
			Vector3 dir = b - a;
			if (dir.LengthSquared() < 0.0001f) continue;
			var mi = new MeshInstance3D
			{
				Mesh = new CylinderMesh { TopRadius = Radius, BottomRadius = Radius, Height = dir.Length(), Material = mat },
				Position = (a + b) * 0.5f,
				Quaternion = new Quaternion(Vector3.Up, dir.Normalized()),
			};
			AddChild(mi);
		}
	}
}
