using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinEventAction_DamageInspection : MinEventActionTargetedBase
{
    public static GameObject displayObject = null;
    public static List<Material> materials = new List<Material>();
    public static int numMaterials = 7;
    public static Material stabilityMtrl = null;
    public static GameObject StabilityViewBoxes = null;
    private Vector3i startPos;
    public static Dictionary<Vector3i, GameObject> boxes = new Dictionary<Vector3i, GameObject>();
    public static List<Vector3i> buildingChunks = new List<Vector3i>();
    public static List<GameObject> damageBlocks = new List<GameObject>();
    public bool worldIsReady;     
    public static bool bGatheringChunks = false;
    public static int TotalIterations = 0;
    public static int GetBlocks = 0;
    public int searchSizeXZ = 5;
    public int searchSizeY = 2;
    public bool destroy = false;
    private RiseHelp riseHelp = new RiseHelp();

    // <triggered_effect trigger = "onSelfBuffUpdate" action="_DamageInspection, Rise_From_The_Ashes" />

    public MinEventAction_DamageInspection() : base()
    {
        Log.Out("Creating _DamageInspection");
        displayObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        displayObject.transform.localScale = Vector3.one * 1.01f;
        displayObject.name = "DamageIndicator";
        displayObject.SetActive(true);

        Object.Destroy(displayObject.GetComponent<BoxCollider>());
        Color yellow = new Color(250 / 255f, 245f / 255f, 212f / 255f, 0.15f);
        // color is controlled like this
        displayObject.GetComponent<Renderer>().material.color = yellow;
        displayObject.GetComponent<Renderer>().enabled = true;
    }

    public override void Execute(MinEventParams _params)
    {
        Log.Out("Executing DamageInspection");
        var entity = _params.Self as EntityPlayerLocal;
        if (entity == null)
            return;

        Log.Out("Player is local");        
        Vector3i vector3I = new Vector3i(entity.position);        
        startPos = vector3I;
        
        BuildStabilityBlocks(vector3I);
        //FindDamageBlocks(entity);

    }

    public void BuildStabilityBlocks(Vector3i _startPos)
    {
        destroy = false;
        startPos = _startPos;
        GameManager.Instance.StartCoroutine(RegisterWhenDone());        
    }

    private IEnumerator RegisterWhenDone()
    {        
        yield return new WaitForSeconds(0.01f);
        List<Vector3i> positions = null;
        Chunk chunk = (Chunk)GameManager.Instance.World.ChunkClusters[0].GetChunkFromWorldPos(startPos);
        if (chunk == null)
        {
            lock (boxes)
            {
                lock (buildingChunks)
                {
                    buildingChunks.Remove(startPos);
                    boxes[startPos] = null;
                }
            }
        }
        else if (chunk.GetAvailable())
        {
            if (chunk.IsEmpty())
            {
                lock (boxes)
                {
                    lock (buildingChunks)
                    {
                        buildingChunks.Remove(startPos);
                        boxes[startPos] = null;
                    }
                }
            }
            else
            {
                positions = riseHelp.GetBlocks(chunk, searchSizeXZ, searchSizeY, startPos,RiseHelp.Check.DamagedBlocks);
            }

            if (damageBlocks.Count == 0)
            {                
                Log.Out("Making DamageIndicator Objects");             
                if (positions != null)
                {

                    foreach (Vector3i position in positions)
                    {
                        Vector3 cubePosition  = position - new Vector3i(Origin.position);                        
                        Log.Out("Adding damage box to a block");
                        Log.Out("List Position = " + cubePosition.ToString());
                        cubePosition += new Vector3(0.5f, 0.5f, 0.5f);
                        BlockValue blockValue = GameManager.Instance.World.GetBlock(position);
                        InstantiateCube(cubePosition, blockValue.damage, blockValue.Block.MaxDamage);                       
                    }
                } 
            }           
        }
    }

    public void InstantiateCube(Vector3 position,int damage, int maxDamage)
    {
        float percent = 100 - (((float)damage / (float)maxDamage) * 100f);
        Log.Out("Damage : " + damage.ToString() + " Max : " + maxDamage.ToString() + " Percent : " + percent.ToString());
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        
        cube.transform.position = position;

        // Set scale 5% larger than default
        Vector3 newScale = cube.transform.localScale * 1.01f; // 1% larger
        cube.transform.localScale = newScale;

        // Set transparency to 25%
        Renderer renderer = cube.GetComponent<Renderer>();
        Material material = renderer.material;
        Color color = Color.white;
        if (percent > 0 & percent < 20)
        {
            color = Color.red;
        }
        else if (percent >= 20 & percent < 40)
        {
            color = new Color32(230, 154, 122, 63);
        }
        else if (percent >= 40 & percent <60)
        {
            color = new Color32(224, 161, 52, 63);
        }
        else if (percent >= 60 & percent < 80)
        {
            color = new Color32(206, 209, 117, 63);
            
        }
        else if (percent >= 80)
        {
            color = new Color32(230, 232, 181, 63);
            
        }
        color.a = 0.25f;
        material.color = color;
        material.shader = Shader.Find("Transparent/Diffuse");
        cube.GetComponent<Collider>().enabled = false;
        cube.AddComponent<SelfDestruct>();
        
        Log.Out("InstantiateCube Executed");
    }
}