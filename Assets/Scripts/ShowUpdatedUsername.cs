using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Normal.Realtime;

public class ShowUpdatedUsername : MonoBehaviour
{
    public Text nameTag;
    public string usernameOnline;
    public RealtimeView thisPlayer;

    // Start is called before the first frame update
    void Start()
    {
        nameTag.text = "Jeff";
        thisPlayer = GetComponentInParent<RealtimeView>();
        thisPlayer.name = usernameOnline;
    }

    // Update is called once per frame
    void Update()
    {
        

		if (thisPlayer.name != nameTag.text)
		{
            nameTag.text = thisPlayer.name;
        }

        
    }
}
