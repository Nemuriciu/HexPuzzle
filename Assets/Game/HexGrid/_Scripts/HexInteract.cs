using UnityEngine;
using UnityEngine.EventSystems;

public class HexInteract : MonoBehaviour, IPointerClickHandler {
    private const float Scale = 1.25f;
    private bool _fadeIn, _isSelected;
    private CanvasGroup _canvas;
    private HexBehaviour _hexBehaviour;
    private GameBehaviour _gameBehaviour;

    private void Start() {
        _hexBehaviour = GameObject.Find("HexGrid").GetComponent<HexBehaviour>();
        _gameBehaviour = _hexBehaviour.GetComponent<GameBehaviour>();
        _canvas = GetComponent<CanvasGroup>();
    }

    private void Update() {
        if (_canvas.alpha <= 0.25 && !_fadeIn)
            _fadeIn = true;
        else if (_canvas.alpha >= 1 && _fadeIn)
            _fadeIn = false;


        if (_isSelected)
            if (_fadeIn)
                _canvas.alpha += Time.deltaTime * Scale;
            else
                _canvas.alpha -= Time.deltaTime * Scale;
    }

    public void Enable(bool flag) {
        _isSelected = flag;
        _canvas.alpha = 1;
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (_gameBehaviour.GamePhase != Phase.Selection)
            return;

        bool res = _hexBehaviour.Select(gameObject);
        Enable(res);
    }
}
