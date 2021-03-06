using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Battlefield : Node
{
    private PackedScene _deployUnitScene = GD.Load<PackedScene>("res://units/DragUnit.tscn");
    private PackedScene _moveUnitScene = GD.Load<PackedScene>("res://units/MoveUnit.tscn");
    private PackedScene _actionUnitScene = GD.Load<PackedScene>("res://units/ActionUnit.tscn");

    private Boolean _deploying = true;
    private Boolean _acting = false;
    private List<DragUnit> _deployUnits = new List<DragUnit>();
    private List<MoveUnit> _moveUnits = new List<MoveUnit>();
    private List<ActionUnit> _actionUnits = new List<ActionUnit>();
    private Area2D _deployZone;
    private Area2D _enemyDeployZone;
    private Area2D _battleZone;
    private Area2D _outBoundsZone;
    private Area2D _neutralZone;
    private Node2D _deployMarker;
    private Node2D _enemyDeployMarker;
    private List<Area2D> _terrain;

    public Action<Boolean> ValidityChanged;
    public Boolean IsValid => (_deploying) ? !_deployUnits.Any(u => !u.Valid) : !_moveUnits.Any(u => !u.Valid);

    private Boolean __busy = false;
    public Action<Boolean> BusyChanged;
    public Boolean Busy
    {
        get => __busy;
        private set
        {
            if (__busy != value)
            {
                __busy = value;
                BusyChanged?.Invoke(__busy);
            }
        }
    }

    public Action ActingDone;
    public float TurnPeriod { get; set; } = 10f;
    public float CurrentTime { get; private set; } = 0f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        base._Ready();
        _deployZone = GetNode<Area2D>("DeployZone");
        _enemyDeployZone = GetNode<Area2D>("EnemyDeployZone");
        _battleZone = GetNode<Area2D>("BattleZone");
        _outBoundsZone = GetNode<Area2D>("OutBoundsZone");
        _neutralZone = GetNode<Area2D>("NeutralZone");
        _deployMarker = GetNode<Node2D>("DeployMarker");
        _enemyDeployMarker = GetNode<Node2D>("EnemyDeployMarker");
        _terrain = GetNode("Terrain").GetChildren().Cast<Area2D>().ToList();
        _deployMarker.Visible = true;
        _enemyDeployMarker.Visible = true;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (!_acting)
            return;

        foreach(var u in _actionUnits)
        {
            u.Act(delta);
        }

        CurrentTime += delta;
        if (CurrentTime >= TurnPeriod)
        {
            _acting = false;
            ActingDone?.Invoke();
        }
    }

    public void DeployUnit(String type)
    {
        Busy = true;
        var u = _deployUnitScene.Instance() as DragUnit;
        GetNode("Units").AddChild(u);
        u.CanDrag = true;
        u.Dragging = true;
        u.Picked += PickedUnit;
        u.Placed += PlacedUnit;
        u.Moved += ValidateDeploy;
        _deployUnits.Add(u);
    }

    public void Deploy2Move()
    {
        var selMan = GetNode<SelectManager>("Units");
        _deployMarker.Visible = false;
        _enemyDeployMarker.Visible = false;
        foreach (var u in _deployUnits)
        {
            var newUnit = ConstructMoveUnit(selMan, u);
            _moveUnits.Add(newUnit);
            GetNode("Units").AddChild(newUnit);
            u.QueueFree();
        }
        _deployUnits.Clear();
        _deploying = false;
    }

    public void ActTurn()
    {
        foreach (var u in _moveUnits)
        {
            var newUnit = _actionUnitScene.Instance() as ActionUnit;
            newUnit.Restart(u.Commands);
            _actionUnits.Add(newUnit);
            GetNode("Units").AddChild(newUnit);
            u.QueueFree();
        }
        _moveUnits.Clear();
        _acting = true;
        CurrentTime = 0;
    }

    public void NewTurn()
    {
        var selMan = GetNode<SelectManager>("Units");
        foreach (var u in _actionUnits)
        {
            var newUnit = ConstructMoveUnit(selMan, u);
            _moveUnits.Add(newUnit);
            GetNode("Units").AddChild(newUnit);
            u.QueueFree();
        }
        _actionUnits.Clear();
    }

    private MoveUnit ConstructMoveUnit(SelectManager selMan, Node2D at)
    {
        var newUnit = _moveUnitScene.Instance() as MoveUnit;
        newUnit.SelectManager = selMan;
        newUnit.GlobalPosition = at.GlobalPosition;
        newUnit.GlobalRotation = at.GlobalRotation;
        newUnit.Moved += ValidateMove;
        newUnit.BusyChanged += _ => CheckBusyUnits();
        return newUnit;
    }

    private void PickedUnit(DragUnit unit)
    {
        Busy = true;
        _deployUnits.ForEach(u => u.CanDrag = false);
        unit.CanDrag = true;
    }

    private void PlacedUnit(DragUnit unit)
    {
        _deployUnits.ForEach(u => u.CanDrag = true);
        Busy = false;
    }

    private void ValidateDeploy(IUnit unit)
    {
        var bvalid = unit.Valid;
        if (!unit.OverlapsArea(_deployZone) ||
            unit.OverlapsArea(_outBoundsZone) ||
            unit.OverlapsArea(_neutralZone) ||
            _terrain.Any(t => unit.OverlapsArea(t)))
        {
            unit.Modulate = Colors.Red;
            unit.Valid = false;
        }
        else
        {
            unit.Modulate = Colors.White;
            unit.Valid = true;
        }

        if (bvalid != unit.Valid)
            ValidityChanged?.Invoke(this.IsValid);
    }

    private void ValidateMove(IUnit unit)
    {
        var bvalid = unit.Valid;
        if (unit.OverlapsArea(_outBoundsZone) ||
            _terrain.Any(t => unit.OverlapsArea(t)))
        {
            unit.Modulate = Colors.Red;
            unit.Valid = false;
        }
        else
        {
            unit.Modulate = Colors.White;
            unit.Valid = true;
        }

        if (bvalid != unit.Valid)
            ValidityChanged?.Invoke(this.IsValid);
    }

    private void CheckBusyUnits()
    {
        Busy = _moveUnits.Any(u => u.IsBusy);
    }
}
