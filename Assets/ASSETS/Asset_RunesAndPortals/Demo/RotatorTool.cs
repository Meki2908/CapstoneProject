using UnityEngine;

namespace PaulosTools
{
    public class RotatorTool : MonoBehaviour
    {
        [SerializeField] private bool rotateOnn;
        [SerializeField] private Transform rotatingTransform;
        [SerializeField] private Vector3 rotationAxis = Vector3.zero;
        [SerializeField] private Space rotationSpace;
        [SerializeField] private float rotationSpeed = 1f;

        private void Start()
        {
            if (!rotatingTransform)
                rotatingTransform = transform;
        }

        private void Update()
        {
            if (rotateOnn)
                rotatingTransform.Rotate(rotationAxis, Time.deltaTime * rotationSpeed, rotationSpace);
        }
    }
}
