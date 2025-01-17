// Copyright (c) 2016-2017 Alexander Ong
// See LICENSE in project root for license information.

using UnityEngine;
using System.Collections;
using System;

public class TimelineMovementController : MovementController
{
    public TimelineHandler timeline;
    public Transform strikeLine;
    public UnityEngine.UI.Text timePosition;
    [SerializeField]
    Texture2D dragCursorPositive;
    [SerializeField]
    Texture2D dragCursorNegative;

    Vector2? middleClickDownScreenPos = null;
    const float c_middleClickMouseDragSensitivity = 10.0f;
    const float autoscrollSpeed = 10.0f;
    readonly Shortcut[] arrowKeyShortcutGroup = new Shortcut[] { Shortcut.MoveStepPositive, Shortcut.MoveStepNegative, Shortcut.MoveMeasurePositive, Shortcut.MoveMeasureNegative };

    public override void SetPosition(uint tick)
    {
        if (Globals.applicationMode == Globals.ApplicationMode.Editor)
        {
            Vector3 pos = initPos;
            pos.y += editor.currentSong.TickToWorldYPosition(tick);
            transform.position = pos;

            explicitChartPos = tick;
        }
    }

    // Use this for initialization
    new void Start () {
        base.Start();
        timeline.handlePos = 0;
        UpdatePosBasedTimelineHandle();
    }

    const float ARROW_INIT_DELAY_TIME = 0.5f;
    const float ARROW_HOLD_MOVE_ITERATION_TIME = 0.1f;
    float arrowMoveTimer = 0;
    float lastMoveTime = 0;
    int sectionHighlightCurrentIndex = 0;
    int sectionHighlightRealOriginIndex = 0;
    int sectionHighlightOffset { get { return  sectionHighlightCurrentIndex - sectionHighlightRealOriginIndex; } }

    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            middleClickDownScreenPos = Input.mousePosition;
        }
        else if (!Input.GetMouseButton(2) && middleClickDownScreenPos.HasValue)
        {
            middleClickDownScreenPos = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        if (Input.GetMouseButtonUp(0) && Globals.applicationMode == Globals.ApplicationMode.Editor)
            cancel = false;

        if (Services.IsInDropDown)
            cancel = true;

        // Update timer text
        if (timePosition)
        {
            bool audioLoaded = false;
            foreach (var stream in editor.currentSong.bassAudioStreams)
            {
                if (AudioManager.StreamIsValid(stream))
                    audioLoaded = true;
            }

            if (!audioLoaded)//editor.currentSong.songAudioLoaded)
            {
                timePosition.color = Color.red;
                timePosition.text = "No audio";               
            }
            else
            {
                timePosition.color = Color.white;
                timePosition.text = Utility.timeConvertion(TickFunctions.WorldYPositionToTime(strikeLine.position.y));
            }
        }

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.PageDown))
            arrowMoveTimer = 0;
        else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.PageDown))
            arrowMoveTimer += Time.deltaTime;
        else
            arrowMoveTimer = 0;
    }

    Vector3 prevPos = Vector3.zero;
    Vector3 lastMouseDownPos = Vector3.zero;
    Vector3 mouseScrollMovePosition = Vector3.zero;

    // Update is called once per frame
    void LateUpdate () {
        if (!ShortcutInput.GetInput(Shortcut.SelectAllSection) || editor.currentSong.sections.Count <= 0)
        {
            sectionHighlightRealOriginIndex = SongObjectHelper.GetIndexOfPrevious(editor.currentSong.sections, editor.currentTickPos);
            sectionHighlightCurrentIndex = sectionHighlightRealOriginIndex;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            lastMouseDownPos = Input.mousePosition;
        }

        if (Globals.applicationMode == Globals.ApplicationMode.Editor)
        {
            Vector2 middleClickDragPercentageDelta = GetMiddleClickDragPercentageDelta();

            if (scrollDelta == 0 && focused)
            {
                scrollDelta = Input.mouseScrollDelta.y;
            }

            if (middleClickDragPercentageDelta != Vector2.zero)
            {
                float sign = Mathf.Sign(middleClickDragPercentageDelta.y);

                Texture2D cursorTexture = sign >= 0 ? dragCursorPositive : dragCursorNegative;
                Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.ForceSoftware);

                scrollDelta = middleClickDragPercentageDelta.y * c_middleClickMouseDragSensitivity;
            }

            if (Services.IsInDropDown)
                scrollDelta = 0;

            // Position changes scroll bar value
            if (scrollDelta != 0 || transform.position != prevPos || Services.HasScreenResized)
            {
                if (ShortcutInput.GetInput(Shortcut.SectionJumpMouseScroll) && editor.currentSong.sections.Count > 0)
                {
                    SectionJump(scrollDelta);
                    RefreshSectionHighlight();
                }
                else
                {
                    // Mouse scroll movement
                    mouseScrollMovePosition.x = transform.position.x;
                    mouseScrollMovePosition.y = transform.position.y + (scrollDelta * c_mouseScrollSensitivity);
                    mouseScrollMovePosition.z = transform.position.z;
                    transform.position = mouseScrollMovePosition;
                    explicitChartPos = null;
                }

                if (transform.position.y < initPos.y)
                    transform.position = initPos;

                if (Services.HasScreenResized)
                    StartCoroutine(resolutionChangePosHold());

                UpdateTimelineHandleBasedPos();
            }
            else if (ShortcutInput.GetInputDown(Shortcut.SectionJumpPositive) && editor.currentSong.sections.Count > 0)
            {
                SectionJump(1);              
                UpdateTimelineHandleBasedPos();
                RefreshSectionHighlight();
            }
            else if (ShortcutInput.GetInputDown(Shortcut.SectionJumpNegative) && editor.currentSong.sections.Count > 0)
            {
                SectionJump(-1);
                UpdateTimelineHandleBasedPos();
                RefreshSectionHighlight();
            }
            else if (ShortcutInput.GetGroupInput(arrowKeyShortcutGroup))
            {
                // Arrow key controls
                uint currentPos;
                if (explicitChartPos != null)
                    currentPos = (uint)explicitChartPos;
                else
                    currentPos = editor.currentTickPos;

                if (arrowMoveTimer == 0 || (arrowMoveTimer > ARROW_INIT_DELAY_TIME && Time.realtimeSinceStartup > lastMoveTime + ARROW_HOLD_MOVE_ITERATION_TIME))
                {
                    uint snappedPos = currentPos;
                    // Navigate to snapped pos ahead or behind
                    if (ShortcutInput.GetInput(Shortcut.MoveStepPositive))
                    {
                        snappedPos = Snapable.ChartIncrementStep(currentPos, GameSettings.step, editor.currentSong.resolution, editor.currentSong);
                    }
                    else if (ShortcutInput.GetInput(Shortcut.MoveStepNegative))
                    {
                        snappedPos = Snapable.ChartDecrementStep(currentPos, GameSettings.step, editor.currentSong.resolution, editor.currentSong);
                    }
                    else if (ShortcutInput.GetInput(Shortcut.MoveMeasurePositive))
                    {
                        snappedPos = Snapable.TickToSnappedTick(currentPos + (uint)(editor.currentSong.resolution * 4), GameSettings.step, editor.currentSong.resolution, editor.currentSong);
                    }
                    // Page Down
                    else if (ShortcutInput.GetInput(Shortcut.MoveMeasureNegative))
                    {
                        snappedPos = Snapable.TickToSnappedTick(currentPos - (uint)(editor.currentSong.resolution * 4), GameSettings.step, editor.currentSong.resolution, editor.currentSong);
                    }

                    if (editor.currentSong.TickToTime(snappedPos, editor.currentSong.resolution) <= editor.currentSong.length)
                    {
                        SetPosition(snappedPos);
                    }

                    lastMoveTime = Time.realtimeSinceStartup;
                }

                UpdateTimelineHandleBasedPos();
            }
            // else check mouse range
            else if (Toolpane.mouseDownInArea && (globals.services.InToolArea && (Input.GetMouseButton(0) || Input.GetMouseButton(1)) && Input.mousePosition != lastMouseDownPos))
            { 
                if (!Toolpane.menuCancel && 
                    UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == null && 
                    Input.mousePosition.y > Camera.main.WorldToScreenPoint(editor.mouseYMaxLimit.position).y)
                {
                    // Autoscroll
                    transform.position = new Vector3(transform.position.x, transform.position.y + autoscrollSpeed * Time.deltaTime, transform.position.z);
                    UpdateTimelineHandleBasedPos();
                }
                else
                {
                    UpdatePosBasedTimelineHandle();
                }
            }
            // Scroll bar value changes position
            else
            {
                UpdatePosBasedTimelineHandle();
            }
        }
        else if(Globals.applicationMode == Globals.ApplicationMode.Playing)
        {
            PlayingMovement();

            // Update timeline handle
            UpdateTimelineHandleBasedPos();

            if (timeline.handlePos >= 1)
                editor.Stop();
        }

        if (Globals.applicationMode != Globals.ApplicationMode.Playing)
            lastUpdatedRealTime = Time.realtimeSinceStartup;

        prevPos = transform.position;
    }
    /*
    void FixedUpdate()
    {
        if (Globals.applicationMode == Globals.ApplicationMode.Playing)
        {
            PlayingMovement();
            UpdateTimelineHandleBasedPos();

            if (timeline.handlePos >= 1)
                editor.Stop();
        }
    }*/

    IEnumerator resolutionChangePosHold()
    {
        yield return null;

        UpdateTimelineHandleBasedPos();
    }

    void UpdateTimelineHandleBasedPos()
    {
        if (editor.currentChart != null)
        {
            // Front cap
            if (Globals.applicationMode == Globals.ApplicationMode.Editor)
            {
                if (transform.position.y < initPos.y)
                    transform.position = initPos;
            }

            float endYPos = TickFunctions.TimeToWorldYPosition(editor.currentSong.length);
            float totalDistance = endYPos - initPos.y - strikeLine.localPosition.y;

            if (transform.position.y + strikeLine.localPosition.y > endYPos)
            {
                transform.position = new Vector3(transform.position.x, endYPos - strikeLine.localPosition.y, transform.position.z);
            }

            float currentDistance = transform.position.y - initPos.y;

            //if (Globals.applicationMode != Globals.ApplicationMode.Playing)
            //{
                if (totalDistance > 0)
                    timeline.handlePos = currentDistance / totalDistance;
                else
                    timeline.handlePos = 0;
            //}
        }
    }

    void UpdatePosBasedTimelineHandle()
    {
        if (editor.currentChart != null)
        {         
            float endYPos = TickFunctions.TimeToWorldYPosition(editor.currentSong.length);
            float totalDistance = endYPos - initPos.y - strikeLine.localPosition.y;

            if (totalDistance > 0)
            {
                float currentDistance = timeline.handlePos * totalDistance;

                transform.position = initPos + new Vector3(0, currentDistance, 0);
            }
            else
            {
                timeline.handlePos = 0;
                transform.position = initPos;
            }
        }
    }

    void SectionJump(float direction)
    {
        // Jump to the previous or next sections
        float position = Mathf.Round(strikeLine.position.y);

        int i = 0;
        while (i < editor.currentSong.sections.Count && Mathf.Round(editor.currentSong.sections[i].worldYPosition) <= position)
        {
            ++i;
        }

        // Jump forward
        if (direction > 0)
        {
            // Found section ahead
            if (i < editor.currentSong.sections.Count && Mathf.Round(editor.currentSong.sections[i].worldYPosition) > position)
                SetPosition(editor.currentSong.sections[i].tick);
            else
                SetPosition(editor.currentSong.TimeToTick(editor.currentSong.length, editor.currentSong.resolution));       // Jump to the end of the song

        }
        // Jump backwards
        else
        {
            while (i > editor.currentSong.sections.Count - 1 || (i >= 0 && Mathf.Round(editor.currentSong.sections[i].worldYPosition) >= position))
                --i;

            if (i >= 0)
                SetPosition(editor.currentSong.sections[i].tick);
            else
                SetPosition(0);
        }
    }

    void RefreshSectionHighlight()
    {
        if (!ShortcutInput.GetInput(Shortcut.SelectAllSection))
            return;

        int currentSectionIndex = SongObjectHelper.GetIndexOfPrevious(editor.currentSong.sections, editor.currentTickPos);
        bool changed = currentSectionIndex != sectionHighlightCurrentIndex;

        editor.currentSelectedObject = null;
        sectionHighlightCurrentIndex = currentSectionIndex;

        for (int i = 0; Mathf.Abs(i) <= Mathf.Abs(sectionHighlightOffset); i -= (int)Mathf.Sign(sectionHighlightOffset))
        {
            globals.AddHighlightCurrentSection(i);
        }
    }

    Vector2 GetMiddleClickDragPixelDelta()
    {
        if (middleClickDownScreenPos.HasValue)
        {
            Vector2 clickPos = middleClickDownScreenPos.Value;
            Vector2 currentScreenPos = Input.mousePosition;

            float xDelta = currentScreenPos.x - clickPos.x;
            float yDelta = currentScreenPos.y - clickPos.y;

            return new Vector2(xDelta, yDelta);
        }
        else
        {
            return Vector2.zero;
        }
    }

    Vector2 GetMiddleClickDragPercentageDelta()
    {
        Vector2 pixelDelta = GetMiddleClickDragPixelDelta();
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float xDelta = pixelDelta.x / screenWidth;
        float yDelta = pixelDelta.y / screenHeight;

        return new Vector2(xDelta, yDelta);
    }
}
