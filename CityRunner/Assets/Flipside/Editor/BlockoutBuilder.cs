using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Flipside.EditorTools
{
    /// <summary>
    /// Builds the full CityRun scene from code: tutorial rooftop, District 1 Rooftops,
    /// District 2 The Crossing, District 3 The Grid, player, camera, HUD, menus and audio.
    /// Layout lives here so it stays reproducible and easy to tune. When an art sprite exists
    /// under Assets/Flipside/Art/Sprites it is used, otherwise the object is a grey block.
    /// </summary>
    public static class BlockoutBuilder
    {
        const string WhitePath = "Assets/Flipside/Art/Blockout/white.png";
        const string ArtFolder = "Assets/Flipside/Art/Sprites/";
        const string AudioFolder = "Assets/Flipside/Audio/";
        const string PhysicsMaterialPath = "Assets/Flipside/Physics/Frictionless.physicsMaterial2D";
        public const string ScenePath = "Assets/Flipside/Scenes/CityRun.unity";

        enum Surface { Rooftop, Underside, Bridge, Grid, Wall }

        static readonly Color RooftopTint = new Color(0.43f, 0.45f, 0.50f);
        static readonly Color UndersideTint = new Color(0.34f, 0.36f, 0.40f);
        static readonly Color BridgeTint = new Color(0.40f, 0.38f, 0.34f);
        static readonly Color GridTint = new Color(0.30f, 0.40f, 0.44f);
        static readonly Color WallTint = new Color(0.22f, 0.24f, 0.28f);
        static readonly Color Robot = new Color(0.49f, 0.98f, 1f);
        static readonly Color SpikesTint = new Color(1f, 0.3f, 0.35f);
        static readonly Color PanelTint = new Color(1f, 0.55f, 0.15f);
        static readonly Color ChipTint = new Color(0.6f, 1f, 0.4f);
        static readonly Color BeamColor = new Color(1f, 0.2f, 0.25f, 0.85f);
        static readonly Color DangerRed = new Color(1f, 0.15f, 0.2f); // every deadly thing glows this one colour
        static readonly Color Background = new Color(0.043f, 0.059f, 0.118f);
        static readonly Color UiPanel = new Color(0.02f, 0.03f, 0.07f, 0.85f);

        static Sprite white;
        static PhysicsMaterial2D frictionless;
        static Font font;      // body text (Rajdhani)
        static Font titleFont; // display text (Audiowide)

        const float LevelEnd = 760f;

        [MenuItem("Flipside/Build CityRun Scene")]
        public static void BuildCityRun()
        {
            white = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
            if (white == null) throw new System.IO.FileNotFoundException("Missing " + WhitePath);
            frictionless = EnsurePhysicsMaterial();
            Font fallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Flipside/Fonts/Rajdhani-Bold.ttf") ?? fallback;
            titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Flipside/Fonts/Audiowide-Regular.ttf") ?? font;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            slabs.Clear();
            new GameObject("RunState").AddComponent<RunState>();
            BuildAudio();

            Transform level = new GameObject("Level").transform;
            Transform deco = new GameObject("Decoration").transform;
            BuildBackground();

            // ================= LAYOUT =================
            // The run is one enclosed corridor: a thick floor below and a thick ceiling above almost
            // everywhere. Floor gaps force a flip up, ceiling gaps force an inverted jump, and in the Grid
            // the alternating floor-only / ceiling-only rooms have electrified "hazard ceilings" so the
            // only safe route is the flip staircase. Surface tops (floorTop) and undersides (ceilBottom) are in world units.

            // ---- Opening rooftop (tutorial) ----
            Wall(level, -3f, -14f, 26f);
            Seg(level, 0f, 14f, 0f, 8f, Surface.Rooftop);            // calm start
            Seg(level, 14f, 17f, null, 8f, Surface.Rooftop);         // small gap: jump
            Seg(level, 17f, 26f, 0f, 8f, Surface.Rooftop);
            Seg(level, 26f, 33f, null, 7f, Surface.Underside);       // wide gap: the taught flip (sign underside)
            Seg(level, 33f, 35.5f, null, null, Surface.Underside);   // ceiling gap: inverted jump
            Seg(level, 35.5f, 40f, null, 7f, Surface.Underside);
            Seg(level, 40f, 56f, 0f, 7f, Surface.Rooftop);           // back down onto the rooftop
            Sign("sign_1", deco, new Vector2(29.5f, 5.2f), 3f);
            Sign("sign_2", deco, new Vector2(46f, 5f), 3f);
            FlipPromptZone(level, new Vector2(24f, 2f), new Vector2(4f, 4f), new Vector2(25f, 3.8f));
            Checkpoint("Checkpoint 1", level, new Vector2(50f, 0f), startsClock: false);
            DirectionArrow(level, new Vector2(5f, 2.4f));
            Prop("prop_water_tower", deco, new Vector2(9f, 0f), false);
            Prop("prop_ac_unit", deco, new Vector2(4.5f, 0f), false);
            Prop("prop_lamp", deco, new Vector2(20f, 8f), true);
            Prop("prop_pipes", deco, new Vector2(48f, 7f), true);
            Prop("prop_crates", deco, new Vector2(44f, 0f), false);

            // ---- District 1: Rooftops ----
            District(level, 1, "Rooftops", 54f);
            Seg(level, 56f, 66f, null, 6f, Surface.Underside);       // flip up, run the underside
            Seg(level, 66f, 80f, -2f, 6f, Surface.Rooftop);          // lower rooftop
            Chip("Chip 1", level, new Vector2(72f, -1f));
            Spikes("Spikes 1", level, new Vector2(77f, -1.75f), 2f, false);
            DangerDemoZone(level, new Vector2(68f, 0f), new Vector2(8f, 6f), new Vector2(69f, 0.4f), 75f);
            Drone("Drone 1", level, new Vector2(95f, 1.1f), new Vector2(95f, 5.9f), 3.6f, 0f);
            Seg(level, 80f, 92f, null, 5f, Surface.Underside);       // gap with the optional chip ledge below
            Block("Chip Ledge", level, new Vector2(86f, -8.5f), new Vector2(4f, 3f), Surface.Rooftop, Edge.Top);
            Chip("Chip 2", level, new Vector2(86f, -6f));
            Seg(level, 92f, 98f, 0f, 7f, Surface.Rooftop);
            Seg(level, 98f, 100.5f, 0f, null, Surface.Rooftop);      // ceiling gap: inverted runners must jump
            Seg(level, 100.5f, 112f, 0f, 7f, Surface.Rooftop);
            Panel("Electric Panel 1", level, new Vector2(104f, 0.15f), 2f);
            Spikes("Ceiling Spikes 1", level, new Vector2(108f, 6.75f), 2f, true);
            Chip("Chip 3", level, new Vector2(104f, 4.6f));
            Sign("sign_3", deco, new Vector2(61f, 3.6f), 3.2f);
            Sign("sign_4", deco, new Vector2(96f, 4.2f), 2.4f);
            Prop("prop_antenna", deco, new Vector2(70f, -2f), false);
            Prop("prop_lamp", deco, new Vector2(88f, 5f), true);
            Prop("prop_vending", deco, new Vector2(94.5f, 0f), false);
            Prop("prop_pipes", deco, new Vector2(106f, 7f), true);

            // ---- District 2: The Crossing ----
            District(level, 2, "The Crossing", 114f);
            Seg(level, 112f, 124f, 0f, 8f, Surface.Bridge);
            Checkpoint("Checkpoint 3", level, new Vector2(116f, 0f), startsClock: false);
            Seg(level, 124f, 156f, null, 4f, Surface.Bridge);        // bridge 1 underside, laser sweeps from below
            LaserRotate("Laser 1", level, new Vector2(142f, -6f), 90f, -32f, 32f, 9f, 4.5f, 0f);
            Seg(level, 156f, 170f, 0f, 9f, Surface.Bridge);          // gantry room
            Chip("Chip 4", level, new Vector2(158f, 1f));
            Spikes("Spikes 2", level, new Vector2(166f, 0.25f), 2f, false);
            Chip("Chip 5", level, new Vector2(163f, 7.5f));
            Seg(level, 170f, 200f, null, 5f, Surface.Bridge);        // bridge 2 underside, sliding laser
            LaserTranslate("Laser 2", level, new Vector2(178f, 5f), new Vector2(194f, 5f), -90f, 8f, 6f, 0.25f);
            Seg(level, 200f, 216f, 0f, 7f, Surface.Bridge);
            Checkpoint("Checkpoint 4", level, new Vector2(202f, 0f), startsClock: false);
            Panel("Electric Panel 2", level, new Vector2(208f, 0.15f), 2f);
            Chip("Chip 6", level, new Vector2(212f, 4.6f));
            Drone("Drone 2", level, new Vector2(211f, 1.1f), new Vector2(211f, 5.9f), 3.2f, 0.5f);
            Seg(level, 216f, 240f, null, 4f, Surface.Bridge);        // bridge 3 underside
            LaserRotate("Laser 3", level, new Vector2(230f, -6f), 90f, -30f, 30f, 9f, 3.8f, 0.5f);
            Sign("sign_2", deco, new Vector2(120f, 5.2f), 3.5f);
            Sign("sign_1", deco, new Vector2(163f, 5.5f), 3.5f);
            Prop("prop_crates", deco, new Vector2(114f, 0f), false);
            Prop("prop_lamp", deco, new Vector2(140f, 4f), true);
            Prop("prop_lamp", deco, new Vector2(185f, 5f), true);
            Prop("prop_ac_unit", deco, new Vector2(205f, 0f), false);
            Prop("prop_pipes", deco, new Vector2(226f, 4f), true);

            // ---- District 3: The Grid ----
            District(level, 3, "The Grid", 240f);
            Seg(level, 240f, 252f, 0f, 8f, Surface.Grid);
            Checkpoint("Checkpoint 5", level, new Vector2(243f, 0f), startsClock: false);
            Seg(level, 252f, 254f, null, null, Surface.Grid);        // void
            Seg(level, 254f, 262f, null, 6f, Surface.Grid);          // ceiling-only room
            Seg(level, 262f, 264f, null, null, Surface.Grid);
            Seg(level, 264f, 272f, 0f, 9f, Surface.Grid, true);     // floor-only room (hazard ceiling)
            Chip("Chip 7", level, new Vector2(268f, 3f));
            Seg(level, 272f, 274f, null, null, Surface.Grid);
            Seg(level, 274f, 282f, null, 6f, Surface.Grid);
            LaserTranslate("Laser 4", level, new Vector2(277f, 6f), new Vector2(281f, 6f), -90f, 6f, 4f, 0f);
            Seg(level, 282f, 284f, null, null, Surface.Grid);
            Seg(level, 284f, 312f, 0f, 9f, Surface.Grid, true);
            Panel("Electric Panel 3", level, new Vector2(288f, 0.15f), 2f);
            Panel("Electric Panel 4", level, new Vector2(293f, 0.15f), 2f);
            Checkpoint("Checkpoint 6", level, new Vector2(299f, 0f), startsClock: false);
            Chip("Chip 8", level, new Vector2(308f, 1f));
            Drone("Drone 3", level, new Vector2(302f, 1.1f), new Vector2(310f, 1.1f), 4.2f, 0f);
            // Final staircase: N (floor 0) -> O (ceiling 8) -> P (floor 2) -> Q (ceiling 9) -> R (floor 0)
            Seg(level, 312f, 314f, null, null, Surface.Grid);
            Seg(level, 314f, 320f, null, 8f, Surface.Grid);
            Seg(level, 320f, 322f, null, null, Surface.Grid);
            Seg(level, 322f, 330f, 2f, 12f, Surface.Grid, true);
            Panel("Electric Panel 5", level, new Vector2(327f, 2.15f), 1.6f);
            Seg(level, 330f, 332f, null, null, Surface.Grid);
            Seg(level, 332f, 338f, null, 9f, Surface.Grid);
            Seg(level, 338f, 340f, null, null, Surface.Grid);
            Seg(level, 340f, 372f, 0f, 10f, Surface.Grid);          // grid exit hall
            LaserRotate("Laser 5", level, new Vector2(340f, -6f), 90f, -26f, 26f, 12f, 4f, 0.25f);
            Chip("Chip 9", level, new Vector2(346f, 1f));
            Drone("Drone 4", level, new Vector2(350f, 1.1f), new Vector2(350f, 8.9f), 3.4f, 0.25f);
            Prop("prop_lamp", deco, new Vector2(246f, 8f), true);
            Prop("prop_pipes", deco, new Vector2(300f, 9f), true);
            Prop("prop_crates", deco, new Vector2(354f, 0f), false);
            Prop("prop_lamp", deco, new Vector2(358f, 10f), true);
            Prop("prop_vending", deco, new Vector2(343f, 0f), false);

            // ================= DISTRICT 4: SUBSTATION =================
            // New verbs, one at a time: crumbling floors, blink gates, gravity lock, switch gates, portals.
            District(level, 4, "Substation", 372f);
            Seg(level, 372f, 380f, 0f, 8f, Surface.Grid);
            Checkpoint("Checkpoint 7", level, new Vector2(375f, 0f), startsClock: false);
            // (a) crumble run: two unstable plates over a pit, ceiling continuous above
            Seg(level, 380f, 392f, null, 8f, Surface.Grid);
            Crumble("Crumble 1", level, new Vector2(383f, -0.75f), new Vector2(6f, 1.5f));
            Crumble("Crumble 2", level, new Vector2(389f, -0.75f), new Vector2(6f, 1.5f));
            Seg(level, 392f, 420f, 0f, 7f, Surface.Grid);
            // (b) blink gate: three vertical beams switching in sequence, a moving safe window
            LaserBlink("Blink 1", level, new Vector2(400f, 7f), 7f, 1.4f, 1.6f, 0f);
            LaserBlink("Blink 2", level, new Vector2(408f, 7f), 7f, 1.4f, 1.6f, 1.0f);
            LaserBlink("Blink 3", level, new Vector2(416f, 7f), 7f, 1.4f, 1.6f, 2.0f);
            Chip("Chip 10", level, new Vector2(412f, 4.5f));
            // (c) gravity lock: no flips inside, cross the gaps by jumping; drone guards the exit
            Seg(level, 420f, 430f, 0f, 7f, Surface.Grid);
            Seg(level, 430f, 434f, null, 7f, Surface.Grid);
            Seg(level, 434f, 440f, 0f, 7f, Surface.Grid);
            Seg(level, 440f, 444f, null, null, Surface.Grid);
            Seg(level, 444f, 452f, 0f, 7f, Surface.Grid);
            GravityLock(level, new Vector2(436f, 3.5f), new Vector2(28f, 7f));
            Drone("Drone 5", level, new Vector2(448f, 1.1f), new Vector2(448f, 5.9f), 3f, 0f);
            Checkpoint("Checkpoint 8", level, new Vector2(454f, 0f), startsClock: false);
            // (d) switch gate: hit the switch, sprint past the spikes before the wall powers back up
            Seg(level, 452f, 490f, 0f, 7f, Surface.Grid);
            Spikes("Spikes 3", level, new Vector2(463f, 0.25f), 2f, false);
            LaserSweep wall1 = LaserStatic("Wall 1a", level, new Vector2(471f, 7f), 7f);
            LaserSweep wall2 = LaserStatic("Wall 1b", level, new Vector2(473.5f, 7f), 7f);
            LaserSweep wall3 = LaserStatic("Wall 1c", level, new Vector2(476f, 7f), 7f);
            Switch("Switch 1", level, new Vector2(457f, 0f), 4.5f, wall1, wall2, wall3);
            Chip("Chip 11", level, new Vector2(486f, 4.8f));
            // (e) portal intro: the only way across the void is the portal; you arrive on the ceiling
            Seg(level, 490f, 498f, 0f, null, Surface.Grid);
            Seg(level, 498f, 512f, null, null, Surface.Grid);
            Seg(level, 512f, 528f, null, 7f, Surface.Grid);
            PortalPair(level, "Portal A", new Vector2(496f, 1.4f), 1, "Portal B", new Vector2(514f, 5.6f), -1);
            Seg(level, 528f, 560f, 0f, 7f, Surface.Grid);
            Chip("Chip 12", level, new Vector2(534f, 4.6f));
            Drone("Drone 6", level, new Vector2(546f, 1.1f), new Vector2(554f, 1.1f), 3.6f, 0.5f);
            Checkpoint("Checkpoint 9", level, new Vector2(558f, 0f), startsClock: false);
            Prop("prop_pipes", deco, new Vector2(404f, 7f), true);
            Prop("prop_lamp", deco, new Vector2(426f, 7f), true);
            Prop("prop_crates", deco, new Vector2(482f, 0f), false);
            Prop("prop_lamp", deco, new Vector2(540f, 7f), true);

            // ================= DISTRICT 5: CORE LINE =================
            District(level, 5, "Core Line", 560f);
            // (g) laser fan: two rotating beams through the whole corridor, half a period apart
            Seg(level, 560f, 600f, 0f, 7f, Surface.Grid);
            // Fan 1 sweeps the floor lane only (tip y = 4), Fan 2 sweeps the ceiling lane only (tip y = 3):
            // cross the first on the ceiling, the second on the floor, and time the flips in between.
            LaserRotate("Fan 1", level, new Vector2(575f, -5.5f), 90f, -36f, 36f, 9.5f, 4.4f, 0f);
            LaserRotate("Fan 2", level, new Vector2(590f, 12.5f), -90f, -36f, 36f, 9.5f, 4.4f, 0.5f);
            Chip("Chip 13", level, new Vector2(582f, 4.6f));
            // (h) portal chain across voids, with a blink beam on the ceiling leg
            Seg(level, 600f, 612f, 0f, null, Surface.Grid);
            Seg(level, 612f, 616f, null, null, Surface.Grid);
            Seg(level, 616f, 632f, null, 7f, Surface.Grid);
            Seg(level, 632f, 640f, null, null, Surface.Grid);
            Seg(level, 640f, 660f, 0f, 7f, Surface.Grid);
            PortalPair(level, "Portal C", new Vector2(610f, 1.4f), 1, "Portal D", new Vector2(618f, 5.6f), -1);
            LaserBlink("Blink 4", level, new Vector2(625f, 7f), 7f, 1.2f, 1.4f, 0.4f);
            PortalPair(level, "Portal E", new Vector2(630f, 5.6f), -1, "Portal F", new Vector2(642f, 1.4f), 1);
            Chip("Chip 14", level, new Vector2(626f, 2f));
            Checkpoint("Checkpoint 10", level, new Vector2(656f, 0f), startsClock: false);
            // (i) crumble staircase: floor plate, ceiling plate, floor plate; keep moving and keep flipping
            Seg(level, 660f, 664f, 0f, 7f, Surface.Grid);
            Seg(level, 664f, 694f, null, null, Surface.Grid);
            Crumble("Crumble 3", level, new Vector2(667f, -0.75f), new Vector2(6f, 1.5f));
            Crumble("Crumble 4", level, new Vector2(677f, 6.75f), new Vector2(6f, 1.5f));
            Crumble("Crumble 5", level, new Vector2(687f, -0.75f), new Vector2(6f, 1.5f));
            Seg(level, 694f, 712f, 0f, 7f, Surface.Grid);
            Checkpoint("Checkpoint 11", level, new Vector2(698f, 0f), startsClock: false);
            Chip("Chip 15", level, new Vector2(706f, 4.6f));
            // (j) final gauntlet: locked gravity through a blink pair, then a switch wall before the core
            Seg(level, 712f, 716f, 0f, 7f, Surface.Grid);
            Seg(level, 716f, 720f, null, 7f, Surface.Grid);
            Seg(level, 720f, 734f, 0f, 7f, Surface.Grid);
            GravityLock(level, new Vector2(724f, 3.5f), new Vector2(20f, 7f));
            LaserBlink("Blink 5", level, new Vector2(723f, 7f), 7f, 1.3f, 1.3f, 0f);
            LaserBlink("Blink 6", level, new Vector2(729f, 7f), 7f, 1.3f, 1.3f, 1.3f);
            Seg(level, 734f, LevelEnd, 0f, 10f, Surface.Grid);    // power core hall
            LaserSweep wall4 = LaserStatic("Wall 2a", level, new Vector2(742f, 10f), 10f);
            LaserSweep wall5 = LaserStatic("Wall 2b", level, new Vector2(744.5f, 10f), 10f);
            Switch("Switch 2", level, new Vector2(737f, 0f), 3.5f, wall4, wall5);
            PowerCore("Power Core", level, new Vector2(753f, 0f));
            Wall(level, LevelEnd, -14f, 26f);
            Prop("prop_lamp", deco, new Vector2(596f, 7f), true);
            Prop("prop_pipes", deco, new Vector2(650f, 7f), true);
            Prop("prop_vending", deco, new Vector2(700f, 0f), false);
            Prop("prop_lamp", deco, new Vector2(748f, 10f), true);
            Prop("prop_crates", deco, new Vector2(739.5f, 0f), false);

            CapExposedEnds();

            // ---- Fall-outs (well outside the corridor) ----
            KillVolume("Fall Out Below", level, new Vector2(LevelEnd / 2f, -17f), new Vector2(LevelEnd + 12f, 4f));
            KillVolume("Fall Out Above", level, new Vector2(LevelEnd / 2f, 29f), new Vector2(LevelEnd + 12f, 4f));

            GameObject player = BuildPlayer(new Vector2(2f, 1f));
            PlayerMotor motor = player.GetComponent<PlayerMotor>();
            BuildCamera(motor, new Rect(-2f, -12f, LevelEnd + 4f, 36f));
            BuildUi(motor);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Built " + ScenePath);
        }

        // ================================================================ player / camera

        static GameObject BuildPlayer(Vector2 position)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = position;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            BoxCollider2D box = player.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 1.4f);
            box.sharedMaterial = frictionless;

            player.AddComponent<PlayerMotor>();
            player.AddComponent<KeyboardPlayerInput>();
            player.AddComponent<PlayerRespawn>();
            player.AddComponent<PlayerAudio>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.AddComponent<PlayerVisual>();

            // Rig root: everything animated by RobotAnimator hangs from here.
            GameObject rig = new GameObject("Rig");
            rig.transform.SetParent(visual.transform, false);

            Sprite bodyArt = Art("robot_body"), legLArt = Art("robot_leg_l"), legRArt = Art("robot_leg_r");
            SpriteRenderer bodyRenderer;
            Transform legL = null, legR = null;
            if (bodyArt != null && legLArt != null && legRArt != null)
            {
                // Pixel layout of the source sprite (557x694 @ 480 ppu): hip line at row 478, leg split at column 340.
                const float ppu = 480f;
                const float groundY = -0.7f;                           // bottom of the collider
                float hipY = groundY + (694f - 478f) / ppu;            // hip joint height above the feet
                float centreX = 557f / 2f;
                legL = Part("Leg L", rig.transform, legLArt, new Vector2((170f - centreX) / ppu, hipY), 9, Art("robot_leg_l_inv")).transform;
                legR = Part("Leg R", rig.transform, legRArt, new Vector2((448f - centreX) / ppu, hipY), 8, Art("robot_leg_r_inv")).transform;
                bodyRenderer = Part("Body", rig.transform, bodyArt, new Vector2(0f, hipY - (492f - 478f) / ppu), 10, Art("robot_body_inv"));
            }
            else
            {
                bodyRenderer = SpriteObject("Body", rig.transform, Vector2.zero, new Vector2(0.8f, 1.4f), Robot, 10).GetComponent<SpriteRenderer>();
                SpriteObject("Eye", bodyRenderer.transform, new Vector2(0.28f, 0.28f), new Vector2(0.28f, 0.16f), new Color(0.05f, 0.1f, 0.15f), 11);
            }
            RobotAnimator animator = rig.AddComponent<RobotAnimator>();
            SerializedObject so = new SerializedObject(animator);
            so.FindProperty("body").objectReferenceValue = bodyRenderer;
            so.FindProperty("legLeft").objectReferenceValue = legL;
            so.FindProperty("legRight").objectReferenceValue = legR;
            so.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        static SpriteRenderer Part(string name, Transform parent, Sprite sprite, Vector2 localPosition, int order, Sprite invertedSprite = null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (invertedSprite != null)
            {
                GravitySpriteSwap swap = go.AddComponent<GravitySpriteSwap>();
                SerializedObject so = new SerializedObject(swap);
                so.FindProperty("normalSprite").objectReferenceValue = sprite;
                so.FindProperty("invertedSprite").objectReferenceValue = invertedSprite;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return sr;
        }

        static void BuildCamera(PlayerMotor target, Rect bounds)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(target.transform.position.x, target.transform.position.y, -10f);

            Camera cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Background;
            cam.GetUniversalAdditionalCameraData();
            cameraObject.AddComponent<AudioListener>();

            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            SerializedObject so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = target;
            so.FindProperty("useBounds").boolValue = true;
            so.FindProperty("bounds").rectValue = bounds;
            so.ApplyModifiedPropertiesWithoutUndo();

            Material wave = FxSetup.Setup();
            FlipWaveEffect fx = cameraObject.AddComponent<FlipWaveEffect>();
            SerializedObject fxo = new SerializedObject(fx);
            fxo.FindProperty("material").objectReferenceValue = wave;
            fxo.FindProperty("player").objectReferenceValue = target;
            fxo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void BuildBackground()
        {
            Transform root = new GameObject("Background").transform;
            AddLayer(root, "bg_far", 0.08f, 0.04f, 4f, 40f, -30);
            AddLayer(root, "bg_mid", 0.22f, 0.08f, 1f, 30f, -20);
            AddLayer(root, "bg_near", 0.45f, 0.14f, -3f, 20f, -10);
        }

        static void AddLayer(Transform root, string art, float fx, float fy, float baseY, float depth, int order)
        {
            Sprite sprite = Art(art);
            if (sprite == null) return;
            GameObject go = new GameObject(art);
            go.transform.SetParent(root, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(sprite.bounds.size.x * 3f, sprite.bounds.size.y);
            sr.sortingOrder = order;
            sr.tileMode = SpriteTileMode.Continuous;
            ParallaxLayer layer = go.AddComponent<ParallaxLayer>();
            SerializedObject so = new SerializedObject(layer);
            so.FindProperty("horizontalFactor").floatValue = fx;
            so.FindProperty("verticalFactor").floatValue = fy;
            so.FindProperty("baseY").floatValue = baseY;
            so.FindProperty("depth").floatValue = depth;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================ audio

        static void BuildAudio()
        {
            GameObject go = new GameObject("Audio");
            AudioManager manager = go.AddComponent<AudioManager>();
            SerializedObject so = new SerializedObject(manager);
            SerializedProperty effects = so.FindProperty("effects");
            string[] names = System.Enum.GetNames(typeof(Sfx));
            effects.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                SerializedProperty e = effects.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = i;
                e.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "sfx_" + names[i].ToLowerInvariant() + ".wav");
                e.FindPropertyRelative("volume").floatValue = names[i] == "Footstep" ? 0.35f : 0.9f;
                e.FindPropertyRelative("pitchVariation").floatValue = names[i] == "Footstep" ? 0.12f : 0.04f;
                e.FindPropertyRelative("minInterval").floatValue = names[i] == "Footstep" ? 0.12f : 0.05f;
            }
            so.FindProperty("musicMain").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "music_city.wav");
            so.FindProperty("musicFinal").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "music_grid.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================ world pieces

        const float SlabThickness = 14f;

        struct Slab { public float x0, x1, surfaceY; public bool floor; public Surface surface; public Transform transform; }
        static readonly System.Collections.Generic.List<Slab> slabs = new System.Collections.Generic.List<Slab>();

        /// <summary>Adds vertical edge caps on every slab end that is not continued by a neighbouring slab at the same height.</summary>
        static void CapExposedEnds()
        {
            foreach (Slab s in slabs)
            {
                foreach (bool right in new[] { false, true })
                {
                    float x = right ? s.x1 : s.x0;
                    bool continued = false;
                    foreach (Slab o in slabs)
                    {
                        if (o.floor != s.floor || o.transform == s.transform) continue;
                        float ox = right ? o.x0 : o.x1;
                        if (Mathf.Abs(ox - x) < 0.01f && Mathf.Abs(o.surfaceY - s.surfaceY) < 0.01f) { continued = true; break; }
                    }
                    if (continued) continue;

                    string edgeName = s.surface == Surface.Rooftop && !s.floor ? "edge_underside" : "edge_" + s.surface.ToString().ToLowerInvariant();
                    Sprite strip = Art(edgeName);
                    if (strip == null) continue;
                    GameObject cap = new GameObject(right ? "Cap R" : "Cap L");
                    cap.transform.SetParent(s.transform, false);
                    float h = strip.bounds.size.y;
                    // Vertical face: the lip of the strip faces outward (+x on the right end, -x on the left end).
                    cap.transform.localPosition = new Vector3((right ? 1f : -1f) * ((s.x1 - s.x0) * 0.5f - h * 0.5f + 0.12f), 0f, 0f);
                    cap.transform.localRotation = Quaternion.Euler(0f, 0f, right ? -90f : 90f);
                    SpriteRenderer sr = cap.AddComponent<SpriteRenderer>();
                    sr.sprite = strip;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.size = new Vector2(SlabThickness, h);
                    sr.sortingOrder = 3;
                }
            }
        }

        /// <summary>One corridor segment: optional floor (top surface at floorTop) and optional ceiling (underside at ceilBottom).</summary>
        static void Seg(Transform parent, float x0, float x1, float? floorTop, float? ceilBottom, Surface surface, bool hazardCeiling = false)
        {
            float width = x1 - x0;
            float cx = (x0 + x1) * 0.5f;
            if (floorTop.HasValue)
            {
                GameObject f = Block("Floor " + x0 + "-" + x1, parent, new Vector2(cx, floorTop.Value - SlabThickness * 0.5f), new Vector2(width, SlabThickness), surface, Edge.Top);
                slabs.Add(new Slab { x0 = x0, x1 = x1, surfaceY = floorTop.Value, floor = true, surface = surface, transform = f.transform });
            }
            if (ceilBottom.HasValue)
            {
                Surface ceilSurface = surface == Surface.Rooftop ? Surface.Underside : surface;
                GameObject c = Block("Ceiling " + x0 + "-" + x1, parent, new Vector2(cx, ceilBottom.Value + SlabThickness * 0.5f), new Vector2(width, SlabThickness), ceilSurface, hazardCeiling ? Edge.None : Edge.Bottom);
                slabs.Add(new Slab { x0 = x0, x1 = x1, surfaceY = ceilBottom.Value, floor = false, surface = surface, transform = c.transform });
                if (hazardCeiling)
                {
                    Panel("Live Ceiling " + x0 + "-" + x1, parent, new Vector2(cx, ceilBottom.Value - 0.15f), width - 0.4f, true);
                }
            }
        }

        static void Wall(Transform parent, float x, float yBottom, float yTop)
        {
            GameObject go = Block("Wall " + x, parent, new Vector2(x - 1f, (yBottom + yTop) * 0.5f), new Vector2(2f, yTop - yBottom), Surface.Wall);
            go.GetComponent<BoxCollider2D>().size = new Vector2(2f, yTop - yBottom);
        }

        static void Prop(string art, Transform parent, Vector2 anchor, bool hangsFromCeiling)
        {
            Sprite sprite = Art(art);
            if (sprite == null) return;
            GameObject go = new GameObject(art);
            go.transform.SetParent(parent, false);
            float h = sprite.bounds.size.y;
            go.transform.position = anchor + new Vector2(0f, hangsFromCeiling ? -h * 0.5f : h * 0.5f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -2;
            sr.color = new Color(0.85f, 0.85f, 0.9f);
        }

        enum Edge { None, Top, Bottom }

        static GameObject Block(string name, Transform parent, Vector2 center, Vector2 size, Surface surface, Edge edge = Edge.None)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;

            Sprite art = null;
            Color tint = Color.white;
            switch (surface)
            {
                case Surface.Rooftop: art = Art("tile_rooftop"); tint = RooftopTint; break;
                case Surface.Underside: art = Art("tile_underside"); tint = UndersideTint; break;
                case Surface.Bridge: art = Art("tile_bridge"); tint = BridgeTint; break;
                case Surface.Grid: art = Art("tile_grid"); tint = GridTint; break;
                case Surface.Wall: art = Art("tile_wall"); tint = WallTint; break;
            }
            if (surface != Surface.Wall) // walls are invisible boundaries
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = art != null ? art : white;
                sr.color = art != null ? Color.white : tint;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                sr.size = size;
                sr.sortingOrder = 0;
            }

            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;

            if (edge != Edge.None)
            {
                string edgeName = surface == Surface.Rooftop && edge == Edge.Bottom ? "edge_underside" : "edge_" + surface.ToString().ToLowerInvariant();
                Sprite strip = Art(edgeName);
                if (strip != null)
                {
                    GameObject lip = new GameObject("Edge");
                    lip.transform.SetParent(go.transform, false);
                    float h = strip.bounds.size.y;
                    float surfaceY = edge == Edge.Top ? size.y * 0.5f : -size.y * 0.5f;
                    // The lip overlaps the slab and pokes 0.12 units past the walkable line.
                    lip.transform.localPosition = new Vector3(0f, edge == Edge.Top ? surfaceY + 0.12f - h * 0.5f : surfaceY - 0.12f + h * 0.5f, 0f);
                    if (edge == Edge.Bottom) lip.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                    SpriteRenderer lr = lip.AddComponent<SpriteRenderer>();
                    lr.sprite = strip;
                    lr.drawMode = SpriteDrawMode.Tiled;
                    lr.tileMode = SpriteTileMode.Continuous;
                    lr.size = new Vector2(size.x, h);
                    lr.sortingOrder = 2;
                }
            }
            return go;
        }

        static void District(Transform parent, int index, string name, float x)
        {
            GameObject go = new GameObject("District " + index + " - " + name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector2(x, 5f);
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, 40f);
            DistrictZone zone = go.AddComponent<DistrictZone>();
            SerializedObject so = new SerializedObject(zone);
            so.FindProperty("districtName").stringValue = name;
            so.FindProperty("districtIndex").intValue = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Checkpoint(string name, Transform parent, Vector2 groundPoint, bool startsClock)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = groundPoint;

            Sprite on = Art("beacon_on");
            Sprite off = Art("beacon_off");
            GameObject light;
            if (on != null && off != null)
            {
                light = new GameObject("Beacon");
                light.transform.SetParent(root.transform, false);
                float h = off.bounds.size.y;
                light.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
                SpriteRenderer sr = light.AddComponent<SpriteRenderer>();
                sr.sprite = off;
                sr.sortingOrder = -1;
            }
            else
            {
                SpriteObject("Post", root.transform, new Vector2(0f, 1.5f), new Vector2(0.35f, 3f), new Color(0.3f, 0.32f, 0.36f), -2);
                light = SpriteObject("Light", root.transform, new Vector2(0f, 3.2f), new Vector2(0.9f, 0.9f), Color.gray, -1);
            }

            GameObject respawn = new GameObject("RespawnPoint");
            respawn.transform.SetParent(root.transform, false);
            respawn.transform.localPosition = new Vector2(0f, 1f);

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 6f);   // floor to ceiling: a beacon cannot be skipped by running the underside
            trigger.size = new Vector2(2.5f, 14f);

            Checkpoint checkpoint = root.AddComponent<Checkpoint>();
            SerializedObject so = new SerializedObject(checkpoint);
            so.FindProperty("respawnPoint").objectReferenceValue = respawn.transform;
            so.FindProperty("respawnGravitySign").intValue = 1;
            so.FindProperty("startsClock").boolValue = startsClock;
            so.FindProperty("lightRenderer").objectReferenceValue = light.GetComponent<SpriteRenderer>();
            so.FindProperty("onSprite").objectReferenceValue = on;
            so.FindProperty("offSprite").objectReferenceValue = off;
            if (on != null) { so.FindProperty("offColor").colorValue = Color.white; so.FindProperty("onColor").colorValue = Color.white; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Chip(string name, Transform parent, Vector2 position)
        {
            Sprite art = Art("chip");
            GameObject chip;
            if (art != null)
            {
                chip = new GameObject(name);
                chip.transform.SetParent(parent, false);
                chip.transform.position = position;
                SpriteRenderer sr = chip.AddComponent<SpriteRenderer>();
                sr.sprite = art;
                sr.sortingOrder = 5;
            }
            else
            {
                chip = SpriteObject(name, parent, position, new Vector2(0.6f, 0.6f), ChipTint, 5);
                chip.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            }
            CircleCollider2D trigger = chip.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = art != null ? 0.6f : 0.9f;
            chip.AddComponent<DataChip>();
        }

        static void Spikes(string name, Transform parent, Vector2 center, float width, bool inverted)
        {
            Hazard(name, parent, center, new Vector2(width, 0.5f), Art("spikes"), SpikesTint, inverted);
        }

        static void Panel(string name, Transform parent, Vector2 center, float width, bool inverted = false)
        {
            Hazard(name, parent, center, new Vector2(width, 0.3f), Art("panel"), PanelTint, inverted);
        }

        /// <summary>Static hazard. Inverted hazards hang from a ceiling (art rotated half a turn).</summary>
        static void Hazard(string name, Transform parent, Vector2 center, Vector2 size, Sprite art, Color tint, bool inverted)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            if (inverted) go.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
            if (art != null && size.x <= art.bounds.size.x * 2.5f)
            {
                sr.sprite = art;
                float scale = size.x / art.bounds.size.x;
                go.transform.localScale = new Vector3(scale, scale, 1f);
                // Keep the base of the art on the surface line regardless of its height.
                sr.transform.localPosition += go.transform.up * ((art.bounds.size.y * scale - size.y) * 0.5f);
            }
            else if (art != null)
            {
                sr.sprite = art;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.tileMode = SpriteTileMode.Continuous;
                float scale = 0.6f / art.bounds.size.y; // thin strip, tiled along the surface
                go.transform.localScale = new Vector3(scale, scale, 1f);
                sr.size = new Vector2(size.x / scale, art.bounds.size.y);
                sr.transform.localPosition += go.transform.up * ((0.6f - size.y) * 0.5f);
            }
            else
            {
                sr.sprite = white;
                sr.color = tint;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = size;
            }
            AddHazardGlow(go.transform, new Vector2(size.x + 2.4f, 2.6f), DangerRed);
            if (art == Art("panel")) AddSparks(go.transform, size.x);

            GameObject hit = new GameObject("Hit");
            hit.transform.SetParent(parent, false);
            hit.transform.position = center;
            BoxCollider2D trigger = hit.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(size.x * 0.9f, size.y * 0.8f); // slightly forgiving hit box
            hit.AddComponent<KillZone>();
            hit.transform.SetParent(go.transform, true);
        }

        static void LaserRotate(string name, Transform parent, Vector2 position, float baseAngle, float from, float to, float length, float period, float phase)
        {
            GameObject go = LaserBase(name, parent, position, length);
            LaserSweep laser = go.GetComponent<LaserSweep>();
            SerializedObject so = new SerializedObject(laser);
            so.FindProperty("mode").enumValueIndex = 0;
            so.FindProperty("baseAngle").floatValue = baseAngle;
            so.FindProperty("angleFrom").floatValue = from;
            so.FindProperty("angleTo").floatValue = to;
            so.FindProperty("period").floatValue = period;
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void LaserTranslate(string name, Transform parent, Vector2 a, Vector2 b, float baseAngle, float length, float period, float phase)
        {
            GameObject go = LaserBase(name, parent, a, length);
            LaserSweep laser = go.GetComponent<LaserSweep>();
            SerializedObject so = new SerializedObject(laser);
            so.FindProperty("mode").enumValueIndex = 1;
            so.FindProperty("baseAngle").floatValue = baseAngle;
            so.FindProperty("pointA").vector2Value = a;
            so.FindProperty("pointB").vector2Value = b;
            so.FindProperty("period").floatValue = period;
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject LaserBase(string name, Transform parent, Vector2 position, float length)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            Sprite emitterArt = Art("laser_emitter");
            GameObject emitter = new GameObject("Emitter");
            emitter.transform.SetParent(go.transform, false);
            SpriteRenderer es = emitter.AddComponent<SpriteRenderer>();
            es.sortingOrder = 7;
            if (emitterArt != null)
            {
                es.sprite = emitterArt;
            }
            else
            {
                es.sprite = white;
                es.color = new Color(0.5f, 0.2f, 0.2f);
                emitter.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            }

            GameObject beam = new GameObject("Beam");
            beam.transform.SetParent(go.transform, false);
            SpriteRenderer bs = beam.AddComponent<SpriteRenderer>();
            bs.sprite = white;
            bs.color = BeamColor;
            bs.sortingOrder = 6;
            BoxCollider2D box = beam.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            beam.AddComponent<KillZone>();

            GameObject core = new GameObject("Core");
            core.transform.SetParent(beam.transform, false);
            core.transform.localScale = new Vector3(1f, 0.35f, 1f);
            SpriteRenderer cs = core.AddComponent<SpriteRenderer>();
            cs.sprite = white;
            cs.color = new Color(1f, 0.9f, 0.85f, 0.95f);
            cs.sortingOrder = 7;
            AddHazardGlow(emitter.transform, new Vector2(3.2f, 3.2f), DangerRed);

            LaserSweep laser = go.AddComponent<LaserSweep>();
            SerializedObject so = new SerializedObject(laser);
            so.FindProperty("beam").objectReferenceValue = beam.transform;
            so.FindProperty("beamLength").floatValue = length;
            so.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        static void PowerCore(string name, Transform parent, Vector2 groundPoint)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = groundPoint;
            Sprite art = Art("power_core");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = -1;
            if (art != null)
            {
                sr.sprite = art;
                go.transform.position = groundPoint + new Vector2(0f, art.bounds.size.y * 0.5f);
            }
            else
            {
                sr.sprite = white;
                sr.color = new Color(1f, 0.4f, 0.9f);
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(2f, 5f);
                go.transform.position = groundPoint + new Vector2(0f, 2.5f);
            }
            BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.5f, 4f);
            go.AddComponent<DeliveryPoint>();
        }

        static void Sign(string art, Transform parent, Vector2 center, float width)
        {
            Sprite sprite = Art(art);
            if (sprite == null) return;
            GameObject go = new GameObject(art);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            float scale = width / sprite.bounds.size.x;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -3;
        }

        static void FlipPromptZone(Transform parent, Vector2 zoneCenter, Vector2 zoneSize, Vector2 iconPosition)
        {
            GameObject zone = new GameObject("Flip Prompt Zone");
            zone.transform.SetParent(parent, false);
            zone.transform.position = zoneCenter;
            BoxCollider2D trigger = zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = zoneSize;

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(zone.transform, false);
            icon.transform.position = iconPosition;
            Sprite keyW = Key("w"), keyUp = Key("up"), keyShift = Key("shift");
            if (keyW != null && keyUp != null && keyShift != null)
            {
                // Three keycaps side by side, then a small "flip" arrow motif above them.
                float scale = 0.8f;
                float x = -(keyW.bounds.size.x + keyUp.bounds.size.x + keyShift.bounds.size.x) * scale * 0.5f - 0.1f;
                foreach (Sprite k in new[] { keyW, keyUp, keyShift })
                {
                    GameObject cap = new GameObject(k.name);
                    cap.transform.SetParent(icon.transform, false);
                    cap.transform.localScale = Vector3.one * scale;
                    cap.transform.localPosition = new Vector3(x + k.bounds.size.x * scale * 0.5f, 0f, 0f);
                    x += k.bounds.size.x * scale + 0.1f;
                    SpriteRenderer sr = cap.AddComponent<SpriteRenderer>();
                    sr.sprite = k;
                    sr.sortingOrder = 20;
                }
            }
            else
            {
                SpriteObject("Key", icon.transform, Vector2.zero, new Vector2(1.2f, 1.2f), new Color(1f, 1f, 1f, 0.15f), 20);
                SpriteObject("Arrow Stem", icon.transform, new Vector2(0f, -0.05f), new Vector2(0.18f, 0.6f), Color.white, 21);
                GameObject head = SpriteObject("Arrow Head", icon.transform, new Vector2(0f, 0.28f), new Vector2(0.45f, 0.45f), Color.white, 21);
                head.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            FlipPrompt prompt = zone.AddComponent<FlipPrompt>();
            SerializedObject so = new SerializedObject(prompt);
            so.FindProperty("icon").objectReferenceValue = icon.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddHazardGlow(Transform parent, Vector2 size, Color color)
        {
            Sprite glowSprite = Art("glow_soft");
            if (glowSprite == null) return;
            GameObject glow = new GameObject("Warning Glow");
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localRotation = Quaternion.identity;
            Vector3 parentScale = parent.lossyScale;
            glow.transform.localScale = new Vector3(size.x / glowSprite.bounds.size.x / Mathf.Max(0.01f, parentScale.x), size.y / glowSprite.bounds.size.y / Mathf.Max(0.01f, parentScale.y), 1f);
            SpriteRenderer sr = glow.AddComponent<SpriteRenderer>();
            sr.sprite = glowSprite;
            sr.color = color;
            sr.sortingOrder = -1;
            glow.AddComponent<HazardGlow>();
        }

        static void AddSparks(Transform parent, float width)
        {
            GameObject go = new GameObject("Sparks");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            go.transform.localScale = Vector3.one;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.1f));
            main.gravityModifier = 1.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var emission = ps.emission;
            emission.rateOverTime = 18f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width / Mathf.Max(0.01f, parent.lossyScale.x), 0.05f, 0.05f);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 4;
            renderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-ParticleSystem.mat");
        }

        /// <summary>Patrolling drone: slides between two points on a sine rhythm; contact kills.</summary>
        static void Drone(string name, Transform parent, Vector2 a, Vector2 b, float period, float phase)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = a;

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            Sprite art = Art("hazard_drone");
            SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;
            if (art != null) sr.sprite = art;
            else { sr.sprite = white; sr.color = DangerRed; visual.transform.localScale = new Vector3(1.4f, 0.8f, 1f); }
            AddHazardGlow(visual.transform, new Vector2(3f, 3f), DangerRed);

            BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = art != null ? new Vector2(art.bounds.size.x * 0.8f, art.bounds.size.y * 0.7f) : new Vector2(1.2f, 0.6f);
            go.AddComponent<KillZone>();

            PatrolHazard patrol = go.AddComponent<PatrolHazard>();
            SerializedObject so = new SerializedObject(patrol);
            so.FindProperty("pointA").vector2Value = a;
            so.FindProperty("pointB").vector2Value = b;
            so.FindProperty("period").floatValue = period;
            so.FindProperty("phase").floatValue = phase;
            so.FindProperty("visual").objectReferenceValue = visual.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void DirectionArrow(Transform parent, Vector2 position)
        {
            Sprite arrow = Art("arrow_right");
            if (arrow == null) return;
            GameObject go = new GameObject("Direction Prompt");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = arrow;
            sr.sortingOrder = 20;
            go.AddComponent<DirectionPrompt>();
        }

        static void DangerDemoZone(Transform parent, Vector2 zoneCenter, Vector2 zoneSize, Vector2 demoPosition, float retireBeyondX)
        {
            GameObject zone = new GameObject("Danger Demo Zone");
            zone.transform.SetParent(parent, false);
            zone.transform.position = zoneCenter;
            BoxCollider2D trigger = zone.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = zoneSize;

            GameObject demo = new GameObject("Demo");
            demo.transform.SetParent(zone.transform, false);
            demo.transform.position = demoPosition;

            GameObject icon = new GameObject("Hazard Icon");
            icon.transform.SetParent(demo.transform, false);
            icon.transform.localPosition = new Vector3(1.0f, 0f, 0f);
            SpriteRenderer iconSr = icon.AddComponent<SpriteRenderer>();
            Sprite spikes = Art("spikes");
            if (spikes != null) { iconSr.sprite = spikes; icon.transform.localScale = Vector3.one * (1.1f / spikes.bounds.size.x); }
            else { iconSr.sprite = white; icon.transform.localScale = new Vector3(1f, 0.4f, 1f); }
            iconSr.color = new Color(1f, 0.6f, 0.6f, 0.4f); // clearly a hint, not a real hazard
            iconSr.sortingOrder = 21;
            AddHazardGlow(icon.transform, new Vector2(2.2f, 2.2f), new Color(DangerRed.r, DangerRed.g, DangerRed.b, 0.35f));

            GameObject ghost = new GameObject("Ghost");
            ghost.transform.SetParent(demo.transform, false);
            ghost.transform.localPosition = new Vector3(-1.2f, 0.35f, 0f);
            ghost.transform.localScale = Vector3.one * 0.7f;
            SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
            Sprite body = Art("robot");
            ghostSr.sprite = body != null ? body : white;
            ghostSr.color = new Color(1f, 1f, 1f, 0.45f);
            ghostSr.sortingOrder = 22;

            DangerDemo dd = zone.AddComponent<DangerDemo>();
            SerializedObject so = new SerializedObject(dd);
            so.FindProperty("ghost").objectReferenceValue = ghost.transform;
            so.FindProperty("ghostRenderer").objectReferenceValue = ghostSr;
            so.FindProperty("hazardIcon").objectReferenceValue = iconSr;
            so.FindProperty("travel").floatValue = 1.6f;
            so.FindProperty("retireBeyondX").floatValue = retireBeyondX;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Always-on vertical beam from a ceiling emitter down to the floor (used as a switchable wall).</summary>
        static LaserSweep LaserStatic(string name, Transform parent, Vector2 topPoint, float length)
        {
            GameObject go = LaserBase(name, parent, topPoint, length - 0.6f);
            LaserSweep laser = go.GetComponent<LaserSweep>();
            SerializedObject so = new SerializedObject(laser);
            so.FindProperty("mode").enumValueIndex = 0;
            so.FindProperty("baseAngle").floatValue = -90f;
            so.FindProperty("angleFrom").floatValue = 0f;
            so.FindProperty("angleTo").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return laser;
        }

        /// <summary>Vertical beam that switches on and off in a fixed rhythm.</summary>
        static void LaserBlink(string name, Transform parent, Vector2 topPoint, float length, float on, float off, float offset)
        {
            LaserSweep laser = LaserStatic(name, parent, topPoint, length);
            SerializedObject so = new SerializedObject(laser);
            so.FindProperty("blinkOn").floatValue = on;
            so.FindProperty("blinkOff").floatValue = off;
            so.FindProperty("blinkOffset").floatValue = offset;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Switch(string name, Transform parent, Vector2 groundPoint, float openSeconds, params LaserSweep[] lasers)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = groundPoint;
            Sprite art = Art("switch_button");
            GameObject button = new GameObject("Button");
            button.transform.SetParent(go.transform, false);
            SpriteRenderer sr = button.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 4;
            if (art != null) { sr.sprite = art; button.transform.localPosition = new Vector3(0f, art.bounds.size.y * 0.5f, 0f); }
            else { sr.sprite = white; button.transform.localScale = new Vector3(1.2f, 0.5f, 1f); button.transform.localPosition = new Vector3(0f, 0.25f, 0f); }
            AddHazardGlow(button.transform, new Vector2(2.6f, 2.6f), new Color(0.4f, 1f, 0.5f));

            GameObject bar = new GameObject("Timer Bar");
            bar.transform.SetParent(go.transform, false);
            bar.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            bar.transform.localScale = new Vector3(2.4f, 0.18f, 1f);
            SpriteRenderer bs = bar.AddComponent<SpriteRenderer>();
            bs.sprite = white;
            bs.color = new Color(1f, 0.85f, 0.3f);
            bs.sortingOrder = 12;

            BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.offset = new Vector2(0f, 0.6f);
            trigger.size = new Vector2(1.6f, 1.4f);

            LaserSwitch sw = go.AddComponent<LaserSwitch>();
            SerializedObject so = new SerializedObject(sw);
            SerializedProperty list = so.FindProperty("lasers");
            list.arraySize = lasers.Length;
            for (int i = 0; i < lasers.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = lasers[i];
            so.FindProperty("openSeconds").floatValue = openSeconds;
            so.FindProperty("button").objectReferenceValue = sr;
            so.FindProperty("timerBar").objectReferenceValue = bar.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Crumble(string name, Transform parent, Vector2 center, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            Sprite art = Art("tile_crumble");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = art != null ? art : white;
            sr.color = art != null ? Color.white : new Color(0.6f, 0.45f, 0.3f);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            sr.sortingOrder = 1;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            go.AddComponent<CrumblePlatform>();
            AddHazardGlow(go.transform, new Vector2(size.x + 1f, 2.2f), new Color(1f, 0.6f, 0.2f));
        }

        static void GravityLock(Transform parent, Vector2 center, Vector2 size)
        {
            GameObject go = new GameObject("Gravity Lock");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            BoxCollider2D trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = size;
            go.AddComponent<GravityZone>();

            // Visible field: a translucent violet band plus a no-flip icon at each end.
            GameObject field = new GameObject("Field");
            field.transform.SetParent(go.transform, false);
            SpriteRenderer fs = field.AddComponent<SpriteRenderer>();
            fs.sprite = white;
            fs.color = new Color(0.6f, 0.3f, 1f, 0.10f);
            fs.drawMode = SpriteDrawMode.Tiled;
            fs.size = size;
            fs.sortingOrder = -4;
            Sprite icon = Art("noflip_icon");
            if (icon != null)
            {
                foreach (float x in new[] { -size.x * 0.5f + 1.2f, size.x * 0.5f - 1.2f })
                {
                    GameObject i = new GameObject("No Flip");
                    i.transform.SetParent(go.transform, false);
                    i.transform.localPosition = new Vector3(x, 0f, 0f);
                    SpriteRenderer isr = i.AddComponent<SpriteRenderer>();
                    isr.sprite = icon;
                    isr.sortingOrder = 20;
                    isr.color = new Color(1f, 1f, 1f, 0.85f);
                }
            }
        }

        static void PortalPair(Transform parent, string nameA, Vector2 posA, int signA, string nameB, Vector2 posB, int signB)
        {
            Portal a = PortalNode(nameA, parent, posA, signA, new Color(0.49f, 0.98f, 1f));
            Portal b = PortalNode(nameB, parent, posB, signB, new Color(1f, 0.45f, 0.9f));
            SerializedObject sa = new SerializedObject(a);
            sa.FindProperty("destination").objectReferenceValue = b;
            sa.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject sb = new SerializedObject(b);
            sb.FindProperty("destination").objectReferenceValue = a;
            sb.ApplyModifiedPropertiesWithoutUndo();
        }

        static Portal PortalNode(string name, Transform parent, Vector2 position, int gravitySign, Color tint)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Sprite art = Art("portal_ring");
            GameObject ring = new GameObject("Ring");
            ring.transform.SetParent(go.transform, false);
            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 6;
            if (art != null) sr.sprite = art;
            else { sr.sprite = white; ring.transform.localScale = new Vector3(1.6f, 2.4f, 1f); }
            sr.color = tint;
            AddHazardGlow(go.transform, new Vector2(3.6f, 3.6f), new Color(tint.r, tint.g, tint.b, 0.7f));

            CircleCollider2D trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.9f;

            Portal portal = go.AddComponent<Portal>();
            SerializedObject so = new SerializedObject(portal);
            so.FindProperty("gravitySign").intValue = gravitySign;
            so.FindProperty("arrivalOffset").vector2Value = new Vector2(1.6f, 0f);
            so.FindProperty("ring").objectReferenceValue = ring.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            return portal;
        }

        static GameObject KillVolume(string name, Transform parent, Vector2 center, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = true;
            go.AddComponent<KillZone>();
            return go;
        }

        static GameObject SpriteObject(string name, Transform parent, Vector2 localPosition, Vector2 size, Color color, int sortingOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = white;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return go;
        }

        // ================================================================ UI

        static void BuildUi(PlayerMotor player)
        {
            GameObject canvasObject = new GameObject("UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            // ---- HUD ----
            GameObject hudRoot = Panel("HUD", canvasObject.transform, Color.clear);
            Sprite chipArt = Art("chip");
            if (chipArt != null)
            {
                Image chipIcon = UiImage("ChipIcon", hudRoot.transform, Color.white);
                chipIcon.sprite = chipArt;
                chipIcon.preserveAspect = true;
                Place(chipIcon.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -26f), new Vector2(44f, 44f));
            }
            Text chips = UiText("ChipText", hudRoot.transform, 36, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(chipArt != null ? 96f : 40f, -30f), new Vector2(500f, 50f));
            // Always-visible controls, bottom left.
            KeyRowSmall(hudRoot.transform, new Vector2(30f, 150f), new[] { "a", "d" }, "RUN");
            KeyRowSmall(hudRoot.transform, new Vector2(30f, 104f), new[] { "space" }, "JUMP");
            KeyRowSmall(hudRoot.transform, new Vector2(30f, 58f), new[] { "w", "up", "shift" }, "FLIP");
            KeyRowSmall(hudRoot.transform, new Vector2(30f, 12f), new[] { "esc" }, "PAUSE");

            GameObject clockRoot = Panel("Clock", hudRoot.transform, Color.clear);
            RectTransform clockRect = clockRoot.GetComponent<RectTransform>();
            clockRect.anchorMin = clockRect.anchorMax = clockRect.pivot = new Vector2(0.5f, 1f);
            clockRect.anchoredPosition = new Vector2(0f, -30f);
            clockRect.sizeDelta = new Vector2(600f, 60f);
            Image back = UiImage("Back", clockRoot.transform, new Color(0f, 0f, 0f, 0.5f));
            Place(back.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(600f, 18f));
            Image fill = UiImage("Fill", clockRoot.transform, Robot);
            Place(fill.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(600f, 18f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Text clockText = UiText("ClockText", clockRoot.transform, 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(300f, 40f));
            clockRoot.SetActive(false);

            HudController hud = canvasObject.AddComponent<HudController>();
            SerializedObject hs = new SerializedObject(hud);
            hs.FindProperty("chipText").objectReferenceValue = chips;
            hs.FindProperty("clockRoot").objectReferenceValue = clockRoot;
            hs.FindProperty("clockFill").objectReferenceValue = fill;
            hs.FindProperty("clockText").objectReferenceValue = clockText;
            hs.FindProperty("player").objectReferenceValue = player;
            hs.ApplyModifiedPropertiesWithoutUndo();

            // ---- District banner ----
            GameObject banner = Panel("DistrictBanner", canvasObject.transform, Color.clear);
            Text bannerText = UiText("Text", banner.transform, 54, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(900f, 160f));
            bannerText.color = Robot;
            banner.SetActive(false);

            // ---- Title ----
            GameObject title = Panel("TitlePanel", canvasObject.transform, UiPanel);
            Sprite titleArt = Art("title_art");
            if (titleArt != null)
            {
                Image art = UiImage("Art", title.transform, Color.white);
                art.sprite = titleArt;
                art.preserveAspect = false; // full-bleed; slight stretch is acceptable for key art
                Stretch(art.rectTransform);
                Image shade = UiImage("Shade", title.transform, new Color(0f, 0f, 0.02f, 0.55f));
                Stretch(shade.rectTransform);
            }
            Text titleText = UiText("Title", title.transform, 132, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(1600f, 170f), true);
            titleText.text = "FLIPSIDE";
            titleText.color = Robot;
            Text sub = UiText("Subtitle", title.transform, 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 80f), true);
            sub.text = "C I T Y   L I G H T S";
            sub.color = new Color(1f, 0.45f, 0.9f);
            Text tagline = UiText("Tagline", title.transform, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1400f, 60f));
            tagline.text = "Deliver the package to the power core before the grid resets.";
            tagline.color = new Color(0.85f, 0.9f, 1f);
            KeyRow(title.transform, new Vector2(-380f, -40f), new[] { "a", "d" }, "RUN");
            KeyRow(title.transform, new Vector2(60f, -40f), new[] { "space" }, "JUMP");
            KeyRow(title.transform, new Vector2(-380f, -140f), new[] { "w", "up", "shift" }, "FLIP GRAVITY");
            KeyRow(title.transform, new Vector2(60f, -140f), new[] { "esc" }, "PAUSE");
            Text start = UiText("Start", title.transform, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -290f), new Vector2(1400f, 60f), true);
            start.text = "PRESS ANY KEY TO START";
            start.color = Robot;
            start.gameObject.AddComponent<PulseText>();
            KeyRow(title.transform, new Vector2(-130f, -380f), new[] { "c" }, "CREDITS");

            // ---- Pause ----
            GameObject pause = Panel("PausePanel", canvasObject.transform, UiPanel);
            Text pauseTitle = UiText("Title", pause.transform, 90, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(1000f, 120f), true);
            pauseTitle.text = "PAUSED";
            pauseTitle.color = Robot;
            KeyRow(pause.transform, new Vector2(-330f, 80f), new[] { "esc", "enter" }, "RESUME");
            KeyRow(pause.transform, new Vector2(120f, 80f), new[] { "r" }, "RESTART RUN");
            KeyRow(pause.transform, new Vector2(-330f, -40f), new[] { "a", "d" }, "MUSIC VOLUME");
            KeyRow(pause.transform, new Vector2(120f, -40f), new[] { "s", "w" }, "SOUND VOLUME");
            Text music = UiText("Music", pause.transform, 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1000f, 50f));
            Text sfx = UiText("Sfx", pause.transform, 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(1000f, 50f));

            // ---- End ----
            GameObject end = Panel("EndPanel", canvasObject.transform, UiPanel);
            Text summary = UiText("Summary", end.transform, 44, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 500f));
            summary.color = Robot;

            // ---- Fail ----
            GameObject fail = Panel("FailPanel", canvasObject.transform, UiPanel);
            Text failTitle = UiText("Title", fail.transform, 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1400f, 120f), true);
            failTitle.text = "POWER GRID RESET";
            failTitle.color = new Color(1f, 0.45f, 0.5f);
            Text failText = UiText("Text", fail.transform, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(1200f, 80f));
            failText.text = "The package was not delivered in time.";
            KeyRow(fail.transform, new Vector2(-150f, -110f), new[] { "r" }, "TRY AGAIN");
            KeyRow(end.transform, new Vector2(-150f, -260f), new[] { "r" }, "RUN AGAIN");

            // ---- Credits ----
            GameObject credits = Panel("CreditsPanel", canvasObject.transform, UiPanel);
            Text creditsText = UiText("Text", credits.transform, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 700f));
            creditsText.text = "FLIPSIDE: City Lights\n\nDesign, code, levels, art and audio by the FLIPSIDE team.\nAll art, sound effects and music were made for this game.\n\nFonts: Audiowide and Rajdhani (SIL Open Font License)\nBuilt with Unity 6\n\nPress any key to return";

            GameFlow flow = canvasObject.AddComponent<GameFlow>();
            SerializedObject fs = new SerializedObject(flow);
            fs.FindProperty("titlePanel").objectReferenceValue = title;
            fs.FindProperty("pausePanel").objectReferenceValue = pause;
            fs.FindProperty("endPanel").objectReferenceValue = end;
            fs.FindProperty("failPanel").objectReferenceValue = fail;
            fs.FindProperty("creditsPanel").objectReferenceValue = credits;
            fs.FindProperty("districtBanner").objectReferenceValue = banner;
            fs.FindProperty("hudRoot").objectReferenceValue = hudRoot;
            fs.FindProperty("endSummary").objectReferenceValue = summary;
            fs.FindProperty("musicVolumeText").objectReferenceValue = music;
            fs.FindProperty("sfxVolumeText").objectReferenceValue = sfx;
            fs.FindProperty("districtBannerText").objectReferenceValue = bannerText;
            fs.FindProperty("player").objectReferenceValue = player;
            fs.ApplyModifiedPropertiesWithoutUndo();

            pause.SetActive(false); end.SetActive(false); fail.SetActive(false); credits.SetActive(false);
        }

        static GameObject Panel(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Stretch(rect);
            if (color.a > 0f)
            {
                Image image = go.AddComponent<Image>();
                image.color = color;
                image.sprite = white;
            }
            return go;
        }

        /// <summary>Compact keycap row anchored to the bottom-left corner (HUD).</summary>
        static void KeyRowSmall(Transform parent, Vector2 anchoredPosition, string[] keys, string label)
        {
            const float capHeight = 38f;
            float x = 0f;
            GameObject row = new GameObject("Keys " + label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rr = row.GetComponent<RectTransform>();
            rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0f, 0f);
            rr.anchoredPosition = anchoredPosition;
            rr.sizeDelta = new Vector2(400f, capHeight);
            foreach (string key in keys)
            {
                Sprite sprite = Key(key);
                if (sprite == null) continue;
                float w = capHeight * sprite.bounds.size.x / sprite.bounds.size.y;
                Image img = UiImage("key_" + key, row.transform, new Color(1f, 1f, 1f, 0.85f));
                img.sprite = sprite;
                img.preserveAspect = true;
                RectTransform r = img.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(x, 0f);
                r.sizeDelta = new Vector2(w, capHeight);
                x += w + 4f;
            }
            Text text = UiText("Label", row.transform, 24, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(x + 8f, 0f), new Vector2(200f, capHeight));
            text.text = label;
            text.color = new Color(0.85f, 0.9f, 1f, 0.85f);
        }

        /// <summary>A row of keycap images followed by a label, laid out left to right from an anchored position.</summary>
        static void KeyRow(Transform parent, Vector2 anchoredPosition, string[] keys, string label)
        {
            const float capHeight = 64f;
            float x = 0f;
            GameObject row = new GameObject("Keys " + label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            Place(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), anchoredPosition, new Vector2(700f, capHeight));
            row.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            foreach (string key in keys)
            {
                Sprite sprite = Key(key);
                if (sprite == null) continue;
                float w = capHeight * sprite.bounds.size.x / sprite.bounds.size.y;
                Image img = UiImage("key_" + key, row.transform, Color.white);
                img.sprite = sprite;
                img.preserveAspect = true;
                RectTransform r = img.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
                r.pivot = new Vector2(0f, 0.5f);
                r.anchoredPosition = new Vector2(x, 0f);
                r.sizeDelta = new Vector2(w, capHeight);
                x += w + 6f;
            }
            Text text = UiText("Label", row.transform, 34, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(x + 14f, 0f), new Vector2(400f, capHeight));
            text.text = label;
            text.color = new Color(0.85f, 0.9f, 1f);
        }

        static Text UiText(string name, Transform parent, int size, TextAnchor anchor, Vector2 anchorPoint, Vector2 anchoredPosition, Vector2 sizeDelta, bool display = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Place(rect, anchorPoint, anchoredPosition, sizeDelta);
            Text text = go.AddComponent<Text>();
            text.font = display ? titleFont : font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = name;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        static Image UiImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.sprite = white;
            return image;
        }

        static void Place(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // ================================================================ assets

        static Sprite Art(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + name + ".png");
        }

        static Sprite Key(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Flipside/Art/UI/key_" + name + ".png");
        }

        static PhysicsMaterial2D EnsurePhysicsMaterial()
        {
            PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysicsMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("Frictionless") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
            }
            return material;
        }
    }
}
