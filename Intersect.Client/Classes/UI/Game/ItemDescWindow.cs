﻿using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.General;
using Intersect.Client.Localization;
using Intersect.Enums;
using Intersect.GameObjects;

namespace Intersect.Client.UI.Game
{
    public class ItemDescWindow
    {
        ImagePanel mDescWindow;

        public ItemDescWindow(ItemBase item, int amount, int x, int y, int[] statBuffs, string titleOverride = "",
            string valueLabel = "")
        {
            var title = titleOverride;
            if (string.IsNullOrWhiteSpace(title))
                title = item.Name;

            mDescWindow = new ImagePanel(Gui.GameUi.GameCanvas, "ItemDescWindow");
            
            if (item != null)
            {
                ImagePanel icon = new ImagePanel(mDescWindow, "ItemIcon");

                Label itemName = new Label(mDescWindow, "ItemNameLabel");
                itemName.Text = title;

                if (amount > 1)
                {
                    itemName.Text += " " + Strings.ItemDesc.quantity.ToString( amount);
                }

                itemName.AddAlignment(Alignments.CenterH);

                Label itemType = new Label(mDescWindow, "ItemTypeLabel");
                Label itemValue = new Label(mDescWindow, "ItemValueLabel");
                itemType.Text = Strings.ItemDesc.itemtypes[(int)item.ItemType];
                itemValue.SetText(valueLabel);

                if (item.ItemType == ItemTypes.Equipment && item.EquipmentSlot > 0 && item.EquipmentSlot < Options.EquipmentSlots.Count)
                {
                    itemType.Text = Options.EquipmentSlots[item.EquipmentSlot];
                    if (item.EquipmentSlot == Options.WeaponIndex && item.TwoHanded)
                    {
                        itemType.Text += " - " + Strings.ItemDesc.twohand;
                    }
                }
                RichLabel itemDesc = new RichLabel(mDescWindow, "ItemDescription");
                //Load this up now so we know what color to make the text when filling out the desc
                mDescWindow.LoadJsonUi(GameContentManager.UI.InGame, GameGraphics.Renderer.GetResolutionString());
                if (item.Description.Length > 0)
                {
                    itemDesc.AddText(Strings.ItemDesc.desc.ToString( item.Description), itemDesc.RenderColor);
                    itemDesc.AddLineBreak();
                    itemDesc.AddLineBreak();
                }

                string stats = "";
                if (item.ItemType == ItemTypes.Equipment)
                {
                    stats = Strings.ItemDesc.bonuses;
                    itemDesc.AddText(stats, itemDesc.RenderColor);
                    itemDesc.AddLineBreak();
                    if (item.ItemType == ItemTypes.Equipment && item.EquipmentSlot == Options.WeaponIndex)
                    {
                        stats = Strings.ItemDesc.damage.ToString( item.Damage);
                        itemDesc.AddText(stats, itemDesc.RenderColor);
                        itemDesc.AddLineBreak();
                    }
                    if (statBuffs != null)
                    {
                        for (int i = 0; i < Options.MaxStats; i++)
                        {
                            stats = Strings.ItemDesc.stats[i].ToString((item.StatsGiven[i] + statBuffs[i]));
                            itemDesc.AddText(stats, itemDesc.RenderColor);
                            itemDesc.AddLineBreak();
                        }
                    }
                }

                if (item.ItemType == ItemTypes.Equipment && item.Effect.Type != EffectType.None && item.Effect.Percentage > 0)
                {
                    itemDesc.AddText(
                        Strings.ItemDesc.effect.ToString( item.Effect.Percentage,
                            Strings.ItemDesc.effects[(int)item.Effect.Type - 1]), itemDesc.RenderColor);
                }

                //Load Again for positioning purposes.
                mDescWindow.LoadJsonUi(GameContentManager.UI.InGame, GameGraphics.Renderer.GetResolutionString());
                var itemTex = Globals.ContentManager.GetTexture(GameContentManager.TextureType.Item, item.Icon);
                if (itemTex != null)
                {
                    icon.Texture = itemTex;
                }
                itemDesc.SizeToChildren(false, true);
                mDescWindow.SetPosition(x, y);
            }
        }

        public void Dispose()
        {
            Gui.GameUi?.GameCanvas?.RemoveChild(mDescWindow, false);
            mDescWindow.Dispose();
        }
    }
}