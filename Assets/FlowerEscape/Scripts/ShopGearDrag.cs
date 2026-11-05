using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlowerEscape
{
    public sealed class ShopGearDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public FlowerEscapeDemo demo;
        public int shopIndex;
        public Image icon;
        private Image ghost;

        private void Awake()
        {
            if (!demo) demo = FindFirstObjectByType<FlowerEscapeDemo>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!demo || demo.state != GameplayUIState.Preparation || !icon) return;
            var obj = new GameObject("Dragged Gear", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(demo.canvas.transform, false);
            ghost = obj.GetComponent<Image>();
            ghost.sprite = icon.sprite;
            ghost.preserveAspect = true;
            ghost.raycastTarget = false;
            ghost.rectTransform.sizeDelta = new Vector2(52f, 52f);
            ghost.transform.SetAsLastSibling();
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
            demo.PlaceFromShop(shopIndex, eventData.position);
            Destroy(ghost.gameObject);
        }
    }
}
