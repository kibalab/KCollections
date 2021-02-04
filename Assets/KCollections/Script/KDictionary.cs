
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace K13A.Collections.Generic
{

    public class KDictionary : UdonSharpBehaviour
    {
        private const int jump = 256;

        [HideInInspector] public object[] Values = new object[jump];
        [HideInInspector] public object[] Keys = new object[jump];
        private int index = -1;
        private int scaled = 1;

        public void Add(object Key, object Item)
        {
            ResizeIfNeeded(true);
            Values[++index] = Item;
            Keys[index] = Key;
        }

        public int Count()
        {
            return index + 1;
        }

        public void Clear()
        {
            Values = new object[jump];
            Keys = new object[jump];
            scaled = 1;
            index = -1;
        }

        public object GetValue(object Key)
        {
            var i = 0;
            foreach (object componentKey in Keys)
            {
                if (componentKey.Equals(Key))
                {
                    return Values[i];
                }
                ++i;
            }
            return null;
        }
        public object GetKey(object Value)
        {
            var i = 0;
            foreach (object componentValue in Values)
            {
                if (componentValue.Equals(Value))
                {
                    return Keys[i];
                }
                ++i;
            }
            return null;
        }

        public bool ContainsKey(object Key)
        {
            return GetValue(Key) != null;
        }

        public bool ContainsValue(object Value)
        {
            return GetKey(Value) != null;
        }

        public object RemoveWithKey(object Key)
        {
            var i = 0;
            foreach (object componentKey in Keys)
            {
                if (componentKey.Equals(Key))
                {
                    var r = Values[i];
                    removeAt(i);
                    return r;
                }
                ++i;
            }
            return null;
        }

        public object RemoveWithValue(object Value)
        {
            var i = 0;
            foreach (object componentValue in Values)
            {
                if (componentValue.Equals(Value))
                {
                    var r = Keys[i];
                    removeAt(i);
                    return r;
                }
                ++i;
            }
            return null;
        }

        private void removeAt(int removeIndex)
        {
            ResizeIfNeeded(false);
            for (var i = removeIndex; i < Values.Length - 1; ++i)
            {
                Values[i] = Values[i + 1];
            }
            --index;
        }

        void ResizeIfNeeded(bool isAdd)
        {
            // scale up needed
            if (isAdd && (index == Values.Length - 1))
            {
                ++scaled;
                var temp = Values;

                Values = new object[scaled * jump];
                for (var i = 0; i < temp.Length; i++)
                {
                    Values[i] = temp[i];
                }
            }

            // scale down needed
            if (!isAdd && (index - 1 < (scaled - 1) * jump) && scaled != 1)
            {
                --scaled;
                var temp = Values;
                Values = new object[scaled * jump];
                for (var i = 0; i < Values.Length; i++)
                {
                    Values[i] = temp[i];
                }
            }
        }
    }
}
