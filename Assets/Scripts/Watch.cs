using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Watch : MonoBehaviour
{
    public Renderer watch;
    public GameObject buttons;

    public GameObject watchRayTarget;

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;

        //check if the watch is facing the target
        if (Physics.Raycast(this.transform.position, transform.forward, out hit) && hit.transform.gameObject.name == watchRayTarget.name)
        {
            //activate the watch if the the watch is facing the target
            if (hit.transform.gameObject.name == watchRayTarget.name)
            {
                watch.enabled = true;
                buttons.SetActive(true);
            }
        }
        else
        {
            watch.enabled = false;
            buttons.SetActive(false);
        }
    }

    public void LoadSceneLake()
    {
        SceneManager.LoadScene("Lake");
    }
    public void LoadSceneTea()
    {
        SceneManager.LoadScene("Tea");
    }
    public void LoadSceneMenu()
    {
        SceneManager.LoadScene("Menu");
    }
}