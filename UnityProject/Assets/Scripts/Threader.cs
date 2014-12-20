using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using System.Collections;

public class Threader : MonoBehaviour {

    public class Item : IComparable<Item>
    {
        public double Priority;
        public Func<object,object> ActionASync;
        public Action<object,object> PostActionSync; 
        public object Data;
        public object Context;
        public bool IsWorking = false;

        public int CompareTo(Item other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }

    public static Threader Active { get; private set; }

    private List<Item> Items = new List<Item>();
    private int workingCount = 0;
    private const int maxWorkingCount = 4;

	// Use this for initialization
	void Start ()
	{
	    Active = this;
	    InvokeRepeating("QueueCheck", 0, 0.04f);
	}

    public void Enqueue(Item item)
    {
        Items.Add(item);
    }

    void QueueCheck()
    {
        var unStarted = Items.Where(o => !o.IsWorking).ToArray();
        if (workingCount < maxWorkingCount && unStarted.Length > 0)
        {
            var item = unStarted.Max();
            item.Data = null;
            Thread thread = new Thread(() =>
            {                
                item.Data = item.ActionASync(item.Context);
            });
            thread.IsBackground = true;
            workingCount++;
            item.IsWorking = true;
            thread.Start();
        }
        var doneItems = Items.Where(o => o.Data != null);
        if (doneItems.Any())
        {
            var doneItem = doneItems.Max();
            doneItem.PostActionSync(doneItem.Context, doneItem.Data);
            Items.Remove(doneItem);
            workingCount--;
        }
    }
}
