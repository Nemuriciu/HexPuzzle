using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Hexagon {
	public int Row, Col, Value;
	public bool IsOpen;
	public GameObject HexObj;
	public List<Hexagon> Neighbours;

	public Hexagon(int row, int col, int value, GameObject hexObj) {
		Row = row;
		Col = col;
		Value = value;
		IsOpen = true;
		HexObj = hexObj;
		Neighbours = new List<Hexagon>();
	}
}
