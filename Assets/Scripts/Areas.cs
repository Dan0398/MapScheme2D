using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AreasDecorator: MapObjectBaseClass
{
    public override System.Type ControlledType => typeof(Area);

    protected override MapObjectDecorator CreateObject(MapObject Obj)
    {
        Area area = Obj as Area;
        GameObject ObjectOnScene = new GameObject();
        ObjectOnScene.tag = "MapObject";
        ObjectOnScene.layer = 5;
        ObjectOnScene.transform.SetParent(Map.MapCanvas);
        ObjectOnScene.AddComponent<RectTransform>().sizeDelta = Vector2.zero;
        ObjectOnScene.AddComponent<Image>();
        MapObjectDecorator Decorator  = new MapObjectDecorator(area, ObjectOnScene);
        return Decorator;
    }

    public override void RefreshSceneView(MapObjectDecorator area)
    {
        if (area.ObjectOnScene == null) return;
        area.ObjectOnScene.GetComponent<Image>().sprite = GetAreaType((area.DataReference as Area).TypeName).AreaSprite;
        area.ObjectOnScene.GetComponent<Image>().type = Image.Type.Tiled;
    }

    public override void RefreshTransform(MapObjectDecorator AreaObject)
    {
        if (AreaObject.ObjectOnScene == null) return;
        Area Data = AreaObject.DataReference as Area;
        RectTransform Rect = AreaObject.ObjectOnScene.GetComponent<RectTransform>();
        Vector2 StartInWorld = MapScaler.GetPositionInWorld(Data.Start);
        Vector2 EndInWorld = MapScaler.GetPositionInWorld(Data.End);
        Rect.position = ((Vector3)(StartInWorld + EndInWorld) * 0.5f) + Vector3.forward * Rect.position.z;
        Rect.sizeDelta = new Vector2(Mathf.Abs(StartInWorld.x - EndInWorld.x), Mathf.Abs(StartInWorld.y - EndInWorld.y));
    }

    static AreaType GetAreaType(string AreaTypeName)
    {
        for (int i=0; i < Map.Reference.Areas.AreaTypes.Count; i++)
        {
            if (Map.Reference.Areas.AreaTypes[i].TypeName == AreaTypeName)
            {
                return Map.Reference.Areas.AreaTypes[i];
            }
        }
        return null;
    }
}


    [System.Serializable]
    class AreasController : ControllerCategory
    {
        public override System.Type ControlledType => typeof(Area);
        [SerializeField] Dropdown AreasTypesDropdown;
        string CurrentAreaTypeName = "Smoke";
        bool IsMovingArea;


        public override void InitCustomLogic()
        {
            CreateAreaTypes();
        }

        protected override void SavePickedObjectData()
        {
             CurrentAreaTypeName = (Decorator.DataReference as Area).TypeName;
        }

        void CreateAreaTypes()
        {
            List<Dropdown.OptionData> Options = new List<Dropdown.OptionData>();
            int KnowedValue = 0;
            for (int i=0; i < Map.Reference.Areas.AreaTypes.Count; i++)
            {
                Dropdown.OptionData optionData = new Dropdown.OptionData(Map.Reference.Areas.AreaTypes[i].AreaSprite);
                if (CurrentAreaTypeName == Map.Reference.Areas.AreaTypes[i].TypeName)
                {
                    KnowedValue = i;
                }
                Options.Add(optionData);
            }
            AreasTypesDropdown.options = Options;
            AreasTypesDropdown.onValueChanged.RemoveAllListeners();
            AreasTypesDropdown.value = KnowedValue;
            AreasTypesDropdown.onValueChanged.AddListener((int PickedValue) => 
            PickNewType(Map.Reference.Areas.AreaTypes[PickedValue].TypeName));
        }

        void PickNewType(string NewTypeName)
        {
            if (CurrentAreaTypeName == NewTypeName) return;
            CurrentAreaTypeName = NewTypeName;
            if (!isObjectExist()) return;
            (Decorator.DataReference as Area).TypeName = NewTypeName;
            RefreshDecoratorView();
        }

        protected override async System.Threading.Tasks.Task<MapObject> CreateNewObject() => new Area(CurrentAreaTypeName);

        public override void ApplyUserControl()
        {
            if (IsPositionSaved) return;
            if (!isObjectExist()) return;
            if (Input.GetMouseButtonDown(0))
            {
                isClickValid = !ParentController.IsMouseClickedEditor();
                return;
            }
            if (Input.GetMouseButtonUp(0))
            {
                isClickValid = false;
                if (IsMovingArea)
                {
                    LastMousePosition = Vector2.zero;
                    (Decorator.DataReference as Area).End = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                    RefreshDecoratorTransform();
                    IsMovingArea = false;
                    base.IsPositionSaved = true;
                    return;
                }
            }
            if (Input.GetMouseButton(0))
            {
                if (!isClickValid) return;
                if (!IsMovingArea)
                {
                    (Decorator.DataReference as Area).Start = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                    RefreshDecoratorTransform();
                    LastMousePosition = Input.mousePosition;
                    IsMovingArea = true;
                    return;
                }
                if (LastMousePosition != (Vector2)Input.mousePosition)
                {
                    (Decorator.DataReference as Area).End = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                    RefreshDecoratorTransform();
                    LastMousePosition = Input.mousePosition;
                }
            }
        }
    }

