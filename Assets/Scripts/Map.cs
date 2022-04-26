using System.Collections.Generic;
using UnityEngine;

public static class Map
{
    public static System.Action onSheetChanged;
    public static ReferenceList Reference;
    public static DataContainer MapData;
    public static MapSheetDecorator ActualDecorator;
    public static int ActualSheet;

    public static RectTransform MapCanvas;
    
    public static MapIO IOSystem;
    public static MapScreenshot Screenshoter;
    
    static Map()
    {
        IOSystem = new MapIO();
        Screenshoter = new MapScreenshot();
        ActualDecorator = new MapSheetDecorator(null);
        GetReference();
    }

    static void GetReference()
    {
        Reference = Resources.Load("ListOfReferences") as ReferenceList;
        MapCanvas = GameObject.Find("MapCanvas").GetComponent<RectTransform>();
    }

    public static bool IsMapExist()
    {
        return (MapData!= null && MapData.MapSheets != null && MapData.MapSheets.Count > 0);
    }

    #region Работа с картами
    public static void CreateNewMap()
    {
        if (IsMapExist())
        {
            ClearMapSheetObjects();
        }
        MapData = new DataContainer();
        ChangeSheet(0);
    }

    #endregion

    #region Работа с листами
    static void CreateNewSheet()
    {
        if (MapData == null) MapData = new DataContainer();
        if (MapData.MapSheets == null) MapData.MapSheets = new List<MapSheet>();
        MapData.MapSheets.Add(new MapSheet());
    }

    public static void ChangeSheet(int NewSheetNumber)
    {
        ClearMapSheetObjects();
        if (MapData.MapSheets == null || MapData.MapSheets.Count == 0) 
        {
            CreateNewSheet();
        }
        while (NewSheetNumber >= MapData.MapSheets.Count)
        {
            CreateNewSheet();
        }
        ActualSheet = NewSheetNumber;
        CreateActualSheetObjects();
        onSheetChanged?.Invoke();
    }

    public static void DeleteSheet(int DeletingSheet)
    {
        if (MapData.MapSheets.Count <= 1) return;
        if (MapData.MapSheets.Count-1 < DeletingSheet) return;
        MapData.MapSheets.RemoveAt(DeletingSheet);
        if (ActualSheet >= DeletingSheet) 
        {
            ActualSheet--;
            if (ActualSheet == (DeletingSheet-1))
            {
                ChangeSheet(ActualSheet);
            }
        }
    }
    #endregion


    #region Работа с объектами листа
    static void ClearMapSheetObjects()
    {
        if (ActualDecorator == null) return;
        ActualDecorator.ClearDecoratorObjectsFromScene();
    }

    static void CreateActualSheetObjects()
    {
        ActualDecorator = new MapSheetDecorator(Map.MapData.MapSheets[ActualSheet]);
        ActualDecorator.InitializeOldObjects();
    }

    //Получить тип объекта
    public static MapObjectDecorator GetContainerOfObject(GameObject PickedObject)
    {
        foreach (var Container in ActualDecorator.ObjectsInList)
        {
            if (Container.ObjectOnScene == PickedObject)
            {
                return Container;
            }
        }
        return null;
    }
    #endregion
}