using Godot;

namespace PawStorm;

/// <summary>Storm ambience: now and then the sun light flashes and thunder rumbles.</summary>
public partial class LightningFlash : Node
{
	[Export] public float MinInterval { get; set; } = 5f;
	[Export] public float MaxInterval { get; set; } = 11f;

	private DirectionalLight3D _light;
	private float _baseEnergy;
	private float _timer;

	public override void _Ready()
	{
		_light = GetParent().GetNodeOrNull<DirectionalLight3D>("Sun");
		_baseEnergy = _light?.LightEnergy ?? 1f;
		_timer = (float)GD.RandRange(MinInterval, MaxInterval);
	}

	public override void _Process(double delta)
	{
		_timer -= (float)delta;
		if (_timer > 0f) return;
		_timer = (float)GD.RandRange(MinInterval, MaxInterval);
		Strike();
	}

	private async void Strike()
	{
		if (_light == null) return;
		for (int i = 0; i < 3; i++)
		{
			_light.LightEnergy = _baseEnergy * 6f;
			await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
			if (!IsInsideTree()) return;
			_light.LightEnergy = _baseEnergy;
			await ToSignal(GetTree().CreateTimer(0.07), SceneTreeTimer.SignalName.Timeout);
			if (!IsInsideTree()) return;
		}
		Sfx.Play(this, "thunder", -4f);
		Vfx.Shake(this, 0.05f, 0.3f);
	}
}
