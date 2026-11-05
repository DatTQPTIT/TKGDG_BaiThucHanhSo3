using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlowerEscape
{
    public enum GameplayUIState { Preparation, Combat }

    public sealed class FlowerEscapeDemo : MonoBehaviour
    {
        public Canvas canvas;
        public RectTransform safeAreaRoot;
        public RectTransform board;
        public RectTransform grid;
        public RectTransform shop;
        public RectTransform controls;
        public RectTransform combatArea;
        public RectTransform boosterPanel;
        public GameObject preparationOnly;
        public GameObject combatOnly;
        public Button startButton;
        public Button refreshButton;
        public Button returnButton;
        public Button buyCoinsButton;
        public Button[] boosterButtons;
        public Text refreshLabel;
        public Text currencyLabel;
        public Text speedLabel;
        public Text powerLabel;
        public Image progressFill;
        public FlowerEscapeCombat combat;
        public Image[] shopIcons;
        public Text[] shopLevels;
        public Sprite[] itemSprites;
        public Image[] cellIcons;
        public Text[] cellLevels;
        public RectTransform[] cells;
        public RectTransform[] weaponSlots;
        public Image boat;
        public GameplayUIState state = GameplayUIState.Preparation;
        public int currency = 165;
        public int[] shopCosts = { 5, 10, 0, 20 };

        private Vector2 lastSize;
        private Vector2 lastSafeRootSize;
        private Rect lastSafeArea;
        private int shopOffset;
        private float feedbackUntil;
        private Vector2 boatStart;

        private void Awake()
        {
            Application.runInBackground = true;
            startButton.onClick.AddListener(() => SetState(GameplayUIState.Combat));
            refreshButton.onClick.AddListener(RefreshShop);
            returnButton.onClick.AddListener(() => SetState(GameplayUIState.Preparation));
            if (buyCoinsButton) buyCoinsButton.onClick.AddListener(() => { currency += 50; UpdateCurrency(); });
            if (boosterButtons != null)
                foreach (var booster in boosterButtons)
                {
                    var captured = booster;
                    captured.onClick.AddListener(() => UseBooster(captured));
                }
            SetState(GameplayUIState.Preparation);
            UpdateCurrency();
            UpdateGearStats();
        }

        private void Start()
        {
            Layout();
            if (boat) boatStart = boat.rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (lastSize != new Vector2(Screen.width, Screen.height) || lastSafeArea != Screen.safeArea ||
                lastSafeRootSize != safeAreaRoot.rect.size)
                Layout();
            if (refreshLabel && Time.unscaledTime > feedbackUntil && refreshLabel.text != "Refresh")
                refreshLabel.text = "Refresh";
            if (boat && state == GameplayUIState.Combat)
                boat.rectTransform.anchoredPosition = boatStart + Vector2.up * (3f * Mathf.Sin(Time.unscaledTime * 1.8f));
        }

        public void SetState(GameplayUIState next)
        {
            if (next == GameplayUIState.Preparation && combat) combat.EndBattle();
            state = next;
            if (preparationOnly) preparationOnly.SetActive(next == GameplayUIState.Preparation);
            if (combatOnly) combatOnly.SetActive(next == GameplayUIState.Combat);
            if (returnButton) returnButton.gameObject.SetActive(next == GameplayUIState.Combat);
            Layout();
            if (next == GameplayUIState.Combat && combat) combat.BeginBattle();
        }

        public void RefreshShop()
        {
            if (itemSprites == null || itemSprites.Length == 0) return;
            if (currency < 5) { refreshLabel.text = "Need 5"; feedbackUntil = Time.unscaledTime + 0.8f; return; }
            currency -= 5;
            UpdateCurrency();
            shopOffset = (shopOffset + 1) % itemSprites.Length;
            for (int i = 0; i < shopIcons.Length; i++)
            {
                int index = (i + shopOffset) % itemSprites.Length;
                shopIcons[i].sprite = itemSprites[index];
                shopIcons[i].preserveAspect = true;
                shopLevels[i].text = new[] { "1", "2", "4", "8" }[index % 4];
            }
            refreshLabel.text = "Updated!";
            feedbackUntil = Time.unscaledTime + 0.8f;
        }

        public bool PlaceFromShop(int shopIndex, Vector2 screenPoint)
        {
            if (state != GameplayUIState.Preparation || shopIndex < 0 || shopIndex >= shopIcons.Length) return false;
            int best = CellAt(screenPoint);
            if (best < 0 || cellIcons[best].enabled || currency < shopCosts[shopIndex]) return false;
            cellIcons[best].sprite = shopIcons[shopIndex].sprite;
            cellIcons[best].enabled = true;
            cellIcons[best].preserveAspect = true;
            cellLevels[best].text = shopLevels[shopIndex].text;
            cellLevels[best].enabled = true;
            currency -= shopCosts[shopIndex];
            UpdateCurrency();
            UpdateGearStats();
            return true;
        }

        public bool MoveGear(int from, Vector2 screenPoint)
        {
            if (state != GameplayUIState.Preparation || from < 0 || from >= cells.Length || !cellIcons[from].enabled)
                return false;
            int to = CellAt(screenPoint);
            if (to < 0 || to == from) return false;
            var sourceSprite = cellIcons[from].sprite;
            var sourceLevel = cellLevels[from].text;
            if (cellIcons[to].enabled && cellIcons[to].sprite == sourceSprite &&
                cellLevels[to].text == sourceLevel && int.TryParse(sourceLevel, out int level) && level < 16)
            {
                cellLevels[to].text = (level * 2).ToString();
                cellIcons[from].enabled = false;
                cellLevels[from].enabled = false;
            }
            else
            {
                var targetSprite = cellIcons[to].sprite;
                var targetLevel = cellLevels[to].text;
                var targetEnabled = cellIcons[to].enabled;
                cellIcons[to].sprite = sourceSprite;
                cellLevels[to].text = sourceLevel;
                cellIcons[to].enabled = true;
                cellLevels[to].enabled = true;
                cellIcons[from].sprite = targetSprite;
                cellLevels[from].text = targetLevel;
                cellIcons[from].enabled = targetEnabled;
                cellLevels[from].enabled = targetEnabled;
            }
            UpdateGearStats();
            return true;
        }

        public int CellAt(Vector2 screenPoint)
        {
            for (int i = 0; i < cells.Length; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(cells[i], screenPoint, canvas.worldCamera))
                    return i;
            return -1;
        }

        public int GearPower()
        {
            int total = 0;
            for (int i = 0; i < cellIcons.Length; i++)
                if (cellIcons[i].enabled && int.TryParse(cellLevels[i].text, out int level)) total += level;
            return total;
        }

        public int GearCount()
        {
            int count = 0;
            foreach (var icon in cellIcons) if (icon.enabled) count++;
            return count;
        }

        public void Earn(int amount)
        {
            currency += amount;
            UpdateCurrency();
        }

        private void UpdateCurrency()
        {
            if (currencyLabel) currencyLabel.text = currency.ToString();
        }

        private void UpdateGearStats()
        {
            if (speedLabel) speedLabel.text = (30f + GearCount() * 1.8f).ToString("0.0") + " mph";
            if (powerLabel) powerLabel.text = (GearPower() * 1.5f).ToString("0.0");
        }

        public void UseBooster(Button button)
        {
            var badge = button.GetComponentInChildren<Text>();
            if (!badge || !int.TryParse(badge.text, out int value) || value <= 0 ||
                state != GameplayUIState.Combat || !combat || !combat.IsRunning) return;
            badge.text = (value - 1).ToString();
            var image = button.GetComponent<Image>();
            if (image) image.color = new Color(0.65f, 0.85f, 1f, 1f);
            if (combat) combat.ApplyBooster(Array.IndexOf(boosterButtons, button));
        }

        public void Layout()
        {
            if (!safeAreaRoot || !board || !grid || !shop || !controls) return;
            lastSize = new Vector2(Screen.width, Screen.height);
            lastSafeArea = Screen.safeArea;
            Rect safe = Screen.safeArea;
            safeAreaRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeAreaRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeAreaRoot.offsetMin = safeAreaRoot.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            float w = safeAreaRoot.rect.width;
            float h = safeAreaRoot.rect.height;
            if (w <= 0 || h <= 0) return;
            lastSafeRootSize = new Vector2(w, h);
            bool compact = h / w < 1.7f;
            float margin = Mathf.Max(10f, w * 0.025f);
            float controlHeight = compact ? 54f : 68f;
            float shopHeight = compact ? 76f : 106f;
            float boardWidth = w - margin * 2f;
            float boardHeight = state == GameplayUIState.Preparation
                ? Mathf.Min(boardWidth * 1.11f, h * (compact ? .43f : .49f))
                : Mathf.Min(boardWidth * .90f, h * (compact ? .40f : .43f));
            if (state == GameplayUIState.Combat) boardWidth -= compact ? 54f : 62f;
            SetBottom(board, state == GameplayUIState.Preparation ? margin + controlHeight + shopHeight + 14f : margin + 8f,
                boardWidth, boardHeight, state == GameplayUIState.Combat ? -27f : 0f);
            SetBottom(shop, margin + controlHeight + 4f, w - margin * 2f, shopHeight);
            SetBottom(controls, margin, w - margin * 2f, controlHeight);
            if (combatArea)
            {
                float battleBottom = board.anchoredPosition.y + boardHeight + 4f;
                SetBottom(combatArea, battleBottom, w, Mathf.Max(90f, h - battleBottom - (compact ? 82f : 110f)));
            }
            if (boosterPanel)
                SetBottom(boosterPanel, margin + 10f, compact ? 48f : 55f, boardHeight - 12f, w * .5f - (compact ? 27f : 31f));

            if (weaponSlots != null)
            {
                for (int i = 0; i < weaponSlots.Length; i++)
                {
                    var slot = weaponSlots[i];
                    slot.sizeDelta = compact ? new Vector2(53f, 40f) : new Vector2(61f, 50f);
                    slot.anchoredPosition = new Vector2((i - 2) * (compact ? 62f : 70f), compact ? -62f : -67f);
                }
            }

            // Six logical columns and four rows. The cell side is limited by both board axes.
            float topSpace = compact ? 78f : 94f;
            float gridW = boardWidth - 20f;
            float gridH = Mathf.Max(1f, boardHeight - topSpace - 12f);
            float gap = compact ? 2f : 4f;
            float cell = Mathf.Floor(Mathf.Min((gridW - gap * 5f) / 6f, (gridH - gap * 3f) / 4f));
            var group = grid.GetComponent<GridLayoutGroup>();
            group.cellSize = new Vector2(cell, cell);
            group.spacing = new Vector2(gap, gap);
            grid.sizeDelta = new Vector2(cell * 6f + gap * 5f, cell * 4f + gap * 3f);
            grid.anchoredPosition = new Vector2(0f, -(topSpace + (gridH - grid.sizeDelta.y) * .5f));
        }

        private static void SetBottom(RectTransform rect, float bottom, float width, float height, float x = 0f)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 0f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, bottom);
        }
    }

}
