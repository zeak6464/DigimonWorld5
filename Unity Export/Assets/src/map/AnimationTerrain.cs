using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AnimationTerrain_ChipVariations
{
    public Vector2 TextureSize = Vector2.zero;
    public uint []ColorData = null;
    public Vector2 []UvList = null;
}

public class AnimationTerrain : MonoBehaviour
{
    int MapTextureHeight = 0;

    [SerializeField]
    public AnimationTerrain_ChipVariations[] ChipVariations = null;
    
    float mElapsedTime = 0;
    int mCurrentUvIndex = 0;
    Texture2D mTexture = null;

    // Use this for initialization
    void Start()
    {
        var mesh = this.GetComponent<MeshRenderer>();
        var material = mesh.material;
        this.mTexture = material.mainTexture as Texture2D;

        this.MapTextureHeight = this.mTexture.height;
    }

    // Update is called once per frame
    void Update()
    {
        //更新するかを確認
        const float interval = 0.33f;
        this.mElapsedTime += Time.deltaTime;
        if (this.mElapsedTime <= interval) return;
        this.mCurrentUvIndex++;
        this.mElapsedTime -= interval;

        //表示の更新
        foreach (var chip in ChipVariations)
        {
            int acount = (int)chip.TextureSize.x / 48;
            if (acount == 1) continue;

            var animIndex = this.mCurrentUvIndex % acount;
            int h = (int)chip.TextureSize.y / 48;
            for (int c = 0; c < h; c++)
            {

                this.storeSubPixel2D((int)chip.UvList[c].x,
                    MapTextureHeight - (int)chip.UvList[c].y - 48, 48, 48, chip.ColorData,
                    48 * animIndex, (h - 1 - c) * 48,
                    (int)chip.TextureSize.x);
            }
        }
    }


    void storeSubPixel2D(int x, int y, int w, int h, uint[] pix, int sx, int sy, int swidth)
    {
        var colors = new UnityEngine.Color32[w * h];

        for (int _y = 0; _y < h; _y++)
        {
            for (int _x = 0; _x < w; _x++)
            {
                var color = pix[(_y + sy) * swidth + (sx + _x)];
                byte r = (byte)((color >> 24) & 0xFF);
                byte g = (byte)((color >> 16) & 0xFF);
                byte b = (byte)((color >> 8) & 0xFF);
                byte a = (byte)((color >> 0) & 0xFF);
                if (a == 0)
                    colors[_y * w + _x] = new UnityEngine.Color32(0, 0, 0, 0);
                else
                    colors[_y * w + _x] = new UnityEngine.Color32(r, g, b, a);
            }
        }

        this.mTexture.SetPixels32(x, y, w, h, colors);
        this.mTexture.Apply();
    }


}
