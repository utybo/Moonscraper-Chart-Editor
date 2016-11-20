﻿using UnityEngine;
using System.Collections;

public class SustainController : MonoBehaviour {

    public NoteController nCon;

	void OnMouseDrag()
    {
        // Update sustain
        if (Globals.applicationMode == Globals.ApplicationMode.Editor && Input.GetMouseButton(1))
        {
            nCon.SustainDrag();
        }
    }
}
