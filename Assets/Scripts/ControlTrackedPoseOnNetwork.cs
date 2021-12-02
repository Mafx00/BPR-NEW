using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Interaction.Toolkit;

public class ControlTrackedPoseOnNetwork : MonoBehaviour
{
    public RealtimeView myPlayer;

    //stuff to disable from camera and main object
    public XRRig rig;
    public Camera camera;
    public AudioListener audioListener;
    public TrackedPoseDriver trackedPose;

    //stuff to disable from hands
    public XRController xRController;
    public XRRayInteractor rRayInteractor;
    public LineRenderer lineRenderer;
    public XRInteractorLineVisual interactorLineVisual;
    public GameObject watch;


    // Start is called before the first frame update
    void Start()
    {

		if (gameObject.name == "Main Camera" && myPlayer.isOwnedRemotelySelf)
		{
            rig.enabled = false;
            camera.enabled = false;
            Destroy(audioListener);
            trackedPose.enabled = false;
		}

        if (gameObject.name == "LeftHand Controller" && myPlayer.isOwnedRemotelySelf)
        {
            xRController.enabled = false;
            rRayInteractor.enabled = false;
            lineRenderer.enabled = false;
            interactorLineVisual.enabled = false;
            watch.SetActive(false);
        }

        if (gameObject.name == "RightHand Controller" && myPlayer.isOwnedRemotelySelf)
        {
            xRController.enabled = false;
            rRayInteractor.enabled = false;
            lineRenderer.enabled = false;
            interactorLineVisual.enabled = false;
        }

    }
}
