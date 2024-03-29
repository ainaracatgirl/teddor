﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityController : MonoBehaviour {
    public Text alldata;
    private int cursor;

    public AudioSource slideSfx;
    public AudioSource clickSfx;

    public PlayerCombat player;
    public List<PlayerAbility> abilities;

    void OnEnable() {
        alldata.text = GetAbilitiesAsText();
    }

    void Update() {
        bool changed = false;

        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            cursor--;
            changed = true;
            slideSfx.Play();
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            cursor++;
            changed = true;
            slideSfx.Play();
        } else if (Input.GetKeyDown(KeyCode.Return)) {
            PlayerAbility ability = abilities[cursor];
            player.stats.ability = ability;
            player.combatAbilities.ResetCD();

            changed = true;
            clickSfx.Play();
        }

        if (changed) {
            if (cursor < 0) cursor = abilities.Count - 1;
            if (cursor >= abilities.Count) cursor = 0;
            alldata.text = GetAbilitiesAsText();
        }
    }

    string GetAbilitiesAsText() {
        string outp = "";

        int start = 0;
        if (cursor > 11) {
            start = cursor - 11;
        }
        int end = start + Mathf.Min(abilities.Count, 12);
        for (int i = start; i < end; i++) {
            PlayerAbility ability = abilities[i];
            if (i == cursor) outp += "> ";
            if (player.stats.ability.Equality(ability))
                outp += "<color=green>*</color> ";
            outp += ability.type.ToString().Replace('_', ' ') + " <color=red>[ Lv. " + ability.level.ToString() + " ]</color> <color=yellow>";
            outp += "".PadLeft(ability.GetStarCount(), '*');
            outp += "</color>\n";
        }

        return outp;
    }
}
