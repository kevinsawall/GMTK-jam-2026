using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_Text))]
public sealed class TmpLinkOpener : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text text;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
        if (linkIndex == -1)
        {
            return;
        }

        TMP_LinkInfo link = text.textInfo.linkInfo[linkIndex];
        Application.OpenURL(link.GetLinkID());
    }
}
