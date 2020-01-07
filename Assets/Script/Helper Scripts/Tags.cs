using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tags : MonoBehaviour
{
    //Tags
    public static string wall = "Wall";
    public static string fruit = "Fruit";
    public static string bomb = "Bomb";
    public static string tail = "Tail";
}
//Metrics
public class Metrics
{
    public static float node = 0.2f;
}
//Player Direction
public enum PlayerDirection
{
    LEFT = 0,
    UP = 1,
    RIGHT = 2,
    DOWN = 3,
    COUNT = 4
}

