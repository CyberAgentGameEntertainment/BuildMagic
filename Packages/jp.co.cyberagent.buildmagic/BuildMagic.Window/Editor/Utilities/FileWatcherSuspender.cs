// --------------------------------------------------------------
// Copyright 2025 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.IO;

namespace BuildMagic.Window.Editor.Utilities
{
    public class FileWatcherSuspender : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly bool _enabledOnEntry;

        public FileWatcherSuspender(FileSystemWatcher watcher)
        {
            _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
            _enabledOnEntry = _watcher.EnableRaisingEvents;
            
            if (_enabledOnEntry)
                _watcher.EnableRaisingEvents = false;
        }
        
        public void Dispose()
        {
            if (_watcher != null)
                _watcher.EnableRaisingEvents = _enabledOnEntry;
        }
    }
}
