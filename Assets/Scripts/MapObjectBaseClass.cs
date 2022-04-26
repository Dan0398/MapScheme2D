public abstract class MapObjectBaseClass
{
    public System.Action OnDestroyed;
    public abstract System.Type ControlledType {get;}

    public MapObjectDecorator CreateDecorator(MapObject Data)
    {
        MapObjectDecorator Decorator = CreateObject(Data);
        RefreshSceneView(Decorator);
        RefreshTransform(Decorator);
        ApplyZPosition(Decorator);
        return Decorator;
    }

    protected abstract MapObjectDecorator CreateObject(MapObject DataObject);

    public abstract void RefreshSceneView(MapObjectDecorator RefreshedObject);

    public abstract void RefreshTransform(MapObjectDecorator RefreshedObject);

    protected void ApplyZPosition(MapObjectDecorator RefreshedObject)
    {
        var Rect = RefreshedObject.ObjectOnScene.GetComponent<UnityEngine.RectTransform>();
        Rect.position = new UnityEngine.Vector3(Rect.position.x, Rect.position.y, Map.ActualDecorator.ObjectsInList.Count * (-1));
    }
}