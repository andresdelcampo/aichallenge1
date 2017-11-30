//**********************************************************************************************
// File:   CircList.cs
// Author: Andrés del Campo Novales
//
// This is a standard circular list class.
//**********************************************************************************************
//**********************************************************************************************
// Copyright 2017 Andrés del Campo Novales
//
// This file is part of CSLearner.

// CSLearner is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// CSLearner is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with CSLearner.  If not, see<http://www.gnu.org/licenses/>.
//**********************************************************************************************

using System;
using System.Collections.Generic;

namespace CSLearnerLib
{
    [Serializable()]
    public class CircList<T>
    {
        private readonly List<T> list;
        private int listIdx;
        private int usedItems;

        public int Length => usedItems;


        //**********************************************************************************************
        // Constructor
        //**********************************************************************************************
        public CircList(int size)
        {
            list = new List<T>(size);
            usedItems = 0;
            listIdx = 0;
            for (int i = 0; i < size; i++)
            {
                list.Add(default(T));
            }
        }

        //**********************************************************************************************
        // Copy
        //**********************************************************************************************
        public CircList<T> Copy()
        {
            var newList = new CircList<T>(list.Count)
            {
                listIdx = listIdx,
                usedItems = usedItems
            };
            return newList;
        }

        //**********************************************************************************************
        // Add
        //**********************************************************************************************
        public void Add(T item)
        {
            listIdx = ++listIdx % list.Count;
            list[listIdx] = item;
            if (usedItems < list.Count)
                usedItems++;
        }

        //**********************************************************************************************
        // Remove
        //**********************************************************************************************
        public T Remove()
        {
            if (usedItems > 0)
            {
                T item = list[listIdx];
                list[listIdx] = default(T);
                listIdx = (--listIdx + list.Count) % list.Count;
                usedItems--;
                return item;
            }
            else
                return default(T);
        }

        //**********************************************************************************************
        // Indexer []
        //**********************************************************************************************
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= list.Count)
                    throw new InvalidOperationException("Requested index out of bounds in CircList");

                // 0 will be the last Add(ed) value, 1 the previous, etc. 
                return list[(listIdx - index + list.Count) % list.Count];
            }
            set
            {
                if (index < 0 || index >= list.Count)
                    throw new InvalidOperationException("Requested index out of bounds in CircList");

                // 0 will be the last Add(ed) value, 1 the previous, etc. 
                list[(listIdx - index + list.Count) % list.Count] = value;
            }
        }

        //**********************************************************************************************
        // GetLastAdded
        //**********************************************************************************************
        public T GetLastAdded()
        {
            return list[listIdx];
        }

        //**********************************************************************************************
        // Contains
        //**********************************************************************************************
        public bool Contains(T t)
        {
            return list.Contains(t);
        }

    }
}
