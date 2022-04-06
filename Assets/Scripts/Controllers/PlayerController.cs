using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
    [Header("Movement")]
    public float speed = 6f;
    public float bSpeed = 6f;
    private float speedmult = 1f;
    private float speedmulttime;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    private float velocity = 0f;
    private float dashTime = 0f;
    public LayerMask groundLayer;
    public Transform cam;
    public CharacterController controller;
    public Animation anim;

    public AudioSource hitAudioSource;
    public AudioClip[] hitSoundClips;

    [Header("Effects")]
    public GameObject slashPrefab;
    public GameObject burstSlashPrefab;
    public GameObject earthquakePrefab;
    public GameObject kaboomPrefab;
    public GameObject lightningPrefab;
    public GameObject splashPrefab;
    public GameObject firePrefab;

    public Transform slasher;
    public TrailRenderer slasherTrail;
    public ParticleSystem slasherParticles;

    public Transform waterVortexTPPoint;

    public Banner banner;
    public ForgeryController forgery;
    private Vector3 attackLock;
    private float attackCD;
    private int attackCnt;
    public bool hasDied;
    public bool canDie = true;
    private float iframes;

    [Header("Stats")]
    public PlayerStats stats;
    public float shieldValue = 0f;
    public Text lvltxt;
    public Transform hpbar;
    public Transform hpdmgbar;
    public Transform hpshieldbar;
    public Transform xpbar;
    public float burstModeMult = 0f;
    public float burstModeTime = 0f;

    public float bleedHP, healHP;

    [Header("Enemy HUD")]
    public GameObject enemyhud;
    public Text enemylvl;
    public Transform enemyhp;
    public Transform enemydmg;
    public Transform enemyshield;
    [HideInInspector]
    public Enemy lasthitenemy;
    [HideInInspector]
    public bool removeEnemyHUD;

    [Header("Storyline")]
    public DialogController dialog;
    public StorylineController story;

    [Header("Ability")]
    public Image abilityProgress;
    public Image abilityIcon;
    public Color abilityActiveColor;
    private float abilityCD = 0f;

    private float cdreduce = 0f;
    private float cdreducetime = 0f;

    [Header("Soul Shard")]
    public int soulCharge;
    public Transform soulShardGraphics;
    public Image soulChargeProgress;
    public Animation soulShardAnimation;
    public AudioSource soulShardAudio;
    public static bool soulShardAnimationPause;

    public GameObject ingameUI;
    public GameObject cinematicUI;

    void Start() {
        stats.Calculate();
        stats.hp = stats.maxhp;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() {
        if (!PauseMenu.open && !dialog.isOpen && !soulShardAnimationPause && !hasDied && !forgery.enabled && !banner.open) Controls();
        if (!PauseMenu.open && !dialog.isOpen && !soulShardAnimationPause && !hasDied && !forgery.enabled && !banner.open) AbilityControls();

        cinematicUI.SetActive(soulShardAnimationPause);
        ingameUI.SetActive(!soulShardAnimationPause);

        Vector3 hpbarscale = new Vector3(stats.hp / stats.maxhp, 1f, 1f);
        hpbar.localScale = hpbarscale;
        hpshieldbar.localScale = Vector3.Lerp(hpshieldbar.localScale, new Vector3(Mathf.Min(shieldValue / stats.maxhp, 1f), 1f, 1f), 5f * Time.deltaTime);
        hpdmgbar.localScale = Vector3.Lerp(hpdmgbar.localScale, hpbarscale, Time.deltaTime * 2.5f);
        lvltxt.text = "Lv. " + stats.level;
        xpbar.localScale = new Vector3(Mathf.Clamp01(stats.xp / stats.xptonext), 1f, 1f);

        if (shieldValue > 0f) {
            shieldValue -= shieldValue * .025f * Time.deltaTime;
        }

        if (removeEnemyHUD && lasthitenemy != null) {
            lasthitenemy = null;
            removeEnemyHUD = false;
        }
        if (lasthitenemy != null) {
            enemylvl.text = "Lv. " + lasthitenemy.level;
            enemyhp.localScale = new Vector3(lasthitenemy.hp / lasthitenemy.maxhp, 1f, 1f);
            enemydmg.localScale = new Vector3(Mathf.Lerp(enemydmg.localScale.x, lasthitenemy.hp / lasthitenemy.maxhp, 2.5f * Time.deltaTime), 1f, 1f);
            enemyshield.localScale = new Vector3(lasthitenemy.shield / lasthitenemy.maxhp, 1f, 1f);
        }
        enemyhud.SetActive(lasthitenemy != null);

        if (stats.hp <= 0f && !hasDied) {
            hasDied = true;
            if (canDie) LoadingScreen.SwitchScene(2);
        }

        int ssgc = soulShardGraphics.transform.childCount;
        for (int i = 0; i < ssgc; i++) {
            if (stats.soulShard.type <= 0) {
                soulShardGraphics.transform.GetChild(i).gameObject.SetActive(false);
                continue;
            }
            soulShardGraphics.transform.GetChild(i).gameObject.SetActive((int) stats.soulShard.type == i + 1);
        }

        float tfill = (float) soulCharge / (float) stats.soulShard.ChargeMax();
        if (soulChargeProgress.fillAmount > tfill) {
            soulChargeProgress.fillAmount = Mathf.Lerp(soulChargeProgress.fillAmount, 0f, 5f * Time.deltaTime);
        } else {
            soulChargeProgress.fillAmount = tfill;
        }

        if (stats.hp < stats.maxhp * .99f && stats.hp > 10f) {
            stats.hp += .001f * stats.maxhp * Time.deltaTime;
        }

        if (bleedHP > 0f) {
            float tobleed = bleedHP * Time.deltaTime;
            stats.hp -= tobleed;
            bleedHP -= tobleed;
        }
        if (healHP > 0f) {
            float toheal = healHP * Time.deltaTime;
            stats.hp += toheal;
            healHP -= toheal;
        }

        stats.hp = Mathf.Clamp(stats.hp, 0f, stats.maxhp);

        if (iframes > 0f) iframes -= Time.deltaTime;
    }

    public void ResetCD() {
        abilityCD = stats.ability.GetCooldown();
    }

    public void ZeroCD() {
        abilityCD = 0f;
    }

    public void Teleport(Vector3 worldPos) {
        controller.enabled = false;
        transform.position = worldPos;
        controller.enabled = true;
    }

    public void Teleport(Transform worldPos) {
        controller.enabled = false;
        transform.position = worldPos.position;
        controller.enabled = true;
    }

    public void HealMax() {
        stats.hp = stats.maxhp;
    }

    public void TeleportFade(Transform worldPos) {
        LoadingScreen.FadeInOutTeleport(1f, this, worldPos.position);
    }

    void AbilityControls() {
        if (abilityCD > stats.ability.GetCooldown()) abilityCD = stats.ability.GetCooldown();

        float tfill = 1f - (abilityCD / stats.ability.GetCooldown());
        if (abilityProgress.fillAmount > tfill) {
            abilityProgress.fillAmount = Mathf.Lerp(abilityProgress.fillAmount, 0f, 5f * Time.deltaTime);
        } else {
            abilityProgress.fillAmount = tfill;
        }
        Color targetCol = Color.white;
        if (abilityCD <= 0f)
            targetCol = abilityActiveColor;
        
        abilityProgress.color = Color.Lerp(abilityProgress.color, targetCol, 15f * Time.deltaTime);
        if (abilityCD > 0f) abilityCD -= Time.deltaTime;
        if (Input.GetMouseButtonDown(1) && abilityCD <= 0f) {
            abilityCD = stats.ability.GetCooldown();
            abilityCD -= cdreduce;
            if (abilityCD < 1f) abilityCD = 1f;
            stats.ability.Perform(this);
        }

        if (cdreduce > 0f && cdreducetime > 0f) {
            cdreducetime -= Time.deltaTime;
        } else if (cdreduce > 0f) {
            cdreduce = 0f;
        }

        if (burstModeMult > 0f) {
            if (burstModeTime > 0f) {
                burstModeTime -= Time.deltaTime;
            } else {
                burstModeMult = 0f;
                burstModeTime = 0f;
            }
        }
    }

    void Controls() {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 dir = new Vector3(horizontal, 0f, vertical).normalized;
        bSpeed = Mathf.Lerp(bSpeed, speed, 7.5f * Time.deltaTime);
        if (dir.magnitude >= 0.1f) {
            if (!anim.isPlaying) anim.Play();
            float targetAngle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);

            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * bSpeed * speedmult * Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashTime <= 0f) {
            iframes = .25f;
            bSpeed *= 5f;
            attackCD = .1f;
            dashTime = .5f;
        }

        if (attackCD > 0f) {
            attackCD -= Time.deltaTime;

            Vector2 toVector = attackLock - transform.position;
            float angleToTarget = Vector2.Angle(transform.forward, toVector);

            float target = FixAngle(angleToTarget + 90f);
            if (attackCnt == 0) {
                slasher.localEulerAngles += Vector3.up * 360f * 2.5f * Time.deltaTime;
            } else {
                slasher.localEulerAngles -= Vector3.up * 360f * 2.5f * Time.deltaTime;
            }
            if (FixAngle(slasher.localEulerAngles.y) >= target) {
                slasherTrail.emitting = false;
                slasherParticles.Stop();
            }
        }

        if (Input.GetMouseButtonDown(0) && attackCD <= 0f) {
            attackCnt++;
            attackCnt %= 2;
            Enemy[] enemies = FindObjectsOfType<Enemy>();
            Enemy tohit = null;
            float tohitdst = 3f;

            foreach (Enemy enemy in enemies) {
                if (!enemy.enabled || !enemy.gameObject.activeSelf) continue;
                float dst = Vector3.Distance(transform.position, enemy.transform.position);
                if (dst < tohitdst) {
                    tohitdst = dst;
                    tohit = enemy;
                }
            }
            if (tohit != null) {
                if (burstModeMult > 0f) {
                    tohit.TakeDamage(GetDamage(true, burstModeMult), this, false);
                } else {
                    tohit.TakeDamage(GetDamage(), this, false);
                }
                attackLock = tohit.transform.position;
                transform.LookAt(tohit.transform);
                Vector2 toVector = attackLock - transform.position;
                float angleToTarget = Vector2.Angle(transform.forward, toVector);
                if (attackCnt == 0) {
                    slasher.localEulerAngles = Vector3.up * (angleToTarget - 90f);
                } else {
                    slasher.localEulerAngles = Vector3.up * (angleToTarget + 90f);
                }

                OnHitEnemy(tohit);
                GameObject obj = Instantiate(burstModeMult > 0f ? burstSlashPrefab : slashPrefab);
                Vector3 midp = Vector3.Lerp(transform.position, tohit.transform.position, .6f);
                midp.y += .5f;
                obj.transform.position = midp;
                lasthitenemy = tohit;

                slasherTrail.Clear();
                slasherTrail.emitting = true;
                slasherParticles.Play();

                hitAudioSource.clip = hitSoundClips[Random.Range(0, hitSoundClips.Length)];
                hitAudioSource.Play();

                if (burstModeMult > 0f) {
                    attackCD = .1f;
                } else {
                    if (attackCnt == 0) {
                        attackCD = .25f;
                    } else {
                        attackCD = .15f;
                    }
                }
            }
        }

        velocity -= Time.deltaTime * 9.81f;
        controller.Move(Vector3.up * velocity * Time.deltaTime);
        if (controller.isGrounded && velocity < 0f) velocity = -.2f;
        if (Input.GetKeyDown(KeyCode.Space) && Physics.CheckSphere(transform.position + Vector3.down * .25f, .1f, groundLayer)) {
            velocity = 9.81f * .66f;
        }

        if (dashTime > 0f) dashTime -= Time.deltaTime;

        if (speedmult > 1f) {
            if (speedmulttime > 0f) {
                speedmulttime -= Time.deltaTime;
            } else {
                speedmult = 1f;
                speedmulttime = 0f;
            }
        }
    }

    public static float FixAngle(float angle) {
        if (angle > 180f) return FixAngle(180f - angle);
        return angle;
    }

    public void AddSpeedMult(float power, float time) {
        speedmult += power;
        speedmulttime = Mathf.Max(speedmulttime, time);
    }

    public void ReduceCD(float reduce, float time) {
        if (cdreduce == 888.888f) return;
        cdreduce += reduce;
        cdreducetime = Mathf.Max(cdreducetime, time);
        abilityCD -= reduce;
        if (abilityCD < 1f) abilityCD = 1f;
    }

    public void OnHitEnemy(Enemy e) {
        stats.hp += stats.maxhp * .001f;
        stats.hp = Mathf.Min(stats.hp, stats.maxhp);

        if (e.hp <= 0f) {
            soulCharge += 50;
        } else {
            soulCharge++;

            if (soulCharge >= stats.soulShard.ChargeMax()) {
                if (stats.soulShard != null && stats.soulShard.type != PlayerSoulShardType.NONE) {
                    soulCharge = 0;
                    soulShardAnimationPause = true;
                    soulShardAudio.Play();
                    StartCoroutine(DisableSoulShardAnimPause());
                    soulShardAnimation.Play();
                    cam.transform.position += cam.transform.forward * 2.5f;
                }
            }
        }
    }

    private IEnumerator DisableSoulShardAnimPause() {
        yield return new WaitForSeconds(1f);
        soulShardAnimationPause = false;
        stats.soulShard.Perform(this);
    }

    public void TakeDamage(float amount) {
        if (amount <= 0f) return;
        if (iframes > 0f) return;
        if (shieldValue > 0f) {
            shieldValue -= amount * .5f;
            if (shieldValue < 0f) {
                stats.hp -= shieldValue * -1f;
                shieldValue = 0f;
            }
        } else {
            stats.hp -= amount;
        }

        GameObject obj = Instantiate(slashPrefab);
        obj.transform.position = transform.position + Random.insideUnitSphere;

        stats.hp = Mathf.Max(stats.hp, 0f);
    }

    public void Heal(float amount) {
        stats.hp += amount;
        stats.hp = Mathf.Min(stats.hp, stats.maxhp);
    }

    public float GetDamage(bool burst = false, float multiplier = 1f) {
        float final = stats.atk;

        final *= multiplier;

        if (burst)
            final *= 1f + stats.GetTotalBuff(PlayerBuffType.BURST_DMG);

        if (Random.value < stats.critrate)
            final *= 1f + stats.critdmg;

        return final;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.GetComponent<BossDamage>() != null) {
            TakeDamage(other.GetComponent<BossDamage>().boss.atk);
        }
        if (other.CompareTag("Water")) {
            stats.hp = 0f;
            Instantiate(splashPrefab, transform.position, Quaternion.identity);
        } else if (other.CompareTag("WaterVortex")) {
            Teleport(waterVortexTPPoint);
            FindObjectOfType<VortexBefall>().Init();
        }
    }
}