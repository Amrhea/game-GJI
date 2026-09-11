# B1 — Multiplayer Foundation (Abim)

Host + Client LAN foundation (NGO 2.13.2 + Unity Transport, UDP port 7777).
**Do not modify Ami's gameplay scripts** — this folder only adds networking.

## Files
- `LanBootstrap.cs` — single bootstrap: NetworkManager + UnityTransport + Host/Client UI + hotkeys + MPPM auto-start.
- `NetworkPlayerMovement.cs` — owner-only input movement (InputSystem_Actions, Player/Move) synced via NetworkTransform (AuthorityMode = Owner).
- `MppmAutoStart.cs` — MPPM instance detection via the `-name` launch argument (works with Unity 6.6's built-in Play Mode Scenarios).

## Test scene
Menu: **Tools > Game Jam > Setup B1 Network Scene** → creates
`Assets/Prefabs/Net/NetworkPlayer.prefab` + `Assets/Scenes/NetTestScene.unity`.
(The scene must exist once; after that just open and press Play.)

## Controls
| Key | Action |
|---|---|
| F9 | Auto start (MPPM role) |
| F10 | Start Host |
| F11 | Start Client |
| F12 | Stop |

Buttons are also on-screen in the Game view.

## Testing with Multiplayer Play Mode (MPPM)
Unity 6.6 ships MPPM as **built-in (version 3.0.0, "Play Mode Scenarios")**;
`com.unity.multiplayer.playmode` is already in the manifest so Ami gets it on pull.

1. Open the Play Mode Scenarios window: **Window > Play Mode > Scenarios**.
2. Create a scenario with **one additional editor instance** (virtual player).
3. Leave the default instance names: main editor → `Player1`, additional → `Player2`.
   (Names drive the auto-start role: `Host`/`Client` in the name wins;
   otherwise Player1 → Host, any other → Client.)
4. Press Play — the main editor auto-starts Host, the virtual player auto-starts
   Client (loopback 127.0.0.1). Two players spawn at (-3,0,0) and (3,0,0).
5. Input follows the focused Game view window: use **Ctrl+F9** to switch focus to
   the virtual player and WASD to move it, or click the window directly.

## Physical LAN test (2 computers)
1. Build the project (add `Assets/Scenes/NetTestScene.unity` to Build Settings).
2. Computer A: run the build (or press Play in the editor) → **F10** (Host). Note the
   LAN IP shown in the panel.
3. Computer B: build → type host IP → **F11** (Client).

## Notes for integration (later tasks)
- Role identity (Killer/Janitor) is intentionally NOT here yet (task B2).
- `KillerMovement` will need an `IsOwner` gate when networked; coordinate with Ami.
- Do not re-run `KillerPrototypeSetup` after adding networking components to
  the Killer prefab (that generator overwrites prefabs).
