using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Windows;

/// <summary>
/// Rise From The Ashes - AutoDrive
/// Hooked via: <triggered_effect trigger="onSelfBuffUpdate" action="AutoDrive, Rise_From_The_Ashes"/>
/// XML must use SHORT action name "AutoDrive" (engine prepends MinEventAction).
/// </summary>
[Preserve]
public class MinEventActionAutoDrive : MinEventActionBase
{
    // ---------------- Constants ----------------
    private const float ArrivalDistance = 2.0f;
    private const float ObstacleCheckMeters = 15f;    // Increased from 10f
    private const float SidestepMeters = 3.0f;        // Increased from 2.0f
    private const float SteeringDeadZoneDeg = 0.5f;   // Reduced from 1.0f for more responsive steering
    private const float MaxSteerDegrees = 35f;        // Reduced from 45f for more precise control
    private const float SidestepCooldown = 0.35f;
    private const float DefaultDesiredSpeed = 20.0f;  // Doubled from 10.0f
    private const float YawTorque = 4000f;            // Doubled from 2000f
    private const float MaxAngularVel = 3.5f;         // Increased from 2.5f
    
    // Configurable settings that can be changed via console commands
    private static float s_ConfigDesiredSpeed = DefaultDesiredSpeed;
    private static float s_ConfigArrivalDistance = ArrivalDistance;
    private static bool s_ConfigFollowPlayer = false;
    private static float s_ConfigFollowDistance = 10.0f;

    // ---------------- Logging ----------------
    private static class Dbg
    {
        private static readonly bool Enabled = true;
        public static void Info(string msg)
        {
            if (!Enabled) return;
            try
            {
                Log.Out("[RFA-AutoDrive] " + msg);
                SdtdConsole.Instance?.Output("[RFA-AutoDrive] " + msg);
            }
            catch { }
        }
    }

    static MinEventActionAutoDrive()
    {
        try { Dbg.Info("MinEventActionAutoDrive type loaded (static ctor)."); } catch { }
    }

    // ---------------- State ----------------
    private sealed class AutoState
    {
        public bool AutoDrive;
        public bool RoadFollow;
        public Vector3 Target = Vector3.negativeInfinity;
        public float ClearPathCooldown;
        public float NextTickLogTime;
        public VehicleDriveAdapter Drive;
        public Rigidbody RB;
        public Transform TR;
        public bool StartedLogged;
        public string ModeLabel;
        public bool VehicleDumped;
        public bool FollowPlayer;
        public float DesiredSpeed = DefaultDesiredSpeed;
        public float ArrivalDistance = 2.0f; // Use literal instead of referencing the class constant
        public float FollowDistance = 10.0f;
        public float NextTargetUpdateTime;
    }

    private static readonly Dictionary<int, AutoState> States = new Dictionary<int, AutoState>(64);
    private static float s_NextHeartbeat;

    // Input cache (per-player)
    private sealed class ActionCache
    {
        public Func<bool> Horn;
        public Func<bool> HeadlightOrFlashlight;
    }
    private static readonly Dictionary<int, ActionCache> s_InputCache = new Dictionary<int, ActionCache>(8);
    private static readonly HashSet<int> s_PlayerActionLogged = new HashSet<int>();

    // -------- Keys & Input Tracking ----------
    private static readonly Dictionary<int, KeyState> s_KeyStates = new Dictionary<int, KeyState>();

    private class KeyState
    {
        public bool HPressed;
        public bool HPreviouslyPressed;
        public bool FPressed;
        public bool FPreviouslyPressed;
        
        // Track key states between frames
        public bool WasHJustPressed() 
        {
            bool result = HPressed && !HPreviouslyPressed;
            HPreviouslyPressed = HPressed;
            return result;
        }
        
        public bool WasFJustPressed() 
        {
            bool result = FPressed && !FPreviouslyPressed;
            FPreviouslyPressed = FPressed;
            return result;
        }
    }
    
    // Tracking input via direct key monitoring
    private static void UpdateKeyStates(EntityPlayerLocal player)
    {
        if (player == null) return;
        
        int playerId = player.entityId;
        if (!s_KeyStates.TryGetValue(playerId, out var state))
        {
            state = new KeyState();
            s_KeyStates[playerId] = state;
        }

        try
        {
            // Reset state for this frame - we'll detect new presses
            bool previousHPressed = state.HPressed;
            bool previousFPressed = state.FPressed;
            state.HPressed = false;
            state.FPressed = false;
            
            // Try to access InControl's input system (used by 7 Days to Die)
            try
            {
                // Look for InControl assembly
                var inControlAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "InControl" || a.GetName().Name.Contains("InControl"));
                
                if (inControlAssembly != null)
                {
                    // Try to find the InputManager type
                    var inputManagerType = inControlAssembly.GetTypes()
                        .FirstOrDefault(t => t.Name == "InputManager" || t.Name.Contains("InputManager"));
                    
                    if (inputManagerType != null)
                    {
                        // Try to access the ActiveDevice property or similar
                        var activeDeviceProp = inputManagerType.GetProperty("ActiveDevice", BindingFlags.Public | BindingFlags.Static) ?? 
                                             inputManagerType.GetProperty("PrimaryInputDevice", BindingFlags.Public | BindingFlags.Static) ??
                                             inputManagerType.GetProperty("CurrentDevice", BindingFlags.Public | BindingFlags.Static);
                        
                        if (activeDeviceProp != null)
                        {
                            var device = activeDeviceProp.GetValue(null, null);
                            if (device != null)
                            {
                                // Try to check for button presses on the device
                                var deviceType = device.GetType();
                                
                                // Look for methods that check if a button is pressed
                                var getButtonMethod = deviceType.GetMethod("GetButton", BindingFlags.Public | BindingFlags.Instance) ?? 
                                                   deviceType.GetMethod("IsPressed", BindingFlags.Public | BindingFlags.Instance);
                                
                                if (getButtonMethod != null)
                                {
                                    // Try common button names for horn (H) and flashlight (F)
                                    object[] hornButtonNames = { "Horn", "Button9", "Button3", "Action3", "H" };
                                    object[] flashButtonNames = { "Flashlight", "Light", "Button10", "Button4", "Action4", "F" };
                                    
                                    foreach (var buttonName in hornButtonNames)
                                    {
                                        try
                                        {
                                            var result = getButtonMethod.Invoke(device, new[] { buttonName });
                                            if (result != null && result is bool && (bool)result)
                                            {
                                                state.HPressed = true;
                                                break;
                                            }
                                        }
                                        catch { /* Ignore errors with individual button names */ }
                                    }
                                    
                                    foreach (var buttonName in flashButtonNames)
                                    {
                                        try
                                        {
                                            var result = getButtonMethod.Invoke(device, new[] { buttonName });
                                            if (result != null && result is bool && (bool)result)
                                            {
                                                state.FPressed = true;
                                                break;
                                            }
                                        }
                                        catch { /* Ignore errors with individual button names */ }
                                    }
                                }
                                
                                // Try to check key state directly
                                var keyStateProperty = deviceType.GetProperty("Keys") ?? 
                                                     deviceType.GetProperty("Buttons") ?? 
                                                     deviceType.GetProperty("KeyboardState");
                                                     
                                if (keyStateProperty != null)
                                {
                                    var keyState = keyStateProperty.GetValue(device, null);
                                    if (keyState != null)
                                    {
                                        // Try to access specific keys either by index or by name
                                        // This varies based on InControl implementation
                                        var keyStateType = keyState.GetType();
                                        var getKeyMethod = keyStateType.GetMethod("GetKey") ?? 
                                                         keyStateType.GetMethod("IsPressed");
                                                         
                                        if (getKeyMethod != null)
                                        {
                                            // Common key codes or names for H and F
                                            object[] hKeyCodes = { 72, "H", "Key_H", "Horn" };
                                            object[] fKeyCodes = { 70, "F", "Key_F", "Flashlight", "Light" };
                                            
                                            foreach (var keyCode in hKeyCodes)
                                            {
                                                try
                                                {
                                                    var result = getKeyMethod.Invoke(keyState, new[] { keyCode });
                                                    if (result != null && result is bool && (bool)result)
                                                    {
                                                        state.HPressed = true;
                                                        break;
                                                    }
                                                }
                                                catch { /* Ignore errors with individual key codes */ }
                                            }
                                            
                                            foreach (var keyCode in fKeyCodes)
                                            {
                                                try
                                                {
                                                    var result = getKeyMethod.Invoke(keyState, new[] { keyCode });
                                                    if (result != null && result is bool && (bool)result)
                                                    {
                                                        state.FPressed = true;
                                                        break;
                                                    }
                                                }
                                                catch { /* Ignore errors with individual key codes */ }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Silently fail for InControl access errors
            }

            // Method 1: Try to access the input manager component through reflection
            var inputField = GetFieldOrProp(player, "input") ?? 
                             GetFieldOrProp(player, "playerInput") ?? 
                             GetFieldOrProp(player, "inputManager");
            
            if (inputField != null)
            {
                var keyDict = GetFieldOrProp(inputField, "keyboardState") as IDictionary;
                if (keyDict != null)
                {
                    // Check for H key (Horn)
                    object hPressed = null;
                    foreach (var key in keyDict.Keys)
                    {
                        if (key.ToString().Contains("H"))
                        {
                            hPressed = keyDict[key];
                            break;
                        }
                    }
                    if (hPressed != null && hPressed is bool && (bool)hPressed)
                        state.HPressed = true;
                        
                    // Check for F key (Headlight/Flashlight)
                    object fPressed = null;
                    foreach (var key in keyDict.Keys)
                    {
                        if (key.ToString().Contains("F"))
                        {
                            fPressed = keyDict[key];
                            break;
                        }
                    }
                    if (fPressed != null && fPressed is bool && (bool)fPressed)
                        state.FPressed = true;
                }
                
                // Alternatively, try to look for specific horn/headlight fields
                var hornField = GetFieldOrProp(inputField, "hornPressed") ?? GetFieldOrProp(inputField, "hornDown");
                if (hornField != null && hornField is bool && (bool)hornField)
                    state.HPressed = true;
                    
                var headlightField = GetFieldOrProp(inputField, "headlightPressed") ?? 
                                    GetFieldOrProp(inputField, "flashlightPressed") ?? 
                                    GetFieldOrProp(inputField, "lightPressed");
                if (headlightField != null && headlightField is bool && (bool)headlightField)
                    state.FPressed = true;
            }
            
            // Method 2: Look for input components
            var inputComponents = player.GetComponentsInChildren<Component>(true)
                .Where(c => c != null && (
                    c.GetType().Name.IndexOf("Input", StringComparison.OrdinalIgnoreCase) >= 0 || 
                    c.GetType().Name.IndexOf("Control", StringComparison.OrdinalIgnoreCase) >= 0
                ));
                
            foreach (var comp in inputComponents)
            {
                // Look for Horn input
                var hornField = GetFieldOrProp(comp, "hornPressed") ?? 
                               GetFieldOrProp(comp, "hornDown") ?? 
                               GetFieldOrProp(comp, "hornInput");
                if (hornField != null && hornField is bool && (bool)hornField)
                {
                    state.HPressed = true;
                }
                
                // Look for Headlight/Flashlight input
                var headlightField = GetFieldOrProp(comp, "headlightPressed") ?? 
                                    GetFieldOrProp(comp, "flashlightPressed") ?? 
                                    GetFieldOrProp(comp, "lightPressed");
                if (headlightField != null && headlightField is bool && (bool)headlightField)
                {
                    state.FPressed = true;
                }
                
                // Try to find a key dictionary
                var keyDictField = GetFieldOrProp(comp, "keys") ?? 
                                  GetFieldOrProp(comp, "keyboardState") ?? 
                                  GetFieldOrProp(comp, "keyState");
                                  
                if (keyDictField is IDictionary keyDict)
                {
                    foreach (var key in keyDict.Keys)
                    {
                        var keyName = key.ToString();
                        if (keyName.Contains("H") && keyDict[key] is bool && (bool)keyDict[key])
                            state.HPressed = true;
                        if (keyName.Contains("F") && keyDict[key] is bool && (bool)keyDict[key])
                            state.FPressed = true;
                    }
                }
            }
            
            // Method 3: Try to find vehicle-specific controls
            var vehicle = player.AttachedToEntity;
            if (vehicle != null)
            {
                var vehicleComponents = vehicle.GetComponentsInChildren<Component>(true)
                    .Where(c => c != null && (
                        c.GetType().Name.IndexOf("Vehicle", StringComparison.OrdinalIgnoreCase) >= 0 || 
                        c.GetType().Name.IndexOf("Control", StringComparison.OrdinalIgnoreCase) >= 0
                    ));
                    
                foreach (var comp in vehicleComponents)
                {
                    var hornField = GetFieldOrProp(comp, "hornPressed") ?? 
                                   GetFieldOrProp(comp, "hornDown") ?? 
                                   GetFieldOrProp(comp, "hornInput");
                    if (hornField != null && hornField is bool && (bool)hornField)
                    {
                        state.HPressed = true;
                    }
                    
                    var headlightField = GetFieldOrProp(comp, "headlightPressed") ?? 
                                        GetFieldOrProp(comp, "flashlightPressed") ?? 
                                        GetFieldOrProp(comp, "lightPressed");
                    if (headlightField != null && headlightField is bool && (bool)headlightField)
                    {
                        state.FPressed = true;
                    }
                }
            }

            // Method 4: Try Unity's input system directly as last resort
            try
            {
                // Use reflection to access Unity's input system
                var inputType = Type.GetType("UnityEngine.Input, UnityEngine") ?? 
                               Type.GetType("UnityEngine.Input");
                
                if (inputType != null)
                {
                    var getKeyMethod = inputType.GetMethod("GetKey", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null) ??
                                      inputType.GetMethod("GetKey", BindingFlags.Public | BindingFlags.Static);
                                      
                    if (getKeyMethod != null)
                    {
                        try
                        {
                            var resultH = getKeyMethod.Invoke(null, new object[] { "h" });
                            if (resultH != null && resultH is bool && (bool)resultH)
                                state.HPressed = true;
                                
                            var resultF = getKeyMethod.Invoke(null, new object[] { "f" });
                            if (resultF != null && resultF is bool && (bool)resultF)
                                state.FPressed = true;
                        }
                        catch { /* Silently fail on individual key checks */ }
                    }
                }
            }
            catch
            {
                // Silently fail for Unity input errors
            }

            // Preserve previous state for edge detection (key just pressed)
            state.HPreviouslyPressed = previousHPressed;
            state.FPreviouslyPressed = previousFPressed;
        }
        catch (Exception ex)
        {
            // Silently fail, don't crash the game for key detection
        }
    }
    
    // Check if a key was just pressed (one-time trigger)
    private static bool WasKeyJustPressed(EntityPlayerLocal player, string keyName)
    {
        if (player == null) return false;
        
        UpdateKeyStates(player);
        
        if (s_KeyStates.TryGetValue(player.entityId, out var state))
        {
            if (keyName.Equals("Horn", StringComparison.OrdinalIgnoreCase) || 
                keyName.Equals("H", StringComparison.OrdinalIgnoreCase))
            {
                return state.WasHJustPressed();
            }
            else if (keyName.Equals("Headlight", StringComparison.OrdinalIgnoreCase) || 
                     keyName.Equals("Flashlight", StringComparison.OrdinalIgnoreCase) || 
                     keyName.Equals("Light", StringComparison.OrdinalIgnoreCase) || 
                     keyName.Equals("F", StringComparison.OrdinalIgnoreCase))
            {
                return state.WasFJustPressed();
            }
        }
        
        return false;
    }

    // ---------------- MinEvent ----------------
    public override void Execute(MinEventParams _params)
    {
        try
        {
            // Lightweight heartbeat so we know Execute is live
            if (Time.time >= s_NextHeartbeat)
            {
                s_NextHeartbeat = Time.time + 2f;
                Dbg.Info("Execute heartbeat (player buff tick).");
            }

            var player = _params.Self as EntityPlayerLocal ?? GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player == null) return;

            var vehEnt = player.AttachedToEntity as Entity;
            if (vehEnt == null) return;

            int vid = vehEnt.entityId;
            if (!States.TryGetValue(vid, out var st))
            {
                st = new AutoState();
                st.DesiredSpeed = s_ConfigDesiredSpeed;
                st.ArrivalDistance = s_ConfigArrivalDistance;
                st.FollowDistance = s_ConfigFollowDistance;
                st.FollowPlayer = s_ConfigFollowPlayer;
                States[vid] = st;
            }

            if (st.TR == null) st.TR = vehEnt.transform;

            // Prefer child rigidbodies as many vehicles mount physics on a child
            if (st.RB == null)
                st.RB = vehEnt.GetComponent<Rigidbody>() ?? vehEnt.GetComponentInChildren<Rigidbody>(true);

            // Build adapter (search across likely controller objects)
            if (st.Drive == null) st.Drive = VehicleDriveAdapter.TryCreate(vehEnt);

            if (!st.StartedLogged)
            {
                st.ModeLabel = (st.Drive != null && st.Drive.IsValid) ? "Adapter" : (st.RB != null ? "Rigidbody" : "Unknown");
                Dbg.Info("Player " + player.entityId + " attached to vehicle " + vid + " | mode=" + st.ModeLabel);
                
                // Add extra diagnostic for kinematic rigidbodies
                if (st.RB != null && st.RB.isKinematic)
                {
                    Dbg.Info("Vehicle has a KINEMATIC Rigidbody - using alternative control methods");
                }
                
                if (st.ModeLabel == "Unknown") Dbg.Info("No known throttle/steer API and no Rigidbody found. Driving will not engage.");
                st.StartedLogged = true;
            }

            // Forensics: dump vehicle component types once to help compatibility
            if (!st.VehicleDumped)
            {
                try
                {
                    var comps = vehEnt.GetComponentsInChildren<Component>(true);
                    var names = comps.Where(c => c != null).Select(c => c.GetType().Name).Distinct().Take(80);
                    Dbg.Info("Vehicle components: " + string.Join(", ", names));
                }
                catch { }
                st.VehicleDumped = true;
            }

            // Key toggles: first try the new direct key detection
            bool hPressed = WasKeyJustPressed(player, "Horn") || WasKeyJustPressed(player, "H");
            bool fPressed = WasKeyJustPressed(player, "F") || WasKeyJustPressed(player, "Headlight") || 
                           WasKeyJustPressed(player, "Flashlight") || WasKeyJustPressed(player, "Light");
                           
            // Fall back to the action system if needed
            if (!hPressed) 
                hPressed = ActionWasPressed(player, "Horn");
                
            if (!fPressed)
                fPressed = ActionWasPressed(player, "Headlight") || ActionWasPressed(player, "Flashlight") || ActionWasPressed(player, "Light");

            // Toggle auto-drive on H key press
            if (hPressed)
            {
                st.AutoDrive = !st.AutoDrive;
                Dbg.Info("H toggle -> AutoDrive " + (st.AutoDrive ? "ON" : "OFF"));
                if (st.AutoDrive) 
                {
                    // If in follow player mode, don't acquire a target yet - it's updated during drive
                    if (!st.FollowPlayer)
                    {
                        AcquireTarget(st.TR, st);
                    }
                }
                else 
                {
                    HardStop(st);
                }
            }

            // Toggle road follow on F key press
            if (fPressed)
            {
                st.RoadFollow = !st.RoadFollow;
                Dbg.Info("F toggle -> RoadFollow " + (st.RoadFollow ? "ON" : "OFF"));
            }

            if (!st.AutoDrive) { ReleaseControl(st); return; }

            // Handle follow player mode
            if (st.FollowPlayer)
            {
                if (Time.time >= st.NextTargetUpdateTime)
                {
                    // Update target to follow behind the player
                    var primaryPlayer = GameManager.Instance?.World?.GetPrimaryPlayer();
                    if (primaryPlayer != null)
                    {
                        Vector3 playerPos = primaryPlayer.position;
                        Vector3 playerFwd = primaryPlayer.transform.forward;
                        
                        // Calculate position behind the player
                        Vector3 targetPos = playerPos - (playerFwd * st.FollowDistance);
                        
                        // Keep the same y-coordinate as the vehicle
                        targetPos.y = st.TR.position.y;
                        
                        st.Target = targetPos;
                        st.NextTargetUpdateTime = Time.time + 0.5f; // Update target position every half second
                    }
                }
            }
            else if (!HasValidTarget(st.Target))
            {
                AcquireTarget(st.TR, st);
            }
            
            if (!HasValidTarget(st.Target)) { HardStop(st); return; }

            if (Time.time >= st.NextTickLogTime)
            {
                float dist = Vector3.Distance(st.TR.position, st.Target);
                string modeInfo = st.FollowPlayer ? " | follow mode" : "";
                Dbg.Info("Driving toward " + st.Target + " | dist=" + dist.ToString("0.0") + " m | mode=" + st.ModeLabel + modeInfo);
                st.NextTickLogTime = Time.time + 1.0f;
            }

            if (st.Drive != null && st.Drive.IsValid) DriveTick_Adapter(st);
            else DriveTick_Rigidbody(st);
        }
        catch (Exception e)
        {
            Log.Error("[RFA-AutoDrive] Execute error: " + e);
        }
    }

    // ---------------- Input (cached) ----------------
    private static bool ActionWasPressed(EntityPlayerLocal player, string actionName)
    {
        if (player == null) return false;

        if (!s_InputCache.TryGetValue(player.entityId, out var cache))
        {
            cache = new ActionCache();
            var vehSet = FindActionSet(player, true);
            var locSet = FindActionSet(player, false);

            if (vehSet != null)
            {
                cache.Horn = BuildPressedAccessor(vehSet, "Horn");
                cache.HeadlightOrFlashlight = BuildPressedAccessor(vehSet, "Headlight")
                                           ?? BuildPressedAccessor(vehSet, "Light")
                                           ?? BuildPressedAccessor(vehSet, "Flashlight");
            }
            if (cache.Horn == null && locSet != null)
                cache.Horn = BuildPressedAccessor(locSet, "Horn");
            if (cache.HeadlightOrFlashlight == null && locSet != null)
                cache.HeadlightOrFlashlight = BuildPressedAccessor(locSet, "Headlight")
                                           ?? BuildPressedAccessor(locSet, "Flashlight");

            s_InputCache[player.entityId] = cache;

            if (s_PlayerActionLogged.Add(player.entityId))
            {
                Dbg.Info("Action discovery | VehicleSet=" + (vehSet != null ? vehSet.GetType().Name : "null") +
                         " | LocalSet=" + (locSet != null ? locSet.GetType().Name : "null"));
                if (vehSet != null) DumpKnownActions(vehSet, "Vehicle");
                if (locSet != null) DumpKnownActions(locSet, "Local");
            }
        }

        if (actionName == "Horn") return cache.Horn != null && cache.Horn();
        if (actionName == "Headlight" || actionName == "Flashlight" || actionName == "Light")
            return cache.HeadlightOrFlashlight != null && cache.HeadlightOrFlashlight();

        return false;
    }

    private static object FindActionSet(EntityPlayerLocal player, bool vehicle)
    {
        object x;
        x = GetFieldOrProp(player, vehicle ? "playerActionsVehicle" : "playerActionsLocal"); if (x != null) return x;
        x = GetFieldOrProp(player, vehicle ? "PlayerActionVehicle" : "PlayerActionsLocal"); if (x != null) return x;
        x = GetFieldOrProp(player, vehicle ? "ActionsVehicle" : "Actions"); if (x != null) return x;
        x = GetComponentNamed(player, vehicle ? "PlayerActionVehicle" : "PlayerActionsLocal"); if (x != null) return x;

        // last resort: scan any "Action" component that exposes Horn/Headlight/Flashlight/Light
        var comps = player.GetComponentsInChildren<Component>(true);
        foreach (var c in comps)
        {
            if (c == null) continue;
            var tn = c.GetType().Name;
            if (tn.IndexOf("Action", StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (GetFieldOrProp(c, "Horn") != null ||
                GetFieldOrProp(c, "Headlight") != null ||
                GetFieldOrProp(c, "Flashlight") != null ||
                GetFieldOrProp(c, "Light") != null)
                return c;
        }
        return null;
    }

    private static Func<bool> BuildPressedAccessor(object actionSet, string memberName)
    {
        if (actionSet == null) return null;
        var actionObj = GetFieldOrProp(actionSet, memberName);
        if (actionObj == null) return null;

        var t = actionObj.GetType();
        var pWas = t.GetProperty("WasPressed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pWas != null && pWas.PropertyType == typeof(bool)) return () => (bool)pWas.GetValue(actionObj, null);

        var pOnce = t.GetProperty("PressedOnce", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pOnce != null && pOnce.PropertyType == typeof(bool)) return () => (bool)pOnce.GetValue(actionObj, null);

        var mWas = t.GetMethod("WasPressed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        if (mWas != null && mWas.ReturnType == typeof(bool)) return () => (bool)mWas.Invoke(actionObj, null);

        return null;
    }

    private static void DumpKnownActions(object actionSet, string label)
    {
        try
        {
            var t = actionSet.GetType();
            foreach (var name in new[] { "Horn", "Headlight", "Light", "Flashlight" })
            {
                var a = GetFieldOrProp(actionSet, name);
                if (a != null) Dbg.Info(label + " action found: " + t.Name + "." + name + " (" + a.GetType().Name + ")");
            }
        }
        catch (Exception e) { Dbg.Info("DumpKnownActions error: " + e.Message); }
    }

    // ---------------- Driving ----------------
    private static void DriveTick_Adapter(AutoState st)
    {
        Vector3 toTarget = st.Target - st.TR.position; toTarget.y = 0f;
        if (toTarget.magnitude <= st.ArrivalDistance) 
        { 
            if (st.FollowPlayer)
            {
                // In follow mode, just stop moving but don't disable autodrive
                st.Drive.SetSteering(0f);
                st.Drive.SetThrottle(0f);
            }
            else
            {
                Dbg.Info("Arrived at target."); 
                HardStop(st);
            }
            return; 
        }

        Vector3 desiredDir = toTarget.normalized;
        if (st.ClearPathCooldown > 0f) st.ClearPathCooldown -= Time.deltaTime;

        Vector3 origin = st.TR.position + Vector3.up * 1f;
        Vector3 fwd = st.TR.forward; fwd.y = 0f; fwd.Normalize();

        bool blocked = Physics.Raycast(origin, fwd, ObstacleCheckMeters, ~0, QueryTriggerInteraction.Ignore);
        if (blocked && st.ClearPathCooldown <= 0f)
        {
            Dbg.Info("Obstacle ahead. Applying sidestep.");
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            bool rightBlocked = Physics.Raycast(origin + right * 0.5f, fwd, ObstacleCheckMeters * 0.7f, ~0, QueryTriggerInteraction.Ignore);
            Vector3 sidestep = (rightBlocked ? -right : right) * SidestepMeters;
            desiredDir = (desiredDir + sidestep).normalized;
            st.ClearPathCooldown = SidestepCooldown;
        }

        float steer = ComputeSteerInput(st.TR, desiredDir);
        
        // Apply throttle based on distance to target in follow player mode
        float throttle = 1.0f;
        if (st.FollowPlayer)
        {
            float distToTarget = toTarget.magnitude;
            if (distToTarget < st.FollowDistance * 1.5f)
            {
                // Slow down as we get closer to the follow distance
                throttle = Mathf.Clamp01((distToTarget - st.ArrivalDistance) / st.FollowDistance);
            }
        }
        
        st.Drive.SetSteering(steer);
        st.Drive.SetThrottle(throttle);
    }

    private static void DriveTick_Rigidbody(AutoState st)
    {
        if (st.RB == null) return;

        Vector3 toTarget = st.Target - st.TR.position; toTarget.y = 0f;
        if (toTarget.magnitude <= st.ArrivalDistance) 
        { 
            if (st.FollowPlayer)
            {
                // In follow mode, just stop moving but don't disable autodrive
                if (!st.RB.isKinematic)
                {
                    st.RB.velocity = new Vector3(0f, st.RB.velocity.y, 0f);
                    st.RB.angularVelocity = Vector3.zero;
                }
                else
                {
                    TryVehicleMovementControl(st.RB.gameObject, 0f, 0f);
                }
            }
            else
            {
                Dbg.Info("Arrived at target."); 
                HardStop(st);
            }
            return; 
        }

        Vector3 desiredDir = toTarget.normalized;
        if (st.ClearPathCooldown > 0f) st.ClearPathCooldown -= Time.deltaTime;

        Vector3 origin = st.TR.position + Vector3.up * 1f;
        Vector3 fwd = st.TR.forward; fwd.y = 0f; fwd.Normalize();

        bool blocked = Physics.Raycast(origin, fwd, ObstacleCheckMeters, ~0, QueryTriggerInteraction.Ignore);
        if (blocked && st.ClearPathCooldown <= 0f)
        {
            Dbg.Info("Obstacle ahead. Applying sidestep.");
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            bool rightBlocked = Physics.Raycast(origin + right * 0.5f, fwd, ObstacleCheckMeters * 0.7f, ~0, QueryTriggerInteraction.Ignore);
            Vector3 sidestep = (rightBlocked ? -right : right) * SidestepMeters;
            desiredDir = (desiredDir + sidestep).normalized;
            st.ClearPathCooldown = SidestepCooldown;
        }

        float steer = ComputeSteerInput(st.TR, desiredDir);
        
        // Calculate throttle based on distance to target in follow player mode
        float throttle = 1.0f;
        if (st.FollowPlayer)
        {
            float distToTarget = toTarget.magnitude;
            if (distToTarget < st.FollowDistance * 1.5f)
            {
                // Slow down as we get closer to the follow distance
                throttle = Mathf.Clamp01((distToTarget - st.ArrivalDistance) / st.FollowDistance);
            }
        }
        
        // Check if the rigidbody is kinematic (non-physics-driven)
        bool isKinematic = st.RB.isKinematic;
        if (isKinematic)
        {
            // For kinematic bodies, try all available methods
            TryVehicleMovementControl(st.RB.gameObject, steer, throttle);
            
            // As a fallback, try to directly modify the transform for kinematic bodies
            // This is a more aggressive approach
            Vector3 forward = st.TR.forward;
            
            // Calculate rotation change
            float rotationAmount = steer * 2f; // More aggressive rotation
            st.TR.Rotate(Vector3.up, rotationAmount);
            
            // Calculate position change
            Vector3 movement = forward * throttle * st.DesiredSpeed * 0.05f; // Scale with desired speed
            st.TR.position += movement;
            
            Dbg.Info($"Applied direct transform changes to kinematic vehicle");
        }
        else
        {
            // Only manipulate non-kinematic rigidbodies directly
            
            // Clamp Y angular speed; then yaw torque toward desired
            Vector3 angVel = st.RB.angularVelocity;
            angVel.y = Mathf.Clamp(angVel.y, -MaxAngularVel * 2, MaxAngularVel * 2); // Double the max angular velocity
            st.RB.angularVelocity = angVel;
            
            // Apply much stronger torque for better steering
            st.RB.AddTorque(Vector3.up * (steer * YawTorque * 2.5f), ForceMode.Acceleration);

            // Apply throttle in forward direction with stronger force
            Vector3 planarVel = st.RB.velocity; planarVel.y = 0f;
            float speed = planarVel.magnitude;
            Vector3 driveDir = st.TR.forward; driveDir.y = 0f; driveDir.Normalize();
            float accel = 18f; // 50% more acceleration
            float newSpeed = Mathf.MoveTowards(speed, st.DesiredSpeed * throttle, accel * Time.deltaTime);
            Vector3 newPlanarVel = driveDir * newSpeed;
            st.RB.velocity = new Vector3(newPlanarVel.x, st.RB.velocity.y, newPlanarVel.z);
            
            // For very slow or stuck vehicles, apply an additional force
            if (speed < 1.0f && throttle > 0.1f)
            {
                st.RB.AddForce(driveDir * st.DesiredSpeed * 100f, ForceMode.Force);
                Dbg.Info("Applied extra force to overcome inertia");
            }
        }
    }

    private static float ComputeSteerInput(Transform tr, Vector3 desiredDir)
    {
        Vector3 f = tr.forward; f.y = 0f; f.Normalize();
        Vector3 d = desiredDir; d.y = 0f; d.Normalize();
        float signedAngle = Vector3.SignedAngle(f, d, Vector3.up);
        if (Mathf.Abs(signedAngle) < SteeringDeadZoneDeg) return 0f;
        return Mathf.Clamp(signedAngle / MaxSteerDegrees, -1f, 1f);
    }

    private static void AcquireTarget(Transform tr, AutoState st)
    {
        st.Target = Vector3.negativeInfinity;
        try
        {
            // First try through NavObjectManager
            TryAcquireTargetViaNavObjectManager(tr, st);
            
            // If that failed, try alternative methods
            if (!HasValidTarget(st.Target))
            {
                TryAcquireTargetViaMapSystem(tr, st);
            }
            
            // If we still don't have a target, try a more direct approach via UI
            if (!HasValidTarget(st.Target))
            {
                TryAcquireTargetViaUI(tr, st);
            }
            
            // If all methods failed, inform user
            if (!HasValidTarget(st.Target))
            {
                Dbg.Info("Failed to find any waypoints through all available methods.");
            }
        }
        catch (Exception e)
        {
            Log.Error("[RFA-AutoDrive] AcquireTarget() error: " + e);
        }
    }

    private static void TryAcquireTargetViaNavObjectManager(Transform tr, AutoState st)
    {
        try
        {
            var mgr = NavObjectManager.Instance;
            if (mgr == null) { Dbg.Info("NavObjectManager.Instance is null."); return; }

            IEnumerable navs = TryEnumerateNavObjects(mgr);
            if (navs == null) { Dbg.Info("NavObjectManager enumeration failed."); return; }

            NavObject best = null;
            int bestScore = int.MinValue;
            foreach (var o in navs)
            {
                var n = o as NavObject;
                if (n == null || !n.IsActive) continue;
                string display = n.usingLocalizationId ? n.localizedName : n.name;
                int score = ScoreMarkerName(n.name, display);
                if (score > bestScore) { best = n; bestScore = score; }
            }

            if (best != null && !IsInvalid(best.trackedPosition))
            {
                Vector3 p = best.trackedPosition;
                st.Target = new Vector3(p.x, tr.position.y, p.z);
                Dbg.Info("Target acquired via NavObjectManager: " + st.Target + " (from '" + best.name + "')");
            }
            else
            {
                Dbg.Info("No active flag/rally/marker found via NavObjectManager.");
            }
        }
        catch (Exception e)
        {
            Dbg.Info("NavObjectManager search failed: " + e.Message);
        }
    }

    private static IEnumerable TryEnumerateNavObjects(NavObjectManager mgr)
    {
        var t = mgr.GetType();
        var mi = t.GetMethod("GetNavObjects", BindingFlags.Public | BindingFlags.Instance); if (mi != null) return mi.Invoke(mgr, null) as IEnumerable;
        mi = t.GetMethod("GetAll", BindingFlags.Public | BindingFlags.Instance); if (mi != null) return mi.Invoke(mgr, null) as IEnumerable;
        var pi = t.GetProperty("NavObjects", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic); if (pi != null) return pi.GetValue(mgr, null) as IEnumerable;
        var fi = t.GetField("NavObjects", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic); if (fi != null) return fi.GetValue(mgr) as IEnumerable;
        fi = t.GetField("m_NavObjects", BindingFlags.NonPublic | BindingFlags.Instance); if (fi != null) return fi.GetValue(mgr) as IEnumerable;
        return null;
    }

    private static void TryAcquireTargetViaMapSystem(Transform tr, AutoState st)
    {
        try
        {
            // Try to access the MapManager or similar system
            var mapManager = GetMapManagerInstance();
            if (mapManager == null)
            {
                Dbg.Info("Could not access MapManager.");
                return;
            }
            
            // Try to get waypoints from the map
            var waypoints = GetWaypointsFromMapManager(mapManager);
            if (waypoints == null || !waypoints.Any())
            {
                Dbg.Info("No waypoints found through MapManager.");
                return;
            }
            
            // Find the best waypoint
            var bestWaypoint = FindBestWaypoint(waypoints, tr.position);
            if (bestWaypoint != null)
            {
                st.Target = new Vector3(bestWaypoint.X, tr.position.y, bestWaypoint.Z);
                Dbg.Info("Target acquired via MapManager: " + st.Target + " (from '" + bestWaypoint.Name + "')");
            }
        }
        catch (Exception e)
        {
            Dbg.Info("MapManager search failed: " + e.Message);
        }
    }

    private static void TryAcquireTargetViaUI(Transform tr, AutoState st)
    {
        try
        {
            // Attempt to find waypoints through UI elements
            var xui = GetXUiInstance();
            if (xui == null)
            {
                Dbg.Info("Could not access XUi system.");
                return;
            }
            
            // Look for map window or compass elements that might contain waypoints
            var mapWindows = FindMapOrCompassWindows(xui);
            if (mapWindows == null || !mapWindows.Any())
            {
                Dbg.Info("No map UI windows found.");
                
                // If no map windows found, try to directly access waypoints through XUi controller
                var waypointPos = FindWaypointFromXUiController();
                if (waypointPos.HasValue)
                {
                    st.Target = new Vector3(waypointPos.Value.x, tr.position.y, waypointPos.Value.z);
                    Dbg.Info("Target acquired via XUi controller: " + st.Target);
                    return;
                }
            }
            
            // Try to extract waypoints from UI
            var waypoint = FindWaypointFromUI(mapWindows, tr.position);
            if (waypoint.HasValue)
            {
                st.Target = new Vector3(waypoint.Value.x, tr.position.y, waypoint.Value.z);
                Dbg.Info("Target acquired via UI: " + st.Target);
                return;
            }
            
            // Try to find player-placed waypoints (red flags)
            var playerWaypoint = FindPlayerWaypoint();
            if (playerWaypoint.HasValue)
            {
                st.Target = new Vector3(playerWaypoint.Value.x, tr.position.y, playerWaypoint.Value.z);
                Dbg.Info("Target acquired from player waypoint: " + st.Target);
                return;
            }
            
            // If still no waypoint, try to use the player position with offset as fallback
            if (!HasValidTarget(st.Target))
            {
                // Find the player position
                var playerPos = GetPlayerPosition();
                if (playerPos.HasValue)
                {
                    // Create a waypoint 100 meters north of the player as fallback
                    var targetPos = playerPos.Value + new Vector3(0, 0, 100);
                    st.Target = new Vector3(targetPos.x, tr.position.y, targetPos.z);
                    Dbg.Info("Using fallback target 100m ahead of player: " + st.Target);
                }
            }
        }
        catch (Exception e)
        {
            Dbg.Info("UI waypoint search failed: " + e.Message);
        }
    }

    private static object GetMapManagerInstance()
    {
        // Try to find the MapManager instance through reflection
        try
        {
            // Look for static Instance property in MapManager or similar classes
            var mapManagerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; }
                })
                .Where(t => t.Name.Contains("Map") && t.Name.Contains("Manager"))
                .ToList();
                
            foreach (var type in mapManagerTypes)
            {
                var instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static) ??
                                  type.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                                  
                if (instanceProp != null)
                {
                    var instance = instanceProp.GetValue(null);
                    if (instance != null)
                    {
                        Dbg.Info($"Found map manager of type {type.Name}");
                        return instance;
                    }
                }
                
                // Look for a static field
                var instanceField = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static) ??
                                   type.GetField("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                                   
                if (instanceField != null)
                {
                    var instance = instanceField.GetValue(null);
                    if (instance != null)
                    {
                        Dbg.Info($"Found map manager via field of type {type.Name}");
                        return instance;
                    }
                }
            }
            
            // If no MapManager found, try to get from GameManager
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                var mapManagerProp = gameManager.GetType().GetProperty("MapManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mapManagerProp != null)
                {
                    var instance = mapManagerProp.GetValue(gameManager);
                    if (instance != null)
                    {
                        Dbg.Info("Found map manager via GameManager");
                        return instance;
                    }
                }
                
                // Try field
                var mapManagerField = gameManager.GetType().GetField("MapManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                     gameManager.GetType().GetField("m_MapManager", BindingFlags.Instance | BindingFlags.NonPublic);
                                     
                if (mapManagerField != null)
                {
                    var instance = mapManagerField.GetValue(gameManager);
                    if (instance != null)
                    {
                        Dbg.Info("Found map manager via GameManager field");
                        return instance;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Dbg.Info("Error finding map manager: " + ex.Message);
        }
        
        return null;
    }

    private class SimpleWaypoint
    {
        public float X;
        public float Z;
        public string Name;
        public int Type; // 0=flag, 1=waypoint, 2=trader, etc.
        
        public float DistanceTo(Vector3 position)
        {
            float dx = X - position.x;
            float dz = Z - position.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }

    private static IEnumerable<SimpleWaypoint> GetWaypointsFromMapManager(object mapManager)
    {
        if (mapManager == null) return null;
        
        var result = new List<SimpleWaypoint>();
        
        try
        {
            // Try different property and method names that might exist
            var waypointsProp = mapManager.GetType().GetProperty("Waypoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                               mapManager.GetType().GetProperty("MapWaypoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                               
            // Try a method
            var getWaypointsMethod = mapManager.GetType().GetMethod("GetWaypoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                    mapManager.GetType().GetMethod("GetAllWaypoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                    
            // Try a field
            var waypointsField = mapManager.GetType().GetField("m_Waypoints", BindingFlags.Instance | BindingFlags.NonPublic) ??
                                mapManager.GetType().GetField("waypoints", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                
            IEnumerable waypoints = null;
            
            if (waypointsProp != null)
            {
                waypoints = waypointsProp.GetValue(mapManager) as IEnumerable;
                if (waypoints != null) Dbg.Info("Found waypoints via property");
            }
            
            if (waypoints == null && getWaypointsMethod != null)
            {
                waypoints = getWaypointsMethod.Invoke(mapManager, null) as IEnumerable;
                if (waypoints != null) Dbg.Info("Found waypoints via method");
            }
            
            if (waypoints == null && waypointsField != null)
            {
                waypoints = waypointsField.GetValue(mapManager) as IEnumerable;
                if (waypoints != null) Dbg.Info("Found waypoints via field");
            }
            
            if (waypoints != null)
            {
                foreach (var wp in waypoints)
                {
                    if (wp == null) continue;
                    
                    SimpleWaypoint simpleWp = new SimpleWaypoint();
                    bool valid = false;
                    
                    // Try to extract position
                    var posProp = wp.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public) ??
                                 wp.GetType().GetProperty("position", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                 
                    if (posProp != null)
                    {
                        var pos = posProp.GetValue(wp);
                        if (pos != null && pos is Vector3)
                        {
                            var vector = (Vector3)pos;
                            simpleWp.X = vector.x;
                            simpleWp.Z = vector.z;
                            valid = true;
                        }
                    }
                    
                    // Alternative: try X and Z separately
                    if (!valid)
                    {
                        var xProp = wp.GetType().GetProperty("X", BindingFlags.Instance | BindingFlags.Public) ??
                                   wp.GetType().GetProperty("x", BindingFlags.Instance | BindingFlags.Public);
                                   
                        var zProp = wp.GetType().GetProperty("Z", BindingFlags.Instance | BindingFlags.Public) ??
                                   wp.GetType().GetProperty("z", BindingFlags.Instance | BindingFlags.Public);
                                   
                        if (xProp != null && zProp != null)
                        {
                            var xObj = xProp.GetValue(wp);
                            var zObj = zProp.GetValue(wp);
                            
                            if (xObj != null && zObj != null)
                            {
                                simpleWp.X = Convert.ToSingle(xObj);
                                simpleWp.Z = Convert.ToSingle(zObj);
                                valid = true;
                            }
                        }
                    }
                    
                    // Try to get the name
                    var nameProp = wp.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public) ??
                                  wp.GetType().GetProperty("name", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                  
                    if (nameProp != null)
                    {
                        var nameObj = nameProp.GetValue(wp);
                        if (nameObj != null)
                        {
                            simpleWp.Name = nameObj.ToString();
                        }
                    }
                    
                    // Try to get the type
                    var typeProp = wp.GetType().GetProperty("Type", BindingFlags.Instance | BindingFlags.Public) ??
                                  wp.GetType().GetProperty("type", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                  wp.GetType().GetProperty("waypointType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                  
                    if (typeProp != null)
                    {
                        var typeObj = typeProp.GetValue(wp);
                        if (typeObj != null)
                        {
                            simpleWp.Type = Convert.ToInt32(typeObj);
                        }
                    }
                    
                    // Add if we got coordinates
                    if (valid)
                    {
                        result.Add(simpleWp);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Dbg.Info("Error extracting waypoints: " + ex.Message);
        }
        
        return result;
    }

    private static SimpleWaypoint FindBestWaypoint(IEnumerable<SimpleWaypoint> waypoints, Vector3 currentPosition)
    {
        if (waypoints == null || !waypoints.Any()) return null;
        
        SimpleWaypoint best = null;
        float bestScore = float.MinValue;
        
        foreach (var wp in waypoints)
        {
            // Calculate score based on type and distance
            float score = 0;
            
            // Score based on type (prefer flags and rally points)
            if (wp.Type == 0 || (wp.Name != null && wp.Name.IndexOf("flag", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                score += 1000; // Flag
            }
            else if (wp.Name != null)
            {
                if (wp.Name.IndexOf("rally", StringComparison.OrdinalIgnoreCase) >= 0) score += 800;
                else if (wp.Name.IndexOf("marker", StringComparison.OrdinalIgnoreCase) >= 0) score += 600;
                else if (wp.Name.IndexOf("waypoint", StringComparison.OrdinalIgnoreCase) >= 0) score += 500;
                else score += 100;
            }
            
            // Apply distance penalty (prefer closer waypoints, but type is more important)
            float distance = wp.DistanceTo(currentPosition);
            score -= distance * 0.1f;
            
            if (score > bestScore)
            {
                bestScore = score;
                best = wp;
            }
        }
        
        return best;
    }

    private static object GetXUiInstance()
    {
        try
        {
            // Try common XUi manager class names
            var xuiManagerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => {
                    try { return a.GetTypes(); }
                    catch { return new Type[0]; }
                })
                .Where(t => (t.Name.Contains("XUi") || t.Name.Contains("UI")) && 
                           (t.Name.Contains("Manager") || t.Name.Contains("System")))
                .ToList();
                
            foreach (var type in xuiManagerTypes)
            {
                var instanceProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceProp != null)
                {
                    var instance = instanceProp.GetValue(null);
                    if (instance != null) return instance;
                }
            }
            
            // Try to get UI manager from GameManager
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                var uiManagerProp = gameManager.GetType().GetProperty("UI", BindingFlags.Instance | BindingFlags.Public) ??
                                  gameManager.GetType().GetProperty("XUi", BindingFlags.Instance | BindingFlags.Public) ??
                                  gameManager.GetType().GetProperty("UIManager", BindingFlags.Instance | BindingFlags.Public);
                                  
                if (uiManagerProp != null)
                {
                    var instance = uiManagerProp.GetValue(gameManager);
                    if (instance != null) return instance;
                }
            }
        }
        catch (Exception ex)
        {
            Dbg.Info("Error finding XUi: " + ex.Message);
        }
        
        return null;
    }

    private static IEnumerable<object> FindMapOrCompassWindows(object xuiManager)
    {
        if (xuiManager == null) return null;
        
        var result = new List<object>();
        
        try
        {
            // Try to get all windows
            var windowsProp = xuiManager.GetType().GetProperty("Windows", BindingFlags.Instance | BindingFlags.Public);
            IEnumerable windows = windowsProp?.GetValue(xuiManager) as IEnumerable;
            
            if (windows == null)
            {
                var windowsField = xuiManager.GetType().GetField("windows", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                windows = windowsField?.GetValue(xuiManager) as IEnumerable;
            }
            
            if (windows != null)
            {
                foreach (var window in windows)
                {
                    if (window == null) continue;
                    
                    string windowName = window.GetType().Name;
                    if (windowName.IndexOf("map", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.IndexOf("compass", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        windowName.IndexOf("waypoint", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result.Add(window);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Dbg.Info("Error finding map windows: " + ex.Message);
        }
        
        return result;
    }

    private static Vector3? FindWaypointFromUI(IEnumerable<object> mapWindows, Vector3 currentPosition)
    {
        if (mapWindows == null) return null;
        
        foreach (var window in mapWindows)
        {
            try
            {
                // Try to find waypoint markers in the window
                var markersProp = window.GetType().GetProperty("Markers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                IEnumerable markers = markersProp?.GetValue(window) as IEnumerable;
                
                if (markers == null)
                {
                    var markersField = window.GetType().GetField("markers", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                     window.GetType().GetField("m_Markers", BindingFlags.Instance | BindingFlags.NonPublic);
                                     
                    markers = markersField?.GetValue(window) as IEnumerable;
                }
                
                if (markers != null)
                {
                    Vector3? bestPos = null;
                    float bestScore = float.MinValue;
                    
                    foreach (var marker in markers)
                    {
                        if (marker == null) continue;
                        
                        // Get marker position
                        var posProp = marker.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public) ??
                                    marker.GetType().GetProperty("WorldPosition", BindingFlags.Instance | BindingFlags.Public);
                                    
                        if (posProp != null)
                        {
                            var pos = posProp.GetValue(marker);
                            if (pos != null && pos is Vector3)
                            {
                                var markerPos = (Vector3)pos;
                                
                                // Get marker type/name if possible
                                var typeProp = marker.GetType().GetProperty("Type", BindingFlags.Instance | BindingFlags.Public) ??
                                             marker.GetType().GetProperty("MarkerType", BindingFlags.Instance | BindingFlags.Public);
                                             
                                var nameProp = marker.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                                
                                float score = 0;
                                
                                // Score by type if available
                                if (typeProp != null)
                                {
                                    var typeObj = typeProp.GetValue(marker);
                                    if (typeObj != null)
                                    {
                                        int type = Convert.ToInt32(typeObj);
                                        if (type == 0) score += 1000; // Flag
                                        else score += 100; // Other marker
                                    }
                                }
                                
                                // Score by name if available
                                if (nameProp != null)
                                {
                                    var nameObj = nameProp.GetValue(marker);
                                    if (nameObj != null)
                                    {
                                        string name = nameObj.ToString();
                                        if (name.IndexOf("flag", StringComparison.OrdinalIgnoreCase) >= 0) score += 1000;
                                        else if (name.IndexOf("rally", StringComparison.OrdinalIgnoreCase) >= 0) score += 800;
                                        else if (name.IndexOf("marker", StringComparison.OrdinalIgnoreCase) >= 0) score += 600;
                                        else if (name.IndexOf("waypoint", StringComparison.OrdinalIgnoreCase) >= 0) score += 500;
                                    }
                                }
                                
                                // Apply distance penalty
                                float dist = Vector3.Distance(markerPos, currentPosition);
                                score -= dist * 0.1f;
                                
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestPos = markerPos;
                                }
                            }
                        }
                    }
                    
                    if (bestPos.HasValue)
                    {
                        return bestPos;
                    }
                }
            }
            catch (Exception ex)
            {
                Dbg.Info("Error processing UI window: " + ex.Message);
            }
        }
        
        return null;
    }

    private static Vector3? FindWaypointFromXUiController()
    {
        try
        {
            // Try to find XUiC_Map controller - this often handles waypoints
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.World == null) return null;
            
            // Try to find the player UI
            var player = gameManager.World.GetPrimaryPlayer();
            if (player == null) return null;
            
            // Try to access the map controller through playerUI
            var playerUI = GetFieldOrProp(player, "playerUI");
            if (playerUI == null) return null;
            
            var windowManager = GetFieldOrProp(playerUI, "windowManager");
            if (windowManager == null) return null;
            
            // Look for map controllers by name
            var windows = GetFieldOrProp(windowManager, "windows") as IDictionary;
            if (windows != null)
            {
                foreach (var key in windows.Keys)
                {
                    if (key.ToString().IndexOf("map", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var window = windows[key];
                        if (window == null) continue;
                        
                        // Look for XUiC_Map controller
                        var controllers = GetFieldOrProp(window, "Controllers") as IEnumerable;
                        if (controllers != null)
                        {
                            foreach (var controller in controllers)
                            {
                                if (controller == null) continue;
                                
                                // Look for waypoint or marker in the controller
                                var waypointProp = controller.GetType().GetProperty("Waypoint", BindingFlags.Instance | BindingFlags.Public) ??
                                                 controller.GetType().GetProperty("ActiveWaypoint", BindingFlags.Instance | BindingFlags.Public);
                                                 
                                if (waypointProp != null)
                                {
                                    var waypoint = waypointProp.GetValue(controller);
                                    if (waypoint != null)
                                    {
                                        // Try to get position from waypoint
                                        var posProp = waypoint.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public);
                                        if (posProp != null)
                                        {
                                            var pos = posProp.GetValue(waypoint);
                                            if (pos != null && pos is Vector3)
                                            {
                                                return (Vector3)pos;
                                            }
                                        }
                                    }
                                }
                                
                                // Alternative: try to find waypoint position directly
                                var waypointPosProp = controller.GetType().GetProperty("WaypointPosition", BindingFlags.Instance | BindingFlags.Public);
                                if (waypointPosProp != null)
                                {
                                    var pos = waypointPosProp.GetValue(controller);
                                    if (pos != null && pos is Vector3)
                                    {
                                        return (Vector3)pos;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Dbg.Info("Error finding waypoint from XUi controller: " + ex.Message);
        }
        
        return null;
    }

    private static Vector3? FindPlayerWaypoint()
    {
        try
        {
            // Try to get the player's waypoint from GameManager
            var gameManager = GameManager.Instance;
            if (gameManager == null) return null;
            
            // Try to access player waypoint through GameManager
            var waypointManager = GetFieldOrProp(gameManager, "WaypointManager") ?? 
                                GetFieldOrProp(gameManager, "PlayerWaypoints") ??
                                GetFieldOrProp(gameManager, "MapWaypoints");
                                
            if (waypointManager != null)
            {
                // Try to get the active/current/selected waypoint
                var activeWaypoint = GetFieldOrProp(waypointManager, "ActiveWaypoint") ??
                                   GetFieldOrProp(waypointManager, "SelectedWaypoint") ??
                                   GetFieldOrProp(waypointManager, "CurrentWaypoint");
                                   
                if (activeWaypoint != null)
                {
                    var posProp = activeWaypoint.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public);
                    if (posProp != null)
                    {
                        var pos = posProp.GetValue(activeWaypoint);
                        if (pos != null && pos is Vector3)
                        {
                            return (Vector3)pos;
                        }
                    }
                }
                
                // If no active waypoint, try to get any player waypoints
                var waypointsList = GetFieldOrProp(waypointManager, "Waypoints") as IEnumerable;
                if (waypointsList != null)
                {
                    foreach (var waypoint in waypointsList)
                    {
                        if (waypoint == null) continue;
                        
                        // Check if it's a player waypoint
                        var isPlayerWaypoint = false;
                        var typeProp = waypoint.GetType().GetProperty("Type", BindingFlags.Instance | BindingFlags.Public) ??
                                     waypoint.GetType().GetProperty("WaypointType", BindingFlags.Instance | BindingFlags.Public);
                                     
                        if (typeProp != null)
                        {
                            var typeObj = typeProp.GetValue(waypoint);
                            if (typeObj != null)
                            {
                                // Usually type 0 is player waypoint/flag
                                if (Convert.ToInt32(typeObj) == 0)
                                {
                                    isPlayerWaypoint = true;
                                }
                            }
                        }
                        
                        // Check if it has a name indicating player waypoint
                        var nameProp = waypoint.GetType().GetProperty("Name", BindingFlags.Instance | BindingFlags.Public);
                        if (nameProp != null)
                        {
                            var nameObj = nameProp.GetValue(waypoint);
                            if (nameObj != null && nameObj.ToString().IndexOf("flag", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                isPlayerWaypoint = true;
                            }
                        }
                        
                        if (isPlayerWaypoint)
                        {
                            var posProp = waypoint.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public);
                            if (posProp != null)
                            {
                                var pos = posProp.GetValue(waypoint);
                                if (pos != null && pos is Vector3)
                                {
                                    return (Vector3)pos;
                                }
                            }
                        }
                    }
                }
            }
            
            // Try to access directly from the player
            var player = gameManager.World?.GetPrimaryPlayer();
            if (player != null)
            {
                var playerWaypoint = GetFieldOrProp(player, "Waypoint") ?? 
                                   GetFieldOrProp(player, "PlayerWaypoint") ??
                                   GetFieldOrProp(player, "CurrentWaypoint");
                                   
                if (playerWaypoint != null)
                {
                    var posProp = playerWaypoint.GetType().GetProperty("Position", BindingFlags.Instance | BindingFlags.Public);
                    if (posProp != null)
                    {
                        var pos = posProp.GetValue(playerWaypoint);
                        if (pos != null && pos is Vector3)
                        {
                            return (Vector3)pos;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Dbg.Info("Error finding player waypoint: " + ex.Message);
        }
        
        return null;
    }

    private static bool HasValidTarget(Vector3 v) => !float.IsNegativeInfinity(v.x);
    private static bool IsInvalid(Vector3 v) => v.x < -90000f && v.y < -90000f && v.z < -90000f;

    private static int ScoreMarkerName(string raw, string display)
    {
        string s = (display ?? raw ?? string.Empty);
        int score = 0;
        if (s.IndexOf("flag", StringComparison.OrdinalIgnoreCase) >= 0) score += 100;
        if (s.IndexOf("rally", StringComparison.OrdinalIgnoreCase) >= 0) score += 80;
        if (s.IndexOf("marker", StringComparison.OrdinalIgnoreCase) >= 0) score += 60;
        if (s.IndexOf("waypoint", StringComparison.OrdinalIgnoreCase) >= 0) score += 50;
        if (s.IndexOf("bedroll", StringComparison.OrdinalIgnoreCase) >= 0) score -= 50;
        if (score == 0) score = 10;
        return score;
    }

    private static void HardStop(AutoState st)
    {
        if (st == null) return;
        st.Target = Vector3.negativeInfinity;
        if (st.Drive != null && st.Drive.IsValid) { st.Drive.SetThrottle(0f); st.Drive.SetSteering(0f); }
        
        if (st.RB != null) 
        {
            // Only try to modify velocity if not kinematic
            if (!st.RB.isKinematic)
            {
                st.RB.velocity = new Vector3(0f, st.RB.velocity.y, 0f); 
                st.RB.angularVelocity = Vector3.zero;
            }
            else
            {
                // For kinematic bodies, try to use controllers
                TryVehicleMovementControl(st.RB.gameObject, 0f, 0f);
            }
        }
    }

    private static void ReleaseControl(AutoState st)
    {
        if (st == null) return;
        if (st.Drive != null && st.Drive.IsValid) { st.Drive.SetThrottle(0f); st.Drive.SetSteering(0f); }
        else if (st.RB != null)
        {
            if (!st.RB.isKinematic)
            {
                var v = st.RB.velocity; st.RB.velocity = new Vector3(0f, v.y, 0f);
                var w = st.RB.angularVelocity; st.RB.angularVelocity = new Vector3(0f, w.y, 0f);
            }
            else
            {
                // For kinematic bodies, try to use controllers
                TryVehicleMovementControl(st.RB.gameObject, 0f, 0f);
            }
        }
    }

    // ---------------- Reflection helpers ----------------
    private static object GetFieldOrProp(object obj, string name)
    {
        if (obj == null || string.IsNullOrEmpty(name)) return null;
        var t = obj.GetType();
        var pi = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (pi != null) return pi.GetValue(obj, null);
        var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fi != null) return fi.GetValue(obj);
        return null;
    }

    private static Component GetComponentNamed(Component owner, string typeName)
    {
        if (owner == null || string.IsNullOrEmpty(typeName)) return null;
        var comps = owner.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            var n = c != null ? c.GetType().Name : null;
            if (!string.IsNullOrEmpty(n) && n.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0) return c;
        }
        return null;
    }

    // ---------------- Vehicle adapter ----------------
    private sealed class VehicleDriveAdapter
    {
        private readonly object _target;
        private readonly Action<float> _setThrottle;
        private readonly Action<float> _setSteering;

        public bool IsValid => _target != null && _setThrottle != null && _setSteering != null;

        private VehicleDriveAdapter(object target, Action<float> throttle, Action<float> steering)
        {
            _target = target; _setThrottle = throttle; _setSteering = steering;
        }

        public void SetThrottle(float v) { _setThrottle?.Invoke(Mathf.Clamp(v, -1f, 1f)); }
        public void SetSteering(float v) { _setSteering?.Invoke(Mathf.Clamp(v, -1f, 1f)); }

        public static VehicleDriveAdapter TryCreate(Entity ent)
        {
            VehicleDriveAdapter TryOnObject(object obj)
            {
                if (obj == null) return null;
                var t = obj.GetType();

                // 1) Preferred methods
                var mSetThr = t.GetMethod("SetThrottle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
                var mSetStr = t.GetMethod("SetSteering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
                if (mSetThr != null && mSetStr != null)
                    return new VehicleDriveAdapter(obj, v => mSetThr.Invoke(obj, new object[] { v }),
                                                   v => mSetStr.Invoke(obj, new object[] { v }));

                // 2) Common alternates in later builds
                var mSetSteerInput = t.GetMethod("SetSteeringInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
                var mSetAccelInput = t.GetMethod("SetAccelInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null)
                                   ?? t.GetMethod("SetMotorInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
                if (mSetSteerInput != null && mSetAccelInput != null)
                    return new VehicleDriveAdapter(obj, v => mSetAccelInput.Invoke(obj, new object[] { v }),
                                                   v => mSetSteerInput.Invoke(obj, new object[] { v }));

                // 3) Combined input pairs: SetInput / ApplyInput (steer, throttle)
                var mSetInput2 = t.GetMethod("SetInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(float) }, null)
                              ?? t.GetMethod("ApplyInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(float) }, null);
                if (mSetInput2 != null)
                    return new VehicleDriveAdapter(obj, v => mSetInput2.Invoke(obj, new object[] { 0f, v }),
                                                   v => mSetInput2.Invoke(obj, new object[] { v, 0f }));

                // 4) Properties (Throttle/AccelInput & Steering/SteerInput)
                var pThr = t.GetProperty("Throttle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? t.GetProperty("AccelInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? t.GetProperty("Motor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var pStr = t.GetProperty("Steering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? t.GetProperty("SteerInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         ?? t.GetProperty("SteeringAngle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pThr != null && pThr.CanWrite && pStr != null && pStr.CanWrite)
                    return new VehicleDriveAdapter(obj, v => pThr.SetValue(obj, v, null),
                                                   v => pStr.SetValue(obj, v, null));

                // 5) Fields
                var fThr = t.GetField("throttle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? t.GetField("Throttle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? t.GetField("m_AccelInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var fStr = t.GetField("steering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? t.GetField("Steering", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? t.GetField("m_SteerInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fThr != null && fStr != null)
                    return new VehicleDriveAdapter(obj, v => fThr.SetValue(obj, v),
                                                   v => fStr.SetValue(obj, v));

                return null;
            }

            // Try entity itself
            var entType = ent.GetType();
            var direct = TryOnObject(ent);
            if (direct != null) { Dbg.Info("Adapter via " + entType.Name); return direct; }

            // Try known controller fields/props on the entity
            var ctrl = GetFieldOrProp(ent, "vehicleController") ?? GetFieldOrProp(ent, "Controller") ?? GetFieldOrProp(ent, "m_Controller");
            var adapterViaCtrl = TryOnObject(ctrl);
            if (adapterViaCtrl != null) { Dbg.Info("Adapter via controller " + (ctrl != null ? ctrl.GetType().Name : "null")); return adapterViaCtrl; }

            // Try any child components that look like Vehicle/Controller/Drive
            var comps = ent.GetComponentsInChildren<Component>(true);
            foreach (var c in comps)
            {
                if (c == null) continue;
                var n = c.GetType().Name;
                if (n.IndexOf("Vehicle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Drive", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var adapterViaComp = TryOnObject(c);
                    if (adapterViaComp != null) { Dbg.Info("Adapter via component " + n); return adapterViaComp; }
                }
            }
            return null;
        }
    }

    // ---------------- Public API ----------------
    // Console command helpers
    public static void ToggleAutoDrive(int vehicleId)
    {
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            st.DesiredSpeed = s_ConfigDesiredSpeed;
            st.ArrivalDistance = s_ConfigArrivalDistance;
            st.FollowPlayer = s_ConfigFollowPlayer;
            st.FollowDistance = s_ConfigFollowDistance;
            States[vehicleId] = st;
        }

        st.AutoDrive = !st.AutoDrive;
        Dbg.Info("Console command -> AutoDrive " + (st.AutoDrive ? "ON" : "OFF") + " for vehicle " + vehicleId);
        
        if (st.AutoDrive) 
        {
            // Find and set target for the vehicle
            var entity = GameManager.Instance?.World?.GetEntity(vehicleId) as Entity;
            if (entity != null && entity.transform != null)
            {
                if (!st.FollowPlayer)
                {
                    AcquireTarget(entity.transform, st);
                }
            }
        }
        else
        {
            HardStop(st);
        }
    }

    public static void SetAutoDrive(int vehicleId, bool enabled)
    {
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            st.DesiredSpeed = s_ConfigDesiredSpeed;
            st.ArrivalDistance = s_ConfigArrivalDistance;
            st.FollowPlayer = s_ConfigFollowPlayer;
            st.FollowDistance = s_ConfigFollowDistance;
            States[vehicleId] = st;
        }

        st.AutoDrive = enabled;
        Dbg.Info("Console command -> AutoDrive " + (st.AutoDrive ? "ON" : "OFF") + " for vehicle " + vehicleId);
        
        if (st.AutoDrive) 
        {
            // Find and set target for the vehicle
            var entity = GameManager.Instance?.World?.GetEntity(vehicleId) as Entity;
            if (entity != null && entity.transform != null)
            {
                if (!st.FollowPlayer)
                {
                    AcquireTarget(entity.transform, st);
                }
            }
        }
        else
        {
            HardStop(st);
        }
    }
    
    public static void DriveToCoordinates(int vehicleId, float x, float z)
    {
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            st.DesiredSpeed = s_ConfigDesiredSpeed;
            st.ArrivalDistance = s_ConfigArrivalDistance;
            st.FollowPlayer = false; // Force disable follow mode when driving to coordinates
            st.FollowDistance = s_ConfigFollowDistance;
            States[vehicleId] = st;
        }
        
        // Get the vehicle entity
        var entity = GameManager.Instance?.World?.GetEntity(vehicleId) as Entity;
        if (entity == null || entity.transform == null)
        {
            Dbg.Info("Cannot find vehicle entity for ID " + vehicleId);
            return;
        }
        
        // Set target and start driving
        st.Target = new Vector3(x, entity.transform.position.y, z);
        st.AutoDrive = true;
        st.FollowPlayer = false; // Force disable follow mode when driving to coordinates
        
        Dbg.Info("AutoDrive targeting coordinates: " + st.Target + " for vehicle " + vehicleId);
    }

    // New methods for configuring autodrive settings
    public static void SetDrivingSpeed(int vehicleId, float speed)
    {
        if (speed <= 0)
        {
            Dbg.Info("Speed must be greater than 0");
            return;
        }
        
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            States[vehicleId] = st;
        }
        
        st.DesiredSpeed = speed;
        // Also update the default for new vehicles
        s_ConfigDesiredSpeed = speed;
        
        Dbg.Info($"Set driving speed to {speed} for vehicle {vehicleId}");
    }
    
    public static void SetArrivalDistance(int vehicleId, float distance)
    {
        if (distance <= 0)
        {
            Dbg.Info("Arrival distance must be greater than 0");
            return;
        }
        
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            States[vehicleId] = st;
        }
        
        st.ArrivalDistance = distance;
        // Also update the default for new vehicles
        s_ConfigArrivalDistance = distance;
        
        Dbg.Info($"Set arrival distance to {distance} for vehicle {vehicleId}");
    }
    
    public static void SetFollowPlayerMode(int vehicleId, bool enabled, float followDistance = 10.0f)
    {
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            States[vehicleId] = st;
        }
        
        st.FollowPlayer = enabled;
        if (followDistance > 0)
        {
            st.FollowDistance = followDistance;
            // Also update the default
            s_ConfigFollowDistance = followDistance;
        }
        
        // Also update the default for new vehicles
        s_ConfigFollowPlayer = enabled;
        
        if (enabled)
        {
            // Start the autodrive if it's not already running
            if (!st.AutoDrive)
            {
                st.AutoDrive = true;
                Dbg.Info($"AutoDrive turned ON with follow mode for vehicle {vehicleId}");
            }
            else
            {
                Dbg.Info($"Follow player mode enabled for vehicle {vehicleId} (distance: {st.FollowDistance}m)");
            }
        }
        else
        {
            Dbg.Info($"Follow player mode disabled for vehicle {vehicleId}");
        }
    }

    private static Vector3? GetPlayerPosition()
    {
        try
        {
            // Try to get player position from GameManager
            var player = GameManager.Instance?.World?.GetPrimaryPlayer();
            if (player != null)
            {
                return player.position;
            }
        }
        catch
        {
            // Silently fail
        }
        
        return null;
    }

    // Try to control the vehicle using movement components
    private static void TryVehicleMovementControl(GameObject vehicle, float steer, float throttle)
    {
        try
        {
            if (vehicle == null) return;
            bool controlApplied = false;
            
            // Get the entity component
            var vehicleEntity = vehicle.GetComponent<EntityVehicle>() ?? 
                                vehicle.GetComponentInParent<EntityVehicle>() ?? 
                                vehicle.GetComponentInChildren<EntityVehicle>();
            
            if (vehicleEntity != null)
            {
                // First try direct entity movement methods (highest priority)
                var moveHelper = GetFieldOrProp(vehicleEntity, "moveHelper") as object;
                if (moveHelper != null)
                {
                    Dbg.Info($"Found moveHelper on vehicle entity");
                    
                    // Try to call MoveToPoint or SetMoveTo
                    var moveToMethod = moveHelper.GetType().GetMethod("MoveToPoint", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (moveToMethod != null)
                    {
                        // Create a target point based on current position, direction and throttle
                        Vector3 forward = vehicle.transform.forward;
                        Vector3 right = vehicle.transform.right;
                        Vector3 targetPoint = vehicle.transform.position + (forward * throttle * 10f) + (right * steer * 5f);
                        moveToMethod.Invoke(moveHelper, new object[] { targetPoint });
                        Dbg.Info($"Applied movement via moveHelper.MoveToPoint()");
                        controlApplied = true;
                    }
                }
                
                // Try to use SetMoveTo on the entity itself
                if (!controlApplied)
                {
                    var entityType = vehicleEntity.GetType();
                    var setMoveToMethod = entityType.GetMethod("SetMoveTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (setMoveToMethod != null)
                    {
                        Vector3 forward = vehicle.transform.forward;
                        Vector3 right = vehicle.transform.right;
                        Vector3 targetPoint = vehicle.transform.position + (forward * throttle * 10f) + (right * steer * 5f);
                        
                        // Check parameter count to determine which overload to call
                        var parameters = setMoveToMethod.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Vector3))
                        {
                            setMoveToMethod.Invoke(vehicleEntity, new object[] { targetPoint });
                            Dbg.Info($"Applied movement via Entity.SetMoveTo(Vector3)");
                            controlApplied = true;
                        }
                        else if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(Vector3))
                        {
                            setMoveToMethod.Invoke(vehicleEntity, new object[] { targetPoint, true });
                            Dbg.Info($"Applied movement via Entity.SetMoveTo(Vector3, bool)");
                            controlApplied = true;
                        }
                    }
                }
                
                // Try common method names for vehicle control
                if (!controlApplied)
                {
                    var entityType = vehicleEntity.GetType();
                    var controlMethod = entityType.GetMethod("SetInputs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? 
                                       entityType.GetMethod("SetVehicleInputs", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                                       entityType.GetMethod("UpdateControls", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (controlMethod != null)
                    {
                        // Check parameter count to determine which overload to call
                        var parameters = controlMethod.GetParameters();
                        if (parameters.Length == 2)
                        {
                            controlMethod.Invoke(vehicleEntity, new object[] { steer, throttle });
                            Dbg.Info($"Applied vehicle control via {entityType.Name}.{controlMethod.Name}({steer}, {throttle})");
                            controlApplied = true;
                        }
                        else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(Vector2))
                        {
                            controlMethod.Invoke(vehicleEntity, new object[] { new Vector2(steer, throttle) });
                            Dbg.Info($"Applied vehicle control via {entityType.Name}.{controlMethod.Name}(Vector2({steer}, {throttle}))");
                            controlApplied = true;
                        }
                    }
                }
            }
            
            // Try to use a Rigidbody directly with much stronger forces
            if (!controlApplied)
            {
                var rb = vehicle.GetComponent<Rigidbody>() ?? vehicle.GetComponentInChildren<Rigidbody>(true);
                if (rb != null)
                {
                    if (rb.isKinematic)
                    {
                        // For kinematic rigidbodies, try to directly modify the transform
                        Vector3 forward = vehicle.transform.forward;
                        
                        // Calculate rotation change
                        float rotationAmount = steer * 2f; // More aggressive rotation
                        vehicle.transform.Rotate(Vector3.up, rotationAmount);
                        
                        // Calculate position change
                        Vector3 movement = forward * throttle * 0.5f; // More aggressive movement
                        vehicle.transform.position += movement;
                        
                        Dbg.Info($"Applied direct transform changes to kinematic vehicle");
                        controlApplied = true;
                    }
                    else
                    {
                        // For normal rigidbodies, apply stronger forces
                        rb.AddTorque(Vector3.up * (steer * 5000f), ForceMode.Force); // Much stronger torque
                        rb.AddForce(vehicle.transform.forward * throttle * 2000f, ForceMode.Force); // Much stronger force
                        Dbg.Info($"Applied strong rigidbody forces to vehicle");
                        controlApplied = true;
                    }
                }
            }
            
            // If nothing else worked, try to find and invoke a character controller
            if (!controlApplied)
            {
                var characterController = vehicle.GetComponentInChildren<CharacterController>(true);
                if (characterController != null)
                {
                    // Create a movement vector based on desired direction with MUCH higher speed
                    Vector3 moveDir = vehicle.transform.forward * throttle + vehicle.transform.right * steer;
                    characterController.Move(moveDir * DefaultDesiredSpeed * 2f * Time.deltaTime); // Use Time.deltaTime for frame-rate independence
                    Dbg.Info("Applied movement via CharacterController.Move() with increased speed");
                    controlApplied = true;
                }
            }
            
            if (!controlApplied)
            {
                Dbg.Info("Warning: No suitable vehicle control method found");
            }
        }
        catch (Exception ex)
        {
            Dbg.Info($"TryVehicleMovementControl exception: {ex.Message}");
        }
    }

    // Road-following methods
    public static void ToggleRoadFollow(int vehicleId)
    {
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            st.DesiredSpeed = s_ConfigDesiredSpeed;
            st.ArrivalDistance = s_ConfigArrivalDistance;
            st.FollowPlayer = s_ConfigFollowPlayer;
            st.FollowDistance = s_ConfigFollowDistance;
            States[vehicleId] = st;
        }

        st.RoadFollow = !st.RoadFollow;
        Dbg.Info($"Road follow mode {(st.RoadFollow ? "enabled" : "disabled")} for vehicle {vehicleId}");
    }
    
    public static void SetRoadFollow(int vehicleId, bool enabled)
    {
        if (!States.TryGetValue(vehicleId, out var st))
        {
            st = new AutoState();
            st.DesiredSpeed = s_ConfigDesiredSpeed;
            st.ArrivalDistance = s_ConfigArrivalDistance;
            st.FollowPlayer = s_ConfigFollowPlayer;
            st.FollowDistance = s_ConfigFollowDistance;
            States[vehicleId] = st;
        }

        st.RoadFollow = enabled;
        Dbg.Info($"Road follow mode {(enabled ? "enabled" : "disabled")} for vehicle {vehicleId}");
    }
}
