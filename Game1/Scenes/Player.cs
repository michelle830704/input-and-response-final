using Godot;

// Body stays unrotated. Only GFX turns and scales; collider changes separately.
public partial class Player : CharacterBody3D
{
	public enum MovementMode { Immediate, Linear, Ease }
	[Export] public MovementMode Mode { get; set; } = MovementMode.Linear;
	[Export] public float Speed { get; set; } = 5f;
	[Export] private MeshInstance3D _mesh;
	[Export] public float Acceleration { get; set; } = 8f;
	[Export] public float Deceleration { get; set; } = 24f;
	[Export] public float EaseDuration { get; set; } = 0.8f;
	[Export] public float TurnSpeed { get; set; } = 12f;
	[Export] public float JumpSpeed { get; set; } = 7f;
	[Export] public float Gravity { get; set; } = 20f;
	[Export] public float HoverDuration { get; set; } = 0.18f;
	[Export] public float CoyoteDuration { get; set; } = 0.12f;
	[Export] public float AnticipationWait { get; set; } = 0.03f;
	[Export] public float SquashDuration { get; set; } = 0.06f;
	[Export] public float DuckSpeed { get; set; } = 2.5f;
	private Node3D gfx;
	private CollisionShape3D collider;
	private CapsuleShape3D capsule;
	private Camera3D camera;
	private float yaw, coyote, anticipation = -1, easeTime, hoverLeft;
	private bool duck, jumpActive, hovering, hadInput;
	private float stretchLeft;
	private Vector3 spawn, originalGfxScale, originalGfxPosition;
	private float standingHeight, radius;
	private Vector3 standingColliderPosition;
	public bool Ducking => duck;

	public override void _Ready()
	{
		// Reuse the original scene, meshes and materials.
		gfx = GetNode<Node3D>("GFX");
		collider = GetNode<CollisionShape3D>("CollisionShape3D");
		capsule = (CapsuleShape3D)collider.Shape.Duplicate();
		collider.Shape = capsule;
		standingHeight = capsule.Height; radius = capsule.Radius;
		standingColliderPosition = collider.Position;
		originalGfxScale = gfx.Scale; originalGfxPosition = gfx.Position;
		spawn = GlobalPosition;
		FloorSnapLength = 0.15f;
		camera = GetParent().GetNode<Camera3D>("Camera3D");
		camera.Near = 0.1f; camera.Current = true;
		Bind("mode1", Key.Key1); Bind("mode2", Key.Key2); Bind("mode3", Key.Key3);
		Bind("reset", Key.R); Bind("orbit_left", Key.Q); Bind("orbit_right", Key.E);
		Bind("hover_less", Key.Bracketleft); Bind("hover_more", Key.Bracketright);
		UpdateCamera();
	}
	private static void Bind(string name, Key key)
	{
		if (!InputMap.HasAction(name)) InputMap.AddAction(name);
		InputMap.ActionAddEvent(name, new InputEventKey { PhysicalKeycode = key });
	}
	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		if (Input.IsActionJustPressed("mode1")) { Mode = MovementMode.Immediate; easeTime = 0; }
		if (Input.IsActionJustPressed("mode2")) { Mode = MovementMode.Linear; easeTime = 0; }
		if (Input.IsActionJustPressed("mode3")) { Mode = MovementMode.Ease; easeTime = 0; }
		if (Input.IsActionJustPressed("reset") || GlobalPosition.Y < -8) ResetPlayer();
		if (Input.IsActionJustPressed("hover_less")) HoverDuration = Mathf.Max(0, HoverDuration - 0.05f);
		if (Input.IsActionJustPressed("hover_more")) HoverDuration = Mathf.Min(1, HoverDuration + 0.05f);
		yaw += (Input.GetActionStrength("orbit_left") - Input.GetActionStrength("orbit_right")) * dt * 1.8f;
		bool grounded = IsOnFloor();
		coyote = grounded && !jumpActive ? CoyoteDuration : Mathf.Max(0, coyote - dt);
		bool wantDuck = Input.IsActionPressed("crouch");
		if (wantDuck) SetDuck(true);
		else if (duck && CanStand()) SetDuck(false);

		Vector2 input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		Vector3 direction = new Basis(Vector3.Up, yaw) * new Vector3(input.X, 0, input.Y);
		float speed = duck ? DuckSpeed : Speed;
		Vector3 horizontal = new Vector3(Velocity.X, 0, Velocity.Z);
		Vector3 target = direction * speed;
		if (Mode == MovementMode.Immediate) horizontal = target;
		else if (Mode == MovementMode.Linear || input.LengthSquared() < 0.001f)
		{
			horizontal = horizontal.MoveToward(target, (input.LengthSquared() > 0 ? Acceleration : Deceleration) * dt);
			easeTime = 0;
		}
		else
		{
			// Smoothstep easing from standstill; steering at speed is linear.
			if (!hadInput) easeTime = 0;
			easeTime = Mathf.Min(easeTime + dt, Mathf.Max(EaseDuration, 0.01f));
			float t = easeTime / Mathf.Max(EaseDuration, 0.01f);
			float easedSpeed = speed * t * t * (3 - 2 * t);
			horizontal = horizontal.MoveToward(target, Acceleration * dt);
			if (horizontal.Length() > easedSpeed) horizontal = horizontal.Normalized() * easedSpeed;
		}
		hadInput = input.LengthSquared() > 0.001f;
		float y = Velocity.Y;
		if (grounded && y <= 0) { y = -0.1f; jumpActive = false; hovering = false; }
		if (Input.IsActionJustPressed("jump") && coyote > 0 && anticipation < 0 && !duck)
		{
			anticipation = 0; coyote = 0;
		}
		if (anticipation >= 0)
		{
			anticipation += dt;
			if (anticipation >= AnticipationWait + SquashDuration)
			{
				y = JumpSpeed; anticipation = -1; jumpActive = true;
				hoverLeft = HoverDuration; hovering = false; stretchLeft = 0.12f;
			}
		}
		// Hold vertical speed at zero at apex; duration independent of gravity.
		if (!grounded || jumpActive)
		{
			if (jumpActive && !hovering && y > 0 && y - Gravity * dt <= 0) hovering = true;
			if (hovering && hoverLeft > 0) { y = 0; hoverLeft = Mathf.Max(0, hoverLeft - dt); }
			else y -= Gravity * dt;
		}
		Velocity = new Vector3(horizontal.X, y, horizontal.Z);
		MoveAndSlide();
		if (IsOnCeiling()) { hovering = false; hoverLeft = 0; }
		Vector3 actual = new Vector3(Velocity.X, 0, Velocity.Z);
		if (actual.LengthSquared() > 0.01f)
			gfx.Rotation = new Vector3(0, Mathf.LerpAngle(gfx.Rotation.Y, Mathf.Atan2(actual.X, actual.Z), 1 - Mathf.Exp(-TurnSpeed * dt)), 0);
		stretchLeft = Mathf.Max(0, stretchLeft - dt);
		float scaleY = duck ? 0.5f : stretchLeft > 0 ? 1.2f : 1;
		if (anticipation >= AnticipationWait)
		{
			float t = Mathf.Clamp((anticipation - AnticipationWait) / Mathf.Max(SquashDuration, 0.001f), 0, 1);
			scaleY = Mathf.Lerp(1, 0.65f, t);
		}
		gfx.Scale = originalGfxScale * new Vector3(1, scaleY, 1);
		// Keep feet fixed while scaling a mesh whose origin is at its centre.
		gfx.Position = originalGfxPosition + Vector3.Up * (standingHeight * (scaleY - 1) / 2);
		UpdateCamera();
	}
	private void SetDuck(bool value)
	{
		duck = value; capsule.Height = value ? Mathf.Max(radius * 2, standingHeight * 0.5f) : standingHeight;
		collider.Position = standingColliderPosition + Vector3.Up * ((capsule.Height - standingHeight) / 2);
	}
	private bool CanStand()
	{
		var query = new PhysicsShapeQueryParameters3D {
			Shape = new CapsuleShape3D { Radius = radius - 0.01f, Height = standingHeight - 0.02f },
			Transform = new Transform3D(Basis.Identity, GlobalPosition + standingColliderPosition + Vector3.Up * 0.02f),
			CollisionMask = CollisionMask,
			Exclude = new Godot.Collections.Array<Rid> { GetRid() }
		};
		return GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
	}
	private void UpdateCamera()
	{
		Vector3 focus = GlobalPosition + Vector3.Up * 0.4f;
		Vector3 desired = focus + new Basis(Vector3.Up, yaw) * new Vector3(0, 3.2f, 6);
		var ray = PhysicsRayQueryParameters3D.Create(focus, desired, CollisionMask);
		ray.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
		var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
		camera.GlobalPosition = hit.Count > 0 ? (Vector3)hit["position"] + ((Vector3)hit["normal"]) * 0.2f : desired;
		camera.LookAt(focus, Vector3.Up);
	}
	private void ResetPlayer()
	{
		GlobalPosition = spawn; Velocity = Vector3.Zero;
		anticipation = -1; jumpActive = false; hovering = false; coyote = 0; easeTime = 0;
		stretchLeft = 0; SetDuck(false);
	}
}
