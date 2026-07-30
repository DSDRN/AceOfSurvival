using System.Collections.Generic;
using UnityEngine;

public class DangerMarker : MonoBehaviour
{
    public static readonly List<DangerMarker> Active = new List<DangerMarker>();

    private void OnEnable() { Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }
}
