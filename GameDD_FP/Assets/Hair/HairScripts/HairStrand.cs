﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HairStrand : MonoBehaviour
{
    [SerializeField]
    Transform boneFather;

    [SerializeField]
    SkinnedMeshRenderer meshRenderer;

    //data
    public HairGenerator hairGenerator;
    List<TransformedSphereCollider> SphereColliders => hairGenerator.SphereColliders;
    // HairCutter Cutter => hairGenerator.Cutter;
    float HairEraseY => hairGenerator.hairEraseY;
    float HairCarl => HairControlPanel.HairCurl;
    List<HairStrandNode> strandNodes = new List<HairStrandNode>();
    List<Spring> springs = new List<Spring>();
    // public GameObject dyer;
    [HideInInspector]
    bool finishedInit = false;

    /// <summary>
    /// Prevent cut downed hair from cut again
    /// </summary>
    bool cutted = false;
    /// <summary>
    /// Called after HairControlPanel Init()
    /// </summary>
    public void Init()
    {
        if (finishedInit)
            return;
        //apply HairStrandNode component to all mass(transform childrens)
        Transform child = boneFather;
        while (child.childCount > 0)
        {
            child = child.GetChild(0);
            HairStrandNode strandNode = child.GetComponent<HairStrandNode>();
            strandNode.Init();
            strandNodes.Add(strandNode);
        }
        strandNodes[0].isPined = true;
        //generate spring
        float springRestLength = HairControlPanel.HairStrandNodeSpan;
        float hairCurl = HairCarl;
        for (int i = 1; i < strandNodes.Count; i++)
        {
            Spring s = new Spring();
            s.node1 = strandNodes[i - 1];
            s.node2 = strandNodes[i];
            s.massIndexDistance = 1;
            s.SetRestLength(springRestLength, hairCurl);
            springs.Add(s);
            if (i + 1 < strandNodes.Count)
            {
                Spring s2 = new Spring();
                s2.node1 = strandNodes[i - 1];
                s2.node2 = strandNodes[i + 1];
                s2.massIndexDistance = 2;
                s2.SetRestLength(springRestLength, hairCurl);
                springs.Add(s2);
            }
        }
        finishedInit = true;
    }
    private void FixedUpdate()
    {
        if (strandNodes[0].transform.position.y <= HairEraseY)
        {
            Destroy(gameObject);
            return;
        }
        if (!finishedInit)
            return;
        GameObject currentTool = HairControlPanel.instance.tool;
        if(currentTool != null)
        {
            switch(currentTool.tag)
            {
                case "CutTool":
                    if (!cutted)
                        CutterProcess(currentTool.GetComponent<HairCutter>());
                    break;
                case "DryTool":
                    DryerProcess(currentTool.GetComponent<HairDryer>());
                    break;
                case "DyeTool":
                    DyerProcess(currentTool.GetComponent<HairDyer>());
                    break;
            }
        }
        VerletMathod(Time.fixedDeltaTime);
    }
    /// <summary>
    /// for HairControlPanel
    /// </summary>
    public void UpdateSpringRestLengthAndCurl()
    {
        springs.ForEach(spring => spring.SetRestLength(HairControlPanel.HairStrandNodeSpan, HairCarl));
    }
    void DyerProcess(HairDyer hairDyer)
    {
        SphereCollider dyeCollider = hairDyer.GetComponent<SphereCollider>();
        Vector3 dyerPosition = hairDyer.transform.position;
        Color dyeColor = hairDyer.dyeColor;
        float sphereRadius = dyeCollider.radius * dyeCollider.transform.lossyScale.x;
        foreach (var item in strandNodes)
        {
            if (Vector3.Distance(item.TempPosition, dyerPosition) < sphereRadius)
            {
                Material hairmat = meshRenderer.material;
                hairmat.SetColor("_Diffuse", dyeColor);
            }

        }
    }
    void DryerProcess(HairDryer hairDryer)
    {
        float windStr = hairDryer.windPower;
        Vector3 windPos = hairDryer.transform.position;
        foreach (var item in strandNodes)
        {
            Vector3 itemPos = item.transform.position;
            Vector3 distVec = itemPos - windPos;
            item.extraForce += distVec * (windStr / distVec.sqrMagnitude);
        }
    }
    void CutterProcess(HairCutter hairCutter)
    {
        TransformedSphereCollider tsphereCollider = hairCutter.TSphereCollider;
        var sphereRadius = tsphereCollider.Radius;
        float sphereRadiusSq = tsphereCollider.RadiusSq;
        var sphereCenter = tsphereCollider.Center;
        bool touchedCutter = false;
        HairStrand copyStrand = null;
        int copyIndex = 0;
        foreach (var item in strandNodes)
        {
            if (!item.IsActive)
                continue;
            if (!touchedCutter)
            {
                Vector3 distVector = item.TempPosition - sphereCenter;
                float sqMagnitude = distVector.sqrMagnitude;
                if (sqMagnitude < sphereRadiusSq)
                {
                    touchedCutter = true;
                    copyStrand = Instantiate(this, null);
                    copyStrand.Init();
                }
            }
            if (touchedCutter)
            {
                var copyNode = copyStrand.strandNodes[copyIndex++];
                copyNode.transform.position = copyNode.lastPosition = copyNode.TempPosition = item.TempPosition;
                item.isFolded = true;
                item.transform.localPosition = Vector3.zero;
                item.transform.localEulerAngles = Vector3.zero;
            }
        }
        if (copyStrand != null)
        {
            for (; copyIndex < strandNodes.Count; ++copyIndex)
            {
                var item = copyStrand.strandNodes[copyIndex];
                item.isFolded = true;
                item.transform.localPosition = Vector3.zero;
                item.transform.localEulerAngles = Vector3.zero;
            }
            copyStrand.strandNodes[0].isPined = false;
            copyStrand.cutted = true; // prevent cut downed hair from cut again
            copyStrand.transform.localScale *= 30;
        }
    }
    public void VerletMathod(float deltaTime)
    {
        //exforce (gravity, ...)
        float sqDeltaTimeDivMass = deltaTime * deltaTime / HairControlPanel.HairStrandNodeMass;
        Vector3 deltaPos_gravity = HairControlPanel.Gravity * sqDeltaTimeDivMass;
        float dragForceFactor = HairControlPanel.HairStrandDragForce;
        foreach (var node in strandNodes)
        {
            if (node.IsActive)
            {
                Vector3 tempPosition = node.TempPosition;
                node.TempPosition += dragForceFactor * (tempPosition - node.lastPosition) + deltaPos_gravity;
                if (node.extraForce != Vector3.zero)
                {
                    node.TempPosition += node.extraForce * sqDeltaTimeDivMass;
                    node.extraForce = Vector3.zero;
                }
                node.lastPosition = tempPosition;
            }
        }
        //spring
        foreach (var spring in springs)
        {
            HairStrandNode node1 = spring.node1, node2 = spring.node2;
            if (node1.isFolded || node2.isFolded || (node1.isPined && node2.isPined))
                continue;
            Vector3 subtract = node1.TempPosition - node2.TempPosition;
            subtract *= 1 - spring.RestLength / subtract.magnitude;
            if (node1.isPined)
            {
                node2.TempPosition += subtract;
            }
            else if (node2.isPined)
            {
                node1.TempPosition -= subtract;
            }
            else
            {
                Vector3 halfSubstract = subtract / 2;
                node1.TempPosition -= halfSubstract;
                node2.TempPosition += halfSubstract;
            }
        }
        //collision
        foreach (TransformedSphereCollider sphereCollider in SphereColliders)
        {
            var sphereRadius = sphereCollider.Radius;
            float sphereRadiusSq = sphereCollider.RadiusSq;
            var sphereCenter = sphereCollider.Center;
            foreach (var item in strandNodes)
            {
                if (!item.IsActive)
                    continue;
                Vector3 distVector = item.TempPosition - sphereCenter;
                if (distVector.sqrMagnitude < sphereRadiusSq)
                {
                    item.TempPosition = sphereCenter + distVector.normalized * sphereRadius;
                }
            }
        }

        //apply
        for (int i = 0; i < strandNodes.Count; ++i)
        {
            HairStrandNode node = strandNodes[i];
            //position update
            if (node.IsActive)
                node.transform.position = node.TempPosition;
            //rotation update
            if (i + 1 < strandNodes.Count && !strandNodes[i + 1].isFolded) //hasNext
                node.transform.up = strandNodes[i + 1].TempPosition - node.TempPosition;
            else if (i - 1 >= 0 && !strandNodes[i - 1].isFolded) //hasPrev
                node.transform.rotation = strandNodes[i - 1].transform.rotation;
        }
    }
}
