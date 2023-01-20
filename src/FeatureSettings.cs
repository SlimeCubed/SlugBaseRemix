using System;
using System.Collections.Generic;

namespace SlugBase
{
    /// <summary>
    /// Properties passed to a feature instance upon creation.
    /// </summary>
    public class FeatureSettings
    {
        /// <summary>
        /// Named properties passed to the feature, typically parsed from a JSON file.
        /// </summary>
        public Dictionary<string, object> Properties { get; }

        /// <summary>
        /// Creates a new instance of <see cref="FeatureSettings"/> from a dictionary of named properties.
        /// </summary>
        /// <param name="json">Properties of any type associated with string keys.</param>
        public FeatureSettings(Dictionary<string, object> json)
        {
            Properties = json ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// Fetches a property from the feature settings.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to return if the property is not found.</param>
        /// <returns>The fetched property, or <paramref name="defaultValue"/> if it was not found or is not assignable to <typeparamref name="T"/>.</returns>
        public T GetProperty<T>(string key, T defaultValue)
        {
            if (Properties.TryGetValue(key, out object val))
            {
                if(val is T valT)
                    return valT;

                try
                {
                    return (T)Convert.ChangeType(val, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            else
                return defaultValue;
        }
    }
}
