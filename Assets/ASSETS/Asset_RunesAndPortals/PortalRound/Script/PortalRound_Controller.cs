using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalRound_Controller : MonoBehaviour 
{
    [Header("Applied to the effects at start")]
    [SerializeField] private Color effectsColor;

    [Header("Changing these might `break` the effects")]
    [Space(20)]
    [SerializeField] private ParticleSystem[] effectsPartSystems;
    [SerializeField] private Light portalLight;
    [SerializeField] private Transform portalRoundMeshTF;
    [SerializeField] private AudioSource portalAudio;
    [Space(10)]
    [SerializeField] private bool floatingAnimationOn = true;
    [SerializeField] private AnimationCurve floatingCurve;

    private bool portalActive, inTransition, isFloating;
    private float transitionF, lightF, evalFloat, floatSpeed = 0.2f;
    private Material portalMaterial;
    private Transform portalTF;
    private Vector3 originalPosition;

    private Coroutine transitionCor, floatingMovementCor;

    private void OnEnable()
    {
        portalTF = transform;
        originalPosition = portalTF.position;

        //get the material to set emmision
        portalMaterial = portalRoundMeshTF.GetComponent<Renderer>().material;
        portalMaterial.SetColor("_EmissionColor", effectsColor);
        portalMaterial.SetFloat("_EmissionStrength", 0);

        //get and set light intensity
        portalLight.color = effectsColor;
        lightF = portalLight.intensity;
        portalLight.intensity = 0;

        foreach (ParticleSystem part in effectsPartSystems)
        {
            ParticleSystem.MainModule mod = part.main;
            mod.startColor = effectsColor;
        }
    }

    IEnumerator PortalTransition()
    {
        inTransition = true;

        if (portalActive)//fade in
        {
            while (transitionF < 1f)
            {
                transitionF = Mathf.MoveTowards(transitionF, 1, Time.deltaTime * 0.1f);

                portalMaterial.SetFloat("_EmissionStrength", transitionF);
                portalLight.intensity = lightF * transitionF;
                portalAudio.volume = transitionF * 0.8f;//max volume

                yield return new WaitForSeconds(Time.deltaTime);
            }

            inTransition = false;
            StopCoroutine(transitionCor);
        }
        else if (!portalActive)//fade out
        {
            while (transitionF > 0f)
            {
                transitionF = Mathf.MoveTowards(transitionF, 0f, Time.deltaTime * 0.4f);

                portalMaterial.SetFloat("_EmissionStrength", transitionF);
                portalLight.intensity = lightF * transitionF;
                portalAudio.volume = transitionF * 0.8f;//max volume

                yield return new WaitForSeconds(Time.deltaTime);
            }

            portalAudio.Stop();
            inTransition = false;
            StopCoroutine(transitionCor);
        }
    }

    private IEnumerator FloatingMovement()
    {
        isFloating = true;
        Vector3 wantedPosition = originalPosition;

        while (true)
        {
            if (evalFloat >= 1)
                evalFloat = 0;

            evalFloat += Time.deltaTime * floatSpeed;

            wantedPosition[1] = originalPosition.y + floatingCurve.Evaluate(evalFloat);
            portalTF.position = wantedPosition;

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    public void F_TogglePortalRound(bool _activate)
    {
        if (portalActive == _activate)
            return;

        portalActive = _activate;

        if (_activate)//activate
        {
            foreach (ParticleSystem partSys in effectsPartSystems)
                partSys.Play();

            portalAudio.Play();

            if (floatingAnimationOn && !isFloating)
                floatingMovementCor = StartCoroutine(FloatingMovement());
        }
        else if (!_activate)//deactivate
        {
            foreach (ParticleSystem partSys in effectsPartSystems)
                partSys.Stop();

            if (isFloating)
            {
                StopCoroutine(floatingMovementCor);
                isFloating = false;
            }
        }

        if (!inTransition)
            transitionCor = StartCoroutine(PortalTransition());
    }
}
