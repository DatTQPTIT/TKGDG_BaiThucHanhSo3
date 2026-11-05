using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape
{
    public sealed class FlowerEscapeCombat : MonoBehaviour
    {
        public FlowerEscapeDemo ui;
        public RectTransform battleArea;
        public RectTransform projectileRoot;
        public RectTransform[] heroes;
        public Image[] enemies;
        public Image[] enemyHpBars;
        public Image boat;
        public Image boatHpFill;
        public Text boatHpText;
        public Text waveText;
        public Image muzzleFlash;
        public Sprite[] projectileSprites;
        public Font damageFont;

        private sealed class Bullet
        {
            public Image image;
            public int target;
            public float damage;
            public float speed;
            public bool hostile;
            public bool active;
        }

        private sealed class DamageText
        {
            public Text label;
            public float life;
        }

        private readonly List<Bullet> bullets = new List<Bullet>();
        private readonly List<DamageText> damageLabels = new List<DamageText>();
        private readonly float[] hp = new float[5];
        private readonly float[] maxHp = new float[5];
        private readonly float[] advance = new float[5];
        private readonly float[] nextAttack = new float[5];
        private bool running;
        private int wave;
        private int killsInWave;
        private int shotNumber;
        private float boatHp;
        private float shotTimer;
        private float waveTimer = -1f;
        private float stunUntil;
        private float flashUntil;
        private float feedbackUntil;
        private string feedback;

        public int CurrentWave => wave;
        public int AliveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < enemies.Length; i++) if (enemies[i].gameObject.activeSelf) count++;
                return count;
            }
        }
        public float BoatHealth => boatHp;
        public int ShotsFired => shotNumber;
        public bool IsRunning => running;

        public void BeginBattle()
        {
            running = true;
            wave = 1;
            boatHp = 100f;
            shotNumber = 0;
            shotTimer = 0.2f;
            waveTimer = -1f;
            stunUntil = 0f;
            feedback = null;
            boat.color = Color.white;
            if (muzzleFlash) muzzleFlash.enabled = false;
            foreach (var bullet in bullets) { bullet.active = false; bullet.image.gameObject.SetActive(false); }
            foreach (var label in damageLabels) label.label.gameObject.SetActive(false);
            UpdateBoatHealth();
            SpawnWave();
        }

        public void EndBattle()
        {
            running = false;
            if (muzzleFlash) muzzleFlash.enabled = false;
            foreach (var bullet in bullets) { bullet.active = false; if (bullet.image) bullet.image.gameObject.SetActive(false); }
        }

        private void Update()
        {
            if (!running || ui.state != GameplayUIState.Combat) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            if (muzzleFlash && muzzleFlash.enabled && Time.time >= flashUntil) muzzleFlash.enabled = false;
            if (boat.color != Color.white) boat.color = Color.Lerp(boat.color, Color.white, dt * 8f);
            if (feedback != null && Time.time >= feedbackUntil) { feedback = null; UpdateWaveLabel(); }
            UpdateEnemies(dt);
            UpdateBullets(dt);
            UpdateDamageLabels(dt);
            if (waveTimer >= 0f)
            {
                waveTimer -= dt;
                if (waveTimer <= 0f)
                {
                    waveTimer = -1f;
                    if (wave < 3) { wave++; SpawnWave(); }
                    else Victory();
                }
                return;
            }
            shotTimer -= dt;
            if (shotTimer <= 0f && AliveCount > 0)
            {
                Fire();
                shotTimer = Mathf.Clamp(1.05f - ui.GearCount() * 0.05f, 0.28f, 0.9f);
            }
        }

        private void SpawnWave()
        {
            killsInWave = 0;
            float[] xs = { .12f, .22f, .38f, .18f, .30f };
            float[] ys = { .30f, .66f, .78f, .46f, .28f };
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                enemy.gameObject.SetActive(true);
                enemy.color = Color.white;
                enemy.rectTransform.anchorMin = enemy.rectTransform.anchorMax = new Vector2(xs[i], ys[i]);
                enemy.rectTransform.anchoredPosition = Vector2.zero;
                hp[i] = maxHp[i] = 24f + wave * 12f + (i == 0 ? 26f : 0f);
                advance[i] = 0f;
                nextAttack[i] = Time.time + 1.2f;
                enemyHpBars[i].rectTransform.localScale = Vector3.one;
            }
            UpdateWaveLabel();
            UpdateProgress();
        }

        private void UpdateEnemies(float dt)
        {
            if (Time.time < stunUntil) return;
            float width = battleArea.rect.width;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].gameObject.activeSelf) continue;
                advance[i] = Mathf.Min(.44f, advance[i] + dt * (.010f + i * .002f + wave * .001f));
                enemies[i].rectTransform.anchoredPosition =
                    new Vector2(advance[i] * width, Mathf.Sin(Time.time * 2f + i) * 3f);
                bool inRange = enemies[i].rectTransform.anchorMin.x + advance[i] >= .60f;
                if ((i == 0 || i == 3 || inRange) && Time.time >= nextAttack[i])
                {
                    nextAttack[i] = Time.time + (inRange ? 1.2f : i == 0 ? 3.4f : 2.5f);
                    FireEnemy(i);
                }
            }
        }

        private void Fire()
        {
            int target = -1;
            float furthest = -1f;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].gameObject.activeSelf) continue;
                float x = enemies[i].rectTransform.anchorMin.x + advance[i];
                if (x > furthest) { furthest = x; target = i; }
            }
            if (target < 0) return;
            Bullet bullet = null;
            foreach (var candidate in bullets) if (!candidate.active) { bullet = candidate; break; }
            if (bullet == null) bullet = CreateBullet();
            bullet.active = true;
            bullet.hostile = false;
            bullet.target = target;
            bullet.damage = 5f + ui.GearPower() * .11f;
            bullet.speed = 300f;
            bullet.image.sprite = projectileSprites[shotNumber % projectileSprites.Length];
            bullet.image.gameObject.SetActive(true);
            var hero = heroes[shotNumber % heroes.Length];
            bullet.image.rectTransform.anchoredPosition = projectileRoot.InverseTransformPoint(hero.position);
            shotNumber++;
            if (muzzleFlash)
            {
                muzzleFlash.enabled = true;
                muzzleFlash.rectTransform.anchorMin = muzzleFlash.rectTransform.anchorMax =
                    new Vector2(shotNumber % 2 == 0 ? .45f : .60f, .75f);
                flashUntil = Time.time + .12f;
            }
        }

        private Bullet CreateBullet()
        {
            var obj = new GameObject("Gear Shot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(projectileRoot, false);
            var image = obj.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(.5f, .5f);
            image.rectTransform.sizeDelta = new Vector2(14f, 14f);
            var bullet = new Bullet { image = image };
            bullets.Add(bullet);
            return bullet;
        }

        private void FireEnemy(int index)
        {
            Bullet bullet = null;
            foreach (var candidate in bullets) if (!candidate.active) { bullet = candidate; break; }
            if (bullet == null) bullet = CreateBullet();
            bullet.active = true;
            bullet.hostile = true;
            bullet.target = index;
            bullet.damage = 1f + wave * .5f;
            bullet.speed = 170f;
            bullet.image.sprite = projectileSprites[projectileSprites.Length - 1];
            bullet.image.gameObject.SetActive(true);
            bullet.image.rectTransform.anchoredPosition =
                projectileRoot.InverseTransformPoint(enemies[index].rectTransform.position);
        }

        private void UpdateBullets(float dt)
        {
            foreach (var bullet in bullets)
            {
                if (!bullet.active) continue;
                if (!bullet.hostile && !enemies[bullet.target].gameObject.activeSelf)
                {
                    bullet.active = false;
                    bullet.image.gameObject.SetActive(false);
                    continue;
                }
                var target = (Vector2)projectileRoot.InverseTransformPoint(
                    bullet.hostile ? boat.rectTransform.position : enemies[bullet.target].rectTransform.position);
                var position = bullet.image.rectTransform.anchoredPosition;
                bullet.image.rectTransform.anchoredPosition = Vector2.MoveTowards(position, target, bullet.speed * dt);
                if (Vector2.Distance(bullet.image.rectTransform.anchoredPosition, target) < 10f)
                {
                    if (bullet.hostile)
                    {
                        boatHp = Mathf.Max(0f, boatHp - bullet.damage);
                        boat.color = new Color(1f, .55f, .55f);
                        UpdateBoatHealth();
                        if (boatHp <= 0f) Defeat();
                    }
                    else Hit(bullet.target, bullet.damage);
                    bullet.active = false;
                    bullet.image.gameObject.SetActive(false);
                }
            }
        }

        private void Hit(int index, float damage)
        {
            if (!enemies[index].gameObject.activeSelf) return;
            hp[index] = Mathf.Max(0f, hp[index] - damage);
            enemyHpBars[index].rectTransform.localScale = new Vector3(hp[index] / maxHp[index], 1f, 1f);
            enemies[index].color = new Color(1f, .7f, .7f);
            ShowDamage(index, Mathf.RoundToInt(damage));
            if (hp[index] > 0f) return;
            enemies[index].gameObject.SetActive(false);
            killsInWave++;
            ui.Earn(index == 0 ? 8 : 3);
            UpdateProgress();
            if (killsInWave == enemies.Length)
            {
                waveTimer = 1.5f;
                waveText.text = wave < 3 ? "WAVE CLEAR!" : "LEVEL CLEAR!";
            }
        }

        private void ShowDamage(int index, int amount)
        {
            DamageText item = null;
            foreach (var candidate in damageLabels) if (!candidate.label.gameObject.activeSelf) { item = candidate; break; }
            if (item == null)
            {
                var obj = new GameObject("Damage Number", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                obj.transform.SetParent(projectileRoot, false);
                var text = obj.GetComponent<Text>();
                text.font = damageFont;
                text.fontSize = 21;
                text.fontStyle = FontStyle.Bold;
                text.alignment = TextAnchor.MiddleCenter;
                text.raycastTarget = false;
                text.rectTransform.sizeDelta = new Vector2(55f, 30f);
                item = new DamageText { label = text };
                damageLabels.Add(item);
            }
            item.label.gameObject.SetActive(true);
            item.label.text = amount.ToString();
            item.label.color = Color.yellow;
            item.label.rectTransform.anchoredPosition =
                projectileRoot.InverseTransformPoint(enemies[index].rectTransform.position);
            item.life = .65f;
        }

        private void UpdateDamageLabels(float dt)
        {
            foreach (var item in damageLabels)
            {
                if (!item.label.gameObject.activeSelf) continue;
                item.life -= dt;
                item.label.rectTransform.anchoredPosition += Vector2.up * (42f * dt);
                var color = item.label.color;
                color.a = Mathf.Clamp01(item.life / .65f);
                item.label.color = color;
                if (item.life <= 0f) item.label.gameObject.SetActive(false);
            }
        }

        public void ApplyBooster(int index)
        {
            if (!running) return;
            switch (index)
            {
                case 0:
                    boatHp = Mathf.Min(100f, boatHp + 30f);
                    UpdateBoatHealth();
                    SetFeedback("HEAL +30");
                    break;
                case 1:
                    stunUntil = Time.time + 3f;
                    SetFeedback("STUN!");
                    break;
                case 2:
                    for (int i = 0; i < enemies.Length; i++) Hit(i, 18f);
                    SetFeedback("CHILI BLAST!");
                    break;
                case 3:
                    for (int i = 0; i < enemies.Length; i++) Hit(i, 35f);
                    SetFeedback("SUPERBEAM!");
                    break;
            }
        }

        private void SetFeedback(string message)
        {
            feedback = message;
            feedbackUntil = Time.time + .8f;
            waveText.text = message;
        }

        private void UpdateBoatHealth()
        {
            if (boatHpFill) boatHpFill.rectTransform.localScale = new Vector3(boatHp / 100f, 1f, 1f);
            if (boatHpText) boatHpText.text = Mathf.CeilToInt(boatHp).ToString();
        }

        private void UpdateWaveLabel()
        {
            if (waveText) waveText.text = "WAVE " + wave + "/3";
        }

        private void UpdateProgress()
        {
            if (!ui.progressFill) return;
            float progress = ((wave - 1) + killsInWave / (float)enemies.Length) / 3f;
            float width = 310f * progress;
            ui.progressFill.rectTransform.sizeDelta = new Vector2(width, 8f);
            ui.progressFill.rectTransform.anchoredPosition = new Vector2(width * .5f, 0f);
        }

        private void Victory()
        {
            running = false;
            waveText.text = "LEVEL CLEAR!";
            ui.Earn(50);
        }

        private void Defeat()
        {
            running = false;
            waveText.text = "BOAT LOST";
        }
    }
}
