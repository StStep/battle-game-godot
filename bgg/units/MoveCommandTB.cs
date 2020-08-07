using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class MoveCommandTB : Control
{
    Plot posPlot;
    Plot velPlot;
    MoveAnimator moveAnim;
    CheckButton playToggle;
    Label tLabel;
    Slider tSlider;
    MobilityEditor mobEditor;
    LineEditWrapper<Single> leDesiredRot;
    LineEditWrapper<Single> lePeriod;
    LineEditWrapper<Single> leDelta;

    Vector2 rangeT;
    float curT;
    Boolean playing;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        posPlot = GetNode<Plot>("PositionPlot");
        velPlot = GetNode<Plot>("VelocityPlot");
        moveAnim = GetNode<MoveAnimator>("MoveAnimator");
        playToggle = GetNode<CheckButton>("PlaybackBox/PlayToggle");
        tLabel = GetNode<Label>("PlaybackBox/CurrentT");
        tSlider = GetNode<Slider>("PlaybackBox/TimeSlider");
        mobEditor = GetNode<MobilityEditor>("MobilityEditor");

        lePeriod = new LineEditWrapper<Single>(GetNode<LineEdit>("MiscFields/lePeriod"), 4f, "0.00");
        lePeriod.ValueChanged = (v) => { lePeriod.LineEdit.Modulate = Colors.Red; };

        leDelta = new LineEditWrapper<Single>(GetNode<LineEdit>("MiscFields/leDelta"), 0.04f, "0.00");
        leDelta.ValueChanged = (v) => { leDelta.LineEdit.Modulate = Colors.Red; };

        // Hook up Desired Rotation and restrict to radians
        leDesiredRot = new LineEditWrapper<Single>(GetNode<LineEdit>("MoveTabs/Rotation/Parameters/leDrot"), 3*Mathf.Pi/2f, "0.###");
        leDesiredRot.ValueChanged = (v) => { if (v < 0f || v > Mathf.Tau) leDesiredRot.SetValue(Mathf.Wrap(v, 0f, Mathf.Tau)); };
        leDesiredRot.ValueChanged += (v) => { leDesiredRot.LineEdit.Modulate = Colors.Red; };

        playToggle.Connect("toggled", this, nameof(PlayPause));

        // Start with Rotation Plot
        PlotRotation();
    }

    public override void _Process(float delta)
    {
        if (playing)
        {
            SetT(curT + delta);
        }
    }

    public void PlayPause(bool playing)
    {
        this.playing = playing;
        tSlider.Editable = !playing;
        if (playToggle.Pressed != playing)
            playToggle.Pressed = playing;
    }

    public void OnSliderSet(float value)
    {
        if (playing)
            return;

        SetT(value/100f);
    }

    public void SetT(float t)
    {
        curT = t > rangeT[1] ? rangeT[0] : t;
        velPlot.SetCurrent(curT, rangeT);
        posPlot.SetCurrent(curT, rangeT);
        tLabel.Text = $"{curT:0.00}/{rangeT[1]:0.00} sec";
        tSlider.Value = t * 100f;
        tSlider.MinValue = rangeT[0] * 100f;
        tSlider.MaxValue = rangeT[1] * 100f;
        moveAnim.SetT(curT);
    }

    public void PlotRotation()
    {
        if (playing)
        {
            PlayPause(false);
        }

        var u = new MoveUnit();
        var yrange = new Vector2(0f, 2 * Mathf.Pi);
        var xrange = new Vector2(0f, lePeriod.Value);
        var init = new MovementState()
        {
            Position = new Vector2(250f, 250f),
            Rotation = 0f,
            RotVelocity = 0f,
            Velocity = new Vector2(0f, 0f)
        };
        try
        {
            var testState = MoveCommand.MakeRotation(lePeriod.Value, mobEditor.Mobility, init, leDesiredRot.Value, leDelta.Value);
            mobEditor.ClearMarks();

            // Log rotation values
            System.IO.Directory.CreateDirectory(".logs");
            using(var w = new StreamWriter(".logs/Rotation.csv"))
            {
                w.WriteLine("time|position|rotation|velocity|rotational velocity");
                testState.Preview.ToList().ForEach(x => w.WriteLine($"{x.Item1}|{x.Item2.Position}|{x.Item2.Rotation}|{x.Item2.Velocity}|{x.Item2.RotVelocity}"));
            }

            rangeT = xrange;
            GD.Print($"{testState.Preview.Count()} Entries, ends at Rot: {testState.Final.Rotation} Vrot: {testState.Final.RotVelocity} t: {testState.Preview.Last().Item1}");

            velPlot.SetTarget(0f, new Vector2(-Mathf.Pi, Mathf.Pi));
            velPlot.SetGrid(leDelta.Value, Mathf.Pi/4f, xrange, yrange);
            velPlot.SetPlot("Rotating Body Velocity", testState.Preview.Select(p => new Vector2(p.Item1, p.Item2.RotVelocity)), xrange, new Vector2(-Mathf.Pi, Mathf.Pi), "Time (s)", "Rot. Velocity\n(rad/s)");
            posPlot.SetTarget(leDesiredRot.Value, yrange);
            posPlot.SetGrid(leDelta.Value, Mathf.Pi/4f, xrange, yrange);
            posPlot.SetPlot("Rotating Body Rotation", testState.Preview.Select(p => new Vector2(p.Item1, p.Item2.Rotation)), xrange, yrange, "Time (s)", "Rotation (rad)");

            moveAnim.SetMove(testState, leDelta.Value, rangeT);

            SetT(rangeT[0]);

            lePeriod.LineEdit.Modulate = Colors.White;
            leDelta.LineEdit.Modulate = Colors.White;
            leDesiredRot.LineEdit.Modulate = Colors.White;
        }
        catch(Exception ex)
        {
            velPlot.Error(ex.Message);
            posPlot.Error(ex.Message);
        }
    }
}
