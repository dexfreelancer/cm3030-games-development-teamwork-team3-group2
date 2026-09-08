using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Flipside.Tests
{
    /// <summary>
    /// Play-mode tests for the gravity-flip motor. Each test builds a tiny world in code:
    /// a floor at y = 0 and a ceiling at y = 6, with the player standing on the floor.
    /// </summary>
    public class PlayerMotorTests
    {
        GameObject root;
        PlayerMotor motor;
        PlayerRespawn respawn;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TestWorld");
            Solid("Floor", new Vector2(0f, -1f), new Vector2(40f, 2f));
            Solid("Ceiling", new Vector2(0f, 7f), new Vector2(40f, 2f));

            GameObject player = new GameObject("Player");
            player.transform.SetParent(root.transform);
            player.transform.position = new Vector2(0f, 0.71f);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.interpolation = RigidbodyInterpolation2D.None;
            BoxCollider2D box = player.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.8f, 1.4f);
            motor = player.AddComponent<PlayerMotor>();
            respawn = player.AddComponent<PlayerRespawn>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
        }

        void Solid(string name, Vector2 center, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(root.transform);
            go.transform.position = center;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Player_starts_grounded_on_the_floor()
        {
            yield return Wait(0.3f);
            Assert.IsTrue(motor.IsGrounded);
            Assert.AreEqual(1, motor.GravitySign);
        }

        [UnityTest]
        public IEnumerator Flip_reverses_gravity_and_lands_on_the_ceiling()
        {
            yield return Wait(0.3f);
            motor.RequestFlip();
            yield return Wait(1.5f);
            Assert.AreEqual(-1, motor.GravitySign);
            Assert.IsTrue(motor.IsGrounded, "should rest against the ceiling");
            Assert.Greater(motor.transform.position.y, 4.5f);
        }

        [UnityTest]
        public IEnumerator Flip_keeps_horizontal_momentum()
        {
            yield return Wait(0.3f);
            motor.SetMoveInput(1f);
            yield return Wait(0.5f);
            float before = motor.Velocity.x;
            motor.RequestFlip();
            yield return Wait(0.15f); // mid transition, airborne
            Assert.Greater(before, 6f);
            Assert.IsTrue(motor.IsFlipping || !motor.IsGrounded);
            Assert.Greater(motor.Velocity.x, before - 0.5f, "horizontal speed should carry through the flip");
        }

        [UnityTest]
        public IEnumerator Repeated_air_flips_cannot_hover()
        {
            yield return Wait(0.3f);
            int landings = 0;
            motor.Landed += () => landings++;
            // Hold the flip key down: a request every physics step for three seconds.
            float end = Time.time + 3f;
            while (Time.time < end)
            {
                motor.RequestFlip();
                yield return new WaitForFixedUpdate();
            }
            Assert.GreaterOrEqual(landings, 2, "with flip spammed the robot must keep touching surfaces instead of hovering");
            Assert.Less(Mathf.Abs(motor.transform.position.y - 0.71f), 2.5f, "should stay near the floor it keeps returning to");
        }

        [UnityTest]
        public IEnumerator Jump_leaves_the_ground_and_returns()
        {
            yield return Wait(0.3f);
            motor.RequestJump();
            motor.SetJumpHeld(true);
            var trace = new System.Text.StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
                trace.Append(string.Format("[{0}: y={1:F2} v={2:F2} g={3}] ", i, motor.transform.position.y, motor.Velocity.y, motor.IsGrounded));
            }
            Assert.IsFalse(motor.IsGrounded, trace.ToString());
            Assert.Greater(motor.transform.position.y, 1.2f);
            motor.SetJumpHeld(false);
            yield return Wait(1.2f);
            Assert.IsTrue(motor.IsGrounded);
        }

        [UnityTest]
        public IEnumerator Respawn_restores_checkpoint_position_and_gravity()
        {
            yield return Wait(0.3f);
            respawn.SetCheckpoint(new Vector2(5f, 0.71f), 1);
            motor.RequestFlip();
            yield return Wait(1.2f);
            respawn.Kill();
            respawn.Kill(); // a second contact in the same death must not count twice
            yield return Wait(0.6f);
            Assert.AreEqual(1, respawn.Deaths);
            Assert.AreEqual(1, motor.GravitySign);
            Assert.AreEqual(5f, motor.transform.position.x, 0.2f);
        }
    }
}
