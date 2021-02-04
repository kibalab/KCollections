
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace K13A.Collections.Generic
{

    public class KQueue : UdonSharpBehaviour
    {
        private const int jump = 256;

        private object[] components = new object[jump];
        private int index = -1;
        private int scaled = 1;

        public object[] Enqueue(object newComponent)
        {
            ResizeIfNeeded(true);
            components[++index] = newComponent;
            return components;
        }

        public object Dequeue()
        {
            ResizeIfNeeded(false);
            var r = components[0];
            for (var i = 0; i < components.Length - 1; ++i)
            {
                components[i] = components[i + 1];
            }
            --index;
            return r;
        }

        public object Peek()
        {
            return components[0];
        }

        public void Clear()
        {
            components = new object[jump];
            scaled = 1;
            index = -1;
        }
        public int Count()
        {
            return index + 1;
        }

        public object[] Clone()
        {
            var copied = new object[Count()];
            for (var i = 0; i < Count(); ++i)
            {
                copied[i] = components[i];
            }
            return copied;
        }

        void ResizeIfNeeded(bool isAdd)
        {
            // scale up needed
            if (isAdd && (index == components.Length - 1))
            {
                ++scaled;
                var temp = components;

                components = new object[scaled * jump];
                for (var i = 0; i < temp.Length; i++)
                {
                    components[i] = temp[i];
                }
            }

            // scale down needed
            if (!isAdd && (index - 1 < (scaled - 1) * jump) && scaled != 1)
            {
                --scaled;
                var temp = components;
                components = new object[scaled * jump];
                for (var i = 0; i < components.Length; i++)
                {
                    components[i] = temp[i];
                }
            }
        }
    }
}
