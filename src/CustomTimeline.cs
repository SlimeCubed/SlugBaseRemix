using System;
using System.Collections.Generic;
using System.Linq;
using Timeline = SlugcatStats.Timeline;

namespace SlugBase
{
    /// <summary>
    /// A timeline added by SlugBase.
    /// </summary>
    public class CustomTimeline
    {
        /// <summary>
        /// Stores all registered <see cref="CustomTimeline"/>s.
        /// </summary>
        public static JsonRegistry<Timeline, CustomTimeline> Registry { get; } = new((key, json) => new(key, json));

        /// <summary>
        /// All registered <see cref="CustomTimeline"/>s to be added to <see cref="SlugcatStats.SlugcatTimelineOrder"/>.
        /// </summary>
        /// <remarks>
        /// This list is topologically sorted. Each timeline may reference those ahead of it in <see cref="InsertBefore"/> or <see cref="InsertAfter"/>.
        /// </remarks>
        internal static List<CustomTimeline> OrderedTimelines
        {
            get
            {
                orderedTimelines ??= GetOrderedTimelines();
                return orderedTimelines;
            }
        }

        /// <summary>
        /// Reload the order of custom timelines text time <see cref="OrderedTimelines"/> is accessed.
        /// </summary>
        internal static void ReloadTimelineOrder()
        {
            orderedTimelines = null;
        }

        private static List<CustomTimeline> orderedTimelines;

        private static List<CustomTimeline> GetOrderedTimelines()
        {
            // Get all new timelines
            var customTimelines = Registry.Values
                .Where(timeline => timeline.InsertBefore.Length > 0 || timeline.InsertAfter.Length > 0)
                .OrderBy(timeline => timeline.ID.value, StringComparer.InvariantCulture)
                .ToList();

            if (customTimelines.Any())
            {
                // Gather a list of references to custom timelines per custom timeline
                var dependencies = new Dictionary<CustomTimeline, HashSet<CustomTimeline>>();
                foreach (var timeline in customTimelines)
                {
                    var deps = new HashSet<CustomTimeline>();
                    foreach (var dep in timeline.InsertAfter.Concat(timeline.InsertBefore))
                    {
                        if (Registry.TryGet(dep, out var customDep))
                            deps.Add(customDep);
                    }
                    dependencies[timeline] = deps;
                }

                // Sort timelines so that each only references custom timelines in front of it
                customTimelines = BepInEx.Utility.TopologicalSort(customTimelines, timeline => dependencies[timeline]).ToList();
            }

            return customTimelines;
        }

        /// <summary>
        /// This timeline's unique ID.
        /// </summary>
        public Timeline ID { get; }

        /// <summary>
        /// An array of timelines this timeline inherits from.
        /// </summary>
        /// <remarks>
        /// If a file specific to this timeline isn't found, these are checked in order before using the default.
        /// </remarks>
        public Timeline[] Base { get; set; }

        /// <summary>
        /// The timeline that comes after this.
        /// </summary>
        /// <remarks>
        /// When determining the order of timelines, this one will be inserted immediately before
        /// the first registered element in <see cref="InsertBefore"/>.
        /// </remarks>
        /// <seealso cref="InsertAfter"/>
        public Timeline[] InsertBefore { get; set; }

        /// <summary>
        /// The timeline that comes before this.
        /// </summary>
        /// <remarks>
        /// When determining the order of timelines, this one will be inserted immediately after
        /// the first registered element in <see cref="InsertBefore"/>.
        /// </remarks>
        /// <seealso cref="InsertAfter"/>
        public Timeline[] InsertAfter { get; set; }

        /// <summary>
        /// All valid options to use for timeline inheritance, starting with <see cref="ID"/> and continuing through <see cref="Base"/>.
        /// This includes parent timelines from <see cref="CustomTimeline"/>s this inherits from.
        /// </summary>
        internal List<Timeline> Priorities => AddFlattenedPriorities(new List<Timeline>());

        private CustomTimeline(Timeline timeline, JsonObject json)
        {
            ID = timeline;

            if (json.TryGet("base") is JsonAny baseNames)
                Base = GetTimelines(baseNames);
            else
                Base = new Timeline[0];

            InsertBefore = new Timeline[0];
            InsertAfter = new Timeline[0];

            if (json.TryGet("insert_before") is JsonAny beforeNames)
            {
                if (json.TryGet("insert_after") != null)
                    throw new JsonException("Only one of \"insert_before\" or \"insert_after\" may be specified!", json.Get("insert_after"));

                InsertBefore = GetTimelines(beforeNames);
            }
            else if (json.TryGet("insert_after") is JsonAny afterNames)
            {
                InsertAfter = GetTimelines(afterNames);
            }
        }

        /// <summary>
        /// List all timelines this inherits from. Unlike <see cref="Priorities"/>,
        /// this includes <see cref="Base"/> timelines from parents.
        /// </summary>
        private List<Timeline> AddFlattenedPriorities(List<Timeline> prios)
        {
            if (prios.Contains(ID))
                return prios;

            prios.Add(ID);

            foreach (var prio in Base)
            {
                if ((int)prio != -1)
                {
                    if (Registry.TryGet(prio, out var customPrio))
                        customPrio.AddFlattenedPriorities(prios);
                    else if (!prios.Contains(prio))
                        prios.Add(prio);
                }
            }

            return prios;
        }

        /// <summary>
        /// Check if <paramref name="parent"/> is in the list of base timelines.
        /// </summary>
        public bool InheritsFrom(Timeline parent)
        {
            return parent == ID
                || Array.IndexOf(Base, parent) >= 0;
        }

        /// <summary>
        /// Converts a string or list of strings to a list of timelines.
        /// </summary>
        private static Timeline[] GetTimelines(JsonAny json)
        {
            if (json.TryList() is JsonList list)
            {
                var timelines = new Timeline[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    timelines[i] = GetTimeline(list.GetString(i));
                }
                return timelines;
            }
            else
            {
                return new Timeline[] { GetTimeline(json.AsString()) };
            }
        }

        /// <summary>
        /// Converts a string to a timeline, ignoring case.
        /// </summary>
        private static Timeline GetTimeline(string name)
        {
            return new Timeline(Utils.MatchCaseInsensitiveEnum<Timeline>(name));
        }
    }
}
