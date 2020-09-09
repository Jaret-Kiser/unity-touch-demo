using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class TouchManager : MonoBehaviour {

    private const float MANIPULATION_SPEED = 7.5f;
    private const float SELECTION_TIME = 0.5f;
    private const float PINCH_TOLERANCE = 0.4f;

    private GameObject initialObject;
    private SelectedObject selectedObject;
    private Collider[] forceObjects;
    private float singleTouchTime;
    private float manipulationDistance;
    private Vector3 scaleChange;
    private Vector3 attractor;

    private enum ActionPairType {
        Pinch,
        Swipe,
        None
    }

    public Text creationObjectNameDisplay;
    public Camera canvasCamera;
    public Object creationObject;
    public OperationMode mode;
    public enum OperationMode {
        Manipulation,
        Creation,
        Destruction
    }

    public void ResetScene () {
        SceneManager.LoadScene (SceneManager.GetActiveScene ().name);
    }

    public void ChangeOperationMode (string newMode) {
        if (newMode == "Manipulation") {
            mode = OperationMode.Manipulation;
        } else if (newMode == "Creation") {
            mode = OperationMode.Creation;
        } else {
            mode = OperationMode.Destruction;
        }
    }

    public void SetCreationObject (Object o) {
        creationObject = o;
        creationObjectNameDisplay.text = o.ToString ();
    }

    private void Awake () {
        scaleChange = new Vector3 (0.01f, 0.01f, 0.01f);
        attractor = Vector3.positiveInfinity;
    }

    private void Update () {
        if (Input.touchCount > 0) {
            if (Input.touchCount == 1) {
                DoSingleTouch (Input.GetTouch (0));
            } else if (Input.touchCount == 2) {
                DoTwoTouches (Input.GetTouch (0), Input.GetTouch (1));
            } else {
                Touch[] touches = new Touch[3];
                for (int i = 0; i < 3; i++) {
                    touches[i] = Input.GetTouch (i);
                }
                DoThreeTouches (touches);
            }
        }
    }

    private void FixedUpdate () {
        MoveSelectedObjectTowardsAttractor ();
    }

    private void DoSingleTouch (Touch touch) {
        if (mode == OperationMode.Manipulation) {
            SingleTouchManipulationMode (touch);
        } else if (mode == OperationMode.Creation) {
            SingleTouchCreationMode (touch);
        } else {
            SingleTouchDestructionMode (touch);
        }
    }

    private void SingleTouchManipulationMode (Touch touch) {
        if (touch.phase == TouchPhase.Began) {
            singleTouchTime = 0;
            var ray = Camera.main.ScreenPointToRay (touch.position);
            RaycastHit hitInfo;
            if (Physics.Raycast (ray, out hitInfo)) {
                var objectHit = hitInfo.transform.gameObject;
                if (objectHit != null && objectHit.tag == "Interactable") {
                    selectedObject = new SelectedObject (objectHit, touch.fingerId);
                    manipulationDistance = Vector3.Distance (Camera.main.transform.position, selectedObject.selection.transform.position);
                }
            }
        } else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) {
            if (selectedObject != null) {
                if (singleTouchTime < SELECTION_TIME) {
                    var ray = Camera.main.ScreenPointToRay (touch.position);
                    RaycastHit hitInfo;
                    if (Physics.Raycast (ray, out hitInfo)) {
                        selectedObject.rigidbody.AddForceAtPosition (ray.direction * 10f, hitInfo.point, ForceMode.VelocityChange);
                    }
                } else {
                    selectedObject.rigidbody.useGravity = true;
                    selectedObject.material.SetColor ("_Color", selectedObject.defaultColor);
                    selectedObject = null;
                    attractor = Vector3.positiveInfinity;
                }
            }
        } else { //TouchPhase.Moved && TouchPhase.Stationary
            singleTouchTime += touch.deltaTime;
            if (selectedObject != null) {
                if (singleTouchTime > SELECTION_TIME) {
                    if (touch.fingerId == selectedObject.touchID) {
                        UpdateAttractorWithTouch (touch);
                    }
                } else {

                }
            }
        }
    }

    private void SingleTouchCreationMode (Touch touch) {
        if (touch.phase == TouchPhase.Began) {
            if (creationObject != null) {
                var ray = canvasCamera.ScreenPointToRay (touch.position);
                RaycastHit hitInfo;
                GameObject hitObject = null;
                if (Physics.Raycast (ray, out hitInfo)) {
                    if (hitInfo.transform.gameObject != null && hitInfo.transform.gameObject.tag == "Interactable") {
                        hitObject = hitInfo.transform.gameObject;
                    }
                }
                if (hitObject == null) {
                    Vector3 creationPoint = Camera.main.ScreenPointToRay (touch.position).GetPoint (5f);
                    Object.Instantiate (creationObject, creationPoint, Quaternion.identity);
                }
            }
        }
    }

    private void SingleTouchDestructionMode (Touch touch) {
        if (touch.phase == TouchPhase.Began) {
            var ray = Camera.main.ScreenPointToRay (touch.position);
            RaycastHit hitInfo;
            if (Physics.Raycast (ray, out hitInfo)) {
                var objectHit = hitInfo.transform.gameObject;
                if (objectHit != null && objectHit.tag == "Interactable") {
                    Object.Destroy (objectHit);
                }
            }
        }
    }

    private void DoTwoTouches (Touch touchOne, Touch touchTwo) {
        if (selectedObject == null) {
            if ((touchOne.phase == TouchPhase.Ended || touchOne.phase == TouchPhase.Ended) && forceObjects.Length > 0) {
                foreach (Collider collider in forceObjects) {
                    Rigidbody rig = collider.attachedRigidbody;
                    if (rig != null) {
                        Transform objectTransform = rig.GetComponent<Transform> ();
                        var ray = new Ray (Camera.main.transform.position, objectTransform.position - Camera.main.transform.position);
                        RaycastHit hitInfo;
                        if (Physics.Raycast (ray, out hitInfo)) {
                            rig.AddForceAtPosition (ray.direction * 10f, hitInfo.point, ForceMode.VelocityChange);
                        }
                    }
                }
            } else {
                forceObjects = FindAllObjectsWithin2DBounds (touchOne.position, touchTwo.position);
            }

        } else {
            if (touchOne.fingerId == selectedObject.touchID) {
                UpdateAttractorWithTouch (touchOne);
                ChangeManipulationDistanceBasedOnTouch (touchTwo);
            } else {
                UpdateAttractorWithTouch (touchTwo);
                ChangeManipulationDistanceBasedOnTouch (touchOne);
            }
        }
    }

    private Collider[] FindAllObjectsWithin2DBounds (Vector2 screenPointOne, Vector2 screenPointTwo) {
        Vector2 midpoint = new Vector2 ((screenPointOne.x + screenPointTwo.x) / 2f, (screenPointOne.y + screenPointTwo.y) / 2f);
        Ray aimLine = Camera.main.ScreenPointToRay (midpoint);
        Vector3 endpoint = aimLine.GetPoint (15f);
        Vector3 startpoint = Camera.main.transform.position;
        float radius = Vector2.Distance (Camera.main.ScreenToViewportPoint (screenPointOne), Camera.main.ScreenToViewportPoint (screenPointTwo)) / 2;
        return Physics.OverlapCapsule (endpoint, startpoint, radius);
    }

    private void DoThreeTouches (Touch[] touches) {
        if (selectedObject != null) {
            Touch activeTouch;
            Touch actionTouchOne;
            Touch actionTouchTwo;

            if (touches[0].fingerId == selectedObject.touchID) {
                activeTouch = touches[0];
                actionTouchOne = touches[1];
                actionTouchTwo = touches[2];
            } else if (touches[1].fingerId == selectedObject.touchID) {
                activeTouch = touches[1];
                actionTouchOne = touches[0];
                actionTouchTwo = touches[2];
            } else {
                activeTouch = touches[2];
                actionTouchOne = touches[0];
                actionTouchTwo = touches[1];
            }
            UpdateAttractorWithTouch (activeTouch);

            ActionPairType type = DetermineActionType (actionTouchOne, actionTouchTwo);

            if (type == ActionPairType.Pinch) {
                ResizeObjectByPinch (actionTouchOne, actionTouchTwo);
            }

        }
    }

    private void UpdateAttractorWithTouch (Touch touch) {
        selectedObject.material.SetColor ("_Color", Color.blue);
        selectedObject.rigidbody.useGravity = false;
        attractor = Camera.main.ScreenPointToRay (touch.position).GetPoint (manipulationDistance);
    }

    private void MoveSelectedObjectTowardsAttractor () {

        if (selectedObject != null && attractor.ToString () != Vector3.positiveInfinity.ToString ()) {
            selectedObject.rigidbody.velocity = (attractor - selectedObject.rigidbody.position) * Mathf.Max (Vector3.Distance (attractor, selectedObject.rigidbody.position), 1f) * MANIPULATION_SPEED;
        }
    }

    private void ChangeManipulationDistanceBasedOnTouch (Touch touch) {
        manipulationDistance += Camera.main.ScreenToViewportPoint (touch.deltaPosition).y * 4f;
    }

    private void ResizeObjectByPinch (Touch touchOne, Touch touchTwo) {
        float currentDistance = Vector2.Distance (touchOne.position, touchTwo.position);
        float previousDistance = Vector2.Distance (touchOne.position - touchOne.deltaPosition, touchTwo.position - touchTwo.deltaPosition);
        if (currentDistance < previousDistance) {
            if (selectedObject.selection.transform.localScale.x > 0.2f) {
                selectedObject.selection.transform.localScale -= scaleChange;
            }
        } else {
            if (selectedObject.selection.transform.localScale.x < 1.0f) {
                selectedObject.selection.transform.localScale += scaleChange;
            }
        }
    }
    private ActionPairType DetermineActionType (Touch touchOne, Touch touchTwo) {
        float direction = Vector2.Dot (touchOne.deltaPosition.normalized, touchTwo.deltaPosition.normalized);
        if (direction < -1f + PINCH_TOLERANCE) {
            return ActionPairType.Pinch;
        } else if (direction > 1f + PINCH_TOLERANCE) {
            return ActionPairType.Swipe;
        } else {
            return ActionPairType.None;
        }

    }

    private void OnGUI () {
        if (Input.touchCount > 0) {
            for (int i = 0; i < Input.touchCount; i++) {
                Touch touch = Input.GetTouch (i);
                GUI.Box (new Rect (touch.position.x, Screen.height - touch.position.y, 30, 30), touch.fingerId.ToString ());
            }
        }
    }

    private class SelectedObject {
        public GameObject selection;
        public Material material;
        public Rigidbody rigidbody;
        public Color defaultColor;
        public int touchID;

        public SelectedObject (GameObject target, int touchStartID) {
            selection = target;
            rigidbody = selection.GetComponent<Rigidbody> ();
            material = selection.GetComponent<MeshRenderer> ().material;
            defaultColor = material.color;
            touchID = touchStartID;
        }
    }
}