using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] color, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(color);
        texture.Apply();
        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] noise)
    {
        int width = noise.GetLength(0);
        int height = noise.GetLength(1);


        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, noise[x, y]);
            }
        }

        return TextureFromColorMap(colorMap, width, height);
    }
}
