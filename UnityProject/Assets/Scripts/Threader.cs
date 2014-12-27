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

        public bool SkipAsync = false; //if set, only sync processing will be done
        public object PriorityData;
        public Func<object, double> PriorityResolver; 
        public Func<IProcessable, string, int, object> ActionASync;
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

        public bool IsDone
        {
            get { return Data != null; }
        }

        public bool IsUnstarted
        {
            get { return !IsWorking && !SkipAsync && !IsAsyncFinished; }
        }

        public bool IsOverrun
        {
            get { return DateTime.Now - InternalStartTime > TimeSpan.FromSeconds(5); }
        }

        public bool IsAsyncFinished { get; set; }
    }

    public static Threader Active { get; private set; }

    private List<Item> Items = new List<Item>();
    private int workingCount = 0;
    public const int MaxWorkingCount = 5;
    private int chunksProcessed = 0;
    private DateTime lastCPSTime = DateTime.Now;
    private int cps; //Chunks per second
    private Thread[] threads = new Thread[MaxWorkingCount];
    private Item[] threadItems = new Item[MaxWorkingCount];
    private bool isPlaying = true;

    public int WorkingCount
    {
        get { return threadItems.Count(o => o!=null && o.IsWorking); }
    }

    public int QueueCount
    {
        get { return Items.Count; }
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
	    isPlaying = true;
	    Active = this;
	    InvokeRepeating("QueueCheck", 0, 0.04f);
	    for (int la = 0; la < MaxWorkingCount; la++)
	    {
	        threads[la] = new Thread(ThreadProc);
	        threads[la].IsBackground = true;
	        threads[la].Start(la);
	    }
	}

    void OnDestroy()
    {
        isPlaying = false;
    }

    private void ThreadProc(object num)
    {
        Stopwatch watch = new Stopwatch();
        var n = (int) num;
        while (isPlaying)
        {
            var ti = threadItems[n];
            if (ti != null && ti.IsUnstarted)
            {
                ti.InternalStartTime = DateTime.Now;
                ti.IsWorking = true;
                watch.Reset();
                watch.Start();
                ti.Data = ti.ActionASync(ti.Context, ti.Tag, n);
                watch.Stop();
                LastGenerationTime = watch.Elapsed;
                ti.IsAsyncFinished = true;
                ti.IsWorking = false;
            }
            else
            {
                Thread.Sleep(5);
            }
        }
        UnityEngine.Debug.Log("Thread " + n + " has exit.");
    }

    public void Enqueue(Item item)
    {
        if (!Items.Any(o => o.Context == item.Context && o.Tag == item.Tag) || (item.Context == null && Items.All(o => o.Tag != item.Tag)))
        {
            Items.Add(item);
        }
    }

    void QueueCheck()
    {
        var unStarted = Items.Where(o => o.IsUnstarted);
        while (WorkingCount < MaxWorkingCount && unStarted.Any())
        {
            var item = unStarted.Max();
            item.Data = null;
            int freeSlot = threadItems.ToList().FindIndex(o => o==null || (o.IsDone && o.IsAsyncFinished));
            if (freeSlot < 0)
            {
                break;
            }
            else
            {
                threadItems[freeSlot] = item;
            }
        }
        
        var doneItems = Items.Where(o => o.Data != null);
        if (doneItems.Any())
        {
            var doneItem = doneItems.Max();
            doneItem.PostActionSync(doneItem.Context, doneItem.Tag, doneItem.Data);
            Items.Remove(doneItem);
            if (doneItem.Context != null) doneItem.Context.IsProcessing = false;            
            chunksProcessed++;            
        }        

        var overrunItems = Items.Where(o => DateTime.Now - o.InternalStartTime > TimeSpan.FromSeconds(5)).ToArray();
        foreach (var oi in overrunItems)
        {
            if (oi.InternalThread != null && oi.InternalThread.ThreadState == ThreadState.Running)
            {
                oi.InternalThread.Abort();
            }
            if(oi.Context!=null) oi.Context.IsProcessing = false;
            Items.Remove(oi);
            workingCount--;
        }
    }
}
