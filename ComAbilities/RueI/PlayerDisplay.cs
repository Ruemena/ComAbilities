﻿namespace RueI
{
    using System.Text;
    using ComAbilities.RueI.Events;
    using Exiled.API.Features;
    using MEC;

    using RueI.Records;

    /// <summary>
    /// Represents a <see cref="PlayerDisplay"/> that hides elements based on an active screen.
    /// </summary>
    /// <typeparam name="T">The enum to be used as the screen identifier.</typeparam>
    public class ScreenPlayerDisplay<T> : PlayerDisplay
        where T : Enum
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScreenPlayerDisplay{T}"/> class.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> to assign the display to.</param>
        /// <param name="defaultScreen">The default <see cref="T"/> to use as a screen.</param>
        public ScreenPlayerDisplay(Player player, T defaultScreen)
            : base(player)
        {
            CurrentScreen = defaultScreen;
        }

        /// <summary>
        /// Gets or sets the current screen that the display is on.
        /// </summary>
        /// <remarks>Updating this does not automatically update the display.</remarks>
        public T CurrentScreen { get; set; }

        /// <summary>
        /// Updates the display if the current screen is a certain screen.
        /// </summary>
        /// <param name="screen">The screen.</param>
        public void Update(T screen)
        {
            if (CurrentScreen.Equals(screen))
            {
                Update();
            }
        }

        internal override string ParseElements()
        {
            if (!elements.Any())
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            float totalOffset = 0;

            float lastPosition = 0;
            float lastOffset = 0;

            elements.Sort(Comparer);

            for (int i = 0; i < elements.Count; i++)
            {
                Element curElement = elements[i];
                if (!curElement.Enabled)
                {
                    continue;
                }

                if (curElement is IScreenElement<T> asScreen)
                {
                    if (!asScreen.Screens.HasFlag(CurrentScreen))
                    {
                        continue;
                    }
                }

                ParsedData parsedData = curElement.ParsedData;
                parsedData.Offset += curElement.AdditionalLineBreaks;

                if (i != 0)
                {
                    float calcedOffset = Element.CalculateOffset(lastPosition, lastOffset, curElement.FunctionalPosition);
                    Log.Debug(calcedOffset);
                    sb.Append($"<line-height={calcedOffset}px>\n</line-height>");
                    totalOffset += calcedOffset;
                }
                else
                {
                    totalOffset += curElement.FunctionalPosition;
                }

                sb.Append(parsedData.Content);
                sb.Append(TAGCLOSER);

                totalOffset += parsedData.Offset;
                lastPosition = curElement.FunctionalPosition;
                lastOffset = parsedData.Offset;
            }

            sb.Insert(0, $"<line-height={totalOffset}px>\n");
            return sb.ToString();
        }

        protected override bool ShouldParse(Element element)
        {
            if (!element.Enabled) return false;

            if (element is IScreenElement<T> screenElement)
            {
                return screenElement.Screens.HasFlag(CurrentScreen);
            }
            return true;
        }
    }

    /// <summary>
    /// Represents a display for a player.
    /// </summary>
    public class PlayerDisplay
    {
        /// <summary>
        /// Gets the default height if a line-height is not provided.
        /// </summary>
        public const float DEFAULTHEIGHT = 41; // in pixels;

        /// <summary>
        /// Gets an approximation of how many pixels are an in an em.
        /// </summary>
        public const float EMSTOPIXELS = 35;

        /// <summary>
        /// Gets a string used to close all tags.
        /// </summary>
        public const string TAGCLOSER = "</noparse></align></color></b></i></cspace></line-height></line-indent></link></lowercase></uppercase></smallcaps></margin></mark></mspace></pos></size></s></u></voffset></width>";

        /// <summary>
        /// Gets the ratelimit used for displaying hints.
        /// </summary>
        public const float HINTRATELIMIT = 0.55f;

        private CoroutineHandle? rateLimitTask;
        private bool rateLimitActive = false;
        private bool shouldUpdate = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerDisplay"/> class.
        /// </summary>
        /// <param name="player">The <see cref="Exiled.API.Features.Player"/> to assign the display to.</param>
        public PlayerDisplay(Player player)
        {
            Player = player;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PlayerDisplay"/> class.
        /// </summary>
        ~PlayerDisplay()
        {
            if (rateLimitTask is CoroutineHandle ch)
            {
                Timing.KillCoroutines(ch);
            }
        }

        /// <summary>
        /// Gets the player that this display is assigned to.
        /// </summary>
        public Player Player { get; }

        protected static Comparison<Element> Comparer { get; } = (Element first, Element other) => other.ZIndex - first.ZIndex;

        protected List<Element> elements { get; } = new();
        /// <summary>
        /// Adds an element to the player's display.
        /// </summary>
        /// <param name="element">The element to add.</param>
        public void Add(Element element) => elements.Add(element);

        /// <summary>
        /// Adds multiple elements to the player's display.
        /// </summary>
        /// <param name="elementArr">The elements to add.</param>
        public void Add(params Element[] elementArr) => elements.AddRange(elementArr);

        /// <summary>
        /// Adds multiple elements to the player's display.
        /// </summary>
        /// <param name="elementIEnum">The <see cref="IEnumerable{Element}>"/> to add.</param>
        public void AddRange(IEnumerable<Element> elementIEnum) => elements.AddRange(elementIEnum);

        /// <summary>
        /// Removes an element from a player's display.
        /// </summary>
        /// <param name="element">The element to remove.</param>
        public void Remove(Element element) => elements.Remove(element);

        /// <summary>
        /// Updates this display and shows the player the new hint, if the rate limit is not active.
        /// </summary>
        public void Update()
        {
            if (!rateLimitActive)
            {
                rateLimitActive = true;
                Timing.CallDelayed(HINTRATELIMIT, OnRateLimitFinished);

                Hint hint = new(ParseElements(), 99999999, true);
                Player.ShowHint(hint);
            }
            else
            {
                shouldUpdate = true;
                return;
            }
        }

        internal virtual string ParseElements()
        {
            if (!elements.Any())
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            float totalOffset = 0;

            float lastPosition = 0;
            float lastOffset = 0;

            elements.Sort(Comparer);

            for (int i = 0; i < elements.Count; i++)
            {
                Element curElement = elements[i];
                if (!ShouldParse(curElement))
                {
                    continue;
                }

                ParsedData parsedData = curElement.ParsedData;
                parsedData.Offset += curElement.AdditionalLineBreaks;

                if (i != 0)
                {
                    float calcedOffset = Element.CalculateOffset(lastPosition, lastOffset, curElement.FunctionalPosition);
                    Log.Debug(calcedOffset);
                    sb.Append($"<line-height={calcedOffset}px>\n</line-height>");
                    totalOffset += calcedOffset;
                }
                else
                {
                    totalOffset += curElement.FunctionalPosition;
                }

                sb.Append(parsedData.Content);
                sb.Append(TAGCLOSER);

                totalOffset += parsedData.Offset;
                lastPosition = curElement.FunctionalPosition;
                lastOffset = parsedData.Offset;
            }

            sb.Insert(0, $"<line-height={totalOffset}px>\n");
            return sb.ToString();
        }

        protected virtual bool ShouldParse(Element element) => element.Enabled;

        private void OnRateLimitFinished()
        {
            rateLimitActive = false;
            if (shouldUpdate)
            {
                shouldUpdate = false;
                Update();
            }

            RateLimitFinishedEventArgs args = new(Player);
            Events.OnRateLimitFinished(args);
        }
    }
}
