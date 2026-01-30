using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using ClickableTransparentOverlay;
using ImGuiNET;
using System.Linq.Expressions;
using System.Runtime.InteropServices;

namespace BoneESP
{
    public class Renderer : Overlay
    {
        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        public Vector2 overlaySize = new Vector2(1920, 1080);
        Vector2 windowLocation = new Vector2(0, 0);
        public List<Entity> entitiesCopy = new List<Entity>();
        public Entity localPlayerCopy = new Entity();
        ImDrawListPtr drawList;
        public bool esp = true;
        public bool showMenu = true;
        bool menuKeyPressed = false;
        Vector4 teamColor = new Vector4(1, 1, 1, 1);
        Vector4 enemyColor = new Vector4(1, 1, 1, 1);

        float boneThickness = 4;

        protected override void Render()
        {
            if (GetAsyncKeyState(0x24) < 0) // VK_HOME, for VK_INSERT use 0x2D
            {
                if (!menuKeyPressed)
                {
                    showMenu = !showMenu;
                    menuKeyPressed = true;
                }
            }
            else
            {
                menuKeyPressed = false;
            }

            if (showMenu)
            {
                ImGui.Begin("Bone Esp Menu");
                ImGui.Checkbox("esp", ref esp);
                ImGui.SliderFloat("bone thickness", ref boneThickness, 4, 20);

                if (ImGui.CollapsingHeader("team color"))
                    ImGui.ColorPicker4("###teamcolor", ref teamColor);

                if (ImGui.CollapsingHeader("enemy color"))
                    ImGui.ColorPicker4("###enemycolor", ref enemyColor);
                
                ImGui.End(); // End the menu window properly
            }

            if (esp)
            {
                DrawOverlay();
                DrawSkeletons();
                ImGui.End(); // End the overlay window properly
            }
        }

        void DrawSkeletons()
        {
            if (entitiesCopy == null || entitiesCopy.Count == 0)
                return;

            List<Entity> tempEntities = new List<Entity>(entitiesCopy).ToList();

            drawList = ImGui.GetWindowDrawList();
            uint uintColor;

            foreach (Entity entity in tempEntities)
            {
                if (entity == null)
                    continue;

                uintColor = localPlayerCopy.team == entity.team
                    ? ImGui.ColorConvertFloat4ToU32(teamColor)
                    : ImGui.ColorConvertFloat4ToU32(enemyColor);

                if (
                    entity.bones2d[2].X > 0 &&
                    entity.bones2d[2].Y > 0 &&
                    entity.bones2d[2].X < overlaySize.X &&
                    entity.bones2d[2].Y < overlaySize.Y
                )
                {
                    float currentBoneThickness = boneThickness; // Disable distance scaling for now to ensure visibility

                    drawList.AddLine(entity.bones2d[1], entity.bones2d[2], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[3], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[6], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[3], entity.bones2d[4], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[6], entity.bones2d[7], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[4], entity.bones2d[5], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[7], entity.bones2d[8], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[1], entity.bones2d[0], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[0], entity.bones2d[9], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[0], entity.bones2d[11], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[9], entity.bones2d[10], uintColor, currentBoneThickness);
                    drawList.AddLine(entity.bones2d[11], entity.bones2d[12], uintColor, currentBoneThickness);
                    drawList.AddCircleFilled(entity.bones2d[2], 3 + currentBoneThickness, uintColor);
                }
            }
        }

        void DrawOverlay()
        {
            ImGui.SetNextWindowSize(overlaySize);
            ImGui.SetNextWindowPos(windowLocation);
            ImGui.Begin(
                "overlay",
                ImGuiWindowFlags.NoDecoration |
                ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoBringToFrontOnFocus |
                ImGuiWindowFlags.NoMove |
                ImGuiWindowFlags.NoInputs |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse
            );
        }
    }
}
