using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObject : MonoBehaviour {
    public AudioSource collisionEffect;
    private Rigidbody rigidbody;
    private Vector3 defaultScale;
    // Start is called before the first frame update
    private void Start () {
        defaultScale = gameObject.transform.localScale;
        rigidbody = gameObject.GetComponent<Rigidbody> ();
    }

    private void FixedUpdate () {
        rigidbody.mass = gameObject.transform.localScale.magnitude / defaultScale.magnitude;
    }

    private void OnCollisionEnter (Collision collision) {
        collisionEffect.Play ();
    }
}