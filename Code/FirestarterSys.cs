using System.Collections.Generic;
using Game.Components;
using Game.Constants;
using Game.Data;
using Game.Systems;
using KL.Randomness;
using KL.Utils;
using UnityEngine;

namespace ModFirestarter.Systems {
    public sealed class FirestarterSys : GameSystem {
        // The convention is that all systems end with Sys, and SysId is equal to the class name
        public const string SysId = "FirestarterSys";
        public override string Id => SysId;
        // If your system can work in sandbox too, set this to false
        public override bool SkipInSandbox => true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register() {
            GameSystems.Register(SysId, () => new FirestarterSys());
        }

        // If your system adds some mechanic that may not be compatible with
        // old saves, you have to set the MinRequiredVersion
        //public override string MinRequiredVersion => "0.6.89";

        protected override void OnInitialize() {
            S.Sig.AreasInitialized.AddListener(OnAreasInit);
        }

        // If your system depends on AreasSys, for example, you may want to
        // start ticking your system only after initial areas have been built
        private void OnAreasInit() {
            S.Clock.OnTickAsync.AddListener(OnTickAsync);
        }

        private long nextFireTick;
        private readonly List<FlammableComp> candidates = new();

        // If the simulation cannot keep up, delta will show how many ticks have been skipped since last call
        private void OnTickAsync(long ticks, int delta) {
            if (ticks < nextFireTick) { return; }
            if (nextFireTick == 0) {
                nextFireTick = ticks + Consts.TicksPerHour;
                return;
            }

            nextFireTick = ticks + Consts.TicksPerHour;

            // time to start fire
            var allFlamables = S.Components.AllByType<FlammableComp>();
            candidates.Clear();
            foreach (var flamable in allFlamables) {
                if (!flamable.IsConstructed) { continue; }
                if (flamable.Entity.Definition.LayerId < WorldLayer.Objects) {
                    continue;
                }
                if (S.Oxygen.Get(flamable.Entity.PosIdx) > Consts.MinOxygen) {
                    candidates.Add(flamable as FlammableComp);
                }
            }
            if (candidates.Count == 0) {
                D.Warn("Zero firestarter candidates!");
                return;
            }
            var target = S.Rng.From(candidates);
            D.Warn("Setting on fire: {0}", target);
            target.SetOnFire();
            EntityUtils.CameraFocusOn(target.Entity);
        }

        public override void Unload() {
            // Release the resources here
        }

        public string GetName() {
            return Id;
        }
    }
}