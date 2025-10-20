using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collectable : MonoBehaviour
{
    [SerializeField] private float _oscillationAmplitude = 0.0f;
    [SerializeField] private float _oscillationFrequency = 0.0f;

    //[SerializeField] private float _rotationSpeed = 0.0f;
    //[SerializeField] private AnimationCurve _rotationCurve = null;

    private Vector3 _basePosition = Vector3.zero;

    private void Awake()
    {
        //On stock la position initiale de l'objet
        _basePosition = transform.position;
    }

    private void Update()
    {
        //On obtient l'oscillation en Y grace a sinus. On multiplie le temps écoulé depuis le lancement du jeu par la fréquence
        float osci = Mathf.Sin(Time.time * _oscillationFrequency);
        //On ramène l'oscillation entre 0 et 1 (de base l'oscillation est entre -1 et 1)
        osci = (osci + 1.0f) / 2.0f;
        //On multiplie par l'amplitude
        osci *= _oscillationAmplitude;
        //On applique l'oscillation à la position
        transform.position = _basePosition + new Vector3(0.0f, osci, 0.0f);
    }
}
