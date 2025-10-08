using System.Collections.Generic;
using System.Text;
using System.Reflection;
using UnityEngine;
using System;

/// <summary>
/// Adds console commands for controlling autodrive functionality
/// </summary>
public class ConsoleCmdAutoDrive : ConsoleCmdAbstract
{
    // Command name for usage in console (lowercase)
    public override string[] getCommands() => new string[] { "autodrive", "ad" };

    // Command description shown in help (lowercase)
    public override string getDescription() => "Controls autodrive functionality";

    // Command syntax examples
    public override string GetHelp() =>
        "Usage:\n" +
        "  autodrive toggle - Toggle autodrive on/off for your current vehicle\n" +
        "  autodrive on - Turn autodrive on for your current vehicle\n" +
        "  autodrive off - Turn autodrive off for your current vehicle\n" +
        "  autodrive goto <x> <z> - Drive to specific coordinates (e.g., autodrive goto 1200 -800)\n" +
        "  autodrive speed <speed> - Set autodrive speed (default: 10.0)\n" +
        "  autodrive arrival <distance> - Set arrival distance (default: 2.0)\n" +
        "  autodrive follow [<distance>] - Enable follow player mode with optional distance\n" +
        "  autodrive nofollow - Disable follow player mode\n" +
        "  autodrive debug - Show debug info about current vehicle for troubleshooting\n" +
        "Shortcuts:\n" +
        "  ad toggle - Shortcut for autodrive toggle\n" +
        "  ad on - Shortcut for autodrive on\n" +
        "  ad off - Shortcut for autodrive off\n" +
        "  ad goto <x> <z> - Shortcut for autodrive goto\n" +
        "  ad speed <speed> - Shortcut for autodrive speed\n" +
        "  ad follow [<distance>] - Shortcut for autodrive follow";

    // Execute the command
    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            // Must have at least one parameter
            if (_params.Count < 1)
            {
                SdtdConsole.Instance.Output("Error: Missing parameters. Type 'help autodrive' for usage.");
                return;
            }

            // Get the command action
            string action = _params[0].ToLower();

            // Find the player's entity - try the local player first, which should work in most cases
            EntityPlayer player = GameManager.Instance?.World?.GetPrimaryPlayer();
            
            if (player == null)
            {
                SdtdConsole.Instance.Output("Error: Could not find player.");
                return;
            }

            // Check if player is in a vehicle
            if (player.AttachedToEntity == null)
            {
                SdtdConsole.Instance.Output("Error: You are not in a vehicle.");
                return;
            }

            // Get vehicle entity ID
            int vehicleId = player.AttachedToEntity.entityId;

            // Process the command based on action
            switch (action)
            {
                case "toggle":
                    MinEventActionAutoDrive.ToggleAutoDrive(vehicleId);
                    break;

                case "on":
                    MinEventActionAutoDrive.SetAutoDrive(vehicleId, true);
                    break;

                case "off":
                    MinEventActionAutoDrive.SetAutoDrive(vehicleId, false);
                    break;
                    
                case "goto":
                    // Check if we have enough parameters for coordinates
                    if (_params.Count < 3)
                    {
                        SdtdConsole.Instance.Output("Error: Missing coordinates. Usage: autodrive goto <x> <z>");
                        return;
                    }
                    
                    // Parse the coordinates
                    if (float.TryParse(_params[1], out float x) && float.TryParse(_params[2], out float z))
                    {
                        MinEventActionAutoDrive.DriveToCoordinates(vehicleId, x, z);
                    }
                    else
                    {
                        SdtdConsole.Instance.Output("Error: Invalid coordinates. Usage: autodrive goto <x> <z>");
                    }
                    break;

                case "speed":
                    // Check if we have enough parameters for speed
                    if (_params.Count < 2)
                    {
                        SdtdConsole.Instance.Output("Error: Missing speed value. Usage: autodrive speed <speed>");
                        return;
                    }
                    
                    // Parse the speed
                    if (float.TryParse(_params[1], out float speed))
                    {
                        MinEventActionAutoDrive.SetDrivingSpeed(vehicleId, speed);
                    }
                    else
                    {
                        SdtdConsole.Instance.Output("Error: Invalid speed value. Usage: autodrive speed <speed>");
                    }
                    break;

                case "arrival":
                    // Check if we have enough parameters for arrival distance
                    if (_params.Count < 2)
                    {
                        SdtdConsole.Instance.Output("Error: Missing distance value. Usage: autodrive arrival <distance>");
                        return;
                    }
                    
                    // Parse the arrival distance
                    if (float.TryParse(_params[1], out float distance))
                    {
                        MinEventActionAutoDrive.SetArrivalDistance(vehicleId, distance);
                    }
                    else
                    {
                        SdtdConsole.Instance.Output("Error: Invalid distance value. Usage: autodrive arrival <distance>");
                    }
                    break;

                case "follow":
                    float followDist = 10.0f; // Default follow distance
                    
                    // Check if we have a distance parameter
                    if (_params.Count >= 2 && float.TryParse(_params[1], out float customDist))
                    {
                        followDist = customDist;
                    }
                    
                    MinEventActionAutoDrive.SetFollowPlayerMode(vehicleId, true, followDist);
                    break;

                case "nofollow":
                    MinEventActionAutoDrive.SetFollowPlayerMode(vehicleId, false);
                    break;

                case "debug":
                    // Show detailed vehicle info
                    ShowVehicleDebugInfo(player.AttachedToEntity as Entity);
                    break;

                default:
                    SdtdConsole.Instance.Output("Error: Unknown action. Type 'help autodrive' for usage.");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            Log.Error("Error executing AutoDrive command: " + ex.Message);
            SdtdConsole.Instance.Output("Error executing command: " + ex.Message);
        }
    }
    
    // Helper method to dump detailed vehicle info for debugging
    private void ShowVehicleDebugInfo(Entity vehicle)
    {
        if (vehicle == null)
        {
            SdtdConsole.Instance.Output("No vehicle entity found.");
            return;
        }
        
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"--- AutoDrive Vehicle Debug Info ---");
        sb.AppendLine($"Vehicle ID: {vehicle.entityId}");
        sb.AppendLine($"Vehicle Type: {vehicle.GetType().Name}");
        
        // Basic transform info
        sb.AppendLine($"Position: {vehicle.transform.position}");
        sb.AppendLine($"Rotation: {vehicle.transform.eulerAngles}");
        sb.AppendLine($"Scale: {vehicle.transform.localScale}");
        
        // Rigidbody info
        var rb = vehicle.GetComponent<Rigidbody>();
        if (rb != null)
        {
            sb.AppendLine($"Rigidbody Info:");
            sb.AppendLine($"  IsKinematic: {rb.isKinematic}");
            sb.AppendLine($"  Mass: {rb.mass}");
            sb.AppendLine($"  Drag: {rb.drag}");
            sb.AppendLine($"  AngularDrag: {rb.angularDrag}");
            sb.AppendLine($"  UseGravity: {rb.useGravity}");
            sb.AppendLine($"  Velocity: {rb.velocity}, magnitude: {rb.velocity.magnitude}");
            sb.AppendLine($"  AngularVelocity: {rb.angularVelocity}, magnitude: {rb.angularVelocity.magnitude}");
        }
        else
        {
            sb.AppendLine("No Rigidbody found on vehicle");
        }
        
        // Look for possible control interfaces
        sb.AppendLine("\nVehicle Components:");
        var comps = vehicle.GetComponentsInChildren<Component>(true);
        foreach (var comp in comps)
        {
            if (comp == null) continue;
            
            // Get the component type name
            string typeName = comp.GetType().Name;
            
            // Skip common components that aren't relevant for control
            if (typeName == "Transform" || typeName == "RectTransform" || 
                typeName == "MeshFilter" || typeName == "MeshRenderer" || 
                typeName.Contains("Collider") || typeName.Contains("Audio"))
                continue;
                
            sb.AppendLine($"  - {typeName}");
            
            // Special handling for potentially interesting components
            if (typeName.Contains("Vehicle") || typeName.Contains("Control") || 
                typeName.Contains("Drive") || typeName.Contains("Motor"))
            {
                sb.AppendLine($"    Detailed inspection of {typeName}:");
                
                // Look for methods related to vehicle control
                var type = comp.GetType();
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    string methodName = method.Name;
                    // Skip common methods that aren't relevant for control
                    if (methodName.StartsWith("get_") || methodName.StartsWith("set_") ||
                        methodName.StartsWith("Add") || methodName.StartsWith("Remove") ||
                        methodName.StartsWith("On") || methodName == "ToString" || 
                        methodName == "GetHashCode" || methodName == "Equals" ||
                        methodName == "GetType")
                        continue;
                        
                    // Report methods that could be relevant for vehicle control
                    if (methodName.Contains("Move") || methodName.Contains("Drive") || 
                        methodName.Contains("Control") || methodName.Contains("Steer") || 
                        methodName.Contains("Throttle") || methodName.Contains("Input"))
                    {
                        var parameters = method.GetParameters();
                        sb.Append($"      Method: {methodName}(");
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            sb.Append($"{parameters[i].ParameterType.Name} {parameters[i].Name}");
                            if (i < parameters.Length - 1) sb.Append(", ");
                        }
                        sb.AppendLine(")");
                    }
                }
                
                // Check for key properties
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    // Report properties that could be relevant for vehicle control
                    if (prop.Name.Contains("Speed") || prop.Name.Contains("Steering") || 
                        prop.Name.Contains("Throttle") || prop.Name.Contains("Acceleration") ||
                        prop.Name.Contains("Input"))
                    {
                        try
                        {
                            object value = prop.GetValue(comp, null);
                            sb.AppendLine($"      Property: {prop.Name} = {value}");
                        }
                        catch
                        {
                            sb.AppendLine($"      Property: {prop.Name} (unable to read value)");
                        }
                    }
                }
            }
        }
        
        // Report animator info if present
        var animator = vehicle.GetComponent<Animator>();
        if (animator != null)
        {
            sb.AppendLine("\nAnimator Info:");
            sb.AppendLine($"  Animator enabled: {animator.enabled}");
            sb.AppendLine($"  Has controller: {animator.runtimeAnimatorController != null}");
            
            // Try to access parameters
            sb.AppendLine("  Parameters:");
            foreach (var param in animator.parameters)
            {
                sb.AppendLine($"    - {param.name} ({param.type})");
            }
        }
        
        SdtdConsole.Instance.Output(sb.ToString());
    }
}