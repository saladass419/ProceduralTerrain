using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnStart : MonoBehaviour
{
    private void Start()
    {
        gameObject.SetActive(false);
    }
}
