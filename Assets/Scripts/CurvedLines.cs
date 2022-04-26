using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CurvedLineDecorator : MapObjectBaseClass
{
    public override System.Type ControlledType => typeof(CurvedLine);

    protected override MapObjectDecorator CreateObject (MapObject DataObject)
    {
        CurvedLine CurvedLineData = (CurvedLine)DataObject;
        GameObject CurvedObject = new GameObject();
        CurvedObject.layer = 5;
        CurvedObject.tag = "MapObject";
        CurvedObject.transform.SetParent(Map.MapCanvas);
        CurvedObject.AddComponent<RectTransform>().localPosition = Vector3.back * 1;
        LineRenderer Line = CurvedObject.AddComponent<LineRenderer>();
        Line.material = new Material(Map.Reference.CurvedLines.DefaultMaterial);
        Line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Line.receiveShadows = false;
        CurvedObject.AddComponent<EdgeCollider2D>();
        MapObjectDecorator Decorator  = new MapObjectDecorator(CurvedLineData, CurvedObject);
        return Decorator;
    }

    public override void RefreshSceneView(MapObjectDecorator Line)
    {
        if (Line.ObjectOnScene == null) return;
        CurvedLine Data = Line.DataReference as CurvedLine;
        LineRenderer renderer = Line.ObjectOnScene.GetComponent<LineRenderer>();
        renderer.alignment = LineAlignment.TransformZ;
        if (renderer.material != null)
        {
            renderer.material.color = Map.Reference.Lines.LineColors[Data.ColorNumber];
        }
        renderer.widthMultiplier = Data.Width;
        renderer.useWorldSpace = false;
        renderer.startColor = Color.white;
        renderer.endColor = Color.white;
    }

    public override void RefreshTransform(MapObjectDecorator RefreshedObject)
    {
        if (RefreshedObject.ObjectOnScene == null) return;
        CurvedLine Data = RefreshedObject.DataReference as CurvedLine;
        if (Data.Points == null || Data.Points.Count ==0) return;
        Vector3[] Points = new Vector3[Data.Points.Count];
        for (int i=0; i<Points.Length; i++)
        {
            Points[i] = MapScaler.GetPositionInWorld(Data.Points[i]);
        }
        Vector2[] PointsIn2d = new Vector2[Points.Length];
        for (int i=0; i<PointsIn2d.Length; i++)
        {
            PointsIn2d[i] =  Points[i];
        }

        LineRenderer renderer = RefreshedObject.ObjectOnScene.GetComponent<LineRenderer>();
        renderer.positionCount = Data.Points.Count;
        renderer.SetPositions(Points);

        var Collider = RefreshedObject.ObjectOnScene.GetComponent<EdgeCollider2D>();
        Collider.edgeRadius = Data.Width * 0.5f;
        Collider.points = PointsIn2d;
    }
}


[System.Serializable]
class CurvedLinesControlled : ControllerCategory
{
    public override System.Type ControlledType => typeof(CurvedLine);
    float ChosenWidth = 3;
    int ChosenColorInList;
    int ControllingNumber;
    [SerializeField] Dropdown LineColors;
    [SerializeField] Button LowerWidth, HigherWidth;
    [SerializeField] Text LineWidthOnScreen;
    List<GameObject> Markers;
    [SerializeField] GameObject MarkerReference;

    public override void InitCustomLogic()
    {
        CreateLineColors();
        ApplyWidthButtonLogic();
    }

    protected override void SavePickedObjectData()
    {
        CurvedLine CurrentLine = (CurvedLine)Decorator.DataReference;
        ChosenColorInList = CurrentLine.ColorNumber;
        ChosenWidth = CurrentLine.Width;
    }

    protected override void PickExistedAdditional()
    {
        TurnOnEditing();
    }

    void CreateLineColors()
    {
        List<Dropdown.OptionData> Options = new List<Dropdown.OptionData>();
        for (int i = 0; i < Map.Reference.Lines.LineColors.Count; i++)
        {
            Texture2D Texture = new Texture2D(1, 1);
            Texture.SetPixel(0, 0, Map.Reference.Lines.LineColors[i]);
            Texture.Apply();
            Sprite sprite = Sprite.Create(Texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            Dropdown.OptionData option = new Dropdown.OptionData(sprite);
            Options.Add(option);
        }
        LineColors.options = Options;
        LineColors.onValueChanged.RemoveAllListeners();
        LineColors.value = ChosenColorInList;
        LineColors.onValueChanged.AddListener((int NewType) => PickNewLineColor(NewType));
    }

    void PickNewLineColor(int NewColorNumber)
    {
        ChosenColorInList = NewColorNumber;
        if (!isObjectExist()) return;
        (Decorator.DataReference as CurvedLine).ColorNumber = NewColorNumber;
        RefreshDecoratorView();
    }

    void ApplyWidthButtonLogic()
    {
        LineWidthOnScreen.text = ChosenWidth.ToString();
        LowerWidth.onClick.RemoveAllListeners();
        LowerWidth.onClick.AddListener(() => ChangeLineWidth(-0.5f));
        HigherWidth.onClick.RemoveAllListeners();
        HigherWidth.onClick.AddListener(() => ChangeLineWidth(+0.5f));
    }

    void ChangeLineWidth(float WidthShift)
    {
        ChosenWidth = Mathf.Clamp(ChosenWidth + WidthShift, 1, 50);
        LineWidthOnScreen.text = ChosenWidth.ToString();
        if (!isObjectExist()) return;
        (Decorator.DataReference as CurvedLine).Width = ChosenWidth;
        RefreshDecoratorView();
    }
    
    public override void ApplyUserControl()
    {
        if (!isObjectExist())  return;
        MakeMarkersFollow();
        if (IsPositionSaved)   return;
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            isClickValid = !ParentController.IsMouseClickedEditor();
            if (!isClickValid) return;
            if (Input.GetMouseButtonDown(0))
            {
                ControllingNumber = PickedMarkerNumber(ParentController.GetCanvasRaycastResults(ParentController.MapCanvas.GetComponent<Canvas>()));
                if (ControllingNumber < 0)
                {
                    (Decorator.DataReference as CurvedLine).Points.Add(MapScaler.GetPositionForSaving(UserInput.GetMousePoint()));
                    AddMarkerInList();
                    ControllingNumber = (Decorator.DataReference as CurvedLine).Points.Count - 1;
                    RefreshDecoratorTransform();
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                ControllingNumber = PickedMarkerNumber(ParentController.GetCanvasRaycastResults(ParentController.MapCanvas.GetComponent<Canvas>()));
                if (ControllingNumber >= 0)
                {
                    (Decorator.DataReference as CurvedLine).Points.RemoveAt(ControllingNumber);
                    GameObject.Destroy(Markers[ControllingNumber]);
                    Markers.RemoveAt(ControllingNumber);
                    RefreshDecoratorTransform();
                    ControllingNumber = -1;
                    return;
                }
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (isClickValid)
            {
                (Decorator.DataReference as CurvedLine).Points[ControllingNumber] = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());// ParentController.GetMousePosOnCanvas();
                RefreshDecoratorTransform();
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            isClickValid = false;
        }
    }

    int PickedMarkerNumber(List<RaycastResult> results)
    {
        if (Markers!= null && Markers.Count > 0)
        {
            for (int i=0; i < results.Count; i++)
            {
                for (int z=0; z< Markers.Count; z++)
                {
                    if (Markers[z] == results[i].gameObject)
                    {
                        return z;
                    }
                }
            }
        }
        return -1;
    }

    void AddMarkerInList()
    {
        if (Markers == null || Markers.Count == 0) Markers = new List<GameObject>();
        GameObject MarkerObj = GameObject.Instantiate(MarkerReference);
        MarkerObj.transform.SetParent(ParentController.MapCanvas);
        Markers.Add(MarkerObj);
    }

    void MakeMarkersFollow()
    {
        if (Markers == null || Markers.Count == 0) return;
        if ((Decorator.DataReference as CurvedLine).Points == null || (Decorator.DataReference as CurvedLine).Points.Count == 0 
        || (Decorator.DataReference as CurvedLine).Points.Count != Markers.Count) return;
        for (int i=0; i < Markers.Count; i++)
        {
            Markers[i].transform.position = MapScaler.GetPositionInWorld((Decorator.DataReference as CurvedLine).Points[i]);
        }
    }

    void TurnOffEditing()
    {
        if (Markers != null && Markers.Count > 0)
        {
            for (int i = Markers.Count - 1; i >= 0; i--)
            {
                GameObject.Destroy(Markers[i]);
            }
            Markers = null;
        }
    }

    void TurnOnEditing()
    {
        if ((Decorator.DataReference as CurvedLine).Points == null 
        || (Decorator.DataReference as CurvedLine).Points.Count == 0) return;
        for (int i=0; i < (Decorator.DataReference as CurvedLine).Points.Count; i++)
        {
            AddMarkerInList();
        }
        MakeMarkersFollow();
    }

    protected override async System.Threading.Tasks.Task<MapObject> CreateNewObject() 
    => new CurvedLine(ChosenWidth, ChosenColorInList);

    protected override void PickApplyAdditional()
    {
        TurnOffEditing();
    }

    protected override void PickDeleteAdditoinal()
    {
        TurnOffEditing();
    }

    protected override void PickModifyAdditional()
    {
        if (IsPositionSaved) 
            TurnOnEditing();
        else 
            TurnOffEditing(); 
    }
}