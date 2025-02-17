﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HairControlPanel : MonoBehaviour
{
    public static HairControlPanel instance;

    [SerializeField]
    HairGenerator hairGenerator;
    [SerializeField]
    Vector3 gravity = new Vector3(0, -9.8f, 0);
    public Slider hairLengthSlider;
    public float hairLengthInitial;
    public Slider hairDensitySlider;
    public float hairDensityInitial = 1;
    public Slider massSlider;
    public float massInitial = 0.1F;
    public Slider dragForceSlider;
    public float dragForceInitial = 0.9F;
    public Slider hairCurlSlider;
    public float hairCurlInitial = 0.5F;

    //data
    bool onInit = true;
    public static float HairStrandNodeMass => instance.massSlider.value;
    public static float HairStrandNodeSpan => instance.hairLengthSlider.value;
    public static float HairStrandDragForce => instance.dragForceSlider.value;
    public static float HairCurl => instance.hairCurlSlider.value;
    public static Vector3 Gravity => instance.gravity;


    public GameObject tool;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    public void Init()
    {
        if (!onInit)
            return;
        hairDensitySlider.onValueChanged.AddListener(OnHairDensityChange);
        hairDensitySlider.value = hairDensityInitial;
        hairLengthSlider.onValueChanged.AddListener(OnHairLengthSliderChange);
        hairLengthSlider.value = hairLengthInitial;
        hairCurlSlider.onValueChanged.AddListener(OnHairLengthSliderChange);
        hairCurlSlider.value = hairCurlInitial;
        massSlider.onValueChanged.AddListener(OnMassSliderChange);
        massSlider.value = massInitial;
        dragForceSlider.onValueChanged.AddListener(OnDragForceSliderChange);
        dragForceSlider.value = dragForceInitial;
        hairGenerator.HairStrands.ForEach(strand => strand.Init());
        onInit = false;
    }
    public void OnHairDensityChange(float value)
    {
        hairGenerator.DensityFactor = value;
    }

    public void OnHairLengthSliderChange(float value)
    {
        hairGenerator.HairStrands.ForEach(strand => strand.UpdateSpringRestLengthAndCurl());
    }
    public void OnMassSliderChange(float value)
    {
    }
    public void OnDragForceSliderChange(float value)
    {
    }
}
