using Godot;
using System;
using System.Runtime.InteropServices;



public partial class Fluid : Node2D
{	
	private float gravity = 98f;
	private int particleCount = 100;
	private Vector2[] pPositions;
	private Vector2[] pVelocities;
	private Rect2 boundingBox;
	private float boxMargin = .8f;
	private int pSize = 5;
	private int particleSpacing = 2;
	private float damping = .6f;
	
	public static class Predef
	{
		public static readonly Color blue = new Color(0.65f, 0.85f, 0.173f);
		
	}
	
	
	public override void _Draw()
	{
		
		DrawRect(boundingBox, Colors.White, false, 2);
		
		for(int i = 0; i< pPositions.Length; i++){
			
			DrawCircle(pPositions[i], pSize, Predef.blue);
		}
		
	}
	
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//position node in center
		Rect2 viewport = GetViewportRect();
		Position = viewport.Size / 2;  
		//Create particle arrays depending on particleCount
		//Place the particles in a grid with spacing
		
		pPositions = new Vector2[particleCount];
		pVelocities = new Vector2[particleCount];
		
		int rowsParticles = (int)Math.Sqrt(particleCount);
		int colsParticles = (particleCount / rowsParticles) + ((particleCount % rowsParticles != 0) ? 1 : 0);
		float spacing = pSize * 2 + particleSpacing;
		
		for (int i = 0; i < particleCount; i++) {
			float x = (i % rowsParticles - rowsParticles / 2f + 0.5f) * spacing;
			float y = (i / colsParticles - colsParticles / 2f + 0.5f) * spacing;
			pPositions[i] = new Vector2(x,y);
		}
		
		// 
		
		//set dimensions of bounding box 
		boundingBox = new Rect2(
			(viewport.Position - (viewport.Size /2)) * boxMargin,
			viewport.Size * boxMargin
		);
		
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		for(int i = 0; i < pPositions.Length; i++) {
			//simulating gravity
			pVelocities[i].Y += gravity * (float)delta;
			pPositions[i] += pVelocities[i] * (float)delta;
			ResolveCollisions(ref pPositions[i], ref pVelocities[i]);
			
		}
		QueueRedraw();
	}
	
	public void ResolveCollisions(ref Vector2 pPosition, ref Vector2 pVelocity)
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
