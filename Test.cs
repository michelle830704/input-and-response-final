using Godot;
using System;

public partial class Test : CharacterBody3D
{
     [Export]
    public int Speed { get; set; } = 14;

    [Export]
    public int FallAcceleration { get; set; } = 75;

    private Vector3 _targetVelocity = Vector3.Zero;

    Something s = new Something();

    public void something(){
        GD.Print("Test");
    }

    public override void _Process(double delta)
    {
        something();

        this.Velocity = Vector3.Forward;

        this.MoveAndSlide();

    }

}


public class Something{
    float x;
}