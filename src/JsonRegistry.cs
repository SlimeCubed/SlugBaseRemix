using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// Represents a collection of values with unique IDs that can be loaded from JSON.
    /// </summary>
    /// <typeparam name="TKey">The type of the <see cref="ExtEnum{T}"/> keys.</typeparam>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    public class JsonRegistry<TKey, TValue>
        where TKey : ExtEnum<TKey>
    {
        private readonly Dictionary<TKey, Entry> _entries = new();
        private readonly Func<TKey, JsonObject, TValue> _fromJson;
        private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
        private readonly ConcurrentQueue<string> _reloadQueue = new();
        private bool _reloadOnChange;

        /// <summary>
        /// Occurs when a loaded JSON file is modified while <see cref="WatchForChanges"/> is set.
        /// </summary>
        public event EventHandler<ReloadedEventArgs> EntryReloaded;

        /// <summary>
        /// Occurs when a JSON file fails to load or reload.
        /// </summary>
        public event EventHandler<LoadErrorEventArgs> LoadFailed;

        /// <summary>
        /// Whether this registry should track files to reload.
        /// Call <see cref="ReloadChangedFiles"/> to apply changes.
        /// </summary>
        public bool WatchForChanges
        {
            get => _reloadOnChange;
            set
            {
                _reloadOnChange = value;
                if(!value)
                {
                    while (_reloadQueue.TryDequeue(out _)) ;
                    foreach (var watcher in _watchers.Values)
                        watcher.Dispose();
                    _watchers.Clear();
                }
            }
        }

        /// <summary>
        /// A collection of all keys used by this registry.
        /// </summary>
        public IEnumerable<TKey> Keys => _entries.Keys;

        /// <summary>
        /// A collection of all values registered.
        /// </summary>
        public IEnumerable<TValue> Values => _entries.Values.Select(entry => entry.Value);

        /// <summary>
        /// Create a new registry.
        /// </summary>
        /// <param name="fromJson">The factory that creates values from JSON.</param>
        public JsonRegistry(Func<TKey, JsonObject, TValue> fromJson)
        {
            _fromJson = fromJson;
        }

        /// <summary>
        /// Register all JSON files in a directory using <see cref="AddFromFile(string)"/>.
        /// </summary>
        /// <param name="directory">The directory to search.</param>
        public void ScanDirectory(string directory)
        {
            var files = AssetManager.ListDirectory(directory, includeAll: true);

            foreach (var file in files.Where(file => file.EndsWith(".json")))
            {
                TryAddFromFile(file);
            }
        }

        /// <summary>
        /// Parse a file as JSON and link it to a new <see cref="ExtEnum{T}"/> ID.
        /// </summary>
        /// <param name="path">The file path to the json.</param>
        /// <returns>The registered key and value.</returns>
        public KeyValuePair<TKey, TValue> AddFromFile(string path)
        {
            path = path.Replace('/', Path.DirectorySeparatorChar);

            // Unload the old entry loaded from this file path
            foreach (var pair in _entries)
            {
                if (pair.Value.Path != null && pair.Value.Path.Equals(path, StringComparison.InvariantCultureIgnoreCase))
                {
                    Remove(pair.Key);
                    break;
                }
            }

            var value = Add(path, JsonAny.Parse(File.ReadAllText(path)).AsObject());

            // Watch file system for edits
            if (WatchForChanges)
            {
                var dir = Path.GetDirectoryName(path);
                if (!_watchers.ContainsKey(dir))
                {
                    var watcher = new FileSystemWatcher(dir, "*.json");
                    watcher.Changed += FileChanged;
                    watcher.EnableRaisingEvents = true;
                    _watchers.Add(dir, watcher);
                }
            }

            SlugBasePlugin.Logger.LogDebug($"Added SlugBase object from {path}");

            return value;
        }

        /// <summary>
        /// Parse a file as JSON and link it to a new <see cref="ExtEnum{T}"/> ID.
        /// Load errors can be monitored with <see cref="LoadFailed"/>.
        /// </summary>
        /// <param name="path">The file path to the json.</param>
        /// <returns>The registered key and value, or <c>null</c> if loading failed.</returns>
        public KeyValuePair<TKey, TValue>? TryAddFromFile(string path)
        {
            try
            {
                return AddFromFile(path);
            }
            catch (Exception e)
            {
                string message;
                if (e is JsonException jsonException)
                {
                    message = $"{jsonException.Message}\nField: {jsonException.JsonPath ?? "unknown"}";
                }
                else if (e is JsonParseException parseException)
                {
                    message = $"{parseException.Message}\nLine: {parseException.Line + 1}";
                }
                else
                {
                    message = e.Message;
                }

                LoadFailed?.Invoke(this, new LoadErrorEventArgs(message, e, path));

                return null;
            }
        }

        /// <summary>
        /// Load a <typeparamref name="TValue"/> from JSON and link it to a new <see cref="ExtEnum{T}"/> ID.
        /// </summary>
        /// <param name="json">The json data for the new object.</param>
        /// <returns>The registered key and value.</returns>
        public KeyValuePair<TKey, TValue> Add(JsonObject json) => Add(null, json);

        private KeyValuePair<TKey, TValue> Add(string path, JsonObject json)
        {
            var key = ClaimID(json.GetString("id"));

            try
            {
                var entry = new Entry(path, _fromJson(key, json));
                _entries[key] = entry;
                return new(key, entry.Value);
            }
            catch
            {
                key.Unregister();
                throw;
            }
        }

        /// <summary>
        /// Unregister a value.
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void Remove(TKey id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            if (!_entries.ContainsKey(id))
                throw new ArgumentException($"ID \"{id}\" is not a value of {typeof(TKey).FullName}");

            if (!_entries.Remove(id))
                throw new ArgumentException($"ID \"{id}\" from {typeof(TKey).FullName} was not in the registry!");

            id.Unregister();
        }

        /// <summary>
        /// Get a registered value by key.
        /// </summary>
        /// <param name="id">The unique ID of the value.</param>
        /// <param name="value">The registered value with the unique ID.</param>
        /// <returns><c>true</c> if the value was found, <c>false</c> otherwise.</returns>
        public bool TryGet(TKey id, out TValue value)
        {
            if(id != null && _entries.TryGetValue(id, out var entry))
            {
                value = entry.Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// Get a registered value by key.
        /// </summary>
        /// <param name="id">The unique ID of the value.</param>
        /// <returns>The registered value with the unique ID.</returns>
        public TValue GetOrDefault(TKey id)
        {
            return id != null && TryGet(id, out var value) ? value : default;
        }

        /// <summary>
        /// Gets the path to a registered value by key.
        /// </summary>
        /// <param name="id">The unique ID of the value.</param>
        /// <param name="path">The path to the value's JSON file.</param>
        /// <returns><c>true</c> if the value was found and has a path, <c>false</c> otherwise.</returns>
        public bool TryGetPath(TKey id, out string path)
        {
            if (id != null && _entries.TryGetValue(id, out var entry))
            {
                path = entry.Path;
                return true;
            }
            else
            {
                path = default;
                return false;
            }
        }

        /// <summary>
        /// Reload all JSON files that have been modified.
        /// </summary>
        public void ReloadChangedFiles()
        {
            while(_reloadQueue.TryDequeue(out string path))
            {
                var pair = TryAddFromFile(path);

                if(pair != null)
                {
                    try
                    {
                        EntryReloaded?.Invoke(this, new(pair.Value.Key, pair.Value.Value));
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            _reloadQueue.Enqueue(e.FullPath);
        }

        private TKey ClaimID(string id)
        {
            if (ExtEnum<TKey>.values.entries.Contains(id))
                throw new ArgumentException($"ID \"{id}\" from {typeof(TKey).FullName} is already in use!");

            var claimedID = (TKey)Activator.CreateInstance(typeof(TKey), id, true);

            // Update existing ExtEnum with the new index
            ExtEnum<TKey>.valuesVersion++;

            return claimedID;
        }

        private readonly struct Entry
        {
            public readonly string Path;
            public readonly TValue Value;

            public Entry(string path, TValue value)
            {
                Path = path;
                Value = value;
            }
        }

        /// <summary>
        /// Provides data for the <see cref="EntryReloaded"/> event.
        /// </summary>
        public class ReloadedEventArgs : EventArgs
        {
            /// <summary>
            /// The unique key of the reloaded entry.
            /// </summary>
            public TKey Key { get; }

            /// <summary>
            /// The reloaded entry.
            /// </summary>
            public TValue Value { get; }

            internal ReloadedEventArgs(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        /// <summary>
        /// Provides data for the <see cref="LoadFailed"/> event.
        /// </summary>
        public class LoadErrorEventArgs : EventArgs
        {
            /// <summary>
            /// The exception that caused this event, or <c>null</c> if it was not from an exception.
            /// </summary>
            public Exception Exception { get; }

            /// <summary>
            /// An error message that may be shown to the user.
            /// </summary>
            public string ErrorMessage { get; }

            /// <summary>
            /// The path to the file that errored, or <c>null</c> if it was not loaded from a file.
            /// </summary>
            public string Path { get; }

            internal LoadErrorEventArgs(Exception exception, string path) : this(exception.Message, exception, path)
            {
            }

            internal LoadErrorEventArgs(string message, Exception exception, string path)
            {
                ErrorMessage = message;
                Exception = exception;
                Path = path;
            }
        }
    }
}
