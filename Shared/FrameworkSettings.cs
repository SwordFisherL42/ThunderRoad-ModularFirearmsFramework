using System;
using System.Collections;
using System.Reflection;
using ThunderRoad;
using UnityEngine;


namespace ModularFirearms.Shared
{
    public class FrameworkSettings : LevelModule
    {
        public static FrameworkSettings local;
        public bool useHitscan = false;
        public float hitscanMaxDistance = 1f;
        public string customEffectID = "PenetrationFisherFirearmModular";

        public override IEnumerator OnLoadCoroutine()
        {
            if (local == null) local = this;
            yield return base.OnLoadCoroutine();
        }
    }
}
