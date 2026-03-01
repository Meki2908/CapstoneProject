using UnityEngine;

namespace ithappy.Animals_FREE
{
    public class AnimalFleeTriggerInput : MonoBehaviour
    {
        [Header("Refs")]
        public CreatureMover mover;             // kéo CreatureMover (ở root) vào đây
        public string playerTag = "Player";

        [Header("Flee")]
        public float fleeDuration = 1.0f;       // chạy bao lâu
        public float lookAhead = 5f;            // target hướng nhìn / hướng chạy
        public bool zigzagLikeChicken = false;  // chạy lệch cho giống gà

        [Header("Return Home")]
        public float homeStopDistance = 0.6f;

        Transform toucher;
        Vector3 homePos;
        float timer;

        enum State { Idle, Flee, Return }
        State state = State.Idle;

        void Awake()
        {
            if (!mover) mover = GetComponentInParent<CreatureMover>();
            homePos = mover.transform.position;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            toucher = other.transform;
            timer = fleeDuration;
            state = State.Flee;
        }

        void Update()
        {
            if (!mover) return;

            switch (state)
            {
                case State.Idle:
                    mover.SetInput(Vector2.zero, mover.transform.position + mover.transform.forward * lookAhead, false, false);
                    break;

                case State.Flee:
                    DoFlee();
                    break;

                case State.Return:
                    DoReturn();
                    break;
            }
        }

        void DoFlee()
        {
            if (!toucher)
            {
                state = State.Return;
                return;
            }

            timer -= Time.deltaTime;

            Vector3 away = mover.transform.position - toucher.position;
            away.y = 0f;

            Vector3 dir = away.sqrMagnitude > 0.001f ? away.normalized : mover.transform.forward;

            if (zigzagLikeChicken)
            {
                float angle = 35f;
                dir = Quaternion.AngleAxis(Random.Range(-angle, angle), Vector3.up) * dir;
                dir.Normalize();
            }

            Vector3 target = mover.transform.position + dir * lookAhead;

            // Axis (0,1) = đi thẳng theo hướng target, isRun = true
            mover.SetInput(new Vector2(0f, 1f), target, true, false);

            if (timer <= 0f)
                state = State.Return;
        }

        void DoReturn()
        {
            Vector3 toHome = homePos - mover.transform.position;
            toHome.y = 0f;

            float dist = toHome.magnitude;
            if (dist <= homeStopDistance)
            {
                state = State.Idle;
                return;
            }

            Vector3 dir = toHome.normalized;
            Vector3 target = mover.transform.position + dir * lookAhead;

            // về nhà đi bộ
            mover.SetInput(new Vector2(0f, 1f), target, false, false);
        }

        public void SetHomeHere() => homePos = mover.transform.position;
    }
}
