using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public bool useFalloff;
    public bool useFlatShading;

    public float uniformScale = 5f;
    public float falloffA;
    public float falloffB;
    public float meshHeightMultiplier;

    public AnimationCurve meshHeightCurve;
    public float minHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }
    public float maxHeight 
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }
}
