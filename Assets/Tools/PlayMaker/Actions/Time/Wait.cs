// (c) Copyright HutongGames, LLC 2010-2013. All rights reserved.

using Chronos;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
    [ActionCategory(ActionCategory.Time)]
    [Tooltip("Delays a State from finishing by the specified time. NOTE: Other actions continue, but FINISHED can't happen before Time.")]
    public class Wait : FsmStateAction
    {

        [RequiredField]
        public FsmFloat time;
        public FsmEvent finishEvent;
        public bool realTime;

        private float startTime;
        private float timer;

        private Timeline timeline;

        public override void Reset()
        {
            time = 1f;
            finishEvent = null;
            realTime = false;
        }

        public override void OnEnter()
        {
            if (time.Value <= 0)
            {
                Fsm.Event(finishEvent);
                Finish();
                return;
            }

            startTime = FsmTime.RealtimeSinceStartup;
            timer = 0f;
        }

        public override void OnUpdate()
        {
            // update time

            if (realTime)
            {
                timer = FsmTime.RealtimeSinceStartup - startTime;
            }
            else
            {
                timer += Time.deltaTime;
            }

            if (timer >= time.Value)
            {
                Finish();
                if (finishEvent != null)
                {
                    Fsm.Event(finishEvent);
                }
            }
        }



#if UNITY_EDITOR

        public override string AutoName()
        {
            return ActionHelpers.AutoName(this, time);
        }

        public override float GetProgress()
        {
            return Mathf.Min(timer / time.Value, 1f);
        }

#endif
    }
}
