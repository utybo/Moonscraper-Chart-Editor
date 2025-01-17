﻿// Copyright (c) 2016-2017 Alexander Ong
// See LICENSE in project root for license information.

using UnityEngine;
using System.Collections;

public abstract class TimelineIndicator : MonoBehaviour {
    protected ChartEditor editor;
    [HideInInspector]
    public TimelineHandler handle;
    public int index = int.MaxValue;

    protected Vector2 previousScreenSize = Vector2.zero;

    protected virtual void Awake()
    {
        editor = GameObject.FindGameObjectWithTag("Editor").GetComponent<ChartEditor>();

        previousScreenSize.x = Screen.width;
        previousScreenSize.y = Screen.height;
    }

    protected Vector3 GetLocalPos(uint position, Song song)
    {
        float time = song.TickToTime(position, song.resolution);

        float endTime = song.length;

        if (endTime > 0)
            return handle.HandlePosToLocal(time / endTime);
        else
            return Vector3.zero;
    }

    public abstract void ExplicitUpdate();
}
