﻿using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class PlayerAbility {
    public PlayerAbilityType type;
    public int level;

    public int GetStarCount() {
        if (type == PlayerAbilityType.METEOR || type == PlayerAbilityType.EARTHQUAKE) {
            return 5;
        } else if (type == PlayerAbilityType.BOLT || type == PlayerAbilityType.WATERJET || type == PlayerAbilityType.IGNITION) {
            return 6;
        }

        return 4;
    }

    public float GetCooldown() {
        switch (type) {
            case PlayerAbilityType.RANGED:
                return 1.5f;
            case PlayerAbilityType.SHIELD:
                return 25f;
            case PlayerAbilityType.HEAL:
                return 20f;
            case PlayerAbilityType.BLINK:
                return 4.5f;
            case PlayerAbilityType.METEOR:
                return 4.5f;
            case PlayerAbilityType.EARTHQUAKE:
                return 5.5f;
            case PlayerAbilityType.BOLT:
                return 3.5f;
            case PlayerAbilityType.WATERJET:
                return 2.0f;
            case PlayerAbilityType.IGNITION:
                return 3.0f;
            default:
                return 5f;
        }
    }

    public void Perform(PlayerCombat player) {
        float level = Mathf.Min(player.stats.level, this.level);

        if (type == PlayerAbilityType.RANGED) {
            Enemy targetenemy = Enemy.GetEnemyInRadius(player.transform.position, 25f);
            if (targetenemy != null) {
                player.lasthitenemy = targetenemy;
                targetenemy.TakeDamage(player.GetDamage(true, .5f + (level - 1) * .02f), player, false);
                player.OnHitEnemy(targetenemy);
                GameObject.Instantiate(player.burstSlashPrefab).transform.position = targetenemy.transform.position;
            }
        } else if (type == PlayerAbilityType.SHIELD) {
            float mult = Mathf.Clamp01(.25f + (level - 1) * .05f);
            player.shieldValue = player.stats.maxhp * mult;
        } else if (type == PlayerAbilityType.HEAL) {
            float mult = .1f + (level - 1) * .05f;
            float value = player.stats.maxhp * mult;
            player.healHP += value;
        } else if (type == PlayerAbilityType.BLINK) {
            float mult = 2.5f + (level - 1) * .1f;
            player.GetComponent<PlayerMovement>().AddSpeedMult(mult, 3.5f);
        } else if (type == PlayerAbilityType.METEOR) {
            float mult = 1f + (level - 1) * .05f;
            if (player.burstModeMult < mult) player.burstModeMult = mult;
            player.burstModeTime += 1.5f;
            GameObject.Instantiate(player.kaboomPrefab, player.transform.position, Quaternion.identity);
            AoE(player, 5f, mult, false, true, false);
        } else if (type == PlayerAbilityType.EARTHQUAKE) {
            float mult = 1f + (level - 1) * .05f;
            GameObject.Instantiate(player.earthquakePrefab, player.transform.position, Quaternion.identity);
            AoE(player, 10f, mult, false, false, true);
        } else if (type == PlayerAbilityType.BOLT) {
            float mult = 2f + (level - 1) * .08f;
            GameObject.Instantiate(player.lightningPrefab, player.transform.position, Quaternion.identity);
            AoE(player, 5f, mult, false, false, false);
        } else if (type == PlayerAbilityType.WATERJET) {
            float mult = 1f + (level - 1) * .05f * (1f + player.stats.GetTotalBuff(PlayerBuffType.BLEED_DMG)) * (1f + player.stats.GetTotalBuff(PlayerBuffType.ATK));
            List<Enemy> hittable = Enemy.GetEnemiesInRadius(player.transform.position, 5f);
            foreach (Enemy enemy in hittable) {
                enemy.bleed += mult;
                player.lasthitenemy = enemy;
            }
            GameObject.Instantiate(player.splashPrefab, player.transform.position, Quaternion.identity);
        } else if (type == PlayerAbilityType.IGNITION) {
            float mult = 1f + (level - 1) * .05f * (1f + player.stats.GetTotalBuff(PlayerBuffType.BLEED_DMG)) * (1f + player.stats.GetTotalBuff(PlayerBuffType.ATK)) * (1f + player.stats.GetTotalBuff(PlayerBuffType.BURST_DMG));

            List<Enemy> hittable = Enemy.GetEnemiesInRadius(player.transform.position, 5f);
            foreach (Enemy enemy in hittable) {
                enemy.flaming += mult;
                player.lasthitenemy = enemy;
            }
            GameObject.Instantiate(player.firePrefab, player.transform.position, Quaternion.identity);
        }
    }

    private void AoE(PlayerCombat pc, float radius, float mult, bool particles, bool shieldDMG, bool trueDMG) {
        List<Enemy> hittable = Enemy.GetEnemiesInRadius(pc.transform.position, radius);
        foreach (Enemy enemy in hittable) {
            if (enemy.shield > 0f && shieldDMG) {
                mult += 1f;
            }
            enemy.TakeDamage(pc.GetDamage(true, mult), pc, trueDMG);
            pc.OnHitEnemy(enemy);
            if (particles) {
                GameObject obj = GameObject.Instantiate(pc.burstSlashPrefab);
                obj.transform.position = enemy.transform.position;
            }
            pc.lasthitenemy = enemy;
        }
    }

    public bool Equality(PlayerAbility obj) {
        return obj.level == level && obj.type == type;
    }
}