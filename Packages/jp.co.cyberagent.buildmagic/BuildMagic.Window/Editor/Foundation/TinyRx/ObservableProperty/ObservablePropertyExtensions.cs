// --------------------------------------------------------------
// Copyright 2023 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagic.Window.Editor.Foundation.TinyRx.ObservableProperty
{
    public static class ObservablePropertyExtensions
    {
        public static ReadOnlyObservableProperty<T> ToReadOnly<T>(this IObservableProperty<T> self) => new(self);
    }
}
