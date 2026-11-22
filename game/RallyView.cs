using Godot;
using NiceTouch.Sim;

namespace NiceTouch;

// Draws the rally: tunnel, ball, both paddles and the marker on the floor
// under the ball. It only draws. The sim's z runs away from the player, so
// it is mapped onto Godot's -z.
public partial class RallyView : Node3D
{
    private const int Rings = 7;

    // Everything that moves, as it stood at the end of one tick.
    private struct Pose
    {
        public Vector3 Ball;
        public Quaternion BallTurn;
        public Vector3 Player;
        public Vector3 Opponent;
    }

    private readonly MeshInstance3D _tunnel = new();
    private readonly MeshInstance3D _ball = new();
    private readonly MeshInstance3D _marker = new();
    private readonly Node3D _player = MakePaddle(new Color(0.45f, 0.8f, 1f), fill: 0.22f);
    private readonly Node3D _opponent = MakePaddle(new Color(1f, 0.5f, 0.4f), fill: 0.55f);

    private Tuning _tuning = new();
    private Pose _previous;
    private Pose _current;

    public override void _Ready()
    {
        _ball.Mesh = new SphereMesh { Radius = 1, Height = 2 };
        _ball.MaterialOverride = new ShaderMaterial { Shader = new Shader { Code = BallShader } };
        _marker.Mesh = new CylinderMesh { TopRadius = 1, BottomRadius = 1, Height = 0.02f };
        _marker.MaterialOverride = Flat(new Color(1f, 0.95f, 0.6f, 0.85f));

        foreach (Node3D node in new Node3D[] { _tunnel, _marker, _opponent, _ball, _player })
            AddChild(node);
    }

    // Starts drawing a new rally session, with whatever sizes its tuning has.
    public void Reset(Match match)
    {
        _tuning = match.Tuning;
        _tunnel.Mesh = BuildTunnelMesh(_tuning);
        _ball.Scale = Vector3.One * _tuning.BallRadius;
        _marker.Scale = new Vector3(_tuning.BallRadius, 1, _tuning.BallRadius);
        var paddleSize = new Vector3(_tuning.PaddleWidth, _tuning.PaddleHeight, 1);
        _player.Scale = paddleSize;
        _opponent.Scale = paddleSize;

        _current = new Pose { BallTurn = Quaternion.Identity };
        Take(match);
        Hold();
    }

    // Called once after every sim tick.
    public void Take(Match match)
    {
        _previous = _current;

        // The pattern on the ball turns exactly as fast as the sim's spin.
        // Spin is an axis in the sim's coordinates, and flipping z to get
        // Godot's flips the sign of its x and y parts.
        var spin = new Vector3(-match.BallSpin.X, -match.BallSpin.Y, 0);
        Quaternion turn = _previous.BallTurn;
        if (spin.Length() > 0)
            turn = (new Quaternion(spin.Normalized(), spin.Length() * Tuning.TickSeconds) * turn).Normalized();

        _current = new Pose
        {
            Ball = new Vector3(match.BallPosition.X, match.BallPosition.Y, -match.BallPosition.Z),
            BallTurn = turn,
            Player = new Vector3(match.PlayerPaddle.X, match.PlayerPaddle.Y, 0),
            Opponent = new Vector3(match.OpponentPaddle.X, match.OpponentPaddle.Y, -_tuning.TunnelDepth),
        };
    }

    // Stops smoothing towards the latest tick, for when the sim is paused.
    public void Hold() => _previous = _current;

    // Smooths between the last two ticks, so the picture is right on a
    // screen that refreshes faster than the sim ticks.
    public override void _Process(double delta)
    {
        float t = (float)Engine.GetPhysicsInterpolationFraction();
        Vector3 ball = _previous.Ball.Lerp(_current.Ball, t);

        _ball.Position = ball;
        _ball.Quaternion = _previous.BallTurn.Slerp(_current.BallTurn, t);
        _marker.Position = new Vector3(ball.X, -_tuning.TunnelHeight / 2 + 0.01f, ball.Z);
        _player.Position = _previous.Player.Lerp(_current.Player, t);
        _opponent.Position = _previous.Opponent.Lerp(_current.Opponent, t);
    }

    // Two tones in eight segments, so a turn about any axis shows.
    private const string BallShader = """
        shader_type spatial;
        render_mode unshaded;
        varying vec3 local;
        void vertex() {
            local = VERTEX;
        }
        void fragment() {
            float side = sign(local.x) * sign(local.y) * sign(local.z);
            ALBEDO = side > 0.0 ? vec3(1.0, 0.96, 0.85) : vec3(0.95, 0.3, 0.2);
        }
        """;

    private static StandardMaterial3D Flat(Color colour) => new()
    {
        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        AlbedoColor = colour,
        Transparency = colour.A < 1 ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
    };

    // A see-through rectangle with a bright edge, one unit square. The
    // player looks through their own paddle, so it must not hide the ball.
    private static Node3D MakePaddle(Color colour, float fill)
    {
        var paddle = new Node3D();
        paddle.AddChild(new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = Vector2.One },
            MaterialOverride = Flat(new Color(colour, fill)),
        });

        var edge = new ImmediateMesh();
        Vector3[] corners = [new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0), new(0.5f, 0.5f, 0), new(-0.5f, 0.5f, 0)];
        edge.SurfaceBegin(Mesh.PrimitiveType.LineStrip, Flat(colour));
        for (int i = 0; i <= corners.Length; i++)
            edge.SurfaceAddVertex(corners[i % corners.Length]);
        edge.SurfaceEnd();
        paddle.AddChild(new MeshInstance3D { Mesh = edge });
        return paddle;
    }

    // A wireframe: rectangles at even depths, joined along the four corners.
    private static ImmediateMesh BuildTunnelMesh(Tuning tuning)
    {
        float w = tuning.TunnelWidth / 2;
        float h = tuning.TunnelHeight / 2;
        Vector2[] corners = [new(-w, -h), new(w, -h), new(w, h), new(-w, h)];

        var mesh = new ImmediateMesh();
        mesh.SurfaceBegin(Mesh.PrimitiveType.Lines, Flat(new Color(0.35f, 0.9f, 0.6f)));
        for (int ring = 0; ring < Rings; ring++)
        {
            float z = -tuning.TunnelDepth * ring / (Rings - 1);
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
            mesh.SurfaceAddVertex(new Vector3(c.X, c.Y, -tuning.TunnelDepth));
        }
        mesh.SurfaceEnd();
        return mesh;
    }
}
