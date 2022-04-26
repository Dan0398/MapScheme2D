using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using Encoder = System.Text.Encoding;


[System.Serializable]
public class DataContainer
{
    public List<MapSheet> MapSheets;
}

[System.Serializable]
public class MapSheet
{
    public float Scale;
    public List<MapObject> Objects;

    public MapSheet()
    {
        Scale = 10;
        Objects = new List<MapObject>();
    }
}

[System.Serializable]
public class MapSheetDecorator
{
    MapSheet DataReference;
    public List<MapObjectDecorator> ObjectsInList;
    public MarkDecorator Marks;
    public StraightLineDecorator StraightLines;
    public CurvedLineDecorator CurvedLines;
    public AreasDecorator Areas;
    public OuterImageDecorator Images;
    public MapObjectBaseClass[] Creators => new MapObjectBaseClass[]
    {Marks, StraightLines, Areas, CurvedLines, Images};

    public MapSheetDecorator(MapSheet SheetData)
    {
        DataReference = SheetData;
        ObjectsInList = new List<MapObjectDecorator>();
        Marks = new MarkDecorator();
        StraightLines = new StraightLineDecorator();
        Areas = new AreasDecorator();
        CurvedLines= new CurvedLineDecorator();
        Images = new OuterImageDecorator();
    }

    public MapObjectDecorator CreateObjWithDecorator(MapObject NewObj)
    {
        DataReference.Objects.Add(NewObj);
        MapObjectDecorator Decorator = null;
        foreach (var DecorateController in Creators)
        {
            if (NewObj.GetType() == DecorateController.ControlledType)
            {
                Decorator = DecorateController.CreateDecorator(NewObj);
                MapScaler.OnUpdated += ()=> {DecorateController.RefreshTransform(Decorator);};
                ObjectsInList.Add(Decorator);
                break;
            }
        }
        return Decorator;
    }

    public void InitializeOldObjects()
    {
        for (int i=0; i<DataReference.Objects.Count;i++)
        {
            foreach (var DecorateController in Creators)
            {
                if (DataReference.Objects[i].GetType() == DecorateController.ControlledType)
                {
                    var Decorator = DecorateController.CreateDecorator(DataReference.Objects[i]);
                    MapScaler.OnUpdated += ()=> {DecorateController.RefreshTransform(Decorator);};
                    DecorateController.RefreshSceneView(Decorator);
                    ObjectsInList.Add(Decorator);
                    break;
                }
            }
        }
    }

    public void RefreshView(MapObjectDecorator PickedObject)
    {
        System.Type ObjType = PickedObject.DataReference.GetType();
        foreach (var a in Creators)
        {
            if (ObjType == a.ControlledType)
            {
                a.RefreshSceneView(PickedObject);
                return;
            }
        }
    }

    public void RefreshTransform(MapObjectDecorator PickedObject)
    {
        System.Type ObjType = PickedObject.DataReference.GetType();
        foreach (var a in Creators)
        {
            if (ObjType == a.ControlledType)
            {
                a.RefreshTransform(PickedObject);
                return;
            }
        }
    }

    public void DeleteObjectByDecorator(MapObjectDecorator ObjectUnderDelete)
    {
        DataReference.Objects.Remove(ObjectUnderDelete.DataReference);
        DeleteDecoratorFromScene(ObjectUnderDelete);
    }

    public void ClearDecoratorObjectsFromScene()
    {
        if (ObjectsInList != null && ObjectsInList.Count >0)
        {
            for(int i=ObjectsInList.Count-1; i>=0; i--)
            {
                DeleteDecoratorFromScene(ObjectsInList[i]);
            }
        }
        ObjectsInList = new List<MapObjectDecorator>();
    }

    void DeleteDecoratorFromScene(MapObjectDecorator ObjectUnderDelete)
    {
        foreach (var DecorateController in Creators)
        {
            if (ObjectUnderDelete.GetType() == DecorateController.ControlledType)
            {
                MapScaler.OnUpdated -= ()=> {DecorateController.RefreshTransform(ObjectUnderDelete);};
                break;
            }
        }
        ObjectsInList.Remove(ObjectUnderDelete);
        GameObject.Destroy(ObjectUnderDelete.ObjectOnScene);
    }

    public MapObjectDecorator GetDecoratorByPicked(GameObject PickedObject)
    {
        for (int i=0; i<ObjectsInList.Count;i++)
        {
            if (ObjectsInList[i].ObjectOnScene == PickedObject)
            {
                return ObjectsInList[i];
            }
        }
        return null;
    }
}

[System.Serializable]
public abstract class MapObject
{
    public abstract Task<MapObject> ReadFromBytes(byte[] Data);

    public abstract byte[] WriteToByte();
}

[System.Serializable]
public class MapObjectDecorator
{
    public MapObject DataReference;
    public GameObject ObjectOnScene;

    public MapObjectDecorator(MapObject Ref, GameObject obj)
    {
        DataReference = Ref;
        ObjectOnScene = obj;
    }
}

[System.Serializable]
public class Mark: MapObject
{
    public string CategoryName;
    public string TypeName;
    public int RotationQuarter;
    public float ScaleOnCanvas;
    public Vector2 PosOnCanvas;

    public Mark(string catName, string type, float Scale)
    {
        CategoryName = catName;
        TypeName = type;
        RotationQuarter = 0;
        ScaleOnCanvas = Scale;
        PosOnCanvas = Vector2.zero;
    }

    public Mark(){}

    public override byte[] WriteToByte()
    {
        byte[] CategoryNameRaw = Encoder.UTF8.GetBytes(CategoryName);
        byte[] CategoryNameLeight = System.BitConverter.GetBytes(CategoryNameRaw.Length);
        byte[] TypeNameRaw = Encoder.UTF8.GetBytes(TypeName);
        byte[] TypeNameLeight = System.BitConverter.GetBytes(TypeNameRaw.Length);
        byte[] RotationQuarterRaw = System.BitConverter.GetBytes(RotationQuarter);
        byte[] ScaleOnCanvasRaw = System.BitConverter.GetBytes(ScaleOnCanvas);
        byte[] PosX = System.BitConverter.GetBytes(PosOnCanvas.x);
        byte[] PosY = System.BitConverter.GetBytes(PosOnCanvas.y);
        List<byte> Result = new List<byte>();
        Result.AddRange(CategoryNameLeight);
        Result.AddRange(CategoryNameRaw);
        Result.AddRange(TypeNameLeight);
        Result.AddRange(TypeNameRaw);
        Result.AddRange(RotationQuarterRaw);
        Result.AddRange(ScaleOnCanvasRaw);
        Result.AddRange(PosX);
        Result.AddRange(PosY);
        return Result.ToArray();
    }

    public override async Task<MapObject> ReadFromBytes(byte[] Data)
    {
        string PreparedCategoryName = string.Empty;
        string PreparedTypeName = string.Empty;
        int PreparedRotationQuarter = 0;
        float PreparedScaleOnCanvas = 0;
        Vector3 PreparedPosOnCanvas = Vector3.zero;
        using (var Splitter = new MemoryStream(Data))
        {
            byte[] CategoryNameLeight = new byte[4];
            await Splitter.ReadAsync(CategoryNameLeight, 0, CategoryNameLeight.Length);
            byte[] CategoryNameRaw = new byte[System.BitConverter.ToInt32(CategoryNameLeight,0)];
            await Splitter.ReadAsync(CategoryNameRaw, 0, CategoryNameRaw.Length);
            PreparedCategoryName = Encoder.UTF8.GetString(CategoryNameRaw);

            byte[] TypeNameLeight = new byte[4];
            await Splitter.ReadAsync(TypeNameLeight, 0, TypeNameLeight.Length);
            byte[] TypeNameRaw = new byte[System.BitConverter.ToInt32(TypeNameLeight,0)];
            await Splitter.ReadAsync(TypeNameRaw, 0, TypeNameRaw.Length);
            PreparedTypeName = Encoder.UTF8.GetString(TypeNameRaw);

            byte[] RotationQuarterRaw = new byte[4];
            await Splitter.ReadAsync(RotationQuarterRaw, 0, RotationQuarterRaw.Length);
            PreparedRotationQuarter = System.BitConverter.ToInt32(RotationQuarterRaw,0);

            byte[] ScaleOnCanvasRaw = new byte[4];
            await Splitter.ReadAsync(ScaleOnCanvasRaw, 0, ScaleOnCanvasRaw.Length);
            PreparedScaleOnCanvas = System.BitConverter.ToSingle(ScaleOnCanvasRaw,0);

            byte[] PosX = new byte[4];
            await Splitter.ReadAsync(PosX, 0, PosX.Length);
            byte[] PosY = new byte[4];
            await Splitter.ReadAsync(PosY, 0, PosY.Length);
            PreparedPosOnCanvas = new Vector2(System.BitConverter.ToSingle(PosX,0),System.BitConverter.ToSingle(PosY,0));
            Splitter.Close();
            Splitter.Dispose();
        }
        Mark NewMark = new Mark
        {
            CategoryName = PreparedCategoryName,
            TypeName = PreparedTypeName,
            ScaleOnCanvas = PreparedScaleOnCanvas,
            PosOnCanvas = PreparedPosOnCanvas,
            RotationQuarter = PreparedRotationQuarter
        };
        return NewMark;
    }
}

[System.Serializable]
public class StraightLine: MapObject
{
    public string LineName;
    public int ColorNumber;
    public LineType.UserInputType InputType;
    public int Width;
    public float FlexibleHeight;
    public Vector2 Start;
    public Vector2 End;

    public StraightLine(){}

    public StraightLine(int NewWidth, int NewColorNumber, string NewTypeName)
    {
        this.LineName = NewTypeName;
        this.ColorNumber = NewColorNumber;

        this.Width = NewWidth;
    }

    public override byte[] WriteToByte()
    {
        byte[] LineNameRaw = Encoder.UTF8.GetBytes(LineName);
        byte[] LineNameLeight = System.BitConverter.GetBytes(LineNameRaw.Length);
        byte[] ColorNumberRaw = System.BitConverter.GetBytes(ColorNumber);
        byte[] InputTypeRaw = System.BitConverter.GetBytes((int)InputType);
        byte[] WidthRaw = System.BitConverter.GetBytes(Width);
        byte[] FlexibleHeightRaw = System.BitConverter.GetBytes(FlexibleHeight);
        byte[] StartX = System.BitConverter.GetBytes(Start.x);
        byte[] StartY = System.BitConverter.GetBytes(Start.y);
        byte[] EndX = System.BitConverter.GetBytes(End.x);
        byte[] EndY = System.BitConverter.GetBytes(End.y);
        List<byte> Result = new List<byte>();
        Result.AddRange(LineNameLeight);
        Result.AddRange(LineNameRaw);
        Result.AddRange(ColorNumberRaw);
        Result.AddRange(InputTypeRaw);
        Result.AddRange(WidthRaw);
        Result.AddRange(FlexibleHeightRaw);
        Result.AddRange(StartX);
        Result.AddRange(StartY);
        Result.AddRange(EndX);
        Result.AddRange(EndY);
        return Result.ToArray();
    }

    public override async Task<MapObject> ReadFromBytes(byte[] Data)
    {
        string PreparedLineName = string.Empty;
        int PreparedColorNumber = 0;
        LineType.UserInputType PreparedInputType = (LineType.UserInputType)0;
        int PreparedWidth = 0;
        float PreparedFlexibleHeight = 0;
        Vector2 PreparedStart = Vector2.zero;
        Vector2 PreparedEnd = Vector2.zero;
        using (var Splitter = new MemoryStream(Data))
        {
            byte[] LineNameLeight = new byte[4];
            await Splitter.ReadAsync(LineNameLeight, 0, LineNameLeight.Length);
            byte[] LineNameRaw = new byte[System.BitConverter.ToInt32(LineNameLeight,0)];
            await Splitter.ReadAsync(LineNameRaw, 0, LineNameRaw.Length);
            PreparedLineName = Encoder.UTF8.GetString(LineNameRaw);

            byte[] ColorNumberRaw = new byte[4];
            await Splitter.ReadAsync(ColorNumberRaw, 0, ColorNumberRaw.Length);
            PreparedColorNumber = System.BitConverter.ToInt32(ColorNumberRaw,0);

            byte[] InputTypeRaw = new byte[4];
            await Splitter.ReadAsync(InputTypeRaw, 0, InputTypeRaw.Length);
            PreparedInputType = (LineType.UserInputType) System.BitConverter.ToInt32(InputTypeRaw,0);

            byte[] WidthRaw = new byte[4];
            await Splitter.ReadAsync(WidthRaw, 0, WidthRaw.Length);
            PreparedWidth = System.BitConverter.ToInt32(WidthRaw,0);

            byte[] FlexibleHeightRaw = new byte[4];
            await Splitter.ReadAsync(FlexibleHeightRaw, 0, FlexibleHeightRaw.Length);
            PreparedFlexibleHeight = System.BitConverter.ToSingle(FlexibleHeightRaw,0);

            byte[] StartX = new byte[4];
            await Splitter.ReadAsync(StartX, 0, StartX.Length);
            byte[] StartY = new byte[4];
            await Splitter.ReadAsync(StartY, 0, StartY.Length);
            PreparedStart = new Vector2(System.BitConverter.ToSingle(StartX,0), System.BitConverter.ToSingle(StartY,0));

            byte[] EndX = new byte[4];
            await Splitter.ReadAsync(EndX, 0, EndX.Length);
            byte[] EndY = new byte[4];
            await Splitter.ReadAsync(EndY, 0, EndY.Length);
            PreparedEnd = new Vector2(System.BitConverter.ToSingle(EndX,0), System.BitConverter.ToSingle(EndY,0));
            Splitter.Close();
            Splitter.Dispose();
        }
        StraightLine NewLine = new StraightLine
        {
            Width = PreparedWidth,
            ColorNumber = PreparedColorNumber,
            InputType = PreparedInputType,
            FlexibleHeight = PreparedFlexibleHeight,
            LineName = PreparedLineName,
            Start = PreparedStart,
            End = PreparedEnd
        };
        return NewLine;
    }
}

[System.Serializable]
public class CurvedLine: MapObject
{
    public int ColorNumber;
    public float Width;
    public List<Vector3> Points;

    public CurvedLine(float width, int colorNumber)
    {
        ColorNumber = colorNumber;
        Width = width;
        Points = new List<Vector3>();
    }

    public CurvedLine() {}

    public override byte[] WriteToByte()
    {
        byte[] ColorNumberRaw = System.BitConverter.GetBytes(ColorNumber);
        byte[] WidthRaw = System.BitConverter.GetBytes(Width);
        byte[] PointsLeight = System.BitConverter.GetBytes(Points.Count);
        List<byte> Result = new List<byte>();
        Result.AddRange(ColorNumberRaw);
        Result.AddRange(WidthRaw);
        Result.AddRange(PointsLeight);
        for (int i=0; i<Points.Count; i++)
        {
            Result.AddRange(System.BitConverter.GetBytes(Points[i].x));
            Result.AddRange(System.BitConverter.GetBytes(Points[i].y));
        }
        return Result.ToArray();
    }

    public override async Task<MapObject> ReadFromBytes(byte[] Data)
    {
        int PreparedColorNumber = 0;
        float PreparedWidth = 0;
        List<Vector3> PreparedPoints = new List<Vector3>();
        using (var Splitter = new MemoryStream(Data))
        {
            byte[] ColorNumberRaw = new byte[4];
            await Splitter.ReadAsync(ColorNumberRaw, 0,ColorNumberRaw.Length);
            PreparedColorNumber = System.BitConverter.ToInt32(ColorNumberRaw,0);
            byte[] WidthRaw = new byte[4];
            await Splitter.ReadAsync(WidthRaw, 0,WidthRaw.Length);
            PreparedWidth = System.BitConverter.ToSingle(WidthRaw,0);
            byte[] PointsLeightRaw = new byte[4];
            await Splitter.ReadAsync(PointsLeightRaw, 0,PointsLeightRaw.Length);
            int PointsLeight = System.BitConverter.ToInt32(PointsLeightRaw,0);
            for (int i=0; i<PointsLeight; i++)
            {
                byte[] PointXRaw = new byte[4];
                await Splitter.ReadAsync(PointXRaw,0,4);
                byte[] PointYRaw = new byte[4];
                await Splitter.ReadAsync(PointYRaw,0,4);
                PreparedPoints.Add(new Vector3(System.BitConverter.ToSingle(PointXRaw,0), System.BitConverter.ToSingle(PointYRaw,0)));
            }
            Splitter.Close();
            Splitter.Dispose();
        }
        CurvedLine Result = new CurvedLine()
        {
            Width = PreparedWidth,
            ColorNumber = PreparedColorNumber,
            Points = PreparedPoints
        };
        return Result;
    }
}

[System.Serializable]
public class Area: MapObject
{
    public string TypeName;
    public Vector2 Start;
    public Vector2 End;

    public Area(string type)
    {
        TypeName = type;
    }

    public Area(){}

    public override byte[] WriteToByte()
    {
        byte[] TypeNameRaw = Encoder.UTF8.GetBytes(TypeName);
        byte[] TypeNameLeight = System.BitConverter.GetBytes(TypeNameRaw.Length);
        byte[] StartX = System.BitConverter.GetBytes(Start.x);
        byte[] StartY = System.BitConverter.GetBytes(Start.y);
        byte[] EndX = System.BitConverter.GetBytes(End.x);
        byte[] EndY = System.BitConverter.GetBytes(End.y);
        List<byte> Result = new List<byte>();
        Result.AddRange(TypeNameLeight);
        Result.AddRange(TypeNameRaw);
        Result.AddRange(StartX);
        Result.AddRange(StartY);
        Result.AddRange(EndX);
        Result.AddRange(EndY);
        return Result.ToArray();
    }

    public override async Task<MapObject> ReadFromBytes(byte[] Data)
    {
        string PreparedTypeName = string.Empty;
        Vector2 PreparedStart = Vector2.zero;
        Vector2 PreparedEnd = Vector2.zero;
        using (var Splitter = new MemoryStream(Data))
        {
            byte[] TypeNameLeight = new byte[4];
            await Splitter.ReadAsync(TypeNameLeight, 0, TypeNameLeight.Length);
            byte[] TypeNameRaw = new byte[System.BitConverter.ToInt32(TypeNameLeight,0)];
            await Splitter.ReadAsync(TypeNameRaw, 0, TypeNameRaw.Length);
            PreparedTypeName = Encoder.UTF8.GetString(TypeNameRaw);

            byte[] StartX = new byte[4];
            await Splitter.ReadAsync(StartX, 0, StartX.Length);
            byte[] StartY = new byte[4];
            await Splitter.ReadAsync(StartY, 0, StartY.Length);
            PreparedStart = new Vector2(System.BitConverter.ToSingle(StartX,0), System.BitConverter.ToSingle(StartY,0));
            
            byte[] EndX = new byte[4];
            await Splitter.ReadAsync(EndX, 0, EndX.Length);
            byte[] EndY = new byte[4];
            await Splitter.ReadAsync(EndY, 0, EndY.Length);
            PreparedEnd = new Vector2(System.BitConverter.ToSingle(EndX, 0), System.BitConverter.ToSingle(EndY, 0));
            Splitter.Close();
            Splitter.Dispose();
        }
        Area Result = new Area()
        {
            TypeName =  PreparedTypeName, 
            Start = PreparedStart, 
            End = PreparedEnd
        };
        return Result;
    }
}

[System.Serializable]
public class OuterImage : MapObject
{
    public byte[] Data;
    public Vector2 Start;
    public Vector2 End;

    public override byte[] WriteToByte()
    {
        byte[] StartX = System.BitConverter.GetBytes(Start.x);
        byte[] StartY = System.BitConverter.GetBytes(Start.y);
        byte[] EndX = System.BitConverter.GetBytes(End.x);
        byte[] EndY = System.BitConverter.GetBytes(End.y);
        byte[] DataLeight = System.BitConverter.GetBytes(Data.Length);
        List<byte> Result = new List<byte>();
        Result.AddRange(StartX);
        Result.AddRange(StartY);
        Result.AddRange(EndX);
        Result.AddRange(EndY);
        Result.AddRange(DataLeight);
        Result.AddRange(Data);
        return Result.ToArray();
    }

    public override async Task<MapObject> ReadFromBytes(byte[] Data)
    {
        Vector2 PreparedStart = Vector2.zero;
        Vector2 PreparedEnd = Vector2.zero;
        byte[] PreparedData = new byte[0];
        using (var Splitter = new MemoryStream(Data))
        {
            byte[] StartX = new byte[4];
            await Splitter.ReadAsync(StartX, 0, StartX.Length);
            byte[] StartY = new byte[4];
            await Splitter.ReadAsync(StartY, 0, StartY.Length);
            PreparedStart = new Vector2(System.BitConverter.ToSingle(StartX,0), System.BitConverter.ToSingle(StartY,0));
            
            byte[] EndX = new byte[4];
            await Splitter.ReadAsync(EndX, 0, EndX.Length);
            byte[] EndY = new byte[4];
            await Splitter.ReadAsync(EndY, 0, EndY.Length);
            PreparedEnd = new Vector2(System.BitConverter.ToSingle(EndX, 0), System.BitConverter.ToSingle(EndY, 0));
            byte[] DataLeight = new byte[4];
            await Splitter.ReadAsync(DataLeight, 0, DataLeight.Length);

            PreparedData = new byte[System.BitConverter.ToInt32(DataLeight,0)];
            await Splitter.ReadAsync(PreparedData, 0, PreparedData.Length);
            Splitter.Close();
            Splitter.Dispose();
        }
        OuterImage Result = new OuterImage()
        {
            Start = PreparedStart,
            End = PreparedEnd,
            Data = PreparedData
        };
        return Result;
    }
}