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
        public float GetDisplayETA();
    }
    public interface IHotkeyAbility
    {
        public AllHotkeys HotkeyButton { get; }
        public void Trigger(); // no args
        public float AuxCost { get; }
    }
    public interface IReductionAbility
    {
        public float AuxModifier { get; }
        public string ActiveDisplayText { get; }
        public bool IsActive { get; }
       // public abstract void OnFinished();

       // void Toggle(params object[] args);
    }
    /*public abstract class ToggleableAbility : Ability
    {
        protected ToggleableAbility(CompManager compManager) : base(compManager)
        {
        }

        public abstract string ActiveDisplayText { get; }
        public abstract float AuxModifier { get; }
        public bool IsActive { get; private set; }

        public abstract void Toggle();

        protected virtual void OnEnabled()
        {
            IsActive = true;
            base.CompManager.ActiveAbilities.Add(this);
            CompManager.DisplayManager.Update();
        }
        protected virtual void OnDisabled()
        {
            IsActive = false;
            base.CompManager.ActiveAbilities.Remove(this);
            CompManager.DisplayManager.Update();
        }

        public virtual void ForceDisable()
        {
            if (IsActive) OnDisabled();
        } 
    }*/
    /// <summary>
    /// Base class for abilities
    /// </summary>
    public abstract class Ability : IKillable
    {
        protected static ComAbilities Instance => ComAbilities.Instance;
        protected CompManager CompManager;

        protected DisplayManager Display => CompManager.Display;

        ~Ability()
        {
            CleanUp();
        }

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
     // public abstract void Trigger(T value);

        public abstract void CleanUp();
    }
}
