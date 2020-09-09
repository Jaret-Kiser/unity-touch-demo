using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonScript : MonoBehaviour {

    public GameObject replicationObject;

    private Transform buttonObject;
    // Start is called before the first frame update
    void Start () {
        buttonObject = gameObject.GetComponentsInChildren<Transform> () [1];
    }

    private void FixedUpdate () {
        buttonObject.Rotate (0.5f, 0.5f, 0.5f, Space.Self);
    }
}