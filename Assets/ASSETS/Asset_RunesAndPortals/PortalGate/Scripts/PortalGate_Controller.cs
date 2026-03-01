using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PortalGate_Controller : MonoBehaviour
{
    [Header("Applied to the effects at start")]
    [SerializeField] private Color portalEffectColor;

    [Header("Changing these might `break` the effects")]
    [Space(20)]
    [SerializeField] private Renderer portalRenderer;
    [SerializeField] private ParticleSystem[] effectsPartSystems;
    [SerializeField] private Light portalLight;
    [SerializeField] private Transform symbolTF;
    [SerializeField] private AudioSource portalAudio, flashAudio;

    private bool portalActive, inTransition;
    private float transitionF, lightF;
    private Material portalMat, portalEffectMat;
    private Vector3 symbolStartPos;

    private Coroutine transitionCor, symbolMovementCor;

    private void OnEnable()
    {
        //get materials to set color and emmision
        Material[] mats = portalRenderer.materials.ToArray();
        portalMat = mats[0];
        portalEffectMat = mats[1];

        portalMat.SetColor("_EmissionColor", portalEffectColor);
        portalMat.SetFloat("_EmissionStrength", 0);
        portalEffectMat.SetColor("_ColorMain", portalEffectColor);
        portalEffectMat.SetFloat("_PortalFade", 0f);

        symbolStartPos = symbolTF.localPosition;
        symbolTF.GetComponent<Renderer>().material = portalMat;

        //get and set light intensity
        portalLight.color = portalEffectColor;
        lightF = portalLight.intensity;
        portalLight.intensity = 0;

        foreach (ParticleSystem part in effectsPartSystems)
        {
            ParticleSystem.MainModule mod = part.main;
            mod.startColor = portalEffectColor;
        }
    }

    public void F_TogglePortalGate(bool _activate)
    {
        if (inTransition || portalActive == _activate)
            return;

        portalActive = _activate;

        if (_activate)//activate
        {
            foreach (ParticleSystem part in effectsPartSystems)
            {
                part.Play();
            }

            portalAudio.Play();
            flashAudio.Play();

            symbolMovementCor = StartCoroutine(SymbolMovement());
        }
        else if (!_activate)//deactivate
        {
            foreach (ParticleSystem part in effectsPartSystems)
            {
                part.Stop();
            }
        }

        if (!inTransition)
            transitionCor = StartCoroutine(PortalTransition());
    }

    IEnumerator PortalTransition()
    {
        inTransition = true;

        if (portalActive)//fade in
        {
            while (transitionF < 1f)
            {
                transitionF = Mathf.MoveTowards(transitionF, 1, Time.deltaTime * 0.2f);

                portalMat.SetFloat("_EmissionStrength", transitionF);
                portalEffectMat.SetFloat("_PortalFade", transitionF * 0.4f);
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

                portalMat.SetFloat("_EmissionStrength", transitionF);
                portalEffectMat.SetFloat("_PortalFade", transitionF * 0.4f);
                portalLight.intensity = lightF * transitionF;
                portalAudio.volume = transitionF * 0.8f;//max volume

                yield return new WaitForSeconds(Time.deltaTime);
            }

            portalAudio.Stop();
            inTransition = false;
            StopCoroutine(symbolMovementCor);
            StopCoroutine(transitionCor);
        }
    }

    private IEnumerator SymbolMovement()
    {
        Vector3 randomPos = symbolStartPos;
        float lerpF = 0;

        while (true)
        {
            if (symbolTF.localPosition == randomPos)
            {
                randomPos[1] = Random.Range(-0.08f, 0.08f);
                randomPos[2] = Random.Range(-0.08f, 0.08f);

                randomPos = symbolStartPos + randomPos;
                lerpF = 0f;
            }
            else
            {
                symbolTF.localPosition = Vector3.Slerp(symbolTF.localPosition, randomPos, lerpF);
                lerpF += 0.001f;
            }

            yield return new WaitForSeconds(0.04f);
        }
    }
}
