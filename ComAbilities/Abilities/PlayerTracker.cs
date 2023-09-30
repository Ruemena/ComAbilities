﻿using ComAbilities.Localizations;
using ComAbilities.Objects;
using ComAbilities.Types;
using Exiled.API.Features;
using Exiled.API.Features.DamageHandlers;
using Exiled.ComAbilitiesEvents;
using PlayerRoles.PlayableScps.Scp079.Rewards;
using PlayerStatsSystem;
using System.Text;

namespace ComAbilities.Abilities
{
    //[Hotkey]
    public sealed class PlayerTracker : Ability, IHotkeyAbility, IReductionAbility, ICooldownAbility
    {

        private readonly static ComAbilities Instance = ComAbilities.Instance;
        private readonly static TrackerT TrackerT = Instance.Localization.Tracker;

        private static PlayerTrackerConfig _config => Instance.Config.PlayerTracker;

        public PlayerTracker(CompManager compManager) : base(compManager) { }
        public override string Name { get; } = TrackerT.Name;
        public override string Description { get; } = TrackerT.Description;
       // public string UsageGuide { get; } = "Once you open the Tracker menu, you can select a tracker slot. Afterwards, ping a person to begin tracking them in that slot. Once you start tracking a person, you can run .goto [slot] to instantly move your camera to the person. However, for every active tracker, your regeneration rate will decrease.";
       // public string Lore { get; } = "As part of an effort to combat the increasing number of breaches by SCP-106, SCP-173, and SCP-████, a network of sensors, light detectors, and other devices was installed within the facility to act as a support system to the Breach Scanner. This system allows for real-time monitoring and tracking of anomalies, although it has been utilized against hostile GOI forces and rogue personnel.";
        public override float AuxCost { get; } = _config.AuxCost;
        public override int ReqLevel { get; } = _config.Level;
        public override string DisplayText => string.Format(TrackerT.DisplayText, Instance.Localization.Shared.Hotkeys[HotkeyButton].ToUpper(), AuxCost);
        public string ActiveDisplayText => string.Format(TrackerT.ActiveDisplayText, _trackers.Count(x => x.Enabled));
        public override bool Enabled => _config.Enabled;

        public float AuxModifier => (float)Math.Pow(_config.AuxMultiplier,
            Math.Min(1, _trackers.Count(x => x.Enabled)));

        public AllHotkeys HotkeyButton { get; } = _config.Hotkey;

        public float CooldownLength { get; } = _config.Cooldown;
        public bool OnCooldown => _cooldown.Active;

        public bool IsActive => _trackers.Any(x => x.Player != null && x.Enabled);

        //public bool IsGettingPlayer => _expireTrackerTask.Enabled;

        private Cooldown _cooldown { get; } = new();
        //public bool InterfaceActive => CompManager.DisplayManager.SelectedScreen == DisplayTypes.Tracker;
        private TrackerManager _trackers => new() {
            new ActiveTracker(_config.Length, UpdateUI, ReqLevel),
            new ActiveTracker(_config.Length, UpdateUI, _config.Slot2Level)
        };
        // --------------------

        public void Trigger()
        {
            if (CompManager.DisplayManager.SelectedScreen == DisplayTypes.Tracker)
            {
                CompManager.DisplayManager.SetScreen(DisplayTypes.Main);
            }
            else
            {
                CompManager.DisplayManager.SetScreen(DisplayTypes.Tracker);
            }
        }

        public void SetSelectedTracker(Player player)
        {
            _trackers.StartSelected(player);
            _cooldown.Start(CooldownLength);
            CompManager.DeductAux(AuxCost);

            UpdateUI();
        }

        public void UpdateUI()
        {
            StringBuilder sb = new();
            sb.Append(_trackers.ConvertToHintString());

            if (_trackers.GetState(_trackers.SelectedTracker) == TrackerState.Selected) 
                sb.Append("\n" + TrackerT.SelectedEmpty);

            if (_trackers.GetState(_trackers.SelectedTracker) == TrackerState.SelectedFull) 
                sb.Append("\n" + TrackerT.SelectedFull);

            sb.Append("\n" + TrackerT.CloseMessage);

            CompManager.DisplayManager.SetElement(Elements.Trackers, sb.ToString());
            CompManager.DisplayManager.Update(DisplayTypes.Tracker);
        }

        public float GetDisplayETA() => _cooldown.GetDisplayETA();

        public bool TryGetTrackerPlayer(int trackerId, out Player player)
        {
            ActiveTracker? activeTracker = _trackers[trackerId];
            if (activeTracker == null || activeTracker.Player == null)
            {
                player = default;
                return false;
            }
            player = activeTracker.Player;
          
            return true;
        }

        public void HandleInputs(AllHotkeys hotkey)
        {
            switch (hotkey)
            {
                case AllHotkeys.HoldReload:
                    if (_trackers.SelectedTracker != -1 && _trackers[_trackers.SelectedTracker].Enabled)
                    {
                        _trackers[_trackers.SelectedTracker].CleanUp();
                    }
                    break;
                        
                case AllHotkeys.Reload:
                    if (_trackers.SelectedTracker == _trackers.Count - 1)
                    {
                        _trackers.SelectedTracker = 0;
                    } else
                    {
                        _trackers.SelectedTracker++;
                    }
                    UpdateUI();
                    break;
                case AllHotkeys.Throw:
                    CompManager.DisplayManager.SetScreen(DisplayTypes.Main);
                    CompManager.DisplayManager.Update();
                    break;
            }
        }

        public override void CleanUp()
        {
            _trackers.CleanUp();
        }
        /*public void HandleInputs(AllHotkeys? hotkey)
        {
            if (!hotkey.HasValue) return;
            switch (hotkey.Value)
            {
                case AllHotkeys.Grenade:

                    if (_trackers.SelectedTracker != default && _trackers[_trackers.SelectedTracker].Enabled)
                    {
                        _trackers[_trackers.SelectedTracker].ForceEnd();
                    }
                    break;
                case AllHotkeys.Medical:
                    CompManager.DisplayManager.SelectedScreen = DisplayTypes.Main;
                    CompManager.DisplayManager.Update();
                    break;
                default:
                    if (TrackerHotkeys.TryGetValue(hotkey.Value, out ActiveTracker tracker)) {
                        _trackers.SelectedTracker = _trackers.IndexOf(tracker);
                    }
                    break;
            }
        } */
    }
}
