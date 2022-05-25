using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxPosition : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        var game = UnityEntry.game;
        if (game == null) return;

        var h = game.mapScene.GetHero();
        if (h == null) return;

        Vector3 pos = Vector3.zero;
        pos.x = -(h.x + h.offsetX);
        pos.z = h.z + h.offsetZ;
        this.transform.position = pos;
    }
}
