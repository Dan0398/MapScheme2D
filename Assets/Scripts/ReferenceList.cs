using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ListOfReferences", menuName = "Config/List Of References", order = 0)]
[System.Serializable]
public class ReferenceList : ScriptableObject
{
    public List<MarkCategory> MarksCategories;
    public StraightLineTypes Lines;
    public CurvedLineTypes CurvedLines;
    public Areas Areas;
    public OuterImageCategory outerImages;
}

[System.Serializable]
public class MarkCategory
{
    public string CategoryName;
    public Sprite CategoryIcon;
    public List<MarkType> MarkTypes;
}

[System.Serializable]
public class MarkType
{
    public string TypeName;
    public Sprite SpriteReference;
    public Vector2 DefaultSize;
}

[System.Serializable]
public class StraightLineTypes
{
    public Sprite ButtonIcon;
    public List<Color> LineColors;
    public List<LineType> LineType;
}

[System.Serializable]
public class LineType
{
    public enum UserInputType
    {
        Tiled,
        Sliced,
        SimpleFixedAspect,
        ThreePointedInput
    }
    public string LineName;
    public UserInputType InputType;
    public Sprite LineSprite;
}

[System.Serializable]
public class CurvedLineTypes
{
    public Sprite ButtonIcon;
    public Material DefaultMaterial;
}

[System.Serializable]
public class Areas
{
    public Sprite ButtonIcon;
    public List<AreaType> AreaTypes;
}

[System.Serializable]
public class AreaType
{
    public string TypeName;
    public Sprite AreaSprite;
}

[System.Serializable]
public class OuterImageCategory
{
    public Sprite ButtonIcon;
}
