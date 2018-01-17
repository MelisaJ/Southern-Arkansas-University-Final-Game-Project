﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour {

	public int gX = 24, gY = 24;
	public int minDistance = 5;

	public Node[,] grid;
	Node start;
	Node end;
	List<Node> path = new List<Node>(0);

	public Material pathMat;

	public LevelGenCategory[] categories;
	/*
	// 0  - Normal Room
	// 1  - Start Room
	// 2  - End Room
	*/

	void Start() {
		if (gX * gY > 1) {
				//First set up the grid
			InitializeGrid(gX, gY);
				//Assign 2 places to serve as our start and end rooms
			int r1Q = Random.Range(1,4); //What quadrant will the start room be in?
			int r2Q = Random.Range(1,4); //Same for the end room
			if (r1Q == r2Q) {
				r2Q++;
				if (r2Q == 5) r2Q = 1;
			}
			bool r1Assigned = false;
			do { r1Assigned = AssignRandomRoom(1, r1Q); } while (!r1Assigned);
			bool r2Assigned = false;
			do { r2Assigned = AssignRandomRoom(2, r2Q); } while (!r2Assigned);
			end.occupied = true;
			for (int i = 0; i < gX; i++) {
				for (int j = 0; j < gY; j++) {
					grid[i,j].weight = Random.Range(0,50);
				}
			}
			start.weight = 0;
			path = FindPath();
			List<Arrangement> roomsToPlace = new List<Arrangement>(0);
			for (int i = 0; i < path.Count; i++) {
				if (roomsToPlace.Count == 0) {
					roomsToPlace.Add(PlaceRoom(path[0], null, true));
				}
				roomsToPlace.Add(PlaceRoom(path[1], path[0], true));
			}
			//roomsToPlace.Add(PlaceRoom(end, null, false));
			InstantiateRooms(roomsToPlace);
		} else {
			Debug.LogWarning("Grid size not large enough for start and end rooms to be put on the grid! Aborting level generation");
		}
	}

	Arrangement PlaceRoom(Node source, Node prev, bool isPathed) {
		if (prev!= null) {
			Debug.Log(source.Location()+"|"+prev.Location());
		} else {
			Debug.Log(source.Location());
		}
		List<Arrangement> potential = new List<Arrangement>();
		/*/
		/// Step 1: Get all the initial arrangements that can be in that spot, assuming nothing's there and we don't have to connect to anything else
		/*/
		foreach (LevelGenPrefab pref in categories[source.roomType].components) {
			if (prev==null) {
				for (int i = 0; i < pref.dimensions.x; i++) {
					for (int j = 0; j < pref.dimensions.y; j++) {
						Arrangement arr = new Arrangement();
						arr.offset = new Vector2(i,j);
						arr.sourceNode = source;
						arr.original = pref;
						potential.Add(arr);
					}
				}
			} else {
				for (int i = 0; i < pref.dimensions.x; i++) {
					for (int j = 0; j < pref.dimensions.y; j++) {
						foreach (Vector2 door in pref.doorLocations) {
							//Debug.Log(door+"|"+source.Location()+"|"+prev.Location()+"|"+new Vector2(i,j));
							if (door == prev.Location()-new Vector2(i,j)-source.Location()+Vector2.one) {
								//Debug.Log("Valid door found: " + door+"|"+new Vector2(i,j)+"|"+prev.Location()+"|"+(source.Location()+Vector2.one));
								Arrangement arr = new Arrangement();
								arr.offset = new Vector2(i,j);
								arr.sourceNode = source;
								arr.doorEntry = door;
								arr.original = pref;
								potential.Add(arr);
							}
						}
					}
				}
			}
		}
		/*/
		/// Step 2: Check every door for every arrangement, see which door leads the furthest along the path, if there aren't any, we don't use that arrangement
		/// This is purely for rooms that are on generated paths
		/*/
		if (isPathed) {
			foreach (Arrangement pot in potential) {
				for (int i = path.IndexOf(pot.sourceNode)+1; i < path.Count; i++) {
					foreach (Vector2 door in pot.original.doorLocations) {
						if (path[i].Location()-pot.sourceNode.Location()+Vector2.one == door) {
							Debug.Log("Room: " +pot.original.name+ " | Door location: "+door+" | path[i] location: " +path[i].Location()+ " | sourceNode location: " + pot.sourceNode.Location() + " | " +Vector2.one);
							pot.pathExitNode = path[i];
						}
					}
				}
				if (pot.pathExitNode == null) {
					pot.markForRemoval=true;
				}
			}
		}
		for (int i = 0; i < potential.Count; i++) {
			if (potential[i].markForRemoval) {
				potential.Remove(potential[i]);
				i--;
			}
		}
		/*/
		/// Step 3: Check every arrangement for collisions with other rooms or any path nodes farther along than where we are, remove the arrangements if there are any
		/*/
		foreach (Arrangement pot in potential) {
			for (int i = (int)(pot.sourceNode.x); i < (int)(pot.sourceNode.x+pot.original.dimensions.x); i++) {
				for (int j = (int)(pot.sourceNode.y); j < (int)(pot.sourceNode.y+pot.original.dimensions.y); j++) {
					if (i < gX && j < gY) {
						if (grid[i,j].occupied || (isPathed && path.Contains(grid[i,j]) && path.IndexOf(grid[i,j]) > path.IndexOf(pot.pathExitNode))) {
							pot.markForRemoval=true;
						}
					}
				}
			}
		}
		for (int i = 0; i < potential.Count; i++) {
			if (potential[i].markForRemoval) {
				potential.Remove(potential[i]);
				i--;
			}
		}
		/*/
		/// Step 4: Clean up eaten path, mark occupied grid spots as occupied
		/*/
		Arrangement use = potential[Random.Range(0,potential.Count-1)];
		path.RemoveRange(0, path.IndexOf(use.pathExitNode)-1);
		for (int i = (int)(use.sourceNode.x); i < (int)(use.sourceNode.x+use.original.dimensions.x); i++) {
			for (int j = (int)(use.sourceNode.y); j < (int)(use.sourceNode.y+use.original.dimensions.y); j++) {
				if (i < gX && j < gY) {
					grid[i,j].occupied=true;
				}
			}
		}
		return use;
	}

	// Use A* pathfinding algorithm to generate a path from start to end, so that we always have a way to the exit
	List<Node> FindPath() {
		List<Node> closedSet = new List<Node>(0);
		List<Node> openSet = new List<Node>(0);
		openSet.Add(start);
		foreach (Node n in grid) {
			if (n != start) {
				n.s_Cost = 9999999;
				n.f_Cost = 9999999;
			}
			if (n == start) {
				n.s_Cost = 0;
				n.f_Cost = Node.NodeDistance(start,end);
			}
		}
		while (openSet.Count > 0) {
			Node current = openSet[0];
			foreach (Node a in openSet) {
				if (a.f_Cost < current.f_Cost) {
					current = a;
				}
			}
			if (current == end) {
				return BuildPath(current);
			}
			openSet.Remove(current);
			closedSet.Add(current);

			Node neighbor;
			if (current.x != 0) {
				neighbor = grid[current.x-1, current.y]; 
				if (!closedSet.Contains(neighbor) && !openSet.Contains(neighbor)) {
					openSet.Add(neighbor);
					CompareNeighbors(neighbor,current);
				}
			}
			if (current.x < gX-1) {
				neighbor = grid[current.x+1, current.y];
				if (!closedSet.Contains(neighbor) && !openSet.Contains(neighbor)) {
					openSet.Add(neighbor);
					CompareNeighbors(neighbor,current);
				}
			}
			if (current.y != 0) {
				neighbor = grid[current.x, current.y-1];
				if (!closedSet.Contains(neighbor) && !openSet.Contains(neighbor)) {
					openSet.Add(neighbor);
					CompareNeighbors(neighbor,current);
				}
			}
			if (current.y < gY - 1) {
				neighbor = grid[current.x, current.y+1];
				if (!closedSet.Contains(neighbor) && !openSet.Contains(neighbor)) {
					openSet.Add(neighbor);
					CompareNeighbors(neighbor,current);
				}
			}
		}
		return null;
	}

	void CompareNeighbors(Node neighbor, Node current) {
		int tempS_Cost = current.s_Cost + Node.NodeDistance(current, neighbor) + neighbor.weight;
		if (tempS_Cost < neighbor.s_Cost) {
			neighbor.cameFrom = current;
			neighbor.s_Cost = tempS_Cost;
			neighbor.f_Cost = neighbor.s_Cost + Node.NodeDistance(neighbor, end);
		}
	}

	//Get a path from the nodes
	List<Node> BuildPath(Node n) {
		Node currentNode = n;
		List<Node> route = new List<Node>(0);
		while (currentNode != start) {
			route.Add(currentNode);
			currentNode = currentNode.cameFrom;
		}
		route.Add(start);
		route.Reverse();
		return route;
	}

	//Creating the rooms for testing grid generation
	void InstantiateRooms() {
		foreach (Node n in path) {
			GameObject room;
			switch (n.roomType) {
			default: 
				room = Instantiate(categories[0].components[Random.Range(0,categories[0].components.Length-1)].prefab);
				room.transform.position = new Vector3(n.x*4, 0, n.y*4);
				room.transform.Find("Cube").GetComponent<MeshRenderer>().material = pathMat;
				break;
			case 1: 
				room = Instantiate(categories[1].components[Random.Range(0,categories[1].components.Length-1)].prefab);
				room.transform.position = new Vector3(n.x*4, 0, n.y*4);
				break;
			case 2: 
				room = Instantiate(categories[2].components[Random.Range(0,categories[2].components.Length-1)].prefab);
				room.transform.position = new Vector3(n.x*4, 0, n.y*4);
				break;
			}
			room.name = n.x + "|" + n.y;
		}
	}

	//Creating the rooms from a list
	void InstantiateRooms(List<Arrangement> roomsToCreate) {
		foreach (Arrangement room in roomsToCreate) {
			GameObject init = Instantiate(room.original.prefab);
			float x = room.original.dimensions.x/2;
			float y = room.original.dimensions.y/2;
			init.transform.position = new Vector3(x+room.sourceNode.x, 0, y+room.sourceNode.y)*4;
		}
	}

	//Make a random? room into either a start or end room
	bool AssignRandomRoom(int i, int quad) {
		int rX, rY;
			// We get a random position in the grid in the quadrant we want - if there's enough room for minimum distance to be taken into account, we use it
		switch (quad) {
		case 1:
			if (minDistance < gX) {
				rX = Random.Range((gX+minDistance)/2, gX);
			} else {rX = Random.Range(gX/2, gX);}
			if (minDistance < gY) {
				rY = Random.Range((gY+minDistance)/2, gY);
			} else {rY = Random.Range(gY/2, gY);}
		break;
		case 2:
			if (minDistance < gX) {
				rX = Random.Range(0, (gX-minDistance)/2);
			} else {rX = Random.Range(0, gX/2);}
			if (minDistance < gY) {
				rY = Random.Range((gY+minDistance)/2, gY);
			} else {rY = Random.Range(gY/2, gY);}
		break;
		case 3:
			if (minDistance < gX) {
				rX = Random.Range(0, (gX-minDistance)/2);
			} else {rX = Random.Range(0, gX/2);}
			if (minDistance < gY) {
				rY = Random.Range(0, (gY-minDistance)/2);
			} else {rY = Random.Range(0, gY/2);}
		break;
		case 4:
			if (minDistance < gX) {
				rX = Random.Range((gX+minDistance)/2, gX);
			} else {rX = Random.Range(gX/2, gX);}
			if (minDistance < gY) {
				rY = Random.Range(0, (gY-minDistance)/2);
			} else {rY = Random.Range(0, gY/2);}
		break;
		default:
			rX = Random.Range(0, gX);
			rY = Random.Range(0, gY);
		break;
		}
		if (grid[rX,rY].roomType == 0) {
			grid[rX,rY].roomType = i;
			if (i == 1) {
				start = grid[rX,rY];
			}
			if (i == 2) {
				end = grid[rX,rY];
			}
			return true;
		}
		return false;
	}

	//Set up the 2d grid array, 0 as default entry
	void InitializeGrid(int x, int y) {
		grid = new Node[x,y];
		for (int i = 0; i < x; i++) {
			for (int j = 0; j < y; j++) {
				grid[i,j] = new Node();
				grid[i,j].roomType = 0;
				grid[i,j].x = i;
				grid[i,j].y = j;
			}
		}
	}
}