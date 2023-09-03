﻿using ComAbilities.Objects;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Roles;
using GameCore;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utf8Json.Resolvers.Internal;

namespace ComAbilities.Types
{
    // public struct EmptyArgs { }
    public interface ICooldownAbility
    {
        public bool OnCooldown { get; }
        public abstract float CooldownLength { get; }
        public int GetETA();
    }
    public interface IHotkeyAbility
    {
        public AllHotkeys HotkeyButton { get; }
        public void Trigger(); // no args
    }
    public interface IReductionAbility
    {
        public float AuxModifier { get; }
        public string ActiveDisplayText { get; }
        public bool IsActive { get; }
       // public abstract void OnFinished();

       // void Toggle(params object[] args);
    }
    public abstract class Ability//<T>
      //  where T: struct
    {
        public CompManager CompManager { get; }

        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract float AuxCost { get; }
        public abstract int ReqLevel { get; }
        public abstract string DisplayText { get; }
        public abstract bool Enabled { get; }

        public Ability(CompManager compManager)
        {
            this.CompManager = compManager;
        }

        public bool ValidateLevel(int currentLevel)
        {
            return currentLevel >= this.ReqLevel;
        }
        public bool ValidateAux(float currentAux)
        {
            return currentAux >= this.AuxCost;
        }

        public bool DidError(Player player, IAbilityConfig config, out string response)
        {
            if (config.Enabled == false)
            {
                response = "Not enabled";
                return true;
            }
            if (player.Role != RoleTypeId.Scp079)
            {
                response = "You must be SCP-079 to use this command.";
                return true;
            }
            Scp079Role role = player.Role.As<Scp079Role>();

            if (role.IsLost)
            {
                response = "- SIGNAL LOST -";
                return true;
            }
            response = "";
            return false;

        }
     // public abstract void Trigger(T value);
        public abstract void KillTasks();
    }
}