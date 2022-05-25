using System;
using UnityEngine;
using Yukar.Common;

public class SGBActivator : MonoBehaviour {
    public UnityEntry entry;
    // Whether the current map name starts with the character string specified here.
    public string[] activeMapNameList;
    // When checking this, it becomes effective when it does not apply to the above.
    public bool invertFlag;
    // If this is checked, it will be judged by forward match
    public bool forwardMatch;

    private Guid currentMapGuid = Guid.Empty;

	// Use this for initialization
	void Start ()
    {
        gameObject.getChildren(true).ForEach(x => x.SetActive(false));

        if (entry == null)
            gameObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {
        if(UnityEntry.game.mapScene != null && UnityEntry.game.mapScene.map != null && UnityEntry.game.mapScene.map.guId != currentMapGuid)
            changeMap();
	}

    private void changeMap()
    {
        var name = UnityEntry.game.mapScene.map.name;
        currentMapGuid = UnityEntry.game.mapScene.map.guId;

        bool matched = false;
        foreach (var item in activeMapNameList)
        {
            if(forwardMatch && name.StartsWith(item))
            {
                matched = true;
                break;
            }
            else if(!forwardMatch && name == item)
            {
                matched = true;
                break;
            }
        }
        
        if(invertFlag)
        	matched = !matched;

        gameObject.getChildren(true).ForEach(x => x.SetActive(matched));
    }
}
