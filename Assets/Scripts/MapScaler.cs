using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MapScaler
{
    public static System.Action OnUpdated;
    static Material GridMaterial;
    static float sheetScale;
    public static float SheetScale
    {
        get => sheetScale;
        set
        {
            sheetScale = value;
            OnUpdated?.Invoke();
        }
    }
    static Vector2 worldOffset;
    public static Vector2 WorldOffset
    {
        get => worldOffset;
        set 
        {
            worldOffset = value;
            OnUpdated?.Invoke();
        }
    }

    public static float GetCellSize()
    {
        return 5*SheetScale;
    }

    static float ScaleFactor() => 10;

    public static Vector2 GetPositionForSaving(Vector2 PosInWorld)
    {
        return (PosInWorld- WorldOffset)/GetCellSize() ;
    }

    public static Vector2 GetPositionInWorld(Vector2 SavedPos)
    {
        return (SavedPos)*GetCellSize() + WorldOffset;
    }

    static MapScaler()
    {
        GridMaterial = GameObject.Find("MapCanvas").transform.GetChild(0).GetComponent<RawImage>().material;
        OnUpdated += ChangeGridPos;
        OnUpdated += ChangeGridScale;
        Map.onSheetChanged += GetSheetScale;
    }

    static void ChangeGridScale()
    {
        GridMaterial.SetFloat("_CellSize", GetCellSize());
    }

    static void ChangeGridPos()
    {
        GridMaterial.SetVector("_CenterOffset", WorldOffset); 
    }

    static void GetSheetScale()
    {
        SheetScale = Map.MapData.MapSheets[Map.ActualSheet].Scale;
    }
}