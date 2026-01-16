using UnityEngine;

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(CreatureMover))]
    public class AnimalFleeOnTouch_CreatureMover : MonoBehaviour
    {
        [Header("Touch")]
        public string playerTag = "Player";

        [Header("Flee")]
        public float fleeDistance = 6f;     // chạy xa bao nhiêu mét
        public float fleeDuration = 1.0f;   // chạy trong bao lâu
        public bool zigzagLikeChicken = false; // bật nếu muốn chạy hơi lệch

        [Header("Return Home")]
        public float homeStopDistance = 0.6f;

        [Header("Look Target Distance")]
        public float lookAhead = 5f;        // target đưa vào CreatureMover để nó hướng theo

        private CreatureMover mover;
        private Vector3 homePos;

        private Transform toucher;
        private float fleeTimer;

        private enum State { Idle, Flee, Return }
        private State state = State.Idle;

        void Awake()
        {
            mover = GetComponent<CreatureMover>();
            homePos = transform.position;
        }

        // GẮN SCRIPT NÀY VÀO OBJECT CÓ TRIGGER (nếu bạn gắn ở root thì trigger có thể nằm child vẫn ok)
        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            toucher = other.transform;
            fleeTimer = fleeDuration;
            state = State.Flee;
        }

        void Update()
        {
            switch (state)
            {
                case State.Idle:
                    // đứng yên
                    mover.SetInput(Vector2.zero, transform.position + transform.forward * lookAhead, false, false);
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

            fleeTimer -= Time.deltaTime;

            // hướng chạy: ngược player
            Vector3 away = transform.position - toucher.position;
            away.y = 0f;

            Vector3 dir = away.sqrMagnitude > 0.001f ? away.normalized : transform.forward;

            // kiểu “gà chạy hơi lệch” cho tự nhiên
            if (zigzagLikeChicken)
            {
                float angle = 35f;
                dir = Quaternion.AngleAxis(Random.Range(-angle, angle), Vector3.up) * dir;
                dir.Normalize();
            }

            Vector3 target = transform.position + dir * lookAhead;

            // Feed input: axis (0,1) = đi thẳng theo hướng target, isRun = true
            mover.SetInput(new Vector2(0f, 1f), target, true, false);

            if (fleeTimer <= 0f)
            {
                state = State.Return;
            }
        }

        void DoReturn()
        {
            Vector3 toHome = homePos - transform.position;
            toHome.y = 0f;

            float dist = toHome.magnitude;
            if (dist <= homeStopDistance)
            {
                state = State.Idle;
                return;
            }

            Vector3 dir = toHome.normalized;
            Vector3 target = transform.position + dir * lookAhead;

            // về nhà đi bộ (isRun = false)
            mover.SetInput(new Vector2(0f, 1f), target, false, false);
        }

        // Nếu bạn spawn animal runtime và muốn set “nhà” theo vị trí spawn:
        public void SetHomeHere() => homePos = transform.position;
    }
}
