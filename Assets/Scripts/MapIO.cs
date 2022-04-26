using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

public class MapIO
{
    const string EXTENTION = ".hch";
    string LastUsedPath;
    
    public bool IsLastPathKnown()
    {
        return !string.IsNullOrEmpty(LastUsedPath);
    }

    public async Task<bool> TrySaveAsNewMap()
    {
        if (await TryGetNewPath())
        {
            return await TrySaveToKnownPath();
        }
        return false;
    }

    async Task<bool> TryGetNewPath()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        eventSystem.gameObject.SetActive(false);
        SimpleFileBrowser.FileBrowser.ShowSaveDialog((string[] path) => { Debug.Log(path[0]); LastUsedPath = path[0] + EXTENTION; }, () => { }, SimpleFileBrowser.FileBrowser.PickMode.Files, false, LastUsedPath);
        while (SimpleFileBrowser.FileBrowser.IsOpen) await Task.Yield();
        eventSystem.gameObject.SetActive(true);
        return SimpleFileBrowser.FileBrowser.Success;
    }

    public async Task<bool> TrySaveToKnownPath()
    {
        if (string.IsNullOrEmpty(LastUsedPath)) return false;
        byte[] MapData = SaveMapToBytes();
        byte[] Result = null;
        List<byte> PreResult = new List<byte>();
        PreResult.AddRange(MapData);
        Result = PreResult.ToArray();
        using (FileStream file = new FileStream(LastUsedPath, FileMode.Create, FileAccess.Write, FileShare.Write))
        {
            await file.WriteAsync(Result, 0, Result.Length);
            file.Close();
            file.Dispose();
        }
        return true;
    }

    byte[] SaveMapToBytes()
    {
        List<byte> Result = new List<byte>();
        byte[] SheetsCount = System.BitConverter.GetBytes(Map.MapData.MapSheets.Count);
        Result.AddRange(SheetsCount);
        List<byte> MapData = new List<byte>();
        foreach (MapSheet Sheet in Map.MapData.MapSheets)
        {
            List<byte> SheetResult = new List<byte>();
            byte[] ScaleRaw = System.BitConverter.GetBytes(Sheet.Scale);
            SheetResult.AddRange(ScaleRaw);
            byte[] ObjectsCount = System.BitConverter.GetBytes(Sheet.Objects.Count);
            SheetResult.AddRange(ObjectsCount);
            foreach(var Object in Sheet.Objects)
            {
                byte[] ObjectDataRaw = Object.WriteToByte();
                byte[] ObjectDataLeight = System.BitConverter.GetBytes(ObjectDataRaw.Length);
                SheetResult.AddRange(ObjectDataLeight);
                SheetResult.Add(GetByteByObjectType(Object.GetType()));
                SheetResult.AddRange(ObjectDataRaw);
            }
            MapData.AddRange(SheetResult);
        }
        Result.AddRange(MapData);
        return Result.ToArray();
    }

    byte GetByteByObjectType(System.Type ObjectType)
    {
        for(byte i=0; i< Map.ActualDecorator.Creators.Length; i++)
        {
            if (ObjectType == Map.ActualDecorator.Creators[i].ControlledType)
            {
                return i;
            }
        }
        return 0;
    }

    public async Task<bool> TryLoadMap()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        eventSystem.gameObject.SetActive(false);
        SimpleFileBrowser.FileBrowser.ShowLoadDialog((string[] path) => LastUsedPath = path[0], () => { }, SimpleFileBrowser.FileBrowser.PickMode.Files, false, LastUsedPath);
        while (SimpleFileBrowser.FileBrowser.IsOpen) await Task.Yield();
        eventSystem.gameObject.SetActive(true);
        if (!SimpleFileBrowser.FileBrowser.Success) return false;
        byte[] LoadedFile;
        using (FileStream file = File.Open(LastUsedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            LoadedFile = new byte[file.Length];
            await file.ReadAsync(LoadedFile, 0, (int)file.Length);
            file.Close();
            file.Dispose();
        }
        Map.MapData = await LoadMapFromBytes(LoadedFile);
        return true;
    }

    async Task<DataContainer> LoadMapFromBytes(byte[] MapRawData)
    {
        DataContainer ResultContainer = new DataContainer();
        List<MapSheet> Sheets = new List<MapSheet>();
        using (var Splitter = new System.IO.MemoryStream(MapRawData))
        {
            byte[] SheetsCountRaw = new byte[4];
            await Splitter.ReadAsync(SheetsCountRaw,0,4);
            int SheetsCount = System.BitConverter.ToInt32(SheetsCountRaw,0);
            for (int i=0; i<SheetsCount; i++)
            {
                MapSheet NewSheet = new MapSheet();
                byte[] SheetScaleRaw = new byte[4];
                await Splitter.ReadAsync(SheetScaleRaw,0,4);
                NewSheet.Scale = System.BitConverter.ToSingle(SheetScaleRaw,0);
                byte[] ObjectsCountRaw = new byte[4];
                await Splitter.ReadAsync(ObjectsCountRaw,0,4);
                int ObjectsCount = System.BitConverter.ToInt32(ObjectsCountRaw,0);
                for (int z = 0; z < ObjectsCount; z++)
                {
                    byte[] ObjectDataLeightRaw = new byte[4];
                    await Splitter.ReadAsync(ObjectDataLeightRaw,0,4);
                    int ObjectDataLeight = System.BitConverter.ToInt32(ObjectDataLeightRaw,0);
                    byte[] ObjectTypeRaw = new byte[1];
                    await Splitter.ReadAsync(ObjectTypeRaw,0,1);
                    byte[] ObjectData = new byte[ObjectDataLeight];
                    await Splitter.ReadAsync(ObjectData,0, ObjectData.Length);
                    System.Type ObjectType = GetObjectTypeByByte(ObjectTypeRaw[0]);
                    MapObject NewObject = null;
                    NewObject = (MapObject) System.Activator.CreateInstance(ObjectType);
                    NewObject = await NewObject.ReadFromBytes(ObjectData); 
                    NewSheet.Objects.Add(NewObject);
                }
                Sheets.Add(NewSheet);
            }
            ResultContainer.MapSheets = Sheets;
            Splitter.Close();
            Splitter.Dispose();
        }
        return ResultContainer;
    }

    System.Type GetObjectTypeByByte(byte TypeNumber)
    {
        return Map.ActualDecorator.Creators[TypeNumber].ControlledType;
    }
}