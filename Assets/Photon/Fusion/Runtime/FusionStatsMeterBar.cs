using System;
using Fusion.StatsInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Fusion
{
    //using Stats = Fusion.Simulation.Statistics;

    public class FusionStatsMeterBar : FusionStatsGraphBase
    {
        public float HoldPeakTime = 0.1f;
        public float DecayTime = 0.25f;

        /// <summary>
        ///     Values greater than 0 will limit the meter to a range of 0 to MeterMax.
        ///     Value of 0 will adjust the max to the largest value occurrence.
        /// </summary>
        [InlineHelp] public int MeterMax; // = 1200;


        /// <summary>
        ///     Exposes the UI labels and controls of <see cref="FusionStatsGraph" />, so they may be modified if customizing this
        ///     graph.
        /// </summary>
        [InlineHelp] [SerializeField] private bool _showUITargets;

        [DrawIf(nameof(_showUITargets), Hide = true)]
        public Text ValueLabel;

        [DrawIf(nameof(_showUITargets), Hide = true)]
        public Image Bar;

        [DrawIf(nameof(_showUITargets), Hide = true)]
        public Image BarPeak;

        private double _currentBarValue;

        private double _currentDisplayValue;
        private double _currentPeakValue;

        private double _currentRangeMax;

        // double _rangeMax;
        private float _lastBarPeakSetTime;

        private double _lastImportedSampleTickTime;
        private Color CurrentColor;

        protected override Color BackColor => base.BackColor * new Color(.5f, .5f, .5f, 1);

        public void LateUpdate()
        {
            if (DecayTime <= 0) return;

            if (_currentBarValue <= 0) return;

            if (Time.time < _lastBarPeakSetTime + HoldPeakTime) return;

            var decayedVal = Math.Max(_currentBarValue - Time.deltaTime / DecayTime * _currentRangeMax, 0);
            SetBar(decayedVal, _currentRangeMax, false);
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            if (MeterMax < 0) MeterMax = 0;

            if (Application.isPlaying == false)
            {
                TryConnect();
                _layoutDirty = _layoutDirty < 1 ? 1 : _layoutDirty;
            }
        }
#endif

        public override void Initialize()
        {
            base.Initialize();

            _currentRangeMax = MeterMax;
            // Prefabs lose editor generated sprites - recreate as needed.
            if (BackImage.sprite == null)
            {
                BackImage.sprite = FusionStatsUtilities.MeterSprite;
                Bar.sprite = BackImage.sprite;
                BarPeak.sprite = BackImage.sprite;
            }
        }

        public override void Refresh()
        {
            if (_layoutDirty > 0) CalculateLayout();

            if (StatSourceInfo == null) return;

            if (StatsObject == null) return;

            var statsBuffer = (OverTimeStatBuffer)FieldInfo.GetValue(StatsObject);

            var newestTime = _lastImportedSampleTickTime;
            double highestNewValue = 0;
            double peakValue = 0;

            for (var i = 0; i < statsBuffer.Length; ++i)
            {
                var e = statsBuffer[i];

                if (e.Time > _lastImportedSampleTickTime)
                {
                    if (e.Value > highestNewValue) highestNewValue = e.Value;
                    newestTime = e.Time;
                }

                // TODO: Use this peak value on graph
                if (e.Value > peakValue) peakValue = e.Value;

                _lastImportedSampleTickTime = newestTime;
            }

            SetValue(highestNewValue, peakValue);
        }

        public void SetValue(double rawvalue, double rawPeak)
        {
            var info = StatSourceInfo;

            var multipliedValue = rawvalue; // * info.Multiplier;
            var multipliedPeak = rawPeak; // * some multiplier

            var meterMax = MeterMax;
            var rangeMax = _currentRangeMax;

            // Clamp value if the Meter is fixed. No need to clamp if not, the meter will always accomodate values.
            double clampedValue, clampedPeak;
            if (meterMax == 0)
            {
                if (multipliedPeak > rangeMax) rangeMax = multipliedPeak;
                clampedValue = multipliedValue;
                clampedPeak = multipliedPeak;
            }
            else
            {
                clampedValue = Math.Max(Math.Min(multipliedValue, rangeMax), 0);
                clampedPeak = Math.Max(Math.Min(multipliedPeak, rangeMax), 0);
            }

            var roundedValue = Math.Round(clampedValue, info.Decimals);
            var roundedPeak = Math.Round(clampedPeak, info.Decimals);

            // Reset the hold time for the bar once it increases from a previous value.
            if (roundedValue >= _currentBarValue) _lastBarPeakSetTime = Time.time;

            // Avoid repaints when nothing has changed.
            if (roundedValue == _currentDisplayValue && roundedPeak == _currentPeakValue &&
                rangeMax == _currentRangeMax) return;

            ValueLabel.text = $"{clampedValue} [{clampedPeak} PK]";
            _currentDisplayValue = roundedValue;
            _currentPeakValue = clampedPeak;
            _currentRangeMax = rangeMax;

            // Only set values greater than the current shown value when using decay.
            if (DecayTime >= 0 && clampedValue <= _currentBarValue) return;

            if (clampedValue != _currentBarValue)
            {
                SetBar(clampedValue, rangeMax, false);
                SetBar(clampedPeak, rangeMax, true);
            }
        }

        private void SetBar(double value, double rangeMax, bool setPeakBar)
        {
            var bar = setPeakBar ? BarPeak : Bar;

            var fusionStats = FusionStats;

            bar.fillAmount = (float)(value / rangeMax);

            _currentBarValue = value;

            if (value < WarnThreshold)
            {
                var GoodColor = setPeakBar ? fusionStats.GraphColorGood * .75f : fusionStats.GraphColorGood;
                if (CurrentColor != GoodColor)
                {
                    CurrentColor = GoodColor;
                    bar.color = GoodColor;
                }
            }
            else if (value < ErrorThreshold)
            {
                var WarnColor = setPeakBar ? fusionStats.GraphColorWarn * .75f : fusionStats.GraphColorWarn;
                if (CurrentColor != WarnColor)
                {
                    bar.color = WarnColor;
                    CurrentColor = WarnColor;
                }
            }
            else
            {
                var ErrorColor = setPeakBar ? fusionStats.GraphColorBad * .75f : fusionStats.GraphColorBad;
                if (CurrentColor != ErrorColor)
                {
                    bar.color = ErrorColor;
                    CurrentColor = ErrorColor;
                }
            }
        }

        public override void CalculateLayout()
        {
            _layoutDirty--;

            // Special padding handling because Arial vertically sits below center.
            var pad = LabelTitle.transform.parent.GetComponent<RectTransform>().rect.height * .2f;
            LabelTitle.rectTransform.offsetMax = new Vector2(0, -pad);
            LabelTitle.rectTransform.offsetMin = new Vector2(PAD, pad * 1.2f);

            ValueLabel.rectTransform.offsetMax = new Vector2(-PAD, -pad);
            ValueLabel.rectTransform.offsetMin = new Vector2(0, pad * 1.2f);

            ApplyTitleText();
        }

        // unfinished
        public static FusionStatsMeterBar Create(
            RectTransform parent,
            FusionStats fusionStats,
            StatSourceTypes statSourceType,
            int statId,
            float warnThreshold,
            float alertThreshold
        )
        {
            var info = statSourceType.GetDescription(statId);
            var barRT = parent.CreateRectTransform(info.meta.Name, true);
            var bar = barRT.gameObject.AddComponent<FusionStatsMeterBar>();
            bar._statSourceInfo = info.meta;
            bar._fusionStats = fusionStats;
            //bar.WarningValueThreshold = warnThreshold;
            //bar.ErrorValueThreshold = alertThreshold;
            bar._statSourceType = statSourceType;
            bar._statId = statId;
            bar.GenerateMeter();
            return bar;
        }

        public void GenerateMeter()
        {
            var fusionStats = FusionStats;
            var info = _statSourceType.GetDescription(_statId);

            var backRT = transform.CreateRectTransform("Back", true);
            BackImage = backRT.gameObject.AddComponent<Image>();
            BackImage.raycastTarget = false;
            BackImage.sprite = FusionStatsUtilities.MeterSprite;
            BackImage.color = BackColor;
            BackImage.type = Image.Type.Simple;

            var barPeakRT = transform.CreateRectTransform("BarPeak", true);
            BarPeak = barPeakRT.gameObject.AddComponent<Image>();
            BarPeak.raycastTarget = false;
            BarPeak.sprite = BackImage.sprite;
            BarPeak.color = fusionStats.GraphColorGood;
            BarPeak.type = Image.Type.Filled;
            BarPeak.fillMethod = Image.FillMethod.Horizontal;
            BarPeak.fillAmount = 0;

            var barRT = transform.CreateRectTransform("Bar", true);
            Bar = barRT.gameObject.AddComponent<Image>();
            Bar.raycastTarget = false;
            Bar.sprite = BackImage.sprite;
            Bar.color = fusionStats.GraphColorGood;
            Bar.type = Image.Type.Filled;
            Bar.fillMethod = Image.FillMethod.Horizontal;
            Bar.fillAmount = 0;

            var titleRT = transform.CreateRectTransform("Label", true)
                .ExpandAnchor()
                .SetAnchors(0.0f, 0.5f, 0.0f, 1.0f)
                .SetOffsets(6, -6, 6, -6);


            LabelTitle = titleRT.AddText(info.meta.Name, TextAnchor.MiddleLeft, fusionStats.FontColor,
                fusionStats.LabelFont);
            LabelTitle.alignByGeometry = false;

            var valueRT = transform.CreateRectTransform("Value", true)
                .ExpandAnchor()
                .SetAnchors(0.5f, 1.0f, 0.0f, 1.0f)
                .SetOffsets(6, -6, 6, -6);

            ValueLabel = valueRT.AddText("- [-]", TextAnchor.MiddleRight, fusionStats.FontColor, fusionStats.ValueFont);
            ValueLabel.alignByGeometry = false;
        }
    }
}