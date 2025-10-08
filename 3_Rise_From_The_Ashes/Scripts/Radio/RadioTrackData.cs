using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace Rise.Radio
{
    /// <summary>
    /// Handles radio track data and playlist management
    /// </summary>
    public struct Track
    {
        public string name;
        public string file;
        public string category;
        public string days;
    }

    public class RadioTrackData
    {
        private static RadioTrackData _instance;
        
        private List<Track> music = new List<Track>();
        private List<Track> podcasts = new List<Track>();
        private List<Track> news = new List<Track>();
        private List<Track> marketplace = new List<Track>();
        private List<Track> weather = new List<Track>();
        private List<Track> talkshows = new List<Track>();
        private List<Track> emergency = new List<Track>();
        private List<Track> misc = new List<Track>();
        private List<Track> dj = new List<Track>();
        private List<Track> allFiles = new List<Track>();

        private bool loaded = false;

        public static RadioTrackData Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new RadioTrackData();
                return _instance;
            }
        }

        private RadioTrackData()
        {
        }

        /// <summary>
        /// Loads radio track data from XML configuration
        /// </summary>
        public void LoadXmlRadioData()
        {
            try
            {
                Log.Out("Loading XML Radio Data...");
                RadioDebug.D("TRACKS", "LoadXmlRadioData");

                string stationsXmlPath = GameIO.GetGameDir("Data/Config") + "/stations.xml";
                Log.Out($"Checking for stations.xml at: {stationsXmlPath}");

                if (!SdFile.Exists(stationsXmlPath))
                {
                    Log.Out("stations.xml not found in Data/Config, checking Mods directory...");
                    
                    // Try to find in mods directory
                    string modsPath = GameIO.GetGameDir("Mods");
                    string[] modDirectories = SdDirectory.GetDirectories(modsPath);
                    
                    foreach (string modDir in modDirectories)
                    {
                        string modStationsPath = modDir + "/Stations/stations.xml";
                        if (SdFile.Exists(modStationsPath))
                        {
                            stationsXmlPath = modStationsPath;
                            Log.Out($"Found stations.xml in mod directory: {stationsXmlPath}");
                            break;
                        }
                    }
                }

                if (!SdFile.Exists(stationsXmlPath))
                {
                    Log.Out("stations.xml not found anywhere. Creating fallback tracks...");
                    CreateFallbackTracks();
                    return;
                }

                XDocument xdoc = XDocument.Load(stationsXmlPath);
                Log.Out("XML Loaded Successfully");

                // Clear existing data
                ClearAllTracks();

                foreach (XElement station in xdoc.Descendants("station"))
                {
                    string stationName = station.Attribute("name")?.Value ?? "Unknown";
                    Log.Out($"Processing station: {stationName}");

                    foreach (XElement track in station.Descendants("track"))
                    {
                        Track newTrack = new Track
                        {
                            name = track.Attribute("name")?.Value ?? "",
                            file = track.Attribute("file")?.Value ?? "",
                            category = track.Attribute("category")?.Value ?? "misc",
                            days = track.Attribute("days")?.Value ?? "0-999"
                        };

                        if (!string.IsNullOrEmpty(newTrack.name))
                        {
                            AddTrackToCategory(newTrack);
                            allFiles.Add(newTrack);
                        }
                    }
                }

                Log.Out($"Total tracks loaded: {allFiles.Count}");
                LogCategoryCounts();
                loaded = true;
            }
            catch (Exception e)
            {
                Log.Out($"Error loading XML radio data: {e.Message}");
                Log.Out($"Stack trace: {e.StackTrace}");
                CreateFallbackTracks();
            }
        }

        private void AddTrackToCategory(Track track)
        {
            switch (track.category.ToLower())
            {
                case "music":
                    music.Add(track);
                    break;
                case "podcast":
                case "podcasts":
                    podcasts.Add(track);
                    break;
                case "news":
                    news.Add(track);
                    break;
                case "marketplace":
                    marketplace.Add(track);
                    break;
                case "weather":
                    weather.Add(track);
                    break;
                case "talkshow":
                case "talkshows":
                    talkshows.Add(track);
                    break;
                case "emergency":
                    emergency.Add(track);
                    break;
                case "dj":
                    dj.Add(track);
                    break;
                default:
                    misc.Add(track);
                    break;
            }
        }

        private void CreateFallbackTracks()
        {
            Log.Out("Creating fallback radio tracks...");
            RadioDebug.D("TRACKS", "CreateFallbackTracks");
            
            // Instead of hardcoded tracks, check if we can load from audio system
            // This allows for dynamic track discovery at runtime
            try
            {
                // Try to discover available audio tracks from the game's audio system
                var discoveredTracks = DiscoverAudioTracks();
                
                if (discoveredTracks.Count > 0)
                {
                    Log.Out($"Discovered {discoveredTracks.Count} audio tracks from game system");
                    
                    foreach (var track in discoveredTracks)
                    {
                        AddTrackToCategory(track);
                        allFiles.Add(track);
                    }
                }
                else
                {
                    Log.Out("No audio tracks discovered, creating minimal fallback");
                    CreateMinimalFallback();
                }
                
                Log.Out($"Created {allFiles.Count} fallback tracks");
                loaded = true;
            }
            catch (Exception e)
            {
                Log.Out($"Error in dynamic track creation: {e.Message}");
                CreateMinimalFallback();
                loaded = true;
            }
        }

        /// <summary>
        /// Attempts to discover available audio tracks from the game's audio system
        /// </summary>
        private List<Track> DiscoverAudioTracks()
        {
            var discoveredTracks = new List<Track>();
            
            try
            {
                Log.Out("Attempting to discover audio tracks from game system...");
                
                // Check if Manager.audioData contains our radio tracks
                if (Audio.Manager.audioData != null)
                {
                    var audioKeys = Audio.Manager.audioData.Keys.ToList();
                    Log.Out($"Found {audioKeys.Count} audio entries in Manager.audioData");
                    
                    // Look for tracks that match radio naming patterns
                    foreach (string audioKey in audioKeys)
                    {
                        if (IsRadioTrack(audioKey))
                        {
                            Track discoveredTrack = new Track
                            {
                                name = audioKey,
                                file = audioKey, // Use the same key for file reference
                                category = "music", // Default category, can be refined
                                days = "0-999" // Available all days
                            };
                            
                            discoveredTracks.Add(discoveredTrack);
                            Log.Out($"Discovered radio track: {audioKey}");
                        }
                    }
                }
                
                // Also check for specifically named radio audio groups
                string[] radioAudioGroups = { "riseRadio", "radioMusic", "radioTrack", "music" };
                
                foreach (string groupName in radioAudioGroups)
                {
                    if (Audio.Manager.audioData != null && Audio.Manager.audioData.ContainsKey(groupName))
                    {
                        // Create a track entry for this audio group
                        Track groupTrack = new Track
                        {
                            name = $"RadioTrack_{groupName}",
                            file = groupName,
                            category = "music",
                            days = "0-999"
                        };
                        
                        // Avoid duplicates
                        if (!discoveredTracks.Any(t => t.file == groupName))
                        {
                            discoveredTracks.Add(groupTrack);
                            Log.Out($"Discovered radio audio group: {groupName}");
                        }
                    }
                }
                
                Log.Out($"Audio discovery completed. Found {discoveredTracks.Count} radio tracks.");
            }
            catch (Exception e)
            {
                Log.Out($"Error during audio track discovery: {e.Message}");
            }
            
            return discoveredTracks;
        }

        /// <summary>
        /// Determines if an audio key represents a radio track
        /// </summary>
        private bool IsRadioTrack(string audioKey)
        {
            if (string.IsNullOrEmpty(audioKey)) return false;
            
            // Define patterns that indicate radio tracks
            string[] radioPatterns = { 
                "radio", "music", "song", "track", "rise", 
                "blood_moon", "chasing", "break_through", "night_of_the_undead",
                "fist_fighting", "machete", "zombie", "survivors", "apocalypse" 
            };
            
            string lowerKey = audioKey.ToLower();
            
            // Check if the key contains any radio-related patterns
            foreach (string pattern in radioPatterns)
            {
                if (lowerKey.Contains(pattern.ToLower()))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Creates a minimal fallback when no tracks can be discovered
        /// </summary>
        private void CreateMinimalFallback()
        {
            Log.Out("Creating minimal fallback track entries...");
            
            // Create placeholder tracks that reference known audio groups
            // These will be resolved at runtime by the PlaylistManager
            Track defaultTrack = new Track
            {
                name = "DefaultRadioTrack",
                file = "riseRadio", // This should match your audio group in sounds.xml
                category = "music",
                days = "0-999"
            };
            
            music.Add(defaultTrack);
            allFiles.Add(defaultTrack);
            
            Log.Out("Created minimal fallback with default radio track reference");
        }

        /// <summary>
        /// Refreshes the track data by reloading from XML or rediscovering audio tracks
        /// Called by PlaylistManager when dynamic reloading is needed
        /// </summary>
        public void RefreshTrackData()
        {
            try
            {
                Log.Out("Refreshing radio track data...");
                
                // Clear existing data
                ClearAllTracks();
                loaded = false;
                
                // Attempt to reload
                LoadXmlRadioData();
                
                Log.Out($"Track data refreshed. Total tracks: {allFiles.Count}");
            }
            catch (Exception e)
            {
                Log.Out($"Error refreshing track data: {e.Message}");
            }
        }

        /// <summary>
        /// Gets tracks dynamically filtered by category and day, with runtime validation
        /// </summary>
        public List<Track> GetDynamicTracksForDay(int currentDay, string category = null)
        {
            var validTracks = new List<Track>();
            
            Log.Out($"=== Getting dynamic tracks for day {currentDay}, category: {category ?? "all"} ===");
            RadioDebug.D("TRACKS", $"GetDynamic day={currentDay} cat={(category ?? "<all>")}");
            
            // Ensure data is loaded
            if (!loaded)
            {
                LoadXmlRadioData();
            }
            
            var sourceList = string.IsNullOrEmpty(category) ? allFiles : GetTracksByCategory(category);
            
            Log.Out($"Source tracks available: {sourceList.Count}");
            
            foreach (var track in sourceList)
            {
                if (IsTrackValidForDay(track, currentDay) && IsTrackAudioAvailable(track))
                {
                    validTracks.Add(track);
                }
            }
            
            Log.Out($"Dynamic tracks found for day {currentDay}: {validTracks.Count}");
            
            return validTracks;
        }

        /// <summary>
        /// Gets tracks by category for playlist management
        /// </summary>
        private List<Track> GetTracksByCategory(string category)
        {
            switch (category.ToLower())
            {
                case "music": return music;
                case "podcast": case "podcasts": return podcasts;
                case "news": return news;
                case "marketplace": return marketplace;
                case "weather": return weather;
                case "talkshow": case "talkshows": return talkshows;
                case "emergency": return emergency;
                case "dj": return dj;
                case "misc": return misc;
                default: return allFiles;
            }
        }

        /// <summary>
        /// Validates that the audio for a track is actually available in the game system
        /// </summary>
        private bool IsTrackAudioAvailable(Track track)
        {
            try
            {
                if (string.IsNullOrEmpty(track.file)) return false;
                
                // Check if the audio exists in the Manager's audio data
                if (Audio.Manager.audioData != null && Audio.Manager.audioData.ContainsKey(track.file))
                {
                    return true;
                }
                
                // Alternative check: see if we can find similar audio keys
                if (Audio.Manager.audioData != null)
                {
                    var audioKeys = Audio.Manager.audioData.Keys;
                    foreach (string key in audioKeys)
                    {
                        if (key.Contains(track.file) || track.file.Contains(key))
                        {
                            return true;
                        }
                    }
                }
                
                // Default to true to avoid breaking existing functionality
                // The actual audio validation will happen when trying to play
                return true;
            }
            catch (Exception e)
            {
                Log.Out($"Error validating audio for track {track.name}: {e.Message}");
                return true; // Default to available to avoid breaking functionality
            }
        }

        /// <summary>
        /// Adds a track dynamically at runtime (for playlist management)
        /// </summary>
        public void AddTrackDynamically(string name, string file, string category = "music", string days = "0-999")
        {
            try
            {
                Track newTrack = new Track
                {
                    name = name,
                    file = file,
                    category = category,
                    days = days
                };
                
                AddTrackToCategory(newTrack);
                allFiles.Add(newTrack);
                
                Log.Out($"Added dynamic track: {name} (file: {file}, category: {category})");
            }
            catch (Exception e)
            {
                Log.Out($"Error adding dynamic track {name}: {e.Message}");
            }
        }

        /// <summary>
        /// Removes a track dynamically at runtime (for playlist management)
        /// </summary>
        public void RemoveTrackDynamically(string name)
        {
            try
            {
                var trackToRemove = allFiles.FirstOrDefault(t => t.name == name);
                if (trackToRemove.name != null) // Check if track was found (struct default)
                {
                    allFiles.Remove(trackToRemove);
                    
                    // Remove from category list
                    var categoryList = GetTracksByCategory(trackToRemove.category);
                    categoryList.Remove(trackToRemove);
                    
                    Log.Out($"Removed dynamic track: {name}");
                }
                else
                {
                    Log.Out($"Track not found for removal: {name}");
                }
            }
            catch (Exception e)
            {
                Log.Out($"Error removing dynamic track {name}: {e.Message}");
            }
        }

        private void ClearAllTracks()
        {
            music.Clear();
            podcasts.Clear();
            news.Clear();
            marketplace.Clear();
            weather.Clear();
            talkshows.Clear();
            emergency.Clear();
            misc.Clear();
            dj.Clear();
            allFiles.Clear();
        }

        private void LogCategoryCounts()
        {
            Log.Out($"Music tracks: {music.Count}");
            Log.Out($"Podcast tracks: {podcasts.Count}");
            Log.Out($"News tracks: {news.Count}");
            Log.Out($"Marketplace tracks: {marketplace.Count}");
            Log.Out($"Weather tracks: {weather.Count}");
            Log.Out($"Talkshow tracks: {talkshows.Count}");
            Log.Out($"Emergency tracks: {emergency.Count}");
            Log.Out($"DJ tracks: {dj.Count}");
            Log.Out($"Misc tracks: {misc.Count}");
        }

        private bool IsTrackValidForDay(Track track, int currentDay)
        {
            try
            {
                // Log the track validation for debugging
                Log.Out($"Validating track '{track.name}' for day {currentDay}, track days: '{track.days}'");
                
                if (string.IsNullOrEmpty(track.days) || track.days == "0-999")
                {
                    Log.Out($"Track '{track.name}' valid - using default range");
                    return true;
                }

                string[] dayRanges = track.days.Split(',');
                
                foreach (string range in dayRanges)
                {
                    string trimmedRange = range.Trim();
                    
                    if (trimmedRange.Contains("-"))
                    {
                        string[] parts = trimmedRange.Split('-');
                        if (parts.Length == 2 && 
                            int.TryParse(parts[0], out int minDay) && 
                            int.TryParse(parts[1], out int maxDay))
                        {
                            Log.Out($"Checking range {minDay}-{maxDay} for day {currentDay}");
                            if (currentDay >= minDay && currentDay <= maxDay)
                            {
                                Log.Out($"Track '{track.name}' valid - matches range {minDay}-{maxDay}");
                                return true;
                            }
                        }
                    }
                    else if (int.TryParse(trimmedRange, out int specificDay))
                    {
                        if (currentDay == specificDay)
                        {
                            Log.Out($"Track '{track.name}' valid - matches specific day {specificDay}");
                            return true;
                        }
                    }
                }
                
                Log.Out($"Track '{track.name}' NOT valid for day {currentDay}");
            }
            catch (Exception e)
            {
                Log.Out($"Error validating track day range for {track.name}: {e.Message}");
                return true; // Default to valid if parsing fails
            }
            
            return false;
        }

        public bool IsLoaded() => loaded;
        
        public int GetTotalTrackCount() => allFiles.Count;

        // Public accessors
        public List<Track> GetAllTracks() => new List<Track>(allFiles);
        public List<Track> GetMusicTracks() => new List<Track>(music);
        public List<Track> GetPodcastTracks() => new List<Track>(podcasts);
        public List<Track> GetNewsTracks() => new List<Track>(news);
        public List<Track> GetMarketplaceTracks() => new List<Track>(marketplace);
        public List<Track> GetWeatherTracks() => new List<Track>(weather);
        public List<Track> GetTalkshowTracks() => new List<Track>(talkshows);
        public List<Track> GetEmergencyTracks() => new List<Track>(emergency);
        public List<Track> GetMiscTracks() => new List<Track>(misc);
        public List<Track> GetDjTracks() => new List<Track>(dj);

        /// <summary>
        /// Gets tracks valid for the current game day (original method for backward compatibility)
        /// </summary>
        public List<Track> GetTracksForDay(int currentDay)
        {
            var validTracks = new List<Track>();
            
            Log.Out($"=== Getting tracks for day {currentDay} ===");
            Log.Out($"Total available tracks: {allFiles.Count}");
            
            foreach (var track in allFiles)
            {
                if (IsTrackValidForDay(track, currentDay))
                {
                    validTracks.Add(track);
                }
            }
            
            Log.Out($"Valid tracks found for day {currentDay}: {validTracks.Count}");
            if (validTracks.Count > 0)
            {
                Log.Out("Valid tracks:");
                for (int i = 0; i < Math.Min(validTracks.Count, 10); i++)
                {
                    Log.Out($"  - {validTracks[i].name} (file: {validTracks[i].file}, days: {validTracks[i].days})");
                }
            }
            else
            {
                Log.Out("*** NO VALID TRACKS FOUND! This will cause radio silence. ***");
            }
            
            return validTracks;
        }
    }
}