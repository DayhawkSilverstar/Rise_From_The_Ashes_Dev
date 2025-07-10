using Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static Audio.Manager;
using static EntityVehicle;


public abstract class RadioSource
{
    public int EntityID { get; set; }

    public string Name { get; set; }
    public bool IsOn { get; set; }
    public string ClipName { get; set; }

    public Vector3 Position { get; set; }

    public int PlayListPosition { get; set; }

    public abstract void Play(string soundGroup);
    public abstract void Stop(string soundGroup);

    public AudioSource AudioSourceObject { get; set; }

    /// <summary>
    /// Returns a list of AudioSources that are playing the specified clip.
    /// </summary>
    /// <param name="ClipName"></param>
    /// <returns>List</returns>
    public static List<AudioSource> GetAudioSources(string ClipName)
    {
        Log.Out("Getting Audio Sources LIST: " + ClipName);
        List< AudioSource> sources = new List<AudioSource>();
        lock (Manager.playingAudioSources)
        {
            Log.Out("Playing Audio Sources Count : " + Manager.playingAudioSources.Count);
            foreach (AudioSource source in Manager.playingAudioSources)
            {   
                if (source != null)
                {
                    Log.Out("Audio Source Name : " + source.name);
                    Log.Out("Audio Source Clip : " + source.clip.name);
                    if (source.clip.name == ClipName)
                    {
                        sources.Add(source);
                    }
                }
            }
        }
        return sources;
    }

    /// <summary>
    /// Returns the AudioSource that is playing the specified clip at the specified position.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="clipName"></param>
    /// <returns>AudioSource</returns>
    public static AudioSource GetAudioSource(Vector3 position, string clipName)
    {
        try
        {
            Log.Out("Getting Audio Source : " + clipName);

            var sources = GetAudioSources(clipName);

            lock (Manager.playingAudioSources)
            {
                Log.Out($"Playing Audio Sources Count : {sources.Count}");
                foreach (AudioSource source in sources)
                {                             
                    Log.Out($"Source Name : {source.name} Clip Found.");                    
                    Log.Out($"Source Origin Position : {Origin.position}");                    
                    Log.Out($"Position : {position}");
                    Log.Out($"Source Playing : {source.isPlaying}");

                    if (source.transform.position == null)
                    {
                        Log.Out("Source Transform Position is null");
                        continue;
                    }

                    Log.Out($"Source Transform Position : {source.transform.position}");
                    Log.Out($"Source Adjusted Position : {source.transform.position + Origin.position}");

                    if ((source.transform.position + Origin.position) == position)
                    {
                        Log.Out($"Found a source : {source.name}");
                        return source;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Out("Error getting audio source.");
            Log.Out($"Exception : {e.Message}");
        }

        Log.Out("No Audio Source Found");
        return null;
    }


    /// <summary>
    /// Returns the AudioSource that is playing the specified clip with the specified entityID.
    /// </summary>
    /// <param name="entityID"></param>
    /// <param name="clipName"></param>
    /// <returns>AudioSource</returns>
    public static AudioSource GetAudioSource(int entityID, string clipName)
    {
        try
        {

            Log.Out("Getting Audio Source ENTITY: " + clipName);

            Entity entity = GameManager.Instance.World.GetEntity(entityID);

            List<AudioSource> audioSources = GetAudioSources(clipName);

            if (audioSources.Count == 0)
            {
                Log.Out("No Audio Sources Found");
                return null;
            }

            if (entity == null)
            {
                Log.Out("Entity is null");
                return null;
            }

            Log.Out("Entity Name: " + entity.name);

            foreach (AudioSource source in audioSources)
            {
                if (source == null)
                {
                    continue;
                }

                if (entity.position == source.transform.position + Origin.position)
                {
                    return source;
                }
            }         
        }
        catch (Exception e)
        {
            Log.Out("Error getting audio source.");
            Log.Out($"Exception : {e.Message}");
        }

        return null;
    }
       
    /// <summary>
    /// Checks the audio source to see if it is playing.
    /// </summary>
    /// <returns>Bool</returns>
    public bool IsPlaying()
    {
        if (AudioSourceObject != null)
        {
            return AudioSourceObject.isPlaying;
        }
        return false;
    }
    
    public static void SyncAudioSource(string ClipName)
    {
        try
        {
            float latestPlayTime = 0;
            List<AudioSource> sources = new List<AudioSource>();
            AudioSource sourcePrimary = null;
            lock (Manager.playingAudioSources)
            {
                foreach (AudioSource source in Manager.playingAudioSources)
                {
                    if (source != null)
                    {
                        if (source.clip.name == ClipName && source.isPlaying)
                        {
                            sources.Add(source);
                            if (source.time >= latestPlayTime)
                            {
                                latestPlayTime = source.time;
                                sourcePrimary = source;
                                Log.Out("Primary Source : " + source.name);
                            }
                        }
                    }
                }
            }

            foreach (AudioSource source in sources)
            {
                if (source == sourcePrimary)
                {
                    Log.Out($"ClipName : {source.clip}  Source.name : {source.name}  Source.time : {source.time} Source.Position : {source.transform.position + Origin.position} ");
                    continue;
                }
                if (source != sourcePrimary)
                {
                    source.time = latestPlayTime;
                    Log.Out($"ClipName : {source.clip}  Source.name : {source.name}  Source.time : {source.time} Source.Position : {source.transform.position + Origin.position} ");                
                }
            }
        }
        catch (Exception e)
        {
            Log.Out("Error syncing audio source.");
            Log.Out($"Exception : {e.Message}");
        }        
    }
}

public class DroneRadioSource : RadioSource
{
    public DroneRadioSource()
    {
        IsOn = false;
        ClipName = "";
        PlayListPosition = 0;        
    }

    public RiseDrone Drone { get; set; }

    public override void Play(string soundGroup)
    {
        try
        {
            Log.Out("DroneRadioSource Playing Radio : " + soundGroup);
            ClipName = soundGroup;
            Entity entity = GameManager.Instance.World.GetEntity(Drone.entityId);
            Log.Out("Entity Name : " + entity.name);
            Manager.Play(entity, soundGroup);            
            AudioSourceObject = GetAudioSource(Drone.entityId, soundGroup);
            AudioSourceObject.dopplerLevel = 0;
            SyncAudioSource(soundGroup);
            IsOn = true;
        }
        catch (Exception e)
        {
            Log.Out("Error playing drone radio source.");
            Log.Out($"Exception : {e.Message}");
        }
    }

    public override void Stop(string soundGroup)
    {
        IsOn = false;
        Manager.Stop(Drone.entityId, soundGroup);
    }

    public AudioSource FindAudioSource(string clipName)
    {
        foreach (AudioSource source in Drone.transform.GetComponentsInChildren<AudioSource>())
        {
            if (source.clip.name == clipName)
            {
                return source;
            }
        }

        return null;
    }
}

public class BlockRadioSource : RadioSource
{

    public BlockRadioSource()
    {
        IsOn = false;
        ClipName = "";
        PlayListPosition = 0;        
    }
    public RiseRadio Block { get; set; }

    public override void Play(string soundGroup)
    {        
        Log.Out("BlockRadioSource Playing Radio : " + soundGroup);
        ClipName = soundGroup;
        Log.Out("Playing Sound At Position : " + Position.ToString() + " : " + soundGroup);
        Log.Out("Source Name : " + Name);
        Manager.Play(Position, soundGroup);
        AudioSourceObject = FindAudioSource(ClipName);
        if (AudioSourceObject == null)
        {
            Log.Out("Audio Source is null. Can't Play");
            return;
        }
        AudioSourceObject.dopplerLevel = 0;
        SyncAudioSource(soundGroup);
        IsOn = true;        
    }

    public override void Stop(string soundGroup)
    {
        Log.Out("Stopping Radio : " + Name);
        Log.Out(("Stopping Sound At Position : " + Position.ToString() + " : " + soundGroup));
        if (AudioSourceObject != null)
        {
            IsOn = false;
            AudioSourceObject.Stop();
        }
        else
        {
            Log.Out("Audio Source is null. Can't Stop");
        }
    }

    void WalkComponents(GameObject obj)
    {
        Log.Out($"GameObject: {obj.name}");

        // Print all components attached to the GameObject
        Component[] components = obj.GetComponents<Component>();
        foreach (Component component in components)
        {
            Log.Out($"Component: {component.GetType().Name}");
        }

        // Recursively walk through all child GameObjects
        foreach (Transform child in obj.transform)
        {
            WalkComponents(child.gameObject);
        }
    }

    public AudioSource FindAudioSource(string clipName)
    {
        try
        {
            foreach (AudioSource source in Manager.playingAudioSources)
            {
                if (source.transform != null)
                {
                    //if (source.transform.GetComponentInParent<AudioSource>() != null)
                    //{
                    //    Log.Out("clipName : " + source.clip.name);
                    //    if (source.clip.name == clipName)
                    //    {
                    //        return source;
                    //    }
                    //}

                    WalkComponents(source.transform.gameObject);
                    
                }
            }

            //foreach (AudioSource source in Manager.playingAudioSources)
            //{
            //    if (source.transform != null)
            //    {
            //        if (source.transform.GetComponentInChildren<AudioSource>() != null)
            //        {
            //            Log.Out("clipName : " + source.clip.name);
            //            if (source.clip.name == clipName)
            //            {
            //                return source;
            //            }
            //        }
            //    }
            //}
        }
        catch (Exception e)
        {
            Log.Out("Error finding audio source.");            
            Log.Out($"Exception : {e.Message}");
            if (e.InnerException != null)
            {
                Log.Out($"Inner Exception : {e.InnerException.Message}");
            }
        }

        return null;
    }
}

public class EntityRadioSource : RadioSource
{
    public EntityRadioSource()
    {
        IsOn = false;
        ClipName = "";
        PlayListPosition = 0;
    }
    public Entity Entity { get; set; }

    public override void Play(string soundGroup)
    {
        throw new NotImplementedException();
    }

    public override void Stop(string soundGroup)
    {
        throw new NotImplementedException();
    }
}



public class RadioManager
{
    private static System.Random rng = new System.Random();
    private Manager m_manager;
    private static RadioManager _instance;
    private List<RadioSource> radioSources = new List<RadioSource>();    
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


    private List<Track> currentPlaylist = new List<Track>();

    private int playListTimeStamp = 0;
    private ulong gameTime = 0;

    private bool loaded = false;

    private int playlistPosition = 0;


    struct Track
    {
        public string name;
        public string file;
        public string category;
        public string days;
    }

    private RadioManager()
    {
        // If its part of the unit test then return.
        if (GameManager.Instance == null & ConnectionManager.Instance == null)
        {
            return;
        }

        if (GameManager.IsDedicatedServer)
        {
            Log.Out($"RadioManager Dedicated Server");
        }
        else if (ConnectionManager.Instance.IsSinglePlayer)
        {
            Log.Out($"RadioManager Singleplayer Server");
        }                
    }
    public void AddRadio(Entity entity)
    {
        RadioSource source = new EntityRadioSource
        {
            Entity = entity,
            IsOn = false,
            ClipName = "",
            PlayListPosition = playlistPosition,
            EntityID = entity.entityId

        };

        radioSources.Add(source);

    }

    public void AddRadio(RiseDrone entity)
    {
        RadioSource source = new DroneRadioSource
        {
            Drone = entity,
            IsOn = false,
            ClipName = "",
            PlayListPosition = playlistPosition,
            EntityID = entity.entityId,
            Name = entity.EntityId.ToString()
        };

        Log.Out("Added Drone : " + entity.EntityId);
        radioSources.Add(source);
    }

    public void AddRadio(RiseRadio block)
    {
        RadioSource source = new BlockRadioSource
        {
            Block = block,
            IsOn = false,
            ClipName = "",
            PlayListPosition = playlistPosition,
            EntityID = block.blockID,
            Position = block.blockPosition,
            Name = block.blockID.ToString() + block.blockPosition.ToString()
        };

        Log.Out("Added Radio : " + block.blockID);
        if (!radioSources.Contains(source))
            radioSources.Add(source);
    }

    public void RemoveRadio(RiseRadio block)
    {
        RadioSource radioSource = radioSources.Find(radio => radio.Name == block.blockID.ToString() + block.blockPosition);
        radioSource.IsOn = false;
        radioSources.Remove(radioSource);
    }

    public void RemoveRadio(Entity entity)
    {
        
    }

    public object GetRadio(object radioObj)
    {
        Log.Out("Entered GetRadio");
        RadioSource _radioSource = null;

        try
        {
            switch (radioObj)
            {
                case RiseDrone drone:
                    Log.Out($"Drone ID : {drone.entityId}");
                    Log.Out($"Drone Name : {drone.name}");
                    Log.Out($"RadioSource Count : {radioSources.Count}");

                    _radioSource = radioSources.FirstOrDefault(source => source.EntityID == drone.entityId);
                    if (_radioSource != null)
                    {
                        Log.Out($"RadioSource Found : {_radioSource.Name}");
                    }
                    break;

                case RiseRadio block:
                    Log.Out($"Block ID : {block.blockID}");
                    _radioSource = radioSources.Find(radioSource => radioSource.Name == block.blockID.ToString() + block.blockPosition);
                    break;

                case Entity entity:
                    Log.Out($"Entity ID : {entity.entityId}");
                    _radioSource = radioSources.Find(radioSource => radioSource.Name == entity.name);
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Out("Error finding radio source.");
            Log.Out($"Exception : {e.Message}");
        }

        return _radioSource;
    }

    public void PlayRadio(object obj)
    {
        Log.Out($"RadioManager Playing Radio : {obj.GetType()}");
        RadioSource source = GetRadio(obj) as RadioSource;
        if (source is RadioSource)
        {
            Log.Out($"Source Found : {source.EntityID}");
            if (playlistPosition == -1)
            {
                playlistPosition = 0;
            }
            if (obj is RiseRadio block)
            {
                Log.Out("Playing Radio : " + block.blockID);
                source.Position = block.blockPosition;
                Log.Out("CurrentPlayList.Count : " + currentPlaylist.Count);
                source.Play(currentPlaylist[playlistPosition].name);                
            }
            else if (obj is RiseDrone drone)
            {
                Log.Out("Playing Drone : " + drone.entityId);
                try
                {
                    Log.Out("PlayListPosition : " + playlistPosition);
                    Log.Out("Current Playlist Count : " + currentPlaylist.Count);
                    source.Play(currentPlaylist[playlistPosition].name);
                }
                catch (Exception e)
                {
                    Log.Out("Error playing drone radio source.");
                    Log.Out($"Exception : {e.Message}");
                }
            }
            else if (obj is Entity entity)
            {
                Log.Out("Playing Entity : " + entity.entityId);
            }

            if (source.IsPlaying())
            {
                RadioSource.SyncAudioSource(currentPlaylist[playlistPosition].name);
            }
        }
    }

    public void StopRadio(object obj)
    {
        Log.Out("Stopping Radio : " + obj.GetType());

        RadioSource _radioSource = GetRadio(obj) as RadioSource;

        if (_radioSource != null)
        {
            Log.Out("Source Found : " + _radioSource.EntityID);
            _radioSource.Stop(currentPlaylist[playlistPosition].name);
        }
    }

    public static RadioManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new RadioManager();

            return _instance;
        }
    }

    private void Init()
    {
        if (!loaded)
        {
            //LoadXmlRadioData();
            loaded = true;
        }

        CheckForNewDay();
    }

    private void CheckForNewDay()
    {
        if (playListTimeStamp < GameManager.Instance.World.WorldDay)
        {
            playListTimeStamp = GameManager.Instance.World.WorldDay;
            Log.Out("New Day :" + playListTimeStamp + ". Creating Playlist");
            CreatePlaylist();
        }
    }

    private bool AnyRadiosOn()
    {
        foreach (RadioSource source in radioSources)
        {
            if (source.IsOn)
            {
                return true;
            }
        }
        return false;
    }   

    private bool SongsPlaying()
    {
        foreach (RadioSource source in radioSources)
        {
            if (source.IsPlaying())
            {
                return true;
            }
        }
        return false;
    }

    private void ChangeToNextTrack()
    {
        if (currentPlaylist.Count == 0)
        {
            return;
        }

        if (playlistPosition == -1)
        {
            playlistPosition = 0;
        }

        playlistPosition = (playlistPosition + 1) % currentPlaylist.Count;
        string nextTrack = currentPlaylist[playlistPosition].name;

        foreach (RadioSource source in radioSources)
        {
            try
            {      
                if (source is DroneRadioSource)
                    Log.Out("Playing Radio Source : " + source.EntityID + " : " + source.Name + " : " + source.IsOn + " : " + source.IsPlaying() + " : " + nextTrack);                
                else if (source is BlockRadioSource)
                    Log.Out("Playing Radio Source : " + ((BlockRadioSource)source).Block.blockID.ToString() + " : " + source.IsOn + " : " + source.IsPlaying() + " : " + nextTrack);
                else if (source is EntityRadioSource)
                    Log.Out("Playing Radio Source : " + source.Name + " : " + source.IsOn + " : " + source.IsPlaying() + " : " + nextTrack);
                
                if (source.IsOn)
                {
                    source.Play(nextTrack);
                }
            }
            catch (Exception e)
            {
                Log.Out("Error playing radio source.");
                Log.Out($"Exception : {e.Message}");
            }
            
        }
    }

    public void Update()
    {
        if (GameManager.Instance.World != null)
        {
            {
                if (GameManager.Instance.World.worldTime > 0)
                {                                        
                    if (!GameManager.Instance.IsPaused())
                    {
                        // Check for Init
                        Init();

                        if (AnyRadiosOn())
                        {                            
                            if (gameTime < GameManager.Instance.World.GetWorldTime())
                            {
                                // This should make it poll every 3 seconds.
                                gameTime = GameManager.Instance.World.GetWorldTime() + 15;

                                // Check for Radio Sources that are currently playing.
                                if (!SongsPlaying())
                                {
                                    // Check for new day
                                    CheckForNewDay();

                                    ChangeToNextTrack();
                                }

                                if (SongsPlaying())
                                {
                                    // Sync the audio sources.
                                    RadioSource.SyncAudioSource(currentPlaylist[playlistPosition].name);
                                }
                            }
                        }
                    }
                }
            }
        }
    }



    public void CreatePlaylist()
    {
        // Create a new playlist for the day.
        currentPlaylist.Clear();

        // Add the music tracks.
        foreach (Track track in music)
        {

            AddTrack(track);
        }

        // Add the podcasts.
        foreach (Track track in podcasts)
        {
            AddTrack(track);
        }

        // Add the news.
        foreach (Track track in news)
        {
            AddTrack(track);
        }

        // Add the marketplace.
        foreach (Track track in marketplace)
        {
            AddTrack(track);
        }

        // Add the weather.
        foreach (Track track in weather)
        {
            AddTrack(track);
        }

        // Add the talkshows.
        foreach (Track track in talkshows)
        {
            AddTrack(track);
        }

        // Add the emergency broadcasts.
        foreach (Track track in emergency)
        {
            AddTrack(track);
        }

        // Add the misc.
        foreach (Track track in misc)
        {
            AddTrack(track);
        }

        foreach (Track track in dj)
        {
            AddTrack(track);
        }

        if (currentPlaylist.Count > 0)
        {
            Shuffle(currentPlaylist);
        }

        Log.Out("Playlist Created : " + currentPlaylist.Count);
        playlistPosition = -1;
    }   

    private void AddTrack(Track track)
    {
        // Add a track to the radio.
        var days = track.days.Split('-');
        int startDay = int.Parse(days[0]);
        int endDay = int.Parse(days[1]);

        Log.Out("Track Days Start : " + startDay.ToString());
        Log.Out("Track Days End : " + endDay.ToString());

        // Check if the track's days fall within the current playlist timestamp
        if (playListTimeStamp >= startDay && playListTimeStamp <= endDay)
        {
            Log.Out("Adding Track : " + track.name);
            currentPlaylist.Add(track);
        }
    }

    

    private void Shuffle<T>(IList<T> list)
    {
        Log.Out("Shuffling Playlist");
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public void LoadXmlRadioData()
    {
        string modfolder = GameIO.GetUserGameDataDir() + "/Mods";

        string xmlFilePath = modfolder + "/3c_Rise_From_The_Ashes_Radio" + "/Stations/stations.xml";

        Log.Out("XMLFilePath :" + " : " + xmlFilePath);

        // Load the XML data for the radio.
        string xmlContent = System.IO.File.ReadAllText(xmlFilePath);        

        // Parse the XML content
        XDocument doc = XDocument.Parse(xmlContent);        

        // Parse the tracks and categorize them
        foreach (var trackElement in doc.Descendants("track"))
        {
            Track track = new Track
            {
                name = trackElement.Attribute("name")?.Value,
                file = trackElement.Attribute("file")?.Value,
                category = trackElement.Attribute("category")?.Value,
                days = trackElement.Attribute("days")?.Value
            };

            allFiles.Add(track);

            switch (track.category)
            {
                case "music":
                    music.Add(track);
                    break;
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
                case "talkshows":
                    talkshows.Add(track);
                    break;
                case "emergency":
                    emergency.Add(track);
                    break;
                case "misc":
                    misc.Add(track);
                    break;
                case "dj":
                    dj.Add(track);
                    break;
            }            
        }

        Log.Out("Loaded Radio Data");
        Log.Out("TotalTracks :" + allFiles.Count);
    }
}

