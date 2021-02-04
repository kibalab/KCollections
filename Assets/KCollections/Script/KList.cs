
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace K13A.Collections.Generic
{
    public class KList : UdonSharpBehaviour
    {
        private const int jump = 256;

        private object[] components = new object[jump];
        private int scaled = 1;
        private int index = -1;

        public object[] Add(object newComponent)
        {
            ResizeIfNeeded(true);
            components[++index] = newComponent;
            return components;
        }

        public object[] Insert(int insertIndex, object component)
        {
            ResizeIfNeeded(true);
            index++;
            for (var i = index; i > insertIndex; --i)
            {
                components[i] = components[i - 1];
            }
            components[insertIndex] = component;

            return components;
        }

        public object RemoveLast()
        {
            var comp = components[index];
            components[index] = null;
            ResizeIfNeeded(false);
            --index;
            return comp;
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

        public void Sort()
        {
            if (index == -1) { return; }

            var type = (At(0)).GetType().Name;
            switch (type)
            {
                case "Int32": Sort_Int(); break;
                default:
                    Debug.Log($"[KList] Can't sort object type {type}");
                    break;
            }
        }

        void Sort_Int()
        {
            for (var i = index; i >= 0; i--)
            {
                for (var j = 1; j <= i; j++)
                {
                    var val1 = (int)components[j - 1];
                    var val2 = (int)components[j];

                    if (val1 > val2)
                    {
                        var temp = val1;
                        components[j - 1] = val2;
                        components[j] = temp;
                    }
                }
            }
        }

        int IndexOf_Int(int number)
        {
            for (var i = 0; i <= index; ++i)
            {
                if (number == (int)components[i])
                {
                    return i;
                }
            }
            return -1;
        }

        int IndexOf_String(string str)
        {
            for (var i = 0; i <= index; ++i)
            {
                if (str == (string)components[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public bool Contains(object obj)
        {
            return IndexOf(obj) != -1;
        }

        public int IndexOf(object obj)
        {
            // primitive type이면 별도로 생성해줘야 함
            // 일단 자주 쓸 것 같은 두개만 만듬
            var typeName = obj.GetType().Name;
            switch (typeName)
            {
                case "Int32": return IndexOf_Int((int)obj);
                case "String": return IndexOf_String((string)obj);
                default: break;
            }

            for (var i = 0; i <= index; ++i)
            {
                if (obj == components[i])
                {
                    return i;
                }
            }
            return -1;
        }

        public object At(int i)
        {
            return components[i];
        }

        public void Clear()
        {
            components = new object[jump];
            scaled = 1;
            index = -1;
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

        public object[] GetRange(int startIndex, int endIndex)
        {
            var l = new object[endIndex - startIndex + 1];
            for (int i = startIndex, j = 0; i <= endIndex; i++, j++)
            {
                l[j] = components[i];
            }
            return l;
        }

        public object RemoveAt(int removeIndex)
        {
            ResizeIfNeeded(false);

            Debug.Log("RemoveIndex : " + removeIndex);
            var r = components[removeIndex];
            for (var i = removeIndex; i < components.Length - 1; ++i)
            {
                components[i] = components[i + 1];
            }
            --index;
            return r;
        }

        public int Count()
        {
            return index + 1;
        }

        int EstimatedLength(int i)
        {
            return ((i / jump) + 1) * jump;
        }

        public void Start()
        {
        }
    }
}