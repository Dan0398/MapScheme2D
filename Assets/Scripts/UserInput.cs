using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserInput : MonoBehaviour
{
    public static UserInput Instance;
    [SerializeField] RectTransform MouseArrow;
    Vector2 RoundedUserInput;
    float LocalCellSize;
    Camera Cam;
    Vector2 WorldOffsetSaved;
    Vector3 LastMousePosition;
    bool isActive;
    bool isMoving = true;
    bool isUseRound;
    float ZoomScale=1;

    public static bool IsUsingRound()=> Instance.isUseRound;

    public static Vector2 GetMousePoint()
    {
        if (Instance.isUseRound)
        {
            return Instance.RoundedUserInput;
        }
        else 
        {
            if (Instance.Cam == null) Instance.Cam = Camera.main;
            if (Instance.Cam == null) return Vector2.zero;
            return Instance.Cam.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    public static void SwitchUseRound()
    {
        Instance.isUseRound = !Instance.isUseRound;
        Instance.CheckRoundStatus();
    }

    public static void Activate()
    {
        if (Instance == null) return;
        Instance.isActive = true;
    }

    public static void Deactivate()
    {
        if (Instance == null) return;
        Instance.isActive = false;
    }

    void CheckRoundStatus()
    {
        if (MouseArrow != null)
        {
            MouseArrow.gameObject.SetActive(isUseRound);
        }
    }

    void Start()
    {
        if (Instance!= null) Destroy(gameObject);
        else Instance = this;
        System.Action GetCellSize = ()=> LocalCellSize = MapScaler.GetCellSize();
        GetCellSize.Invoke();
        MapScaler.OnUpdated += GetCellSize.Invoke;
    }

    private void FixedUpdate()
    {
        if (!isActive) return;
        CalculateRoundedPoint();
        MoveArrow();
        ShiftMap();
        ZoomCamera();
    }

    void CalculateRoundedPoint()
    {
        if (Cam == null) Cam = Camera.main;
        if (Cam == null) return;
        Vector2 MouseWorldPos = Cam.ScreenToWorldPoint(Input.mousePosition);
        float XPosRemains = Mathf.Repeat(MouseWorldPos.x - MapScaler.WorldOffset.x,LocalCellSize);
        float XPos = MouseWorldPos.x - XPosRemains;
        if (XPosRemains > LocalCellSize*0.5f) XPos += LocalCellSize; 
        float YPosRemains = Mathf.Repeat(MouseWorldPos.y - MapScaler.WorldOffset.y,LocalCellSize);
        float YPos = MouseWorldPos.y - YPosRemains;
        if (YPosRemains > LocalCellSize*0.5f) YPos += LocalCellSize; 
        RoundedUserInput = new Vector2(XPos, YPos);
    }

    void MoveArrow()
    {
        if (MouseArrow == null) return;
        if (isUseRound)
        {
            MouseArrow.position = RoundedUserInput;
        }
    }

    void ShiftMap()
    {
        if (Input.GetMouseButton(2))
        {
            if (!isMoving)
            {
                LastMousePosition = Input.mousePosition;
                WorldOffsetSaved = MapScaler.WorldOffset;
                isMoving = true;
                return;
            }
            MapScaler.WorldOffset = 
            WorldOffsetSaved - ((Vector2)(LastMousePosition - Input.mousePosition)) 
            * ZoomScale * 1.5f;
        }
        else
        {
            if (isMoving) isMoving = false;
        }
    }

    void ZoomCamera()
    {
        if (Input.mouseScrollDelta!= Vector2.zero)
        {
            ZoomScale = Mathf.Clamp(ZoomScale - Input.mouseScrollDelta.y * 0.2f, 0.05f,10f);
            GetComponent<Camera>().orthographicSize = 540 * ZoomScale;
        }
    }
}