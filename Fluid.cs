using Godot;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;



public partial class Fluid : Node2D
{	
	[Export(PropertyHint.Range, "-10,10,")] float gravity = 9.8f;
	private int particleCount = 400;
	private Vector2[] pPositions;
	private Vector2[] pVelocities;
	private Rect2 boundingBox;
	private float boxMargin = .8f;
	private float pSize = 0.1f;
	private float particleSpacing = 0.11f;
	private float damping = .98f;
	float boxWidth = 16f;
	float boxHeight = 9f;

	//approx 4x particle size
	[Export(PropertyHint.Range, "0,20,")] float smoothingRadius = 1.2f;
	[Export(PropertyHint.Range, "0,1000,")] float mass = .15f;
	
	private float[] densities;
	private Vector2 densityPoint; 
	private Label densityLabel;   
	
	[Export(PropertyHint.Range, ".5,200,")] float targetDensity = 2.75f;
	[Export(PropertyHint.Range, "0,300,")] float pressureMultiplier = 0.5f;
	
	
	
	
	
	public static class Predef
	{
		public static readonly Color blue = new Color(0.65f, 0.85f, 0.173f);
		
	}
	
	
	public override void _Draw()
	{
		
		DrawRect(boundingBox, Colors.White, false, (float).1f);
		
		for(int i = 0; i< pPositions.Length; i++){
			
			DrawCircle(pPositions[i], pSize, Predef.blue);
		}
		
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
	return Math.Max(0, value);
	
	}
	
	public static Vector2 SmoothKernelGrad(float radius, Vector2 r)
	{
		float distance = r.Length();
		
		
		float normalization =  240f / (7f * (float)Math.PI * radius * radius * radius);
		//loat normalization = 1;
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
	
	float DensityToPressure(float density) {
		//Tait Equation of State compute pressure
		//float gamma = 2f;
		//float pressure = ((float)Math.Pow(density / targetDensity, gamma) - 1) * pressureMultiplier * .01f;

		float densityError = density - targetDensity;
		
		float pressure = densityError * pressureMultiplier;
		
		return Math.Max(pressure, 0);
		
	}
	
	float ComputeSharedPressure(float densityA, float densityB) 
	{
		float pressureA = DensityToPressure(densityA);
		float pressureB = DensityToPressure(densityB);
		
		//if (densityA + densityB == 0) return 0;
		
		return (pressureA + pressureB) / 2;
		
	}
	
	
	float Density(Vector2 point)
	{
		float density = 0;
		
		//density measure of a certain point
		//iterate through all particles, and determine their in distance to that point, and influence at that location
		//add to desnity by mass * influence
		foreach (Vector2 position in pPositions) {
			float distance = position.DistanceTo(point);
			float influence = SmoothingKernel(smoothingRadius, distance);
			density += mass * influence;
		}
		return density;
	}
	
	
	public static Vector2 GetRandomDir()
	{
	Random random = new Random();
	float angle = (float)(random.NextDouble() * 2 * Math.PI); // Random angle in radians
	return new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
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

			float density_i = densities[particleIndex];
			float density_j = densities[i];

			//float pressure = DensityToPressure(density);
			float sharedPressure = ComputeSharedPressure(density_i, density_j);
			
			if (density_i <= 0 || density_j <= 0) continue;
			// Separate magnitude and direction
			float magnitude = gradW.Length();
			Vector2 direction = distance == 0 ? GetRandomDir() : gradW.Normalized();
			
			
			pressureForce += mass * magnitude * direction * sharedPressure / density_j;
			
			
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
		
		Camera2D camera = new Camera2D();
		camera.Position = Vector2.Zero; // Centered at (0,0)
		Vector2 baseZoom = new Vector2(GetViewport().GetVisibleRect().Size.X / boxWidth,
							  GetViewport().GetVisibleRect().Size.Y / boxHeight);
		camera.Zoom = baseZoom * boxMargin;
		camera.MakeCurrent(); // Make it the active camera
		AddChild(camera);
		
		//set dimensions of bounding box 
		boundingBox = new Rect2(-boxWidth / 2, -boxHeight / 2, boxWidth, boxHeight);
		// Initialize density measurement point
		densityPoint = new Vector2(0, 0); 
		
		//initialize densities
		densities = new float[particleCount];

	}
	

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
		Parallel.For(0, particleCount, i => {
			//simulating gravity
			pVelocities[i].Y += gravity * (float)delta;
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
			//update positions
			pPositions[i] += pVelocities[i] * (float)delta;
			
			}
		);
		
		
		Parallel.For(0, particleCount, i => {
			
			
			//resolve collisions
			ResolveCollisions(ref pPositions[i], ref pVelocities[i]);
			}
		);
		
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
