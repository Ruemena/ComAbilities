﻿namespace Exiled.ComAbilitiesEvents
{
    using ComAbilities;
    using ComAbilities.Objects;
    using ComAbilities.Types;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs.Scp079;
    using System.Text;
    using UnityEngine;

    internal sealed class Scp079Handler : MonoBehaviour
    {

        private static ComAbilities Instance => ComAbilities.Instance;

        public void OnPinging(PingingEventArgs ev)
        {
            if (ev.Type != API.Enums.PingType.Human) return;

            CompManager compManager = Instance.CompDict.GetOrError(ev.Player);
            if (compManager.Display.CurrentScreen == Screens.Tracker)
            {
                bool didHit = Physics.Raycast(ev.Position, Vector3.up, out RaycastHit hit, 1, LayerMask.GetMask("Default", "Player", "Hitbox"));
                if (!didHit) return;

                bool isPlayer = Player.TryGet(hit.transform.GetComponentInParent<ReferenceHub>(), out Player pingedPlayer);
                if (!isPlayer) return;

                if (pingedPlayer.IsHuman)
                {
                    compManager.PlayerTracker.AssignToSelectedTracker(pingedPlayer);
                }
            }
        }

        public void OnGainingLevel(GainingLevelEventArgs ev)
        {
            CompManager compManager = Instance.CompDict.GetOrError(ev.Player);
            compManager.UpdatePermissions();

            IEnumerable<Ability> newAbilities = compManager.AbilityInstances.Where(x => x.ReqLevel == ev.NewLevel);
            if (newAbilities.Any())
            {
                StringBuilder sb = new();
                sb.Append("<size=65%>");
                sb.Append(Instance.Localization.Shared.LevelUpUnlockedAbilities);
                sb.Append("</size>\n<size=50%>");
                foreach (Ability ability in newAbilities)
                {
                    string formatted = string.Format(Instance.Localization.Shared.UnlockedAbilityFormat, ability.Name, ability.Description);
                    sb.Append(formatted + "\n");
                }
                sb.Append("</size>");
                Broadcast broadcast = new(sb.ToString(), 7, true);
                ev.Player.Broadcast(broadcast);
            }

            compManager.Display.Update();
        }
    }
}
