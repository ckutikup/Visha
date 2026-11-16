using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Visha.UI;

namespace Visha.Combat.Flow.Tests
{
    public class SanctumPresentationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject root, flowRoot, actorRoot;
        private SanctumPresentation effect;
        private CombatFlowController flow;
        private SpriteRenderer actor;

        [SetUp] public void Setup()
        {
            flowRoot = new GameObject("Flow");
            flowRoot.SetActive(false);
            flow = flowRoot.AddComponent<CombatFlowController>();
            actorRoot = new GameObject("Actor", typeof(SpriteRenderer));
            actor = actorRoot.GetComponent<SpriteRenderer>();
            actor.color = new Color(.3f, .4f, .5f, .58f);
            actor.transform.localScale = new Vector3(2, 3, 1);
            root = new GameObject("Presentation");
            effect = root.AddComponent<SanctumPresentation>();
            Set("guardianRenderer", actor);
            Set("playerRenderer", actor);
            effect.Configure(flow);
        }

        [TearDown] public void Cleanup()
        {
            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(actorRoot);
            UnityEngine.Object.DestroyImmediate(flowRoot);
        }

        private void Set(string name, object value) => typeof(SanctumPresentation).GetField(name, Private).SetValue(effect, value);
        private T Get<T>(string name) => (T)typeof(SanctumPresentation).GetField(name, Private).GetValue(effect);
        private void Call(string name) => typeof(SanctumPresentation).GetMethod(name, Private).Invoke(effect, null);
        private Action<BattleViewState> Listeners => (Action<BattleViewState>)typeof(CombatFlowController).GetField("OnStateUpdated", Private).GetValue(flow);
        private void Publish(int boss, BattlePhase phase, int damage = 0) => Listeners?.Invoke(new BattleViewState
        { bossPhase = boss, bossPhaseCount = 2, phase = phase, incomingDamage = damage });

        [Test] public void ReconfigureAndEnableKeepOneSubscription()
        {
            effect.Configure(flow);
            Assert.AreEqual(1, Listeners.GetInvocationList().Length);
            effect.enabled = false;
            Call("OnDisable");
            Assert.IsNull(Listeners);
            effect.Configure(flow);
            Assert.IsNull(Listeners);
            effect.enabled = true;
            Call("OnEnable");
            Assert.AreEqual(1, Listeners.GetInvocationList().Length);
        }

        [Test] public void EnemyPhaseDoesNotPlayAttackBeforePoisonResolves()
        {
            Publish(0, BattlePhase.EnemyTurn, 10);
            Assert.AreEqual(0, Get<float>("bossUntil"));
            Publish(1, BattlePhase.PlayerTurn);
            Publish(1, BattlePhase.EnemyTurn, 10);
            Assert.AreEqual(0, Get<float>("bossUntil"));
            effect.PlayBossAttack();
            Assert.Greater(Get<float>("bossUntil"), Time.time);
            Set("bossUntil", 0f);
            Publish(1, BattlePhase.EnemyTurn, 10);
            Assert.AreEqual(0, Get<float>("bossUntil"));
        }

        [Test] public void PhaseEffectClearsOnRetryAndLeavingBattle()
        {
            Publish(1, BattlePhase.PlayerTurn);
            Publish(2, BattlePhase.PlayerTurn);
            Assert.Greater(Get<float>("bossUntil"), Time.time);
            Call("LateUpdate");
            var glow = Get<SpriteRenderer>("bossGlow");
            Assert.IsTrue(glow.enabled);
            Call("ResetEffects");
            Assert.IsFalse(glow.enabled);
            effect.PlayPhaseChange();
            Publish(2, BattlePhase.Victory);
            Assert.AreEqual(0, Get<float>("bossUntil"));
            effect.PlayPhaseChange();
            effect.enabled = false;
            Call("OnDisable");
            Assert.AreEqual(0, Get<float>("bossUntil"));
        }

        [Test] public void FeedbackDoesNotOverwriteActorColorOrScale()
        {
            var color = actor.color;
            var scale = actor.transform.localScale;
            effect.PlayPhaseChange();
            Call("HandleBlessingTriggered");
            Call("LateUpdate");
            Assert.AreEqual(color, actor.color);
            Assert.AreEqual(scale, actor.transform.localScale);
            Call("ResetEffects");
            Assert.IsFalse(Get<SpriteRenderer>("playerGlow").enabled);
            Assert.IsFalse(Get<SpriteRenderer>("bossGlow").enabled);
        }
    }
}
