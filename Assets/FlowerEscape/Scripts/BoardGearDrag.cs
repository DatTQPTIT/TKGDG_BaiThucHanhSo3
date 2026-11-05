using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlowerEscape
{
    public sealed class BoardGearDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public FlowerEscapeDemo demo;
        public int cellIndex;
        private Image ghost;

        private void Awake()
        {
            if (!demo) demo = FindFirstObjectByType<FlowerEscapeDemo>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!demo || demo.state != GameplayUIState.Preparation || !demo.cellIcons[cellIndex].enabled) return;
            var obj = new GameObject("Moving Gear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(demo.canvas.transform, false);
            ghost = obj.GetComponent<Image>();
            ghost.sprite = demo.cellIcons[cellIndex].sprite;
            ghost.preserveAspect = true;
            ghost.raycastTarget = false;
            ghost.rectTransform.sizeDelta = new Vector2(52f, 52f);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!ghost) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                demo.canvas.transform as RectTransform, eventData.position, demo.canvas.worldCamera, out Vector2 local);
            ghost.rectTransform.anchoredPosition = local;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!ghost) return;
            demo.MoveGear(cellIndex, eventData.position);
            Destroy(ghost.gameObject);
        }
    }
}
