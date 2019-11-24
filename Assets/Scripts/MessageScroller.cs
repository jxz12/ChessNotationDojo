using UnityEngine;

public class MessageScroller : MonoBehaviour
{
    [SerializeField] float scrollSpeed;
    [SerializeField] TMPro.TextMeshProUGUI message;

    RectTransform maskRT, textRT;
    bool scrolling = false;
    void Start()
    {
        maskRT = GetComponent<RectTransform>();
        textRT = message.GetComponent<RectTransform>();
    }
    string separator = "                      ";
    public void SetText(string text)
    {
        message.text = "   " + text;
        // Canvas.ForceUpdateCanvases();
        if (textRT.rect.width <= maskRT.rect.width)
        {
            scrolling = false;
        }
        else
        {
            message.text = separator + text + separator + text;
            scrolling = true;
        }
        textRT.anchoredPosition = Vector2.zero;
    }
    void FixedUpdate()
    {
        if (scrolling)
        {
            textRT.anchoredPosition -= new Vector2(scrollSpeed, 0);
            if (textRT.anchoredPosition.x < -textRT.rect.width/2)
            {
                textRT.anchoredPosition += new Vector2(textRT.rect.width/2, 0);
            }
        }
    }
}