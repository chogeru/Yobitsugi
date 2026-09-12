using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yobitsugi.UI
{
    /// <summary>
    /// Detects clicks on TMP `&lt;link="id"&gt;` spans and reports the id — used by the backlog to make
    /// individual voiced lines clickable without building a whole scroll-list of per-line buttons.
    /// </summary>
    public class TMPLinkClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private TMP_Text text;

        public event Action<string> LinkClicked;

        public void Initialize(TMP_Text target) => text = target;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (text == null) return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
            if (linkIndex < 0) return;

            LinkClicked?.Invoke(text.textInfo.linkInfo[linkIndex].GetLinkID());
        }
    }
}
