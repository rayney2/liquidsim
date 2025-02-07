using Godot;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;



public partial class Fluid : Node2D
{	
	private float gravity = 98f;
	private int particleCount = 200;
	private Vector2[] pPositions;
	private Vector2[] pVelocities;
	private Rect2 boundingBox;
	private float boxMargin = .8f;
	private int pSize = 5;
	private int particleSpacing = 3;
	private float damping = .95f;
	//approx 4x particle size
	private float smoothingRadius = 20f;
	private float mass = 1000f;
	
	private float[] densities;
	private Vector2 densityPoint; 
	private Label densityLabel;   
	
	private float targetDensity = 10f;
	private float pressureMultiplier = 400f;
	
	
	
	
	
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
		DrawCircle(densityPoint, 5, Colors.Red);
		DrawCircle(densityPoint, smoothingRadius, new Color(1, 0, 0, 0.3f));
	}
	public static float SmoothingKernel(float radius, float distance)
	{
	
	//returns smoothed value based on smoothing kernel implementation
	float normalization =  240f / (7f * (float)Math.PI * radius * radius);
	float normDist = distance / radius;
	
	float value = 0;
	if (0 <= normDist && normDist <= 0.5f) {
		value = ((6f*(normDist * normDist * normDist) - 6f*(normDist * normDist)) + 1f) * normalization;
	}
	else if (0.5f < normDist && normDist <= 1){
		value = (2 * (float)Math.Pow(1-normDist, 3)) * normalization;
	}
	else {
		value =  0;
	}
	return value;
	
	}
	
	public static Vector2 SmoothKernelGrad(float radius, Vector2 r)
	{
		float distance = r.Length();
		const float epsilon = 1.0e-6f;
	
		if (distance < epsilon) {
		// when particles are exactly on top of each other, generate a random direction
   
		float randomAngle = (float)(new Random().NextDouble() * 2 * Math.PI);
		Vector2 randomDirection = new Vector2((float)Math.Cos(randomAngle), (float)Math.Sin(randomAngle));
		return randomDirection; 
		}
		
		
		float normalization =  240f / (7f * (float)Math.PI * radius * radius);
		float normDist = distance / radius;
		Vector2 value = Vector2.Zero;
		
		if (distance > 1.0e-6f && normDist <= 1.0f) 
		{
  		Vector2 gradq = r / distance; // normalized direction
		gradq /= radius;
		
		if (normDist <= 0.5f)
		{
			value = normalization * (3f * normDist - 2f) * gradq;
			
		}
		else
		{
			float factor = 1f - normDist;
			value = normalization * (-factor * factor) * gradq;
			
		}
	}

	return value; //returning direction and magnitude
	}
	
	
	float Density(Vector2 point)
	{
		float density = 0;
		
		//density is a measure of a certain point
		//iterate through all particles, and determine their in distance to that point, and influence at that location
		//add to desnity by mass * influence
		foreach (Vector2 position in pPositions) {
			float distance = position.DistanceTo(point);
			float influence = SmoothingKernel(smoothingRadius, distance);
			density += mass * influence;
		}
		return density;
	}
	
	
	
	Vector2 ComputePressureForce(int particleIndex)
	{
		Vector2 pressureForce = Vector2.Zero;
		Vector2 point = pPositions[particleIndex];
		for (int i = 0; i < particleCount; i++) {
			if(particleIndex == i) continue;
			
			float distance = pPositions[i].DistanceTo(point);
			Vector2 r = pPositions[i] - point;
			//unpacked magnitude and direction of smoothing kernel gradient
			Vector2 gradW = SmoothKernelGrad(smoothingRadius, r);
			
			
			
			float density = densities[i];
			float pressure = DensityToPressure(density);
			//GD.Print($"pressure: {pressure}");
			pressureForce += mass * pressure / density * gradW;
			
		}
		
		return pressureForce;
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
			float y = (i / rowsParticles - colsParticles / 2f + 0.5f) * spacing;
			pPositions[i] = new Vector2(x,y);
		
		}
		
		// 
		
		//set dimensions of bounding box 
		boundingBox = new Rect2(
			(viewport.Position - (viewport.Size /2)) * boxMargin,
			viewport.Size * boxMargin
		);
		// Initialize density measurement point
		densityPoint = new Vector2(0, 0); 
		
		//initialize densities
		densities = new float[particleCount];

		// Create UI Label to display density
		densityLabel = new Label();
		densityLabel.AddThemeFontSizeOverride("font_size", 24);
		densityLabel.SetPosition(densityPoint + new Vector2(10, -20));
		densityLabel.Modulate = Colors.White;
		AddChild(densityLabel);
		
		
	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
		Parallel.For(0, particleCount, i => {
			//simulating gravity
			//pVelocities[i].Y += gravity * (float)delta;
			//calculate densities
			densities[i] = Density(pPositions[i]);
			
			}
		);
		
		Parallel.For(0, particleCount, i => {
			//simulate pressure forces
			Vector2 pressureForce = ComputePressureForce(i);
			//GD.Print($"pressureForce: {pressureForce}");
			Vector2 pressureAcceleration = pressureForce / densities[i];
			pVelocities[i] += pressureAcceleration * (float)delta;
			}
		);
		
		
		Parallel.For(0, particleCount, i => {
			//update positions
			pPositions[i] += pVelocities[i] * (float)delta;
			
			//resolve collisions
			ResolveCollisions(ref pPositions[i], ref pVelocities[i]);
			}
		);
		
			
			
			
		
		
		float densityValue = Density(densityPoint);
		densityLabel.Text = $"ρ: {densityValue:F2}";
		densityLabel.SetPosition(densityPoint + new Vector2(10, -20));
		
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
	
	
	float DensityToPressure(float density) {
		//Tait Equation of State compute pressure
		float gamma = 7f;
		float pressure = ((float)Math.Pow(density / targetDensity, gamma) - 1) * pressureMultiplier;
		
		
		//float densityError = density - targetDensity;
		
		//float pressure = densityError * pressureMultiplier;
		
		return pressure;
		
	}
	
}
