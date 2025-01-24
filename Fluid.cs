using Godot;
using System;
using System.Runtime.InteropServices;



public partial class Fluid : Node2D
{	
	public float gravity;
	Vector2 Pposition;
	Vector2 Pvelocity;
	
	public static class Predef
	{
		public static readonly Color blue = new Color(0.65f, 0.85f, 0.173f);
	}
	
	
	public override void _Draw()
	{
		DrawCircle(Pposition, 5, Predef.blue);
		
	
	}
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
		Position = GetViewportRect().Size / 2;
		
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Pvelocity.Y += gravity * (float)delta;
		Pposition += Pvelocity * (float)delta;
		QueueRedraw();
	}
	
	
}
