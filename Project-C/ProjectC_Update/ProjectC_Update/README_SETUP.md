# Project C update: how to apply

0. **First**, in Window > Package Manager (Unity Registry) install: Multiplayer Services, Leaderboards, Authentication. The new scripts will not compile without them, so do this BEFORE copying files. Then link the project under Project Settings > Services.
1. Copy `Assets/Scripts` and `Assets/Editor` into your project (replace TimeController, PlayerController, LobbyHostStarter, PlayerInteractor, CropPlot, TeamWallet if present). Delete the old `MenuManager` button hookups if you see missing-script warnings.
2. (Packages already installed in step 0.)
3. Unity menu: **Project C > Run Full Setup** (builds prefabs, crop plots, shop, bench, pixel perfect cameras, sprite import fixes).
4. In the Unity Cloud Dashboard create a leaderboard with ID `nights_survived` (descending, keep best). Skip if you only want local scores.
5. Test: open Lobby, Play, "Play Solo / Same PC", Start Farm. For two players use ParrelSync: host in the main editor, "Join Local Host" in the clone.
6. Commit (and add a Unity `.gitignore`; remove `Project-C_clone_0` from the repo first).

Controls: WASD move, Shift run, Space attack, E interact/revive, Esc pause.
