using Godot;

namespace PawStorm;

/// <summary>
/// Side barrier along local -Z: repeats a modelled railing piece and adds an invisible tall wall
/// (collision layer 2, which the player collides with but the camera arm ignores) so the player cannot fall off the edge.
/// </summary>
public partial class BarrierLine : Node3D
{
	public const uint BarrierLayer = 2;

	[Export] public PackedScene Piece { get; set; }
	[Export] public float Length { get; set; } = 10f;
	[Export] public float PieceLength { get; set; } = 4f;
	[Export] public float WallHeight { get; set; } = 8f;

	public override void _Ready()
	{
		// invisible collision wall
		var body = new StaticBody3D { CollisionLayer = BarrierLayer, CollisionMask = 0 };
		var shape = new CollisionShape3D
		{
			Shape = new BoxShape3D { Size = new Vector3(0.6f, WallHeight, Length) },
			Position = new Vector3(0, WallHeight / 2f, -Length / 2f),
		};
		body.AddChild(shape);
		AddChild(body);

		// visible railing
		Mesh mesh = ToonModel.ToonMesh(Piece);
		if (mesh == null) return;
		int n = Mathf.Max(1, Mathf.RoundToInt(Length / PieceLength));
		float step = Length / n;
		var mm = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh, InstanceCount = n };
		for (int i = 0; i < n; i++)
		{
			var xf = new Transform3D(Basis.Identity.Scaled(new Vector3(1, 1, step / PieceLength)), new Vector3(0, 0, -(i + 0.5f) * step));
			mm.SetInstanceTransform(i, xf);
		}
		AddChild(new MultiMeshInstance3D { Multimesh = mm });
	}
}
