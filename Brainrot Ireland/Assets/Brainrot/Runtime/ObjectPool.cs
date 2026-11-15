using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    /// <summary>A bounded, reusable component pool. No scene searches on its hot path.</summary>
    public sealed class ObjectPool<T> where T : Component
    {
        readonly Stack<T> available = new Stack<T>();
        readonly HashSet<T> pooled = new HashSet<T>();
        readonly Func<T> factory;
        readonly int capacity;
        public ObjectPool(Func<T> factory, int capacity = 256)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.capacity = Mathf.Max(1, capacity);
        }
        public T Get()
        {
            T instance=null;
            while(available.Count>0&&instance==null){instance=available.Pop();pooled.Remove(instance);}
            if(instance==null)instance=factory();
            instance.gameObject.SetActive(true);
            return instance;
        }
        public void Release(T instance)
        {
            if (instance == null || pooled.Contains(instance)) return;
            instance.gameObject.SetActive(false);
            if (available.Count < capacity){available.Push(instance);pooled.Add(instance);}
            else UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
