using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A PreviewTaskRunner executes a collection of IPreviewTasks.
/// </summary>
public class PreviewTaskRunner
{
    internal class Promise
    {
        internal Func<bool> Func { get; private set; }
        internal Promise NextPromise { get; private set; }

        internal Promise(Func<bool> func)
        {
            Func = func;
        }

        internal Promise Then(Func<bool> func)
        {
            NextPromise = new Promise(func);
            return NextPromise;
        }
    }

    //enum TaskStatus { Waiting, Running }

    string m_guidSeed;
    //Dictionary<IPreviewTask, string> m_taskToTaskId;
    //Dictionary<string, TaskStatus> m_taskIdToTaskStatus;

    Dictionary<Func<bool>, Guid> m_funcToId;
    Dictionary<Guid, Promise> m_idToPromise;

    public PreviewTaskRunner()
    {
        m_guidSeed = Math.Abs(GetHashCode()).ToString();
        m_funcToId = new Dictionary<Func<bool>, Guid>();
        m_idToPromise = new Dictionary<Guid, Promise>();
        //StartCoroutine(RunPreviewTasks());
    }

    Guid GenerateActionId()
    {
        return new Guid(m_guidSeed);
    }

    // this action returns a bool to indicate if it is done
    // if it is done, it checks the promise for another action
    // if there is another action, it does it
    internal Promise Do(Func<bool> func)
    {
        Promise promise = new Promise(func);
        AddFunc(func, promise);
        return promise;
    }

    void AddFunc(Func<bool> func, Promise promise)
    {
        Guid guid = GenerateActionId();
        m_funcToId[func] = guid;
        m_idToPromise[guid] = promise;
    }

    /// <summary>
    /// Adds an IPreviewTask to be executed.
    /// </summary>
    /// <param name="task">The task to be executed.</param>
    /// <returns>a task id that can be used to check on the task status</returns>
    //public string Add(IPreviewTask task)
    //{
    //    // generate a job id
    //    Guid guid = new Guid(m_guidSeed);
    //    string guidString = guid.ToString();
    //    m_taskToTaskId[task] = guidString;
    //    // add it to the tasks in Waiting status
    //    m_taskIdToTaskStatus[guidString] = TaskStatus.Waiting;
    //    return guidString;
    //}

    internal IEnumerator RunPreviewTasks()
    {
        while (true)
        {
            foreach (Func<bool> func in m_funcToId.Keys)
            {
                Guid id = m_funcToId[func];
                bool isDone = func();
                if (!isDone)
                    continue;
                // remove this task
                Promise promise = m_idToPromise[id];
                m_funcToId.Remove(func);
                m_idToPromise.Remove(id);
                // add next if the promise has one
                if (promise.NextPromise != null)
                {
                    AddFunc(promise.NextPromise.Func, promise.NextPromise);
                }
            }

            if (m_funcToId.Keys.Count <= 0)
                yield return new WaitForSeconds(.5f);
            else
                yield return new WaitForSeconds(.1f);

            //foreach (IPreviewTask task in m_taskToTaskId.Keys)
            //{
            //    string id = m_taskToTaskId[task];
            //    TaskStatus taskStatus = m_taskIdToTaskStatus[id];
            //    switch (taskStatus)
            //    {
            //        case TaskStatus.Waiting:
            //            StartTask(task);
            //            break;
            //        case TaskStatus.Running:
            //            if (task.IsComplete())
            //                FinishTask(task);
            //            break;
            //        default:
            //            Debug.LogWarning($"PreviewTaskRunner has a task with unknown status {taskStatus}");
            //            break;

            //    }
            //}

        }
    }

    /// <summary>
    /// Starts a task and records that it is running.
    /// </summary>
    //private void StartTask(IPreviewTask task)
    //{
    //    task.Start();
    //    string id = m_taskToTaskId[task];
    //    m_taskIdToTaskStatus[id] = TaskStatus.Running;
    //}

    /// <summary>
    /// Finishes a task and removes it from tracking.
    /// </summary>
    //private void FinishTask(IPreviewTask task)
    //{
    //    task.Finish();
    //    string id = m_taskToTaskId[task];
    //    m_taskToTaskId.Remove(task);
    //    m_taskIdToTaskStatus.Remove(id);
    //}
}
