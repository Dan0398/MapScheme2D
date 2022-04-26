using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StraightLineDecorator : MapObjectBaseClass
{
    public override System.Type ControlledType => typeof(StraightLine);
    protected override MapObjectDecorator CreateObject(MapObject Obj)
    {
        StraightLine NewLine = Obj as StraightLine;
        GameObject LineObject = new GameObject();
        LineObject.tag = "MapObject";
        LineObject.layer = 5;
        LineObject.transform.SetParent(Map.MapCanvas);
        LineObject.AddComponent<RectTransform>().sizeDelta = Vector2.zero;
        LineObject.AddComponent<Image>();
        MapObjectDecorator Decorator = new MapObjectDecorator(NewLine, LineObject);
        return Decorator;
    }

    static void ApplyInputTypeStraightLine (StraightLine Line)
    {
        for (int i = 0; i < Map.Reference.Lines.LineType.Count; i++)
        {
            if (Map.Reference.Lines.LineType[i].LineName == Line.LineName)
            {
                Line.InputType = Map.Reference.Lines.LineType[i].InputType;
            }
        }
    }

    public override void RefreshSceneView(MapObjectDecorator LineDecorator)
    {
        if (LineDecorator.ObjectOnScene == null) return;
        StraightLine Data = LineDecorator.DataReference as StraightLine;
        LineType Info = GetStraightLineTypeByName(Data.LineName); 
        if (LineDecorator.ObjectOnScene.GetComponent<Image>() == null) LineDecorator.ObjectOnScene.AddComponent<Image>();
        var Img = LineDecorator.ObjectOnScene.GetComponent<Image>();
        if (Data.InputType != LineType.UserInputType.SimpleFixedAspect && Data.InputType != LineType.UserInputType.ThreePointedInput)
        {
            Sprite SpriteRef = Info.LineSprite;
            Texture2D LineTexture = new Texture2D(SpriteRef.texture.width, SpriteRef.texture.height);
            var Colors = SpriteRef.texture.GetPixels();
            Color MultiplyingColor = Map.Reference.Lines.LineColors[Data.ColorNumber];
            for (int i = 0; i < Colors.Length; i++)
            {
                Colors[i] *= MultiplyingColor;
            }
            LineTexture.SetPixels(Colors);
            LineTexture.Apply();
            Sprite NewSprite = Sprite.Create(LineTexture, new Rect(0, 0, LineTexture.width, LineTexture.height), Vector2.one * 0.5f, SpriteRef.pixelsPerUnit, 0, SpriteMeshType.FullRect, border: SpriteRef.border);
            Img.sprite = NewSprite;
        }
        else
        {
            Img.sprite = Info.LineSprite;
        }
        if (Data.InputType == LineType.UserInputType.Tiled)
        {
            Img.type = Image.Type.Tiled;
        }
        if (Data.InputType == LineType.UserInputType.Sliced)
        {
            Img.type = Image.Type.Sliced;
        }
        if (Data.InputType == LineType.UserInputType.SimpleFixedAspect || Data.InputType == LineType.UserInputType.ThreePointedInput)
        {
            Img.type = Image.Type.Simple;
        }
    }

    public override void RefreshTransform(MapObjectDecorator LineData)
    {
        if (LineData.ObjectOnScene == null) return;
        StraightLine Data = LineData.DataReference as StraightLine;
        Image Img = LineData.ObjectOnScene.GetComponent<Image>();
        RectTransform Rect = LineData.ObjectOnScene.GetComponent<RectTransform>();
        Vector2 StartOnScene = MapScaler.GetPositionInWorld(Data.Start);
        Vector2 EndOnScene= MapScaler.GetPositionInWorld(Data.End);
        if (Mathf.Approximately((EndOnScene - StartOnScene).magnitude,0))
        {
            Rect.sizeDelta = Vector2.zero;
            return;
        }
        if (Data.InputType == LineType.UserInputType.Tiled || Data.InputType == LineType.UserInputType.Sliced) 
        {
            Rect.position = ((Vector3)(StartOnScene + EndOnScene) * 0.5f) + Vector3.forward * Rect.position.z;
            Rect.sizeDelta = new Vector2((EndOnScene - StartOnScene).magnitude + Data.Width*0.5f, Data.Width);
            Img.pixelsPerUnitMultiplier = Img.sprite.texture.height / (float)Data.Width;
        }

        if (Data.InputType == LineType.UserInputType.SimpleFixedAspect) 
        {
            Rect.position = ((Vector3)(StartOnScene + EndOnScene) * 0.5f) + Vector3.forward * Rect.position.z;
            Img.preserveAspect = true;
            var ActiveImg = Img.sprite.texture;
            float AspectRatio = ActiveImg.height / (float)ActiveImg.width;
            float LineLeight = (EndOnScene- StartOnScene).magnitude;
            Rect.sizeDelta = new Vector2(LineLeight, LineLeight * AspectRatio);
        }

        if (Data.InputType == LineType.UserInputType.ThreePointedInput)
        {
            Vector2 BaseVector = EndOnScene - StartOnScene;
            Vector2 NormalVector = new Vector2(-BaseVector.y, BaseVector.x).normalized;
            Rect.position = ((Vector3)((EndOnScene + StartOnScene)*0.5f + Data.FlexibleHeight * NormalVector)*0.5f) + Vector3.forward * Rect.position.z;
            Rect.pivot = Vector2.zero;
            Rect.sizeDelta = new Vector2((Data.FlexibleHeight * NormalVector).magnitude, BaseVector.magnitude);
        }
        Rect.eulerAngles = Vector3.forward * Mathf.Acos((EndOnScene - StartOnScene).normalized.x) * Mathf.Rad2Deg;
        if (EndOnScene.y < StartOnScene.y) Rect.eulerAngles = -Rect.eulerAngles;
    }

    static LineType GetStraightLineTypeByName(string TypeName)
    {
        for (int i=0; i < Map.Reference.Lines.LineType.Count; i++)
        {
            if (Map.Reference.Lines.LineType[i].LineName == TypeName)
            {
                return Map.Reference.Lines.LineType[i];
            }
        }
        return null;
    }

}


[System.Serializable]
class StraightLinesController : ControllerCategory
{
    public override System.Type ControlledType => typeof(StraightLine);
    int ChosenColorInList = 0;
    int ChosenWidth = 1;
    string ChosenTypeName = "Default";
    bool IsMovingLine;
    bool isCreateHeightMode;
    [Space(40), SerializeField] Dropdown LineTypes;
    [SerializeField] Dropdown LineColors;
    [SerializeField] Button LowerWidth, HigherWidth;
    [SerializeField] Text LineWidthOnScreen;

    public override void InitCustomLogic()
    {
        CreateLineTypes();
        CreateLineColors();
        ApplyWidthButtonLogic();
    }
    
    protected override void SavePickedObjectData()
    {
        StraightLine CurrentLine = Decorator.DataReference as StraightLine;
        ChosenColorInList = CurrentLine.ColorNumber;
        ChosenWidth = CurrentLine.Width;
        ChosenTypeName = CurrentLine.LineName;
    }

    protected override void PickExistedAdditional()
    {
        IsMovingLine = false;
    }

    void CreateLineTypes()
    {
        List<Dropdown.OptionData> Options = new List<Dropdown.OptionData>();
        int KnowedValue = 0;
        foreach (var Type in Map.Reference.Lines.LineType)
        {
            if (Type.InputType == LineType.UserInputType.Tiled || Type.InputType == LineType.UserInputType.Sliced)
            {
                //красим спрайт
                Texture2D Texture = new Texture2D(Type.LineSprite.texture.width, Type.LineSprite.texture.height);
                Color color = Map.Reference.Lines.LineColors[ChosenColorInList];
                var Colors = Type.LineSprite.texture.GetPixels();
                for (int z =0; z < Colors.Length; z++)
                {
                    Colors[z] *= color;
                }
                Texture.SetPixels(Colors);
                Texture.Apply();
                Sprite OptionSprite = Sprite.Create(Texture, new Rect(0, 0, Texture.width, Texture.height), Vector2.one * 0.5f);
                Dropdown.OptionData option = new Dropdown.OptionData(OptionSprite);
                Options.Add(option);
            }
            else
            {
                Dropdown.OptionData option = new Dropdown.OptionData(Type.LineSprite);
                Options.Add(option);
            }
            if (ChosenTypeName == Type.LineName)
            {
                KnowedValue = Map.Reference.Lines.LineType.IndexOf(Type);
            }
        }
        LineTypes.options = Options;
        LineTypes.captionImage.sprite = LineTypes.options[0].image;
        LineTypes.onValueChanged.RemoveAllListeners();
        LineTypes.value = KnowedValue;
        LineTypes.onValueChanged.AddListener((int NewType) => 
            PickNewLineType(Map.Reference.Lines.LineType[NewType]));
    }

    void PickNewLineType(LineType NewTypeName)
    {
        ChosenTypeName = NewTypeName.LineName;
        if (!isObjectExist()) return;
        (Decorator.DataReference as StraightLine).LineName = NewTypeName.LineName;
        (Decorator.DataReference as StraightLine).InputType = NewTypeName.InputType;
        RefreshDecoratorView();
        RefreshDecoratorTransform();
    }

    void CreateLineColors()
    {
        List<Dropdown.OptionData> Options = new List<Dropdown.OptionData>();
        for (int i = 0; i < Map.Reference.Lines.LineColors.Count; i++)
        {
            Texture2D Texture = new Texture2D(1, 1);
            Texture.SetPixel(0, 0, Map.Reference.Lines.LineColors[i]);
            Texture.Apply();
            Sprite sprite = Sprite.Create(Texture, new Rect(0,0,1,1), Vector2.one * 0.5f);
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
        (Decorator.DataReference as StraightLine).ColorNumber = NewColorNumber;
        RefreshDecoratorView();
    }

    void ApplyWidthButtonLogic()
    {
        LineWidthOnScreen.text = ChosenWidth.ToString();
        LowerWidth.onClick.RemoveAllListeners();
        LowerWidth.onClick.AddListener(() => ChangeLineWidth(-1));
        HigherWidth.onClick.RemoveAllListeners();
        HigherWidth.onClick.AddListener(() => ChangeLineWidth(+1));
    }

    void ChangeLineWidth(int WidthShift)
    {
        ChosenWidth = Mathf.Clamp(ChosenWidth+WidthShift, 1, 50);
        LineWidthOnScreen.text = ChosenWidth.ToString();
        if (!isObjectExist()) return;
        (Decorator.DataReference as StraightLine).Width = ChosenWidth;
        RefreshDecoratorTransform();
    }

    protected override async System.Threading.Tasks.Task<MapObject> CreateNewObject()
    =>new StraightLine(ChosenWidth, ChosenColorInList, ChosenTypeName);

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
            if (IsMovingLine)
            {
                if ((Decorator.DataReference as StraightLine).InputType == 
                LineType.UserInputType.ThreePointedInput)
                {
                    isCreateHeightMode = true;
                }
                IsPositionSaved = true;
                base.LastMousePosition = Vector2.zero;
                (Decorator.DataReference as StraightLine).End = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                RefreshDecoratorTransform();
                IsMovingLine = false;
                if (isClickValid) 
                {
                    if (WorkMode == ControllerCategory.WorkModes.CreateNew)
                    {
                        PickApplyButton();
                    }
                }
                return;
            }
            isClickValid = false;
            if (isCreateHeightMode)
            {
                CalculateOutVector();
                RefreshDecoratorTransform();
                isCreateHeightMode = false;
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (!isClickValid) return;
            if (!IsMovingLine)
            {
                (Decorator.DataReference as StraightLine).Start = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
                LastMousePosition = Input.mousePosition;
                IsMovingLine = true;
                return;
            }
            if (LastMousePosition != (Vector2)Input.mousePosition)
            {
                (Decorator.DataReference as StraightLine).End = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());

                if ((Decorator.DataReference as StraightLine).InputType == LineType.UserInputType.ThreePointedInput)
                {
                    (Decorator.DataReference as StraightLine).FlexibleHeight = 
                    ((Decorator.DataReference as StraightLine).End - (Decorator.DataReference as StraightLine).Start).magnitude * 0.5f;
                }
                RefreshDecoratorTransform();
                LastMousePosition = Input.mousePosition;
            }
        }
        else
        {
            if (!isCreateHeightMode) return;
            CalculateOutVector();
        }
    }

    void CalculateOutVector()
    {
        StraightLine CurrentLine = (Decorator.DataReference as StraightLine);
        Vector3 X;
        Vector2 P = MapScaler.GetPositionForSaving(UserInput.GetMousePoint());
        if (Vector3.Dot(CurrentLine.Start - CurrentLine.End, P - CurrentLine.Start) > 0) X = CurrentLine.Start;
        else if (Vector3.Dot(CurrentLine.End - CurrentLine.Start, P - CurrentLine.End) > 0) X = CurrentLine.End;
        else X = CurrentLine.Start + (Vector2)Vector3.Project(P - CurrentLine.Start, CurrentLine.End - CurrentLine.Start);
        CurrentLine.FlexibleHeight = X.magnitude*0.5f;
    }

    protected override void PickApplyAdditional()
    {
        if (WorkMode == ControllerCategory.WorkModes.CreateNew)
        {
            PickAddButton();
        }
    }

    protected override async void PickAddAdditional()
    {
        if (WorkMode == ControllerCategory.WorkModes.CreateNew)
        {
            await System.Threading.Tasks.Task.Yield();
            await System.Threading.Tasks.Task.Yield();
            base.ApplyButton.SetActive(false);
        }
    }
}