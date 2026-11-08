using Godot;

namespace PawStorm;

public partial class FollowCamera : Node3D
{
	[Export] public PlayerController Target { get; set; }
	[Export] public SpringArm3D Arm { get; set; }
	[Export] public Camera3D Cam { get; set; }
	[Export] public Vector3 FocusOffset { get; set; } = new Vector3(0, 1.2f, 0);

	private PlayerStats Stats => Target.Stats;
	private float _yaw;
	private float _pitch = -0.25f;

	public override void _Ready()
	{
		TopLevel = true;
		AddToGroup("follow_camera");
		Arm.SpringLength = Stats.CameraDistance;
		Arm.AddExcludedObject(Target.GetRid());
		GlobalPosition = Target.GlobalPosition + FocusOffset;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	private float _shakeTime, _shakeAmp;

	/// <summary>Short camera shake (hits, explosions).</summary>
	public void Shake(float amplitude, float duration)
	{
		if (amplitude * duration < _shakeAmp * _shakeTime) return; // keep the stronger one
		_shakeAmp = amplitude;
		_shakeTime = duration;
	}

	/// <summary>Set the orbit angles directly (used by the menu-screenshot tool).</summary>
	public void SetView(float yaw, float pitch, float distance)
	{
		_yaw = yaw; _pitch = pitch;
		Stats.CameraDistance = distance;
		Arm.SpringLength = distance;
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseMotion m && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_yaw -= m.Relative.X * Stats.MouseSensitivity;
			_pitch -= m.Relative.Y * Stats.MouseSensitivity;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		Vector2 stick = Input.GetVector("cam_left", "cam_right", "cam_up", "cam_down");
		_yaw -= stick.X * Stats.StickSensitivity * dt;
		_pitch -= stick.Y * Stats.StickSensitivity * dt;
		_pitch = Mathf.Clamp(_pitch, -1.3f, 0.6f);

		Vector3 goal = Target.GlobalPosition + FocusOffset;
		GlobalPosition = GlobalPosition.Lerp(goal, 1f - Mathf.Exp(-Stats.CameraFollowSpeed * dt));
		Rotation = new Vector3(_pitch, _yaw, 0);

		if (_shakeTime > 0f)
		{
			_shakeTime -= dt;
			float k = _shakeAmp * Mathf.Clamp(_shakeTime * 6f, 0f, 1f);
			Cam.HOffset = (GD.Randf() * 2f - 1f) * k;
			Cam.VOffset = (GD.Randf() * 2f - 1f) * k;
		}
		else if (Cam.HOffset != 0f || Cam.VOffset != 0f) { Cam.HOffset = 0f; Cam.VOffset = 0f; }

		float t = Mathf.Clamp(Target.Speed / Stats.MaxSpeed, 0f, 1f);
		Cam.Fov = Mathf.Lerp(Cam.Fov, Mathf.Lerp(Stats.FovMin, Stats.FovMax, t), 1f - Mathf.Exp(-4f * dt));
		Arm.SpringLength = Mathf.Lerp(Arm.SpringLength, Stats.CameraDistance * (1f + 0.35f * t), 1f - Mathf.Exp(-3f * dt));
	}
}
