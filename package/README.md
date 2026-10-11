# Cartur's Mining

More to the Pickaxes skill you already have: hit deposits harder, get more ore out of them, and
once you're good enough, sometimes bring a whole deposit down in one blow.

Inspired by **Smoothbrain's [Mining](https://thunderstore.io/c/valheim/p/Smoothbrain/Mining/)**
(deprecated in 2026) - credit to Smoothbrain for the idea. This is a separate mod, written from
scratch for Valheim 1.0; it shares no code with theirs.

## What it does

Everything builds on the game's own Pickaxes skill. There is no second skill to grind.

| Pickaxes level | You get |
| --- | --- |
| any | More pickaxe damage on ore and rock - double at 100, on top of the game's own skill bonus. |
| any | More from every deposit - copper, silver, tin, obsidian, stone - double at 100. |
| 50 | A small chance (1 hit in 50) that a hit shatters the whole deposit at once, every chunk dropping its ore. |

**Ctrl + T** turns shattering off and on, for when you want a deposit to last.

## Settings

`BepInEx/config/com.jekkle.valheim.carturmining.cfg`

| Setting | Default | What it does |
| --- | --- | --- |
| `DamageBonus` | 1 | Extra pickaxe damage at level 100. 1 = double, 0 = vanilla. |
| `YieldBonus` | 1 | Extra ore at level 100, as a share of the normal drop. 1 = double. |
| `ShatterLevel` | 50 | Level from which a hit can shatter a deposit. |
| `ShatterChance` | 0.02 | Chance per hit, once you have the level. |
| `EnforceVersion` | true | On a server, refuse clients on a different version. |
| `ShatterKey` | Ctrl + T | Turns shattering off and on. |

Install it on the server too and its numbers apply to everyone. The key stays your own.

## Install

r2modman or Thunderstore Mod Manager, or drop `CarturMining.dll` into `BepInEx/plugins`.
Needs BepInEx.

---

*Free, and always will be. If it improved your game you can [tip me on Patreon](https://www.patreon.com/c/cartur).*

**[Discord](https://discord.gg/nd5RqpwNkz)** — bug reports, install help, and mod requests.
Bug reports get their own thread so nothing is lost in a chat scroll, and requests are voted on.

**More from Cartur:**
[Map Pins](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Map_Pins/) ·
[HD Blood](https://thunderstore.io/c/valheim/p/Cartur/Carturs_HD_Blood/) ·
[Compass and Clock](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Compass_and_Clock/) ·
[Safe Stamina](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Safe_Stamina/) ·
[Follow Command](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Follow_Command/) ·
[Flooring](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Flooring/) ·
[UI](https://thunderstore.io/c/valheim/p/Cartur/Carturs_UI_HUD/) ·
[Waste Management](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Waste_Management/) ·
[Feeding Trough](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Feeding_Trough/) ·
[Build Camera](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Build_Camera/) ·
[Combat Text](https://thunderstore.io/c/valheim/p/Cartur/Carturs_Combat_Text/)
