using Godot;
using System;
using System.Runtime.InteropServices;


[StructLayout(LayoutKind.Sequential)]
[StructLayout(LayoutKind.Sequential, Size=44)]
public struct Particle {
	public float density;
	public float pressure;
	public Vector3 currentForce;
	public Vector3 velocity;
	public Vector3 position;
}


public partial class Fluid : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
