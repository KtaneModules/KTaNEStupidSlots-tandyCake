using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FontInfo
{
    public Font font;
    public Material mat;
    public Vector3 position;
    public Vector3 scale;

    public FontInfo(Font fnt, Material mtl, Vector3 pos, Vector3 scl)
    {
        font = fnt;
        mat = mtl;
        position = pos;
        scale = scl;
    }
}
