﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
 
public class LoadingTask
{
    public string description;
    public ThreadStart task;

    public LoadingTask(string description, ThreadStart task)
    {
        this.description = description;
        this.task = task;
    }
}

public class LoadingTasksManager : MonoBehaviour
{
    public LoadingScreenFader loadingScreen;
    public bool isRunningTask { get; private set; }

    private void Start()
    {
        loadingScreen.gameObject.SetActive(true);   // This is initially hidden so we can actually see things in the editor.
    }

    public void KickTasks(IList<LoadingTask> tasks)
    {
        StartCoroutine(_KickTask(tasks));
    }

    IEnumerator _KickTask(IList<LoadingTask> tasks)
    {
        isRunningTask = true;

        Globals.applicationMode = Globals.ApplicationMode.Loading;
        loadingScreen.FadeIn();

        for (int i = 0; i < tasks.Count; ++i)
        {
            LoadingTask currentTask = tasks[i];
            loadingScreen.loadingInformation.text = currentTask.description;

            Thread taskThread = new Thread(currentTask.task);
            taskThread.Start();

            while (taskThread.ThreadState == ThreadState.Running)
                yield return null;
        }

        loadingScreen.FadeOut();
        loadingScreen.loadingInformation.text = "Complete!";

        isRunningTask = false;
    }
}
