using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace AiCodo.Codes
{
    public class ConfigFileWatchCommands
    {
        Dictionary<string, List<Action<string>>> _FileActions = new Dictionary<string, List<Action<string>>>();

        Dictionary<string, DateTime> _LastChangedFiles = new Dictionary<string, DateTime>();

        const int _ChangedDelayMS = 1000;

        object _Lock = new object();

        bool _IsChanging = false;

        string _ConfigRoot = ApplicationConfig.LocalConfigFolder;

        public ConfigFileWatchCommands() { }

        public ConfigFileWatchCommands StartWatch()
        {
            FileSystemWatcher _ConfigWatcher = new FileSystemWatcher(_ConfigRoot);
            _ConfigWatcher.Deleted += _ConfigWatcher_Deleted;
            _ConfigWatcher.Created += _ConfigWatcher_Created;
            _ConfigWatcher.Changed += _ConfigWatcher_Changed;
            _ConfigWatcher.EnableRaisingEvents = true;
            return this;
        }

        public ConfigFileWatchCommands AddAction(string fileName, Action<string> action)
        {
            if (!_FileActions.TryGetValue(fileName.ToLower(), out List<Action<string>> actions))
            {
                actions = new List<Action<string>>();
                _FileActions[fileName.ToLower()] = actions;
            }
            actions.Add(action);
            return this;
        }

        private void _ConfigWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            CheckChanged(e);
        }

        private void _ConfigWatcher_Created(object sender, FileSystemEventArgs e)
        {
            CheckChanged(e);
        }

        private void CheckChanged(FileSystemEventArgs e)
        {
            var fileName = e.FullPath.ToLower();
            if (IsFileWatching(fileName))
            {
                lock (_Lock)
                {
                    _LastChangedFiles[fileName] = DateTime.Now;
                }
                CheckStartChanging();
            }
        }

        private void _ConfigWatcher_Deleted(object sender, FileSystemEventArgs e)
        {

        }

        private void CheckStartChanging()
        {
            if (_IsChanging) return;
            lock (_Lock)
            {
                if (_IsChanging) return;
                _IsChanging = true;
                Threads.StartNew(() =>
                {
                    while (_IsChanging)
                    {
                        lock (_Lock)
                        {
                            foreach (var item in _LastChangedFiles.ToList())
                            {
                                if (item.Value.AddMilliseconds(_ChangedDelayMS) > DateTime.Now)
                                    continue;
                                try
                                {
                                    RunFileActions(item.Key);
                                    _LastChangedFiles.Remove(item.Key);
                                }
                                catch (Exception ex)
                                {
                                    ex.WriteErrorLog();
                                }
                            }
                        }
                        Thread.Sleep(500);
                        if (_LastChangedFiles.Count == 0)
                        {
                            lock (_Lock)
                            {
                                if (_LastChangedFiles.Count == 0)
                                {
                                    _IsChanging = false;
                                    break;
                                }
                            }
                        }
                    }
                });
            }
        }

        private bool IsFileWatching(string fileName)
        {
            var subFile = fileName.Substring(_ConfigRoot.Length).TrimStart('\\').ToLower();
            return _FileActions.ContainsKey(subFile);
        }

        private void RunFileActions(string fileName)
        {
            if (CodeCommands.IsRunning)
            {
                return;
            }

            if (fileName.IsFileExists())
            {
                var subFile = fileName.Substring(_ConfigRoot.Length).TrimStart('\\').ToLower();
                if (_FileActions.TryGetValue(subFile, out List<Action<string>> actions))
                {
                    foreach (var action in actions)
                    {
                        try
                        {
                            action(fileName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
        }
    }
}
