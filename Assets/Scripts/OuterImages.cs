using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class OuterImageDecorator : MapObjectBaseClass
{
    public override System.Type ControlledType => typeof(OuterImage);
    protected override MapObjectDecorator CreateObject(MapObject Data)
    {
        return CreateOuterImageObject(Data as OuterImage);
    }
    
    public async Task<OuterImage> CreateOuterImage()
    {
        string Path = await PrepareOuterImagePath();
        if (Path == null)
        {
            Debug.Log("Путь пуст");
            return null;
        }
        byte[] RawImage = await ReadImageFromDisk(Path);

        if (!new Texture2D(2,2).LoadImage(RawImage))
        {
            Debug.Log("Изображение не подходит");
            return null;
        }
        OuterImage image = new OuterImage();
        image.Data = RawImage;
        return image;
    }

    static async Task<string> PrepareOuterImagePath()
    {
        UserInput.Deactivate();
        string Path = string.Empty;
        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        eventSystem.gameObject.SetActive(false);
        SimpleFileBrowser.FileBrowser.ShowLoadDialog((string[] paths) => Path = paths[0], () => { }, SimpleFileBrowser.FileBrowser.PickMode.Files, false);
        while (SimpleFileBrowser.FileBrowser.IsOpen) await Task.Yield();
        eventSystem.gameObject.SetActive(true);
        UserInput.Activate();
        return Path;
    }

    static async Task<byte[]> ReadImageFromDisk(string Path)
    {
        byte[] RawImageData = null;
        try
        {
            using (var Reader = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                RawImageData = new byte[Reader.Length];
                await Reader.ReadAsync(RawImageData, 0, RawImageData.Length);
                Reader.Close();
                Reader.Dispose();
            }
        }
        catch(System.Exception ex)
        {
            Debug.Log("Не удалось загрузить изображение:" + ex.Message);
            return RawImageData;
        }
        return RawImageData;
    }

    static MapObjectDecorator CreateOuterImageObject(OuterImage ImageData)
    {
        Texture2D PreparedImage = new Texture2D(2, 2);
        if (!PreparedImage.LoadImage(ImageData.Data))
        {
            Debug.LogError("Не удалось считать изображение");
            return null;
        }
        PreparedImage.Apply();
        GameObject objectOnScene = new GameObject();
        objectOnScene.tag = "MapObject";
        objectOnScene.transform.SetParent(Map.MapCanvas);
        objectOnScene.AddComponent<RectTransform>();
        objectOnScene.AddComponent<RawImage>().texture = PreparedImage;
        MapObjectDecorator Decorator = new MapObjectDecorator(ImageData, objectOnScene);
        return Decorator;
    }

    public override void RefreshSceneView(MapObjectDecorator Image)
    {
        return;
    }

    public override void RefreshTransform(MapObjectDecorator RefreshedObject)
    {
        if (RefreshedObject.ObjectOnScene == null) return;
        OuterImage Data = RefreshedObject.DataReference as OuterImage;
        RectTransform Rect = RefreshedObject.ObjectOnScene.GetComponent<RectTransform>();
        Vector2 StartInWorld = MapScaler.GetPositionInWorld(Data.Start);
        Vector2 EndInWorld = MapScaler.GetPositionInWorld(Data.End);
        Rect.position = ((Vector3)(StartInWorld + EndInWorld) * 0.5f) + Vector3.forward *  Rect.position.z; 
        Rect.sizeDelta = new Vector2(Mathf.Abs(StartInWorld.x - EndInWorld.x), Mathf.Abs(StartInWorld.y - EndInWorld.y));
    }
}


[System.Serializable]
class OuterImageController : ControllerCategory
{
    public override System.Type ControlledType => typeof(OuterImage);
    [SerializeField] bool isMovingArea;
    public override void InitCustomLogic() { }

    public override void ApplyUserControl()
    {
        if (!isObjectExist()) return;
        if (base.IsPositionSaved) return;
        if (Input.GetMouseButtonDown(0))
        {
            isClickValid = !ParentController.IsMouseClickedEditor();
            return;
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (isMovingArea)
            {
                IsPositionSaved = true;
                isClickValid = true;
                isMovingArea = false;
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (!isClickValid) return;
            if (!isMovingArea)
            {
                (Decorator.DataReference as OuterImage).Start = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                (Decorator.DataReference as OuterImage).End = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                RefreshDecoratorTransform();
                isMovingArea = true;
                return;
            }
            else
            {
                (Decorator.DataReference as OuterImage).End = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                RefreshDecoratorTransform();
            }
        }
    }

    protected override void SavePickedObjectData()
    {
        return;
    }
    
    protected override async System.Threading.Tasks.Task<MapObject> CreateNewObject() 
    =>  await Map.ActualDecorator.outerimgs.CreateOuterImage();

    protected override void PickModifyAdditional()
    {
        isClickValid = true;
        isMovingArea = false;
    }
}