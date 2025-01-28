using Godot;
using System;
using System.Runtime.InteropServices;



public partial class Fluid : Node2D
{	
	public float gravity = 98f;
	Vector2 pPosition;
	Vector2 pVelocity;
	private Rect2 boundingBox;
	float boxMargin = .8f;
	int pSize = 5;
	float damping = .6f;
	
	public static class Predef
	{
		public static readonly Color blue = new Color(0.65f, 0.85f, 0.173f);
		
	}
	
	
	public override void _Draw()
	{
		DrawCircle(pPosition, pSize, Predef.blue);
		DrawRect(boundingBox, Colors.White, false, 2);
	
	}
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Rect2 viewport = GetViewportRect();
		Position = viewport.Size / 2;
		//set dimensions of bounding box 
		boundingBox = new Rect2(
			(viewport.Position - (viewport.Size /2)) * boxMargin,
			viewport.Size * boxMargin
		);
		//draw bounding box
		//pass bounding box to resolve collisions
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		pVelocity.Y += gravity * (float)delta;
		pPosition += pVelocity * (float)delta;
		ResolveCollisions();
		QueueRedraw();
	}
	
	public void ResolveCollisions()
	{
		Vector2 halfBounds = boundingBox.Size / 2 - new Vector2(1, 1) * pSize;
		//if position x or y is touches border make velocity negative
		if(Math.Abs(pPosition.X) > halfBounds.X) {
			pPosition.X = halfBounds.X * Math.Sign(pPosition.X);
			 pVelocity.X *= -1 * damping;
		}
		
		if(Math.Abs(pPosition.Y) > halfBounds.Y) {
			pPosition.Y = halfBounds.Y * Math.Sign(pPosition.Y);
			pVelocity.Y *= -1 * damping;
		}
		
	}
	
	
}
