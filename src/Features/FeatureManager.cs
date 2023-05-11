using SlugBase.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SlugBase.Features
{
    internal static class FeatureManager
    {
        private static readonly Dictionary<string, Feature> _features = new();
        private static readonly List<string> _queuedErrors = new();

        public static void Register(Feature feature)
        {
            if (feature == null)
                throw new ArgumentNullException(nameof(Feature));

            string id = feature.ID;
            if(!_features.ContainsKey(id))
            {
                _features.Add(id, feature);
            }
            else
            {
                // This is often called from a plugin's cctor, so it is important that errors are not thrown
                string msg = $"Duplicate feature: {id}!";

                var trace = new StackTrace(false);
                for(int i = 0; i < trace.FrameCount; i++)
                {
                    var method = trace.GetFrame(i).GetMethod();
                    var asm = method?.ReflectedType?.Assembly;
                    if(asm != typeof(RainWorld).Assembly && asm != typeof(SlugBasePlugin).Assembly)
                    {
                        msg += $"\nRegistered by: {method.ReflectedType.Name} in {asm.GetName().Name}.dll";
                        break;
                    }
                }

                SlugBasePlugin.Logger.LogDebug(msg);

                if (ErrorList.Instance != null)
                {
                    ErrorList.Instance.AddError(ErrorList.ErrorIcon.Plugin, msg, null, null);
                }
                else
                {
                    _queuedErrors.Add(msg);
                }
            }
        }

        public static bool TryGetFeature(string id, out Feature feature)
        {
            return _features.TryGetValue(id, out feature);
        }

        public static void LogErrors()
        {
            foreach (var msg in _queuedErrors)
            {
                ErrorList.Instance.AddError(ErrorList.ErrorIcon.Plugin, msg, null, null);
            }
            _queuedErrors.Clear();
        }
    }
}
