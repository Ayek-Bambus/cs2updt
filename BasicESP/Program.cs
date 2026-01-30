using BoneESP;
using Swed64;
using System.Numerics;
using System.Threading.Tasks;
using System.IO;

// Check for updates from GitHub before starting main logic
string repoSpecPath = Path.Combine(AppContext.BaseDirectory, "update_repo.txt");
if (File.Exists(repoSpecPath))
{
    try
    {
        string repoSpec = File.ReadAllText(repoSpecPath).Trim();
        if (!string.IsNullOrEmpty(repoSpec) && repoSpec.Contains('/'))
        {
            var parts = repoSpec.Split('/');
            var task = Updater.CheckAndRunUpdateAsync(parts[0], parts[1]);
            task.Wait();
            if (task.Result)
            {
                return; // update launched, exit current process
            }
        }
    }
    catch { }
}

Swed swed = new Swed("cs2");

IntPtr client = swed.GetModuleBase("client.dll");

Reader reader = new Reader(swed);

Renderer renderer = new Renderer();
renderer.Start().Wait();

List<Entity> entities = new List<Entity>();
Entity localPlayer = new Entity();
Vector2 screen = new Vector2(1920, 1080);

renderer.overlaySize = screen;

while (true)
{
    entities.Clear();
    // Console.Clear(); // Removed to prevent flickering

    IntPtr entityList = swed.ReadPointer(client, Offsets.dwEntityList);
    IntPtr localPlayerController = swed.ReadPointer(client, Offsets.dwLocalPlayerController);
    
    // Resolve LocalPlayer Pawn via Handle (Robust Method)
    int localPawnHandle = swed.ReadInt(localPlayerController, Offsets.m_hPlayerPawn);
    IntPtr localListEntry2 = swed.ReadPointer(entityList, 0x8 * ((localPawnHandle & 0x7FFF) >> 9) + 0x10);
    localPlayer.pawnAddress = swed.ReadPointer(localListEntry2, 112 * (localPawnHandle & 0x1FF));
    
    localPlayer.team = swed.ReadInt(localPlayer.pawnAddress, Offsets.m_iTeamNum);
    localPlayer.origin = swed.ReadVec(localPlayer.pawnAddress, Offsets.m_vOldOrigin);

    // Loop through entity list (Max 64 players)
    for (int i = 0; i < 64; i++)
    {
        // 1. Get List Entry for Controller
        IntPtr listEntry = swed.ReadPointer(entityList, (0x8 * (i & 0x7FFF) >> 9) + 0x10);
        if (listEntry == IntPtr.Zero) continue;
        
        // 2. Get Controller (Stride 112 - as per C++ reference)
        IntPtr currentController = swed.ReadPointer(listEntry, 112 * (i & 0x1FF));
        if (currentController == IntPtr.Zero) continue;
        
        if (currentController == localPlayerController) continue;

        // 3. Get Pawn Handle
        int pawnHandle = swed.ReadInt(currentController, Offsets.m_hPlayerPawn);
        if (pawnHandle == 0) continue;

        // 4. Get List Entry for Pawn
        IntPtr listEntry2 = swed.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
        if (listEntry2 == IntPtr.Zero) continue;

        // 5. Get Pawn (Stride 112)
        IntPtr currentPawn = swed.ReadPointer(listEntry2, 112 * (pawnHandle & 0x1FF));
        if (currentPawn == IntPtr.Zero) continue;
        if (currentPawn == localPlayer.pawnAddress) continue;

        // 6. Check Life State & Health
        // Remove lifeState check for now to be safe
        // int lifeState = swed.ReadInt(currentPawn, Offsets.m_lifeState);
        // if (lifeState != 256) continue;
        uint lifeState = 256; // Mock value to satisfy Entity struct

        int health = swed.ReadInt(currentPawn, Offsets.m_iHealth);

        if (health <= 0 || health > 100) continue;

        int team = swed.ReadInt(currentPawn, Offsets.m_iTeamNum);
        if (team == localPlayer.team) continue; // Teammate check

        // 7. Get Bone Matrix
        IntPtr sceneNode = swed.ReadPointer(currentPawn, Offsets.m_pGameSceneNode);
        
        // Probe multiple offsets to find the correct BoneMatrix
        IntPtr boneMatrix = swed.ReadPointer(sceneNode, 0x1E0);
        if (boneMatrix == IntPtr.Zero) boneMatrix = swed.ReadPointer(sceneNode, 0x1F0);
        if (boneMatrix == IntPtr.Zero) boneMatrix = swed.ReadPointer(sceneNode, 0x210); // C++ reference often uses this or similar

        ViewMatrix viewMatrix = reader.readMatrix(client + Offsets.dwViewMatrix);

        Entity entity = new Entity();
        entity.pawnAddress = currentPawn;
        entity.controllerAdress = currentController;
        entity.team = team;
        entity.health = health;
        entity.lifeState = (uint)lifeState;
        entity.origin = swed.ReadVec(currentPawn, Offsets.m_vOldOrigin);
        entity.distance = Vector3.Distance(localPlayer.origin, entity.origin);
        entity.bones = reader.ReadBones(boneMatrix);
        entity.bones2d = reader.ReadBones2d(entity.bones, viewMatrix, screen);
        
        entities.Add(entity);
        }
    
    // STATUS OUTPUT
    Console.SetCursorPosition(0, 0);
    Console.WriteLine($"BoneESP Active | Entities Found: {entities.Count}      ");
    
    // Create a thread-safe copy of the list for the renderer
    renderer.entitiesCopy = new List<Entity>(entities);
    renderer.localPlayerCopy = localPlayer;
    Thread.Sleep(3);
}
