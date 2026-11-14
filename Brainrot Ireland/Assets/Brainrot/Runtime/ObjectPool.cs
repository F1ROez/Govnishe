using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brainrot
{
    /// <summary>A bounded, reusable component pool. No scene searches on its hot path.</summary>
    public sealed class ObjectPool<T> where T : Component
    {
        readonly Stack<T> available = new Stack<T>();
        readonly Func<T> factory;
        readonly int capacity;
        public ObjectPool(Func<T> factory, int capacity = 256)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.capacity = Mathf.Max(1, capacity);
        }
        public T Get()
        {
            T instance = available.Count > 0 ? available.Pop() : factory();
            instance.gameObject.SetActive(true);
            return instance;
        }
        public void Release(T instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            if (available.Count < capacity) available.Push(instance);
            else UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
