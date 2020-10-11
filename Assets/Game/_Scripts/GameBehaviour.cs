using UnityEngine;

public class GameBehaviour : MonoBehaviour {
    public Phase GamePhase { get; set; }
    private HexBehaviour _hexBehaviour;

    private void Start() {
        _hexBehaviour = GetComponent<HexBehaviour>();
        GamePhase = Phase.Begin;
    }

    private void Update() {
        switch (GamePhase) {
            case Phase.Begin:
                _hexBehaviour.Init();
                GamePhase = Phase.Selection;
                break;
            
            case Phase.Selection:
                break;
            
            case Phase.Moving:
                break;
            
            case Phase.Checking:
                GamePhase = Phase.Selection;
                break;
        }
    }
}

public enum Phase {
    Begin,
    Selection,
    Moving,
    Checking
}
