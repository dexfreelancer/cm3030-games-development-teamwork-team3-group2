# FLIPSIDE: City Lights

A 2D gravity-flip platformer. You are a small courier robot with one package to deliver to the
city's power core before the morning grid reset. Run, jump, and flip gravity to travel along
rooftops, bridge undersides and the maintenance grid, dodging maintenance lasers, spikes and
electrified panels while collecting optional data chips.

## Controls

| Action | Keys |
| --- | --- |
| Run | A / D or Left / Right |
| Jump | Space |
| Flip gravity | W, Up or Shift |
| Pause | Esc |
| Restart run (in pause / end screens) | R |
| Volume (in pause) | A / D music, S / W sound |

## Play

The game is built for the browser (WebGL). Open the published page or the contents of
`CityRunner/Builds/WebGL` from a local web server.

## Project

- Engine: Unity 6 (6000.3.6f1), Universal Render Pipeline, Input System, 2D physics.
- Project folder: `CityRunner`. Open it with Unity Hub.
- Game code and content live in `CityRunner/Assets/Flipside`:
  - `Scripts/Player` - movement, gravity flip, respawn, input, animation
  - `Scripts/World` - checkpoints, hazards, lasers, chips, districts, parallax
  - `Scripts/Game`, `Scripts/UI`, `Scripts/Audio` - run state, menus, HUD, audio
  - `Editor` - scene builder, import settings, build script
  - `Tests` - play-mode tests for the movement system
  - `Art`, `Audio`, `Fonts`, `Scenes`
- The level is generated from code. Use the editor menu **Flipside > Build CityRun Scene**
  to rebuild `Assets/Flipside/Scenes/CityRun.unity` after changing the layout in
  `Editor/BlockoutBuilder.cs`.
- **Flipside > Build WebGL** produces the browser build in `CityRunner/Builds/WebGL`.
- Tests: Window > General > Test Runner, PlayMode tab.

## Credits

Design, programming, level design, art and audio by the FLIPSIDE team.
Fonts: Audiowide and Rajdhani (SIL Open Font License, see `CityRunner/Assets/Flipside/Fonts`).
