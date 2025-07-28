using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomizationDataStore
{
    public static string LatestDataJson = null;

    public static void Clear() => LatestDataJson = null;
}
