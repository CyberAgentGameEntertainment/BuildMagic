﻿// --------------------------------------------------------------
// Copyright 2023 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagic.Window.Editor.Foundation.TinyRx.ObservableCollection
{
    public readonly struct ListAddEvent<TValue>
    {
        public readonly int Index;
        public readonly TValue Value;

        public ListAddEvent(int index, TValue value)
        {
            Index = index;
            Value = value;
        }
    }

    public readonly struct ListRemoveEvent<TValue>
    {
        public readonly int Index;
        public readonly TValue Value;

        public ListRemoveEvent(int index, TValue value)
        {
            Index = index;
            Value = value;
        }
    }

    public readonly struct ListReplaceEvent<TValue>
    {
        public readonly int Index;
        public readonly TValue OldValue;
        public readonly TValue NewValue;

        public ListReplaceEvent(int index, TValue oldValue, TValue newValue)
        {
            Index = index;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
