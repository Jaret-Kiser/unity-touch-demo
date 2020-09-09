using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModeDropdown : MonoBehaviour {

    public TouchManager manager;
    private Dropdown dropdown;

    // Start is called before the first frame update
    void Start () {
        dropdown = gameObject.GetComponent<Dropdown> ();
        dropdown.onValueChanged.AddListener (delegate {
            DropdownValueChanged (dropdown);
        });
    }

    // Update is called once per frame
    void Update () {

    }

    void DropdownValueChanged (Dropdown change) {
        manager.ChangeOperationMode (change.options[change.value].text);
    }
}