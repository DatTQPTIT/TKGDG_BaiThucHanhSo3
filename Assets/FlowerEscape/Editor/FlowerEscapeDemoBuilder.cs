using System;
using System.Linq;
using FlowerEscape;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlowerEscape.Editor
{
    public static class FlowerEscapeDemoBuilder
    {
        private const string Source = "Assets/Thực hành UI Game (4)/";
        private const string Output = "Assets/FlowerEscape/";
        private static Font font;

        [MenuItem("Tools/Flower Escape/Build UI Demo")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(Output.TrimEnd('/')))
                AssetDatabase.CreateFolder("Assets", "FlowerEscape");
            if (!AssetDatabase.IsValidFolder(Output + "Prefabs"))
                AssetDatabase.CreateFolder(Output.TrimEnd('/'), "Prefabs");
            if (!AssetDatabase.IsValidFolder(Output + "Scenes"))
                AssetDatabase.CreateFolder(Output.TrimEnd('/'), "Scenes");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.backgroundColor = new Color(.33f, .66f, .75f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var urpCameraType = TypeCache.GetTypesDerivedFrom<MonoBehaviour>()
                .FirstOrDefault(t => t.FullName == "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (urpCameraType != null) cameraObject.AddComponent(urpCameraType);
            new GameObject("Directional Light", typeof(Light)).GetComponent<Light>().type = LightType.Directional;
            var eventObject = new GameObject("EventSystem", typeof(EventSystem));
            var inputType = TypeCache.GetTypesDerivedFrom<BaseInputModule>()
                .FirstOrDefault(t => t.FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            var inputModule = eventObject.AddComponent(inputType ?? typeof(StandaloneInputModule));
            if (inputType != null)
                inputType.GetMethod("AssignDefaultActions")?.Invoke(inputModule, null);

            var canvasObject = new GameObject("FlowerEscapeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 1120);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            var canvasRect = canvasObject.GetComponent<RectTransform>();
            Full(canvasRect);
            var background = Img(canvasRect, "Background", "environment background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background.preserveAspect = false;
            background.raycastTarget = false;
            var safe = Rect(canvasRect, "SafeAreaRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var hud = Rect(safe, "SharedHUD", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 116), new Vector2(0, -58));
            var settings = Btn(hud, "Settings", "Setting", new Vector2(0, 1), new Vector2(0, 1), new Vector2(35, 39), new Vector2(23, -30));
            var returnButton = Btn(hud, "Return to Preparation", "Group 25", new Vector2(1, 1), new Vector2(1, 1), new Vector2(35, 38), new Vector2(-22, -30));
            var track = Img(hud, "Progress Track", "slider gameplay_background", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(310, 12), new Vector2(0, -28));
            track.preserveAspect = false;
            var fill = Img(track.rectTransform, "Progress Fill", "slider gameplay_fill_cyan", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(164, 8), new Vector2(82, 0));
            fill.preserveAspect = false;
            for (int i = 0; i < 4; i++)
            {
                var marker = Img(track.rectTransform, "Checkpoint " + (i + 1), "icon_skull", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(16, 16), new Vector2(30 + i * 86, 0));
                marker.preserveAspect = true;
            }
            var flag = Img(track.rectTransform, "Finish", "icon_flag finish", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(18, 18), new Vector2(-3, 0));
            flag.preserveAspect = true;
            Txt(hud, "Level", "Level 12", 18, TextAnchor.MiddleCenter, Color.white,
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(130, 27), new Vector2(0, -12));
            Img(hud, "Difficulty", "Hard", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(63, 24), new Vector2(0, -64)).preserveAspect = true;
            var coinBack = Solid(hud, "Currency Back", new Color(.17f, .28f, .42f, .82f), new Vector2(0, 1), new Vector2(0, 1), new Vector2(89, 28), new Vector2(44, -70));
            Img(coinBack.rectTransform, "Sun", "icon_sun", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(25, 25), new Vector2(13, 0)).preserveAspect = true;
            var currencyLabel = Txt(coinBack.rectTransform, "Currency Value", "165", 17, TextAnchor.MiddleCenter, Color.yellow,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(12, 0));

            var preparationOnly = Rect(safe, "PreparationRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var buy = Btn(preparationOnly, "Buy Coins", "Group", new Vector2(1, 1), new Vector2(1, 1), new Vector2(105, 46), new Vector2(-60, -143));
            Img(buy.GetComponent<RectTransform>(), "Buy Sun", "icon_sun", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(25, 25), new Vector2(27, 0)).preserveAspect = true;
            Txt(buy.GetComponent<RectTransform>(), "Buy Price", "50", 19, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(10, 0));

            var combatOnly = Rect(safe, "CombatRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var combatArea = Rect(combatOnly, "BattleArea", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(480, 380), Vector2.zero);
            var enemies = Rect(combatArea, "Enemies", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var enemyImages = new[]
            {
                AddEnemy(enemies, "Boss", "Boss1", new Vector2(.16f, .32f), new Vector2(126, 90)),
                AddEnemy(enemies, "Melee", "Mob1_Melee", new Vector2(.22f, .65f), new Vector2(39, 35)),
                AddEnemy(enemies, "Flying", "Mob1_Flying", new Vector2(.46f, .81f), new Vector2(37, 33)),
                AddEnemy(enemies, "Range", "Mob1_Range", new Vector2(.42f, .43f), new Vector2(39, 35)),
                AddEnemy(enemies, "Tank", "Mob1_Tank", new Vector2(.11f, .75f), new Vector2(53, 40))
            };
            var boat = Img(combatArea, "Boat", "boat", new Vector2(.72f, .36f), new Vector2(.72f, .36f), new Vector2(190, 150), Vector2.zero);
            boat.preserveAspect = true;
            var heroA = Img(boat.rectTransform, "Hero A", "char sexy", new Vector2(.45f, .69f), new Vector2(.45f, .69f), new Vector2(40, 45), Vector2.zero);
            heroA.preserveAspect = true;
            var heroB = Img(boat.rectTransform, "Hero B", "char tomboy", new Vector2(.60f, .69f), new Vector2(.60f, .69f), new Vector2(40, 45), Vector2.zero);
            heroB.preserveAspect = true;
            var boatHpBack = Solid(boat.rectTransform, "Boat HP Back", new Color(.13f, .21f, .32f),
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(112, 9), new Vector2(0, 10));
            var boatHpFill = Solid(boatHpBack.rectTransform, "Boat HP Fill", new Color(.20f, .85f, .36f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var boatHpText = Txt(boatHpBack.rectTransform, "Boat HP", "100", 10, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var muzzleFlash = Img(boat.rectTransform, "Muzzle Flash", "Glow",
                new Vector2(.5f, .75f), new Vector2(.5f, .75f), new Vector2(38, 38), Vector2.zero);
            muzzleFlash.enabled = false;
            var projectileRoot = Rect(combatArea, "Projectiles and Damage", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var waveLabel = Txt(combatArea, "Wave Label", "WAVE 1/3", 15, TextAnchor.MiddleCenter, Color.white,
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(130, 24), new Vector2(0, -17));

            var board = Rect(safe, "GearBoard", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(450, 500), Vector2.zero);
            Img(board, "Board Panel", "panel_rec_lightgreen-2", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).preserveAspect = false;
            var stats = Solid(board, "Gear Statistics", new Color(.12f, .23f, .37f, .92f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(-18, 30), new Vector2(0, -20));
            Img(stats.rectTransform, "Speed Icon", "wheel", new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(22, 22), new Vector2(21, 0)).preserveAspect = true;
            var speedLabel = Txt(stats.rectTransform, "Speed", "52.3 mph", 13, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0, 0), new Vector2(.5f, 1), Vector2.zero, new Vector2(9, 0));
            Img(stats.rectTransform, "Power Icon", "icon_sun", new Vector2(.52f, .5f), new Vector2(.52f, .5f), new Vector2(21, 21), Vector2.zero).preserveAspect = true;
            var powerLabel = Txt(stats.rectTransform, "Power", "73.3", 13, TextAnchor.MiddleCenter, Color.white,
                new Vector2(.5f, 0), Vector2.one, Vector2.zero, new Vector2(11, 0));
            string[] weapons = { "wheel", "Gun_berry", "gun", "", "sail" };
            var weaponSlots = new RectTransform[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                float x = (i - 2) * 70f;
                var slot = Img(board, "Weapon Slot " + i, "Slot gear - Empty", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(61, 55), new Vector2(x, -69));
                weaponSlots[i] = slot.rectTransform;
                if (weapons[i].Length > 0)
                    Img(slot.rectTransform, "Weapon", weapons[i], Vector2.zero, Vector2.one, new Vector2(-8, -8), Vector2.zero).preserveAspect = true;
            }
            var grid = Rect(board, "Gear Grid 6x4", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(420, 380), new Vector2(0, -96));
            grid.pivot = new Vector2(.5f, 1);
            var group = grid.gameObject.AddComponent<GridLayoutGroup>();
            group.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            group.constraintCount = 6;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.spacing = new Vector2(4, 4);
            var cells = new RectTransform[24];
            var cellIcons = new Image[24];
            var cellLevels = new Text[24];
            string[] flowerSprites = { "wheel", "Gear connected - highlighted", "Gear_frozen", "Gear_spider", "Gear connected" };
            int[] occupied = { 0, 1, 2, 5, 8, 11, 12, 15, 17, 19, 21, 23 };
            for (int i = 0; i < 24; i++)
            {
                var cell = Img(grid, "Cell " + (i / 6) + "," + (i % 6), "pad-2", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                cell.preserveAspect = true;
                cell.raycastTarget = true;
                var boardDrag = cell.gameObject.AddComponent<BoardGearDrag>();
                boardDrag.cellIndex = i;
                cells[i] = cell.rectTransform;
                var icon = Img(cell.rectTransform, "Gear", flowerSprites[i % flowerSprites.Length],
                    Vector2.zero, Vector2.one, new Vector2(-9, -9), Vector2.zero);
                icon.preserveAspect = true;
                icon.enabled = occupied.Contains(i);
                cellIcons[i] = icon;
                var level = Txt(cell.rectTransform, "Level", new[] { "2", "8", "4", "1" }[i % 4],
                    23, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                level.enabled = occupied.Contains(i);
                cellLevels[i] = level;
                if (i == 0)
                    PrefabUtility.SaveAsPrefabAsset(cell.gameObject, Output + "Prefabs/GearCell.prefab");
            }

            var shop = Rect(preparationOnly, "ShopPanel", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(450, 106), Vector2.zero);
            Solid(shop, "Shop Back", new Color(.48f, .30f, .69f, .98f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var shopIcons = new Image[4];
            var shopLevels = new Text[4];
            string[] shopSprites = { "Gear connected", "wheel", "Gear_frozen", "Gear connected - highlighted" };
            for (int i = 0; i < 4; i++)
            {
                var card = Img(shop, "Shop Card " + i, "button_buy gear", new Vector2((i + .5f) / 4f, .5f),
                    new Vector2((i + .5f) / 4f, .5f), new Vector2(72, 77), Vector2.zero);
                card.raycastTarget = true;
                var icon = Img(card.rectTransform, "Shop Gear", shopSprites[i], new Vector2(.5f, .62f), new Vector2(.5f, .62f),
                    new Vector2(46, 42), Vector2.zero);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                shopIcons[i] = icon;
                shopLevels[i] = Txt(card.rectTransform, "Shop Level", new[] { "1", "2", "4", "8" }[i],
                    25, TextAnchor.MiddleCenter, Color.white, new Vector2(.5f, .6f), new Vector2(.5f, .6f), new Vector2(48, 38), Vector2.zero);
                shopLevels[i].raycastTarget = false;
                Txt(card.rectTransform, "Cost", i == 2 ? "Free" : (5 + i * 5).ToString(), 13,
                    TextAnchor.MiddleCenter, Color.white, new Vector2(.5f, .15f), new Vector2(.5f, .15f), new Vector2(50, 20), Vector2.zero).raycastTarget = false;
                var drag = card.gameObject.AddComponent<ShopGearDrag>();
                drag.shopIndex = i;
                drag.icon = icon;
                if (i == 0)
                    PrefabUtility.SaveAsPrefabAsset(card.gameObject, Output + "Prefabs/ShopCard.prefab");
            }

            var controls = Rect(preparationOnly, "BottomControls", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(450, 68), Vector2.zero);
            Solid(controls, "Controls Back", new Color(.37f, .21f, .68f, .98f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var refresh = Btn(controls, "Refresh Button", "button_big_green", new Vector2(.3f, .5f), new Vector2(.3f, .5f), new Vector2(112, 49), Vector2.zero);
            var refreshLabel = Txt(refresh.GetComponent<RectTransform>(), "Refresh Label", "Refresh", 17, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, 5));
            Txt(refresh.GetComponent<RectTransform>(), "Refresh Price", "● 5", 12, TextAnchor.MiddleCenter, Color.yellow,
                new Vector2(0, 0), new Vector2(1, .48f), Vector2.zero, Vector2.zero);
            var start = Btn(controls, "START Button", "button_big_cyan", new Vector2(.72f, .5f), new Vector2(.72f, .5f), new Vector2(185, 52), Vector2.zero);
            Txt(start.GetComponent<RectTransform>(), "START Label", "START!", 23, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var boosters = Rect(combatOnly, "BoosterPanel", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(55, 400), Vector2.zero);
            string[] boosterSprites = { "icon_Booster_heart heal", "icon_Booster_Stun storm", "icon_Booster_chili belle", "icon_Booster_superbeam" };
            var boosterButtons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                var b = Btn(boosters, "Booster " + i, "button_booster", new Vector2(.5f, 1f - (i + .5f) / 4f),
                    new Vector2(.5f, 1f - (i + .5f) / 4f), new Vector2(48, 55), Vector2.zero);
                Img(b.GetComponent<RectTransform>(), "Booster Icon", boosterSprites[i], new Vector2(.5f, .55f), new Vector2(.5f, .55f),
                    new Vector2(34, 34), Vector2.zero).preserveAspect = true;
                Txt(b.GetComponent<RectTransform>(), "Quantity", "3", 14, TextAnchor.MiddleCenter, Color.white,
                    new Vector2(.8f, .16f), new Vector2(.8f, .16f), new Vector2(18, 18), Vector2.zero);
                boosterButtons[i] = b;
            }
            var controller = new GameObject("UIControllers").AddComponent<FlowerEscapeDemo>();
            controller.canvas = canvas;
            controller.safeAreaRoot = safe;
            controller.board = board;
            controller.grid = grid;
            controller.shop = shop;
            controller.controls = controls;
            controller.combatArea = combatArea;
            controller.boosterPanel = boosters;
            controller.preparationOnly = preparationOnly.gameObject;
            controller.combatOnly = combatOnly.gameObject;
            controller.startButton = start;
            controller.refreshButton = refresh;
            controller.returnButton = returnButton;
            controller.buyCoinsButton = buy;
            controller.boosterButtons = boosterButtons;
            controller.refreshLabel = refreshLabel;
            controller.currencyLabel = currencyLabel;
            controller.speedLabel = speedLabel;
            controller.powerLabel = powerLabel;
            controller.progressFill = fill;
            controller.shopIcons = shopIcons;
            controller.shopLevels = shopLevels;
            controller.itemSprites = shopSprites.Select(Sprite).ToArray();
            controller.cellIcons = cellIcons;
            controller.cellLevels = cellLevels;
            controller.cells = cells;
            controller.weaponSlots = weaponSlots;
            controller.boat = boat;
            foreach (var drag in shop.GetComponentsInChildren<ShopGearDrag>()) drag.demo = controller;
            foreach (var drag in grid.GetComponentsInChildren<BoardGearDrag>()) drag.demo = controller;
            var combat = new GameObject("CombatController").AddComponent<FlowerEscapeCombat>();
            combat.transform.SetParent(controller.transform, false);
            combat.ui = controller;
            combat.battleArea = combatArea;
            combat.projectileRoot = projectileRoot;
            combat.heroes = new[] { heroA.rectTransform, heroB.rectTransform };
            combat.enemies = enemyImages;
            combat.enemyHpBars = enemyImages.Select(e => e.transform.Find("HP").GetComponent<Image>()).ToArray();
            combat.boat = boat;
            combat.boatHpFill = boatHpFill;
            combat.boatHpText = boatHpText;
            combat.waveText = waveLabel;
            combat.muzzleFlash = muzzleFlash;
            combat.projectileSprites = new[] { Sprite("dot_cyan"), Sprite("dot_green"), Sprite("dot_red") };
            combat.damageFont = font;
            controller.combat = combat;
            preparationOnly.gameObject.SetActive(true);
            combatOnly.gameObject.SetActive(false);
            returnButton.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, Output + "Scenes/FlowerEscape_UI_Demo.unity");
            AssetDatabase.SaveAssets();
            Debug.Log("Flower Escape UI demo built: 6x4 gear board, 4 shop cards, 4 boosters.");
        }

        private static Image AddEnemy(RectTransform parent, string name, string sprite, Vector2 anchor, Vector2 size)
        {
            var enemy = Img(parent, name, sprite, anchor, anchor, size, Vector2.zero);
            enemy.preserveAspect = true;
            var hp = Solid(enemy.rectTransform, "HP", new Color(.74f, .11f, .32f),
                new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(size.x * .7f, 5), new Vector2(0, 5));
            hp.raycastTarget = false;
            return enemy;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void Full(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static Image Img(Transform parent, string name, string sprite, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            var rect = Rect(parent, name, min, max, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            if (!string.IsNullOrEmpty(sprite)) image.sprite = Sprite(sprite);
            image.raycastTarget = false;
            return image;
        }

        private static Image Solid(Transform parent, string name, Color color, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            var image = Img(parent, name, "", min, max, size, position);
            image.color = color;
            return image;
        }

        private static Button Btn(Transform parent, string name, string sprite, Vector2 min, Vector2 max, Vector2 size, Vector2 position)
        {
            var image = Img(parent, name, sprite, min, max, size, position);
            image.raycastTarget = true;
            return image.gameObject.AddComponent<Button>();
        }

        private static Text Txt(Transform parent, string name, string text, int size, TextAnchor align, Color color,
            Vector2 min, Vector2 max, Vector2 rectSize, Vector2 position)
        {
            var rect = Rect(parent, name, min, max, rectSize, position);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = align;
            label.color = color;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = size;
            return label;
        }

        private static Sprite Sprite(string name)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Source + name + ".png").OfType<Sprite>().ToArray();
            if (sprites.Length == 0) Debug.LogError("Missing Flower Escape sprite: " + name);
            return sprites.Length > 0 ? sprites[0] : null;
        }
    }
}
