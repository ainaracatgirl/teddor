using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForgeryController : MonoBehaviour {
    public GameObject menu;
    public Text alldata;
    public Text buyinfo;
    private int cursor;

    public PlayerController player;
    public ResourceController resources;
    public BuffController buffs;

    public AudioSource sfxCash;
    public AudioSource sfxSlide;
    public AudioSource sfxClick;

    private List<ForgeryOption> options = new List<ForgeryOption>();

    private void Start() {
        TranslateKey.Init();
    }

    void OnEnable() {
        alldata.text = GetOptionsAsText();
        menu.SetActive(true);
    }

    void OnDisable() {
        menu.SetActive(false);
    }

    void Update() {
        bool changed = false;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape)) enabled = false;

        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            cursor--;
            changed = true;
            sfxSlide.Play();
        } else if (Input.GetKeyDown(KeyCode.DownArrow)) {
            cursor++;
            changed = true;
            sfxSlide.Play();
        } else if (Input.GetKeyDown(KeyCode.Return)) {
            if (options.Count > 0) {
                ForgeryOption opt = options[cursor];

                int priceCoins = opt.reforge ? 2250 : 1750;
                int priceMatter = opt.reforge ? 125 : 75;

                if (resources.coins >= priceCoins && resources.bmatter >= priceMatter) {
                    if (opt.reforge) {
                        buffs.buffs.Sort((x, y) => {
                            return x.value.CompareTo(y.value);
                        });
                        
                        buffs.buffs.Sort((x, y) => {
                            return x.reforged.CompareTo(y.reforged);
                        });

                        PlayerBuff bufi = FindInReturn(buffs.buffs[0], player.stats.buffs);

                        if (buffs.buffs[0].reforged) {
                            buffs.buffs[0].value = player.stats.level + 35;
                        } else {
                            buffs.buffs[0].value = player.stats.level + 25;
                            buffs.buffs[0].reforged = true;
                        }

                        if (bufi != null) {
                            bufi.value = buffs.buffs[0].value;
                            bufi.reforged = true;

                            player.stats.Calculate();
                        }
                    } else {
                        PlayerBuff pb = new PlayerBuff();
                        pb.type = opt.type;
                        pb.value = player.stats.level + 10;
                        pb.reforged = false;

                        buffs.buffs.Add(pb);
                    }
                    resources.coins -= priceCoins;
                    resources.bmatter -= priceMatter;
                    sfxCash.Play();
                } else {
                    sfxClick.Play();
                }
                changed = true;
            }
        }

        if (changed) {
            if (cursor < 0) cursor = options.Count - 1;
            if (cursor >= options.Count) cursor = 0;
            alldata.text = GetOptionsAsText();
        }
    }

    void CalcOptions() {
        options.Clear();

        foreach (PlayerBuffType type in System.Enum.GetValues(typeof(PlayerBuffType))) {
            int cnt = CountOfType(type);
            if (cnt < 3)
                options.Add(new ForgeryOption(type, false));
            if (cnt > 0)
                options.Add(new ForgeryOption(type, true));
        }
    }

    string GetOptionsAsText() {
        string outp = "";
        buyinfo.text = "";

        CalcOptions();

        buffs.buffs.Sort((x, y) => {
            return x.value.CompareTo(y.value);
        });

        buffs.buffs.Sort((x, y) => {
            return x.reforged.CompareTo(y.reforged);
        });

        int end = Mathf.Min(options.Count, 10);
        for (int i = 0; i < end; i++) {
            ForgeryOption opt = options[i];
            if (i == cursor) {
                outp += "> ";
                int priceCoins = opt.reforge ? 2250 : 1750;
                int priceMatter = opt.reforge ? 125 : 75;

                if (resources.coins < priceCoins) buyinfo.text += "<color=red>";
                buyinfo.text += priceCoins;
                if (resources.coins < priceCoins) buyinfo.text += "</color>";
                buyinfo.text += "\n";
                if (resources.bmatter < priceMatter) buyinfo.text += "<color=red>";
                buyinfo.text += priceMatter;
                if (resources.bmatter < priceMatter) buyinfo.text += "</color>";
                buyinfo.text += "\n\n";

                if (opt.reforge) {
                    buyinfo.text += "<color=red>";
                    buyinfo.text += buffs.buffs[0].value;
                    buyinfo.text += "%</color> <color=yellow>>></color> <color=green>";
                    if (buffs.buffs[0].reforged) {
                        buyinfo.text += (player.stats.level + 35).ToString();
                    } else {
                        buyinfo.text += (player.stats.level + 25).ToString();
                    }
                    buyinfo.text += "%</color>";
                }

                buyinfo.text += "\n\n";

                if (resources.coins >= priceCoins && resources.bmatter >= priceMatter) {
                    buyinfo.text += TranslateKey.Translate("ui.foundry.forge") + "\n";
                }
            }


            outp += opt.type + " <color=yellow>";
            if (opt.reforge) outp += TranslateKey.Translate("ui.foundry.reforge");
            outp += "</color>\n";
        }

        buyinfo.text += TranslateKey.Translate("ui.foundry.back");

        return outp;
    }

    private int CountOfType(PlayerBuffType type) {
        int count = 0;
        foreach (PlayerBuff itm in buffs.buffs)
            if (itm.type == type) count++;
        return count;
    }

    private PlayerBuff FindInReturn(PlayerBuff obj, List<PlayerBuff> list) {
        foreach (PlayerBuff itm in list)
            if (itm.Equality(obj)) return itm;
        return null;
    }
}

[System.Serializable]
public class ForgeryOption {
    public PlayerBuffType type;
    public bool reforge;

    public ForgeryOption(PlayerBuffType type, bool reforge) {
        this.type = type;
        this.reforge = reforge;
    }
}