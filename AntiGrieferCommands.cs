using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Eco.Gameplay.Components;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Items;
using Eco.Gameplay.Utils;
using Eco.Gameplay.Systems.Chat;
using Eco.Gameplay.Systems.TextLinks;
using Eco.Gameplay.Players;
using Eco.Shared.IoC;
using Eco.Shared.Math;
using Eco.Shared.Utils;
using Eco.Shared.Localization;
using Eco.World.Blocks;
using Eco.Mods.TechTree;

namespace AntiGrieferCommands
{
    public class AntiGrieferCommands : IChatCommandHandler
    {
        [ChatSubCommand("Manage", "Search in storages/inventories containing item", "searchstorages", ChatAuthorizationLevel.Admin)]
        public static void SearchStorages(User user, string itemName)
        {
            var searchItem = CommandsUtil.ClosestMatchingEntity(user.Player, itemName, Item.AllItems, x => x.GetType().Name, x => x.DisplayName);
            if (searchItem == null)
            {
                user.MsgLocStr($"Object {itemName} unknown.");
                return;
            }

            int total = 0;
            IEnumerable<StorageComponent> storages = WorldObjectUtil.AllObjsWithComponent<StorageComponent>();
            foreach (StorageComponent storage in storages)
            {
                if(storage.Inventory.Stacks.Where(slot => slot.Item != null && slot.Item.GetType().IsEquivalentTo(searchItem.GetType())).Count() > 0)
                {
                    user.MsgLocStr($"Found item here: {storage.Parent.UILink()}");
                    total++;
                }
            }

            foreach (User u in UserManager.Users)
            {
                bool found = u.Inventory.ToolbarBackpack.Stacks.Where(slot => slot.Item != null && slot.Item.GetType().IsEquivalentTo(searchItem.GetType())).ToArray().Length > 0;

                if (!found)
                    found = u.Inventory.Carried.Stacks.Where(slot => slot.Item != null && slot.Item.GetType().IsEquivalentTo(searchItem.GetType())).ToArray().Length > 0;

                if (found)
                {
                    user.MsgLocStr($"User {u.UILink()} is carrying a {searchItem.UILink()}.");
                    total++;
                }
            }

            if (total == 0)
                user.MsgLocStr($"No storages with {searchItem.UILink()} found.");
        }

        [ChatSubCommand("Manage", "Search in World for placeable objects, blocks are not supported yet.", "searchworld", ChatAuthorizationLevel.Admin)]
        public static void SearchWorld(User user, string itemName)
        {
            Item searchItem = CommandsUtil.ClosestMatchingEntity(user.Player, itemName, Item.AllItems, x => x.GetType().Name, x => x.DisplayName);
            if (searchItem == null)
            {
                user.MsgLocStr($"Object {itemName} unknown.");
                return;
            }

            if (searchItem.GetType().IsSubclassOf(typeof(BlockItem)))
            {
                user.MsgLocStr("It is not possible to search for block items yet.");
                return;
            }

            if (!(searchItem.GetType().IsSubclassOf(typeof(WorldObjectItem)) || searchItem.GetType().IsSubclassOf(typeof(BlockItem))))
            {
                user.MsgLocStr($"{searchItem.UILink()} is not a placeable object.");
                return;
            }

            IEnumerable<LocString> contents = ServiceHolder<IWorldObjectManager>.Obj.All
                .Where(worldObject => worldObject.GetType() == searchItem.GetType())
                .Select(worldObject => (worldObject: worldObject, distance: Vector3.Distance(worldObject.Position, user.Player.Position)))
                .OrderBy(x => x.distance)
                .Select(x => x.worldObject.UILink(x.worldObject.UILinkContent().Concat($" {Text.Info(ShortLocs.Meters(x.distance))}")));

            if (contents.Count() > 0)
                user.MsgLocStr($"Found {searchItem.UILink()} here:{Environment.NewLine}{contents.NewlineList()}");
            else
                user.MsgLocStr($"No {searchItem.UILink()} found in the world.");
        }

        [ChatSubCommand("Manage", "Lists user inventory", "listinventory", ChatAuthorizationLevel.Admin)]
        public static void ListInventory(User user, User victim)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ItemStack stack in victim.Inventory.ToolbarBackpack.Stacks.Where(stack => stack.Quantity > 0))
                sb.AppendLineLoc($"{stack.Quantity}x {stack.Item.UILink()}");

            foreach (ItemStack stack in victim.Inventory.Carried.Stacks.Where(stack => stack.Quantity > 0))
                sb.AppendLineLoc($"{stack.Quantity}x {stack.Item.UILink()}");

            if(sb.Length > 0)
                user.MsgLocStr(sb.ToString());
            else
                user.MsgLocStr($"{victim.UILink()} has absolutely nothing, that poor person.");
        }
    }
}
