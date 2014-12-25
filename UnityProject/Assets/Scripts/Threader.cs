using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;
using System.Collections;
using ThreadState = System.Threading.ThreadState;

public class Threader : MonoBehaviour {

    public class Item : IComparable<Item>
    {
        public double Priority
        {
            get { return PriorityResolver(PriorityData); }
        }

        public object PriorityData;
        public Func<object, double> PriorityResolver; 
        public Func<IProcessable, string, object> ActionASync;
        public Action<IProcessable,string, object> PostActionSync; 
        public object Data;
        public IProcessable Context;
        public bool IsWorking = false;
        public string Tag = "";
        public Thread InternalThread;
        public DateTime InternalStartTime = DateTime.MaxValue;

        public int CompareTo(Item other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }

    public static Threader Active { get; private set; }

    private List<Item> Items = new List<Item>();
    private int workingCount = 0;
    private const int maxWorkingCount = 5;
    private int chunksProcessed = 0;
    private DateTime lastCPSTime = DateTime.Now;
    private int cps; //Chunks per second

    public int WorkingCount
    {
        get { return workingCount; }
    }

    public int CPS
    {
        get
        {
            if (DateTime.Now - lastCPSTime > TimeSpan.FromSeconds(1))
            {
                cps = chunksProcessed;
                lastCPSTime = DateTime.Now;
                chunksProcessed = 0;
            }
            return cps;
        }
    }

    public TimeSpan LastGenerationTime { get; set; }

    // Use this for initialization
	void Start ()
	{
	    Active = this;
	    InvokeRepeating("QueueCheck", 0, 0.04f);
	}

    public void Enqueue(Item item)
    {
        if (!Items.Any(o => o.Context == item.Context && o.Tag == item.Tag))
        {
            Items.Add(item);
        }
    }

    void QueueCheck()
    {
        var unStarted = Items.Where(o => !o.IsWorking).ToArray();
        if (workingCount < maxWorkingCount && unStarted.Length > 0)
        {
            var item = unStarted.Max();
            item.Data = null;
            item.InternalThread = new Thread(() =>
            {
                item.Context.IsProcessing = true;
                Stopwatch watch = new Stopwatch();
                watch.Start();
                item.Data = item.ActionASync(item.Context, item.Tag);
                watch.Stop();
                LastGenerationTime = watch.Elapsed;               
            });
            item.InternalThread.IsBackground = true;
            workingCount++;
            item.IsWorking = true;
            item.InternalThread.Start();
            item.InternalStartTime = DateTime.Now;
        }
        var doneItems = Items.Where(o => o.Data != null);
        if (doneItems.Any())
        {
            var doneItem = doneItems.Max();
            doneItem.PostActionSync(doneItem.Context, doneItem.Tag, doneItem.Data);
            Items.Remove(doneItem);
            doneItem.Context.IsProcessing = false;
            workingCount--;
            chunksProcessed++;
        }
        var overrunItems = Items.Where(o => DateTime.Now - o.InternalStartTime > TimeSpan.FromSeconds(4)).ToArray();
        foreach (var oi in overrunItems)
        {
            if (oi.InternalThread != null && oi.InternalThread.ThreadState == ThreadState.Running)
            {
                oi.InternalThread.Abort();
            }
            oi.Context.IsProcessing = false;
            Items.Remove(oi);
            workingCount--;
        }
    }
}
