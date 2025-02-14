using Godot;
using System;

public partial class SpatialHashing : RefCounted
{
	const int hash1 = 11261;
	const int hash2 = 2280587;

	
	public (int x, int y) GetCellCoord(Vector2 position, float radius) {
		int x = (int)(position.X / radius);
		int y = (int)(position.Y / radius);
		return (x, y);
		
	}
	
	public int CellHash(int x, int y) {
		return (x * hash1) + (y * hash2);
	}
	
	public int KeyFromHash(int hash, int size) {
		return hash % size;
		
	}
	
	
}
