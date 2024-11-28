// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

namespace BuildMagicEditor
{
    public abstract class BuildTaskBuilderBase<TTask, TValue> : IBuildTaskBuilder<TTask, TValue>
        where TTask : IBuildTask
    {
        IBuildTask IBuildTaskBuilder.Build(object value)
        {
            return Build((TValue)value);
        }

        public abstract TTask Build(TValue value);
    }
}
