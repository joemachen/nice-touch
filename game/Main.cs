using Godot;
using NiceTouch.Sim;

namespace NiceTouch;

// Draws the sim and feeds it ticks. No game rules live here.
public partial class Main : Node3D
{
    private const int Rings = 7;
    private const float FitMargin = 1.1f;

    private readonly Match _match = new();
    private Camera3D _camera = null!;
    private Label _tickLabel = null!;

    public override void _Ready()
    {
        // The sim owns the tick rate. Godot's fixed tick follows it.
        Engine.PhysicsTicksPerSecond = Tuning.TicksPerSecond;

        _camera = GetNode<Camera3D>("Camera");
        _tickLabel = GetNode<Label>("Hud/TickLabel");
        GetNode<MeshInstance3D>("Tunnel").Mesh = BuildTunnelMesh();

        GetViewport().SizeChanged += FitCameraToTunnelFace;
        FitCameraToTunnelFace();
    }

    public override void _PhysicsProcess(double delta) => _match.Step();

    public override void _Process(double delta) => _tickLabel.Text = $"sim tick {_match.Tick}";

    // The boot smoke test in test.bat reads this line.
    public override void _ExitTree() => GD.Print($"sim ticks at exit: {_match.Tick}");

    // Pulls the camera back until the whole near face of the tunnel fits,
    // whatever shape the window is.
    private void FitCameraToTunnelFace()
    {
        Vector2 size = GetViewport().GetVisibleRect().Size;
        if (size.X <= 0 || size.Y <= 0)
            return;

        float tanY = Mathf.Tan(Mathf.DegToRad(_camera.Fov) / 2);
        float tanX = tanY * size.X / size.Y;
        float distance = Mathf.Max(Tuning.TunnelHeight / 2 / tanY, Tuning.TunnelWidth / 2 / tanX);
        _camera.Position = new Vector3(0, 0, distance * FitMargin);
    }

    // A wireframe: rectangles at even depths, joined along the four corners.
    // The player's end is at z = 0 and the tunnel runs away along -z.
    private static ImmediateMesh BuildTunnelMesh()
    {
        float w = Tuning.TunnelWidth / 2;
        float h = Tuning.TunnelHeight / 2;
        Vector2[] corners = [new(-w, -h), new(w, -h), new(w, h), new(-w, h)];

        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.35f, 0.9f, 0.6f),
        };

        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, material);
        for (int ring = 0; ring < Rings; ring++)
        {
            float z = -Tuning.TunnelDepth * ring / (Rings - 1);
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % corners.Length];
                mesh.SurfaceAddVertex(new Vector3(a.X, a.Y, z));
                mesh.SurfaceAddVertex(new Vector3(b.X, b.Y, z));
            }
        }
        foreach (Vector2 c in corners)
        {
            mesh.SurfaceAddVertex(new Vector3(c.X, c.Y, 0));
            mesh.SurfaceAddVertex(new Vector3(c.X, c.Y, -Tuning.TunnelDepth));
        }
        mesh.SurfaceEnd();
        return mesh;
    }
}
