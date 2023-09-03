﻿namespace Exiled.ComAbilitiesEvents
{
    using ComAbilities;
    using ComAbilities.Abilities;
    using ComAbilities.Objects;
    using ComAbilities.Types;
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs.Interfaces;
    using Exiled.Events.EventArgs.Player;
    using HarmonyLib;
    using MapGeneration;
    using MEC;
    using PlayerRoles;
    using PlayerRoles.PlayableScps.Scp079.GUI;

    // using PluginAPI.Core;
    using System.Collections.Generic;
    using UnityEngine;
    using KeycardPermissions = Interactables.Interobjects.DoorUtils.KeycardPermissions;
    using Scp079Role = API.Features.Roles.Scp079Role;

    internal sealed class PlayerHandler : MonoBehaviour
    {
        private readonly ComAbilities Instance = ComAbilities.Instance;
        /* private Dictionary<HotkeyButton, Ability> _hotkeyDict = GetHotkeys();

         private Dictionary<HotkeyButton, Ability> GetHotkeys()
         {
             Dictionary<HotkeyButton, Ability> _dict = new();
             Assembly assembly = Assembly.GetExecutingAssembly();
             foreach (Type type in assembly.GetTypes())
             {
                 if ( !(type.BaseType == typeof(Ability) && type.BaseType is IHotkeyAbility) ) continue;
                 Attribute[] attrib = (Attribute[])type.GetCustomAttributes(typeof(HotkeyAttribute), true);
                 if (attrib.Length > 0 && (type is IHotkeyAbility))
                 {
                     _dict.Add((type as IHotkeyAbility)!.hotkeyButton, type);
                 }
             }
         } */

        public void OnChangingItem(ChangingItemEventArgs ev)
        {
            if (ev.Player == null || ev.Player.Role == null) return;
            
            Log.Debug("Caught");
            
            if (ev.Player.Role == RoleTypeId.Scp106 && ev.Player.SessionVariables.ContainsKey(Hologram.SessionVariable))
            {
                if (ev.Item.Type != ItemType.Medkit) return;
                CompManager compManager = Instance.CompDict.GetOrError(ev.Player);
                if (compManager.Hologram.ConfirmationPressed)
                {
                    compManager.Hologram.ChangeBack();
                }
                else
                {
                    compManager.Hologram.ActivateConfirmation();
                }
            }
            if (ev.Player.Role == RoleTypeId.Scp079)
            {
                Scp079Role role = ev.Player.Role.Cast<Scp079Role>();
                if (Guards.SignalLost(role)) { ev.IsAllowed = false; return; }
                CompManager compManager = Instance.CompDict.GetOrError(ev.Player);

                AllHotkeys? hotkey = ev.Item.Type switch // convert hotkey from HotkeyButton to FullHotkeys (support for all actions)
                {
                    ItemType.GunCOM15 => AllHotkeys.PrimaryFirearm,
                    ItemType.GunCOM18 => AllHotkeys.SecondaryFirearm,
                    ItemType.Medkit => AllHotkeys.Medical,
                    ItemType.GrenadeFlash => AllHotkeys.Grenade,
                    ItemType.KeycardJanitor => AllHotkeys.Keycard,
                    _ => null
                };

                if (compManager.DisplayManager.SelectedScreen == DisplayTypes.Tracker)
                {
                    compManager.PlayerTracker.HandleInputs(hotkey);
                    return;
                }
                if (hotkey == null || !compManager.Hotkeys.TryGetValue(hotkey.Value, out Ability ability)) return;
                if (ability is ICooldownAbility rateLimitedAbility)
                {
                    if (Guards.OnCooldown(rateLimitedAbility, out string errorCooldown)) {
                        compManager.TryShowErrorHint(errorCooldown);
                        ev.IsAllowed = false;
                        return;
                    }
                }
                if (Guards.NotEnoughAuxDisplay(role, ability.AuxCost, out string response))
                {
                    compManager.TryShowErrorHint(response);
                    ev.IsAllowed = false;
                    return;
                }
                IHotkeyAbility? hotkeyAbility = ability as IHotkeyAbility;
                hotkeyAbility?.Trigger();
                ev.IsAllowed = false;
            }
        }

        public void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Player.Role == RoleTypeId.Scp079 && ev.IsThrown)
            {
                CompManager compManager = Instance.CompDict.GetOrError(ev.Player);

                IHotkeyAbility hotkeyTracker = compManager.PlayerTracker;
                hotkeyTracker.Trigger();
            }
        }

        public void OnShot(ShotEventArgs ev)
        {
            if (ev.Player == null || ev.Player.Role == null) return;

            if (ev.Target != null)
            {
                if (ev.Target.SessionVariables[Hologram.SessionVariable] is not null and (bool)true)
                {
                    RaycastHit hit = ev.RaycastHit;
                    Physics.Raycast(hit.point, hit.normal * -1, out RaycastHit newHit);
                }
            }
        }

        public void OnLeft(LeftEventArgs ev)
        {
            if (ev.Player == null || ev.Player.Role == null) return;

            if (Instance.CompDict.Contains(ev.Player))
            {
                Instance.CompDict.Remove(ev.Player);
            }
        }
        
        public void OnChangingRole(ChangingRoleEventArgs ev)
        {
            if (ev.Player == null || ev.Player.Role == null) return;

            Player player = ev.Player;
            if (Instance.CompDict.Contains(player) && !player.SessionVariables.ContainsKey(Hologram.SessionVariable))
            {
                Instance.CompDict.Remove(player);
            }
            if (ev.NewRole == RoleTypeId.Scp079)
            {
                //  ev.Player.ReferenceHub.roleManager.ServerSetRole(RoleTypeId.Scp079, RoleChangeReason.RemoteAdmin);
                Log.Debug("Player role is now 079 - attempting to create new CompDict");
                Instance.CompDict.Add(player);
                Timing.CallDelayed(3, () =>
                {
                    Scp079IntroCutscene.IsPlaying = false;
                });

            }
        }

        public void OnSpawning(SpawningEventArgs ev)
        {
            ImageGenerator gen = ImageGenerator.ZoneGenerators.First();
            List<ImageGenerator.MinimapElement>? minimap = Traverse.Create(gen).Field("minimap").GetValue() as List<ImageGenerator.MinimapElement>;
          //  Hint hint = new(sb.ToString(), 5000);
          //  ev.Player.ShowHint(hint);
            if (ev.Player == null || ev.Player.Role == null) return;
            if (ev.Player.Role == RoleTypeId.Scp106 && ev.Player.SessionVariables.ContainsKey(Hologram.SessionVariable))
            {
                ev.Player.AddItem(ItemType.Painkillers);
            }
            if (ev.Player.Role == RoleTypeId.Scp079)
            {
                Traverse.Create(typeof(Scp079IntroCutscene)).Field("IsPlaying").SetValue(false);
                Timing.CallDelayed(15, () =>
                {

                    ev.Player.AddItem(ItemType.KeycardJanitor);
                    ev.Player.AddItem(ItemType.GrenadeFlash);
                    ev.Player.AddItem(ItemType.Painkillers);
                    ev.Player.AddItem(ItemType.GunCOM15);
                    ev.Player.AddItem(ItemType.GunCOM18);
                });
            }
        }

        public void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Player == null || ev.Player.Role == null) return;

            if (!Instance.Config.DoComputerPerms)
            {
                return;
            }
            Player player = ev.Player;
            if (player.Role == PlayerRoles.RoleTypeId.Scp079)
            {
                KeycardPermissions computerPermissions = new KeycardPermissions();
                KeycardPermissions doorPerms = ev.Door.RequiredPermissions.RequiredPermissions;

                Scp079Role role = player.Role.As<Scp079Role>();
                int accessLevel = role.Level;
                foreach (KeyValuePair<KeycardPermissions, int> pair in Instance.Config.DoorPermissions)
                {
                    if (accessLevel >= pair.Value)
                    {
                        computerPermissions |= pair.Key;
                    }
                }
                if (!computerPermissions.HasFlag(doorPerms))
                {
                    ev.IsAllowed = false;
                    ev.Door.PlaySound(DoorBeepType.PermissionDenied);
                    CompManager compManager = Instance.CompDict.GetOrError(player);
                    compManager.TryShowErrorHint("- DOOR ACCESS DENIED -");
                }
            }
        }

        public void DenyHologram<T>(T ev)
        where T : IDeniableEvent, IPlayerEvent
        {
            if (ev.Player == null || ev.Player.Role == null) return;

            if (ev.Player.Role == RoleTypeId.Scp106 && ev.Player.SessionVariables[Hologram.SessionVariable] != null)
            {
                ev.IsAllowed = false;
            }
        }
    }
}