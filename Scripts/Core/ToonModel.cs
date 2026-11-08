using System.Collections.Generic;
using Godot;

namespace PawStorm;

/// <summary>
/// Put on an imported .glb instance: gives every mesh toon shading and the cartoon outline
/// (inverted-hull overlay), without touching the .glb itself.
/// </summary>
public partial class ToonModel : Node3D
{
	[Export] public bool Outline { get; set; } = true;

	private static readonly Dictionary<Material, Material> ToonCache = new();
	private static Material _outline;

	public override void _Ready()
	{
		_outline ??= GD.Load<Material>("res://Art/Materials/Outline.tres");
		Apply(this);
	}

	private void Apply(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is MeshInstance3D mi && mi.Mesh != null)
			{
				if (Outline) mi.MaterialOverlay = _outline;
				for (int s = 0; s < mi.Mesh.GetSurfaceCount(); s++)
				{
					if (mi.Mesh.SurfaceGetMaterial(s) is not BaseMaterial3D src) continue;
					if (!ToonCache.TryGetValue(src, out var toon))
					{
						var d = (BaseMaterial3D)src.Duplicate();
						d.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon;
						d.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
						ToonCache[src] = toon = d;
					}
					mi.SetSurfaceOverrideMaterial(s, toon);
				}
			}
			Apply(child);
		}
	}

	private static readonly Dictionary<string, Mesh> MeshCache = new();

	/// <summary>
	/// The (single) mesh of a static .glb with toon materials, for MultiMesh use (tiles, barriers).
	/// No outline: repeated pieces would draw a grid of lines.
	/// </summary>
	public static Mesh ToonMesh(PackedScene scene)
	{
		if (scene == null) return null;
		if (MeshCache.TryGetValue(scene.ResourcePath, out var cached)) return cached;
		var inst = scene.Instantiate<Node3D>();
		Mesh found = null;
		foreach (var mi in Meshes(inst)) { found = mi.Mesh; break; }
		Mesh result = null;
		if (found != null)
		{
			result = (Mesh)found.Duplicate();
			for (int s = 0; s < result.GetSurfaceCount(); s++)
			{
				if (result.SurfaceGetMaterial(s) is not BaseMaterial3D src) continue;
				if (!ToonCache.TryGetValue(src, out var toon))
				{
					var d = (BaseMaterial3D)src.Duplicate();
					d.DiffuseMode = BaseMaterial3D.DiffuseModeEnum.Toon;
					d.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;
					ToonCache[src] = toon = d;
				}
				result.SurfaceSetMaterial(s, toon);
			}
		}
		inst.Free();
		MeshCache[scene.ResourcePath] = result;
		return result;
	}

	/// <summary>All mesh instances below <paramref name="root"/> (recursive).</summary>
	public static IEnumerable<MeshInstance3D> Meshes(Node root)
	{
		foreach (Node c in root.GetChildren())
		{
			if (c is MeshInstance3D m) yield return m;
			foreach (var sub in Meshes(c)) yield return sub;
		}
	}
}
