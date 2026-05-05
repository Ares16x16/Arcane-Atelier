using UnityEngine;
using TMPro;
using System.Collections;

public class TextTypewriter : MonoBehaviour
{
    public TextMeshProUGUI textDisplay;
    public string fullText;
    public float delay = 0.05f;

    void Start()
    {
        StartCoroutine(ShowText());
    }

    IEnumerator ShowText()
    {
        textDisplay.text = "";
        foreach (char c in fullText)
        {
            textDisplay.text += c;
            yield return new WaitForSeconds(delay);
        }
    }
}