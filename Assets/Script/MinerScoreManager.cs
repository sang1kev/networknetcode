using TMPro;
using UnityEngine;

public class MinerScoreManager : MonoBehaviour
{
    private int score;
    [SerializeField] private TextMeshProUGUI scoreUI;

    void Start()
    {
        scoreUI.text = $"ÇöÀç È¹µæÇÑ ±¤¹°ÀÇ ¼ö : {score}";
    }

    public void AddScore()
    {
        score++;
        scoreUI.text = $"ÇöÀç È¹µæÇÑ ±¤¹°ÀÇ ¼ö : {score}";
    }
}
