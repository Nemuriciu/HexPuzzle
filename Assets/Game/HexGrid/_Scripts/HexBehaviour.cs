using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HexBehaviour : MonoBehaviour {
    public Sprite[] sprites;
    public GameObject[] nextObj; 

    private const int MaxCol = 7, MaxRow = 8;
    private int[] _next;
    private int[] _num;
    
    private List<Hexagon> _hexGrid;
    private Hexagon _selected;
    private GameBehaviour _gameBehaviour;
    private Score _score;
    private bool _recheckDone;

    private void Start() {
        _gameBehaviour = GetComponent<GameBehaviour>();
        _score = GameObject.Find("Score").GetComponentInChildren<Score>();
        _hexGrid = new List<Hexagon>();
        _num =  new [] {1,2,4,8,16,32};
        _next = new int[4];
        Transform gridT = GameObject.Find("HexGrid").GetComponent<Transform>();

        /* Generate Hexagon struct list */
        for (int i = 0; i < gridT.childCount; ++i)
            _hexGrid.Add(new Hexagon(i % 8, i / 8, 0,
                gridT.GetChild(i).gameObject));

        /* Generate Hexagon Neighbours */
        SetNeighbours();
    }

    /* Populate grid with values on game start */
    public void Init() {
        for (int i = 0; i < 16; i++) {
            int nextIx = UnityEngine.Random.Range(0, _num.Length - 2);
            int gridIx = UnityEngine.Random.Range(0, _hexGrid.Count);
            
            /* Search for an open hex */
            while(!_hexGrid[gridIx].IsOpen)
                gridIx = UnityEngine.Random.Range(0, _hexGrid.Count); 
            
            SetValue(_hexGrid[gridIx], _num[nextIx]);
        }
        
        SetNextList();
    }

    public bool Select(GameObject hexObj) {
        Hexagon hex = _hexGrid.FirstOrDefault(p => p.HexObj.Equals(hexObj));
        
        /* Another hex already selected */
        if (_selected != null) {
            if (hex != null) {
                /* Clicked an open hex */
                if (hex.IsOpen) {
                    // TODO: Move
                    List<Hexagon> path = FindPath(_selected, hex);
                    
                    _selected.HexObj.GetComponent<HexInteract>().Enable(false);
                    _selected = null;

                    if (path.Count > 0) {
                        StartCoroutine(Move(path));
                    }
                    //TODO: Else error message
                        
                    
                    return false;
                }
                /* Clicked the same used hex */
                if (_selected.HexObj.Equals(hexObj)) {
                    _selected = null;
                    return false;
                }
                /* Clicked another used hex */
                _selected.HexObj.GetComponent<HexInteract>().Enable(false);
                _selected = hex;
                return true;
            }

            return false;
        }
        /* No hex is selected */
        if (hex != null && hex.IsOpen)
            return false;
        
        _selected = hex;
        return true;
    }

    public void Reset() {
        foreach (Hexagon hex in _hexGrid) {
            SetValue(hex, 0);
        }
        
        _score.ResetScore();
        _gameBehaviour.GamePhase = Phase.Begin;
    }

    private void SetNextList() {
        for (int i = 0; i < 4; i++) {
            int ix = UnityEngine.Random.Range(0, 4);

            _next[i] = _num[ix];
            SetValue(nextObj[i], _next[i]);
        }
    }

    private List<Hexagon> UpdateHexGrid() {
        List<Hexagon> openHex = _hexGrid.FindAll(p =>
            p.IsOpen.Equals(true));
        List<Hexagon> newHex = new List<Hexagon>();

        foreach (int val in _next) {
            int ix = UnityEngine.Random.Range(0, openHex.Count);

            SetValue(openHex[ix], val);
            newHex.Add(openHex[ix]);
            openHex.Remove(openHex[ix]);
        }

        return newHex;
    }

    private IEnumerator CheckMatch(Hexagon hex, bool isRecheck) {
        bool matched = false;
        
        /* Avoid late check when called from UpdateHexGrid() */
        if (hex.IsOpen) yield break;

        while (true) {
            Dictionary<Tuple<int, int>, bool> visited = _hexGrid.ToDictionary(h => new Tuple<int, int>(h.Row, h.Col), h => false);
            List<Hexagon> path = new List<Hexagon>();
            int val = hex.Value;

            visited[Tuple.Create(hex.Row, hex.Col)] = true;
            path.Add(hex);
            foreach (Hexagon hexNeighbour in hex.Neighbours) {
                if (!hexNeighbour.IsOpen && hexNeighbour.Value == val) {
                    if (!visited[Tuple.Create(hexNeighbour.Row, hexNeighbour.Col)]) {
                        path.Add(hexNeighbour);
                        CheckMatchRecursive(hexNeighbour, path, visited);
                    }
                }
            }
            
            if (path.Count >= 4) {
                matched = true;
                yield return new WaitForSeconds(0.2f);
                
                /* Increase score */
                _score.AddScore(path.Count * path[0].Value);
                
                /* Merge matching */
                SetValue(path[0], 4 * path[0].Value);
                for (int i = 1; i < path.Count; i++)
                    SetValue(path[i], 0); 
                
                /* Particle animation */
                ParticleSystem p = path[0].HexObj.GetComponentInChildren<ParticleSystem>();
                p.Play();
                
                hex = path[0];
                continue;
            }

            /* Add next hexagons to grid */
            if (!matched && !isRecheck) { 
                List<Hexagon> newHex = UpdateHexGrid();

                foreach (Hexagon h in newHex) {
                    _recheckDone = false;
                    StartCoroutine(CheckMatch(h, true));
                    yield return new WaitUntil(() => _recheckDone.Equals(true));
                }
                
                SetNextList();
            }
            
            break;
        }

        if (isRecheck)
            _recheckDone = true;
        else
            _gameBehaviour.GamePhase = Phase.Selection;
    }

    private void CheckMatchRecursive(Hexagon hex,
        List<Hexagon> path, Dictionary<Tuple<int, int>, bool> visited) {
        int val = hex.Value;

        visited[Tuple.Create(hex.Row, hex.Col)] = true;
        foreach (Hexagon hexNeighbour in hex.Neighbours) {

            if (!hexNeighbour.IsOpen && hexNeighbour.Value == val) {
                if (!visited[Tuple.Create(hexNeighbour.Row, hexNeighbour.Col)]) {
                    path.Add(hexNeighbour);
                    CheckMatchRecursive(hexNeighbour, path, visited);
                }
            }
        }
    }

    private IEnumerator Move(List<Hexagon> path) {
        _gameBehaviour.GamePhase = Phase.Moving;
        
        /* Move one position every 0.05 seconds */
        for (int i = 0; i < path.Count - 1; i++) {
            Hexagon h1 = path[i], h2 = path[i + 1];
            
            SetValue(h2, h1.Value);
            SetValue(h1, 0);

            yield return new WaitForSeconds(0.05f);
        }
        
        _gameBehaviour.GamePhase = Phase.Checking;
        StartCoroutine(CheckMatch(path[path.Count - 1], false));
    }

    private List<Hexagon> FindPath(Hexagon start, Hexagon end) {
        List<Hexagon> bestPath = new List<Hexagon>();
        Dictionary<Tuple<int, int>, bool> visited = _hexGrid.ToDictionary(hex =>
            new Tuple<int, int>(hex.Row, hex.Col), hex => false);
        
        /* Visit current node */
        visited[Tuple.Create(start.Row, start.Col)] = true;
        
        /*Sort Neighbours by Euclidean distance */
        List<Hexagon> neighbours = start.Neighbours.ToList();
        neighbours.Sort((h1,h2) => 
                Math.Sqrt(Math.Pow(h1.Row - end.Row, 2) + Math.Pow(h1.Col - end.Col, 2)).CompareTo(
                    Math.Sqrt(Math.Pow(h2.Row - end.Row, 2) + Math.Pow(h2.Col - end.Col, 2))));
        
        foreach (var hexNeighbour in neighbours) {
            List<Hexagon> path = new List<Hexagon>();
            var visitedClone = new Dictionary<Tuple<int, int>, bool>(visited);
            
            path.Add(start);
            
            /* if hexagon is empty */
            if (hexNeighbour.IsOpen) {
                path.Add(hexNeighbour);
                visitedClone[Tuple.Create(hexNeighbour.Row, hexNeighbour.Col)] = true;

                if (hexNeighbour.Row.Equals(end.Row) && 
                    hexNeighbour.Col.Equals(end.Col))
                    return path;
                
                List<Hexagon> resPath = FindPathRecursive(hexNeighbour, end, path, visitedClone);

                if (resPath.Count > 0) {
                    bestPath = resPath.ToList();
                    break;
                }
            }
        }

        return bestPath;
    }

    private static List<Hexagon> FindPathRecursive(Hexagon current, Hexagon end, List<Hexagon> path, 
        Dictionary<Tuple<int, int>, bool> visited) {
        List<Hexagon> bestPath = new List<Hexagon>();

        if (current.Row.Equals(end.Row) && 
            current.Col.Equals(end.Col))
            return path;
        
        /*Sort Neighbours by Euclidean distance */
        List<Hexagon> neighbours = current.Neighbours.ToList();
        neighbours.Sort((h1,h2) => 
            Math.Sqrt(Math.Pow(h1.Row - end.Row, 2) + Math.Pow(h1.Col - end.Col, 2)).CompareTo(
                Math.Sqrt(Math.Pow(h2.Row - end.Row, 2) + Math.Pow(h2.Col - end.Col, 2))));

        foreach (var hexNeighbour in neighbours) {
            /* if hexagon is empty */
            if (hexNeighbour.IsOpen) {
                if (!visited[Tuple.Create(hexNeighbour.Row, hexNeighbour.Col)]) {
                    var visitedClone = new Dictionary<Tuple<int, int>, bool>(visited);
                    var pathClone = path.ToList();

                    pathClone.Add(hexNeighbour);
                    visitedClone[Tuple.Create(hexNeighbour.Row, hexNeighbour.Col)] = true;

                    List<Hexagon> resPath = FindPathRecursive(hexNeighbour, end, pathClone, visitedClone);

                    if (resPath.Count > 0) {
                        bestPath = resPath.ToList();
                        break;
                    }
                }
            }
        }

        return bestPath;
    }

    private void SetNeighbours() {
        foreach (Hexagon hex in _hexGrid) {
            /* Check top neighbour */
            if (hex.Row > 0)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row - 1) && p.Col.Equals(hex.Col)));
            
            /* Check bottom neighbour */
            if (hex.Row < MaxRow - 1)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row + 1) && p.Col.Equals(hex.Col)));
            
            /* Check top-left neighbour */
            if (hex.Col % 2 == 1)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row) && p.Col.Equals(hex.Col - 1)));
            else if (hex.Row > 0 && hex.Col > 0)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row - 1) && p.Col.Equals(hex.Col - 1)));
            
            /* Check top-right neighbour */
            if (hex.Col % 2 == 1)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row) && p.Col.Equals(hex.Col + 1)));
            else if (hex.Row > 0 && hex.Col < MaxCol - 1)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row - 1) && p.Col.Equals(hex.Col + 1)));
            
            /* Check bottom-left neighbour */
            if (hex.Col % 2 == 1) {
                if (hex.Row < MaxRow - 1)
                    hex.Neighbours.Add(_hexGrid.FirstOrDefault(p =>
                        p.Row.Equals(hex.Row + 1) && p.Col.Equals(hex.Col - 1)));
            }
            else if (hex.Col > 0)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row) && p.Col.Equals(hex.Col - 1)));
            
            /* Check bottom-right neighbour */
            if (hex.Col % 2 == 1) {
                if (hex.Row < MaxRow - 1)
                    hex.Neighbours.Add(_hexGrid.FirstOrDefault(p =>
                        p.Row.Equals(hex.Row + 1) && p.Col.Equals(hex.Col + 1)));
            }
            else if (hex.Col < MaxCol - 1)
                hex.Neighbours.Add(_hexGrid.FirstOrDefault(p => 
                    p.Row.Equals(hex.Row) && p.Col.Equals(hex.Col + 1)));
        }
    }

    private void SetValue(Hexagon hex, int value) {
        hex.Value = value;
        
        /* Set hexagon color */
        hex.HexObj.GetComponent<Image>().sprite = GetSprite(hex.Value);
        
        if (value == 0) {
            hex.HexObj.GetComponentInChildren<TextMeshProUGUI>().text = "";
            hex.IsOpen = true;
        }
        else {
            hex.HexObj.GetComponentInChildren<TextMeshProUGUI>().text = hex.Value.ToString();
            hex.IsOpen = false;
        }
    }
    
    private void SetValue(GameObject hex, int value) {
        hex.GetComponent<Image>().sprite = GetSprite(value);
        hex.GetComponentInChildren<TextMeshProUGUI>().text = value.ToString();
    }

    private Sprite GetSprite(int value) {
        switch (value) {
            case 0:
                return sprites[0];
            case 1:
                return sprites[1];
            case 2:
                return sprites[2];
            case 4:
                return sprites[3];
            case 8:
                return sprites[4];
            case 16:
                return sprites[5];
            case 32:
                return sprites[6];
            case 64:
                return sprites[7];
            case 128:
                return sprites[8];
            case 256:
                return sprites[9];
            case 512:
                return sprites[10];
            case 1024:
                return sprites[11];
            case 2048:
                return sprites[12];
            case 4096:
                return sprites[13];
            case 8192:
                return sprites[14];
            case 16384:
                return sprites[15];
            default:
                return sprites[0];
        }
    }
}
