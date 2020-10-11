using TMPro;
using UnityEngine;

public class Score : MonoBehaviour {
    private int _score;
    private TextMeshProUGUI _text;

    private void Start() {
        _text = GetComponent<TextMeshProUGUI>();
    }

    public void AddScore(int value) {
        _score += value;
        _text.text = _score.ToString();
    }

    public void ResetScore() {
        _score = 0;
        _text.text = _score.ToString();
    }
}
