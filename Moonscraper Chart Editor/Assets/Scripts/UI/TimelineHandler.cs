﻿// Copyright (c) 2016-2017 Alexander Ong
// See LICENSE in project root for license information.

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class TimelineHandler : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    [SerializeField]
    GameObject handle;
    public UnityEngine.UI.Text percentage;
    public GameObject sectionIndicatorPrefab;
    public GameObject starpowerIndicatorPrefab;
    public GameObject highlightedIndicator;

    const int POOL_SIZE = 100;
    const int POOL_EXTEND_SIZE = 50;
    GameObject sectionIndicatorParent;
    SectionGuiController[] sectionIndicatorPool = new SectionGuiController[POOL_SIZE];
    GameObject starpowerIndicatorParent;
    StarpowerGUIController[] starpowerIndicatorPool = new StarpowerGUIController[POOL_SIZE];

    RectTransform rectTransform;
    MovementController movement;

    float halfHeight;
    float scaledHalfHeight;

    Vector2 previousScreenSize = Vector2.zero;
    int previousPercentageValue = 0;

    // Value between 0 and 1
    public float handlePosRound
    {
        get
        {
            return (handle.transform.localPosition.y.Round(2) + halfHeight.Round(2)) / rectTransform.rect.height.Round(2);
        }
        set
        {
            handle.transform.localPosition = HandlePosToLocal(value);
        }
    }

    public float handlePos
    {
        get
        {
            return (handle.transform.localPosition.y + halfHeight) / rectTransform.rect.height;
        }
        set
        {
            handle.transform.localPosition = HandlePosToLocal(value);
        }
    }

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        halfHeight = rectTransform.rect.height / 2.0f;
        scaledHalfHeight = halfHeight * transform.lossyScale.y;

        movement = GameObject.FindGameObjectWithTag("Movement").GetComponent<MovementController>();
    }

    void Start()
    {
        sectionIndicatorParent = new GameObject("Section Indicators");
        sectionIndicatorParent.transform.SetParent(this.transform.parent);
        sectionIndicatorParent.transform.localPosition = Vector3.zero;
        sectionIndicatorParent.transform.localScale = new Vector3(1, 1, 1);
        sectionIndicatorParent.transform.SetSiblingIndex(1);

        starpowerIndicatorParent = new GameObject("Starpower Indicators");
        starpowerIndicatorParent.transform.SetParent(this.transform.parent);
        starpowerIndicatorParent.transform.localPosition = Vector3.zero;
        starpowerIndicatorParent.transform.localScale = new Vector3(1, 1, 1);
        starpowerIndicatorParent.transform.SetSiblingIndex(1);

        // Create section pool
        for (int i = 0; i < sectionIndicatorPool.Length; ++i)
            sectionIndicatorPool[i] = CreateSectionIndicator(i);

        // Create starpower pool
        for (int i = 0; i < starpowerIndicatorPool.Length; ++i)
        {
            starpowerIndicatorPool[i] = CreateSPIndicator(i);
        }

        previousScreenSize.x = Screen.width;
        previousScreenSize.y = Screen.height;

        RefreshHighlightIndicator();

        EventsManager.onChartReloadEventList.Add(QueueExternalUpdate);

        UpdatePercentageText(0);
    }

    int prevSectionLength = 0;
    int prevSPLength = 0;
    float prevSongLength = 0;
    Song prevSong;
    Resolution prevRes;

    public static bool externalUpdate = false;
    void Update()
    {
        ChartEditor editor = ChartEditor.Instance;

        halfHeight = rectTransform.rect.height / 2.0f;
        scaledHalfHeight = halfHeight * transform.lossyScale.y;

        int newPercentageValue = (int)(handlePosRound * 100);
        if (previousPercentageValue != newPercentageValue)
        {
            UpdatePercentageText(newPercentageValue);
        }

        bool update = (!ReferenceEquals(prevSong, editor.currentSong) || prevSongLength != editor.currentSong.length
             || previousScreenSize.x != Screen.width || previousScreenSize.y != Screen.height || 
             prevRes.height != Screen.currentResolution.height ||
             prevRes.width != Screen.currentResolution.width || prevRes.refreshRate != Screen.currentResolution.refreshRate);

        // Check if indicator pools need to be extended
        
        while (sectionIndicatorPool.Length < editor.currentSong.sections.Count)
        {
            SectionGuiController[] controllers = new SectionGuiController[sectionIndicatorPool.Length + POOL_EXTEND_SIZE];
            System.Array.Copy(sectionIndicatorPool, controllers, sectionIndicatorPool.Length);

            for (int i = sectionIndicatorPool.Length; i < controllers.Length; ++i)
                controllers[i] = CreateSectionIndicator(i);

            sectionIndicatorPool = controllers;
        }
        
        while (starpowerIndicatorPool.Length < editor.currentChart.starPower.Count)
        {
            StarpowerGUIController[] controllers = new StarpowerGUIController[starpowerIndicatorPool.Length + POOL_EXTEND_SIZE];
            System.Array.Copy(starpowerIndicatorPool, controllers, starpowerIndicatorPool.Length);

            for (int i = starpowerIndicatorPool.Length; i < controllers.Length; ++i)
                controllers[i] = CreateSPIndicator(i);

            starpowerIndicatorPool = controllers;
        }

        // Set the sections
        if (update || editor.currentSong.sections.Count != prevSectionLength || externalUpdate)
        {
            StartCoroutine(UpdateSectionIndicator());
        }

        // Set the sp
        if (update || editor.currentChart.starPower.Count != prevSPLength || externalUpdate)
        {
            StartCoroutine(UpdateStarpowerIndicators());
        }

        prevSong = editor.currentSong;
        prevSongLength = editor.currentSong.length;
        prevSPLength = editor.currentChart.starPower.Count;
        prevSectionLength = editor.currentSong.sections.Count;
        previousScreenSize.x = Screen.width;
        previousScreenSize.y = Screen.height;
        prevRes = Screen.currentResolution;

        externalUpdate = false;
    }

    void UpdatePercentageText(int newValue)
    {
        percentage.text = newValue.ToString() + "%";
        previousPercentageValue = newValue;
    }

    void QueueExternalUpdate()
    {
        externalUpdate = true;
    }

    IEnumerator UpdateSectionIndicator()
    {
        yield return null;
        yield return null;

        ChartEditor editor = ChartEditor.Instance;

        int i;
        for (i = 0; i < editor.currentSong.sections.Count; ++i)
        {
            if (i < sectionIndicatorPool.Length && editor.currentSong.sections[i].time <= editor.currentSong.length)
            {
                sectionIndicatorPool[i].gameObject.SetActive(true);
                sectionIndicatorPool[i].ExplicitUpdate();
            }
            else
            {
                break;
            }
        }

        while (i < sectionIndicatorPool.Length)
        {
            sectionIndicatorPool[i++].gameObject.SetActive(false);
        }
    }

    IEnumerator UpdateStarpowerIndicators()
    {
        yield return null;
        yield return null;

        ChartEditor editor = ChartEditor.Instance;

        int i;
        for (i = 0; i < editor.currentChart.starPower.Count; ++i)
        {  
            if (i < starpowerIndicatorPool.Length && editor.currentChart.starPower[i].time <= editor.currentSong.length)
            {
                starpowerIndicatorPool[i].gameObject.SetActive(true);
                starpowerIndicatorPool[i].ExplicitUpdate();
            }
            else
            {
                break;
            }
        }

        while (i < starpowerIndicatorPool.Length)
        {
            starpowerIndicatorPool[i++].gameObject.SetActive(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveHandle(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveHandle(eventData);
    }

    SectionGuiController CreateSectionIndicator(int index)
    {
        GameObject indicatorObject = Instantiate(sectionIndicatorPrefab);
        indicatorObject.transform.SetParent(sectionIndicatorParent.transform);
        indicatorObject.transform.localScale = new Vector3(1, 1, 1);

        SectionGuiController indicator = indicatorObject.GetComponent<SectionGuiController>();
        indicator.handle = this;
        indicatorObject.SetActive(false);
        indicator.index = index;

        return indicator;
    }

    StarpowerGUIController CreateSPIndicator(int index)
    {
        GameObject indicatorObject = Instantiate(starpowerIndicatorPrefab);
        indicatorObject.transform.SetParent(starpowerIndicatorParent.transform);
        indicatorObject.transform.localScale = new Vector3(1, 1, 1);

        StarpowerGUIController indicator = indicatorObject.GetComponent<StarpowerGUIController>();
        indicator.handle = this;
        indicatorObject.SetActive(false);
        indicator.index = index;

        return indicator;
    }

    void MoveHandle(PointerEventData eventData)
    {
        ChartEditor editor = ChartEditor.Instance;
        movement.editor.Stop();
        if (Globals.applicationMode == Globals.ApplicationMode.Editor)
        {
            Vector3 pos = handle.transform.position;
            pos.y = editor.uiServices.uiCamera.ScreenToWorldPoint(eventData.position).y;        

            if (pos.y > transform.position.y + scaledHalfHeight)
            {
                pos = handle.transform.localPosition;
                pos.y = HandlePosToLocal(1).y;
                handle.transform.localPosition = pos;
            }
            else if (pos.y < transform.position.y - scaledHalfHeight)
            {
                pos = handle.transform.localPosition;
                pos.y = HandlePosToLocal(0).y;
                handle.transform.localPosition = pos;
            }
            else
                handle.transform.position = pos;
        }
        MovementController.explicitChartPos = null;
    }

    public Vector3 HandlePosToLocal(float pos)
    {
        // Pos is a value between 0 and 1, 0 representing the start of the song and 1 being the end

        if (pos < 0)
            pos = 0;
        return new Vector3(handle.transform.localPosition.x, pos * rectTransform.rect.height - halfHeight, handle.transform.localPosition.z);
    }

    float minTimeRange = 0;
    float maxTimeRange = 300; // editor.currentSong.length

    public Vector3? TimeToLocalPosition(float timeInSeconds)
    {
        if (timeInSeconds < minTimeRange || timeInSeconds > maxTimeRange)
            return null;
        else
            return HandlePosToLocal((timeInSeconds - minTimeRange) / (maxTimeRange - minTimeRange));
    }

    public void RefreshHighlightIndicator()
    {
        bool highlightIndicatorActive = false;
        ChartEditor editor = ChartEditor.Instance;

        if (editor.currentSelectedObjects.Count > 0)
        {
            uint highlightRangeMin = uint.MaxValue, highlightRangeMax = 0;

            foreach (SongObject so in editor.currentSelectedObjects)
            {
                if (so.tick < highlightRangeMin)
                    highlightRangeMin = so.tick;

                if (so.tick > highlightRangeMax)
                    highlightRangeMax = so.tick;
            }

            float minTime = editor.currentSong.TickToTime(highlightRangeMin, editor.currentSong.resolution);
            float maxTime = editor.currentSong.TickToTime(highlightRangeMax, editor.currentSong.resolution);

            float endTime = editor.currentSong.length;

            Vector3 minPos = Vector3.zero, maxPos = Vector3.zero;

            if (endTime > 0)
            {
                minPos = HandlePosToLocal(minTime / endTime);
                maxPos = HandlePosToLocal(maxTime / endTime);
            }

            float size = (maxPos - minPos).y;
            Vector3 position = minPos;
            position.y += size / 2;

            highlightedIndicator.transform.localPosition = position;
            highlightedIndicator.transform.localScale = new Vector3(highlightedIndicator.transform.localScale.x, size, highlightedIndicator.transform.localScale.z);

            highlightIndicatorActive = true;
        }

        highlightedIndicator.SetActive(highlightIndicatorActive);
    }
}
