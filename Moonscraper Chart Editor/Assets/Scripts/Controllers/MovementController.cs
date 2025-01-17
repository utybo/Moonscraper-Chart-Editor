﻿// Copyright (c) 2016-2017 Alexander Ong
// See LICENSE in project root for license information.

using UnityEngine;
using System.Collections;

public abstract class MovementController : MonoBehaviour {
    public static bool cancel = false;
    public ChartEditor editor;
    protected Globals globals;

    public Vector3 initPos { get; protected set; }
    protected float scrollDelta = 0;

    protected bool focused = true;
    public static uint? explicitChartPos = null;

    protected float lastUpdatedRealTime = 0;
    [HideInInspector]
    public float? playStartTime;
    [HideInInspector]
    public float? playStartPosition;

    Transform selfTransform;

    // Program options
    protected float c_mouseScrollSensitivity = 0.2f;      // May miss snap gaps if placed too high

    // Jump to a chart position
    public abstract void SetPosition(uint tick);

    public static TimeSync timeSync;

    public void SetTime(float time)
    {
        if (Globals.applicationMode == Globals.ApplicationMode.Editor)
        {
            Vector3 pos = initPos;
            pos.y += TickFunctions.TimeToWorldYPosition(time);
            transform.position = pos;
        }
    }

    protected void Start()
    {
        timeSync = new TimeSync();
        initPos = transform.position;
        globals = GameObject.FindGameObjectWithTag("Globals").GetComponent<Globals>();
        selfTransform = transform;
    }

    public void PlayingMovement()
    {   
        float speed = GameSettings.hyperspeed;
        Vector3 pos = transform.position;
        float deltaTime = Time.deltaTime;

        //float oldPos = pos.y;

        if (playStartTime != null && playStartPosition != null)
        {
            float time = Time.realtimeSinceStartup - (float)playStartTime; //(float)timeSync.GetTime();//
            if (time < 0)
                time = 0;

            pos.y = (float)playStartPosition + TickFunctions.TimeToWorldYPosition(time * GameSettings.gameSpeed);
            
            //time -= (Globals.audioCalibrationMS / 1000f * Globals.gameSpeed + editor.currentSong.offset);

            //pos.y = /*(float)playStartPosition +*/ Song.TimeToWorldYPosition(time);
        }
        else
        {
            pos.y += (speed * deltaTime);
        }

        //float newPos = pos.y;

        //if ((newPos - oldPos) > 0.4)
        //Debug.Log("Position difference: " + (newPos - oldPos) + ", Delta time: " + Time.deltaTime + ", Frame: " + Time.frameCount);
        //Debug.Log(Time.renderedFrameCount);
        selfTransform.position = pos;
        explicitChartPos = null;

        lastUpdatedRealTime = Time.time;
    }

    /*void OnApplicationFocus(bool hasFocus)
    {        
        focused = hasFocus;
    }*/

    void OnGUI()
    {
        if (focused)
        {
            if (UnityEngine.Event.current.type == EventType.ScrollWheel)
            {
                scrollDelta = -UnityEngine.Event.current.delta.y;
            }
            else
            {
                scrollDelta = 0;
            }
        }
        else
            scrollDelta = 0;
    }
}
