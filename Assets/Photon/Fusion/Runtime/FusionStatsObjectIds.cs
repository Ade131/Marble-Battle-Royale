using Fusion.StatsInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Fusion
{
    public class FusionStatsObjectIds : Behaviour, IFusionStatsView
    {
        protected const int PAD = FusionStatsUtilities.PAD;
        protected const int MARGIN = FusionStatsUtilities.MARGIN;

        private const float LABEL_DIVIDING_POINT = .7f;
        private const float TEXT_PAD = 4;
        private const float TEXT_PAD_HORIZ = 6;
        private const int MAX_TAG_FONT_SIZE = 18;

        private static readonly Color _noneAuthColor = new(0.2f, 0.2f, 0.2f, 0.9f);
        private static readonly Color _inputAuthColor = new(0.1f, 0.6f, 0.1f, 1.0f);
        private static readonly Color _stateAuthColor = new(0.8f, 0.4f, 0.0f, 1.0f);

        [SerializeField] private Text _inputValueText;
        [SerializeField] private Text _stateValueText;
        [SerializeField] private Text _objectIdLabel;

        [SerializeField] private Image _stateAuthBackImage;
        [SerializeField] private Image _inputAuthBackImage;

        private FusionStats _fusionStats;

        // cache of last applied UI values
        private bool _previousHasInputAuth;
        private bool _previousHasStateAuth;
        private int _previousInputAuthValue;
        private uint _previousObjectIdValue;
        private int _previousStateAuthValue;

        private void Awake()
        {
            _fusionStats = GetComponentInParent<FusionStats>();
        }

        void IFusionStatsView.Initialize()
        {
        }

        void IFusionStatsView.CalculateLayout()
        {
            //throw new System.NotImplementedException();
        }

        void IFusionStatsView.Refresh()
        {
            if (_fusionStats == null) return;

            var obj = _fusionStats.Object;

            // if (obj == null) {
            //   return;
            // }

            var objIsValid = obj && obj.IsValid;

            // if (obj.IsValid) {

            // Set colors
            var hasInputAuth = objIsValid && obj.HasInputAuthority;
            if (_previousHasInputAuth != hasInputAuth)
            {
                _inputAuthBackImage.color = hasInputAuth ? _inputAuthColor : _noneAuthColor;
                _previousHasInputAuth = hasInputAuth;
            }

            var hasStateAuth = objIsValid && (obj.HasStateAuthority || obj.Runner.IsServer);
            if (_previousHasStateAuth != hasStateAuth)
            {
                _stateAuthBackImage.color = hasStateAuth ? _stateAuthColor : _noneAuthColor;
                _previousHasStateAuth = hasStateAuth;
            }

            // Set values
            var stateAuth = objIsValid ? obj.StateAuthority.PlayerId : PlayerRef.None.PlayerId;
            if (_previousStateAuthValue != stateAuth)
            {
                _stateValueText.text = stateAuth.ToString();
                _previousStateAuthValue = stateAuth;
            }

            var inputAuth = objIsValid ? obj.InputAuthority.PlayerId : PlayerRef.None.PlayerId;
            if (_previousInputAuthValue != inputAuth)
            {
                _inputValueText.text = inputAuth.ToString();
                _previousInputAuthValue = inputAuth;
            }
            // }

            var objectId = objIsValid ? obj.Id.Raw : 0;
            if (objectId != _previousObjectIdValue)
            {
                _objectIdLabel.text = objectId.ToString();
                _previousObjectIdValue = objectId;
            }
        }

        public static FusionStatsObjectIds Create(RectTransform parent, FusionStats fusionStats, out RectTransform rt)
        {
            rt = parent.CreateRectTransform("Object Ids Panel")
                .ExpandTopAnchor(MARGIN);

            var stats = rt.gameObject.AddComponent<FusionStatsObjectIds>();

            stats._fusionStats = fusionStats;

            stats.Generate();

            return stats;
        }

        public void Generate()
        {
            var labelFont = _fusionStats.LabelFont;
            var fontColor = _fusionStats.FontColor;

            var layoutRT = transform.CreateRectTransform("IDs Layout")
                    .ExpandAnchor()
                    .AddCircleSprite(_fusionStats.ObjDataBackColor)
                ;

            // Object ID panel on Left
            {
                var idPanelRT = layoutRT.CreateRectTransform("Object Id Panel", true)
                    .ExpandTopAnchor()
                    .SetAnchors(0, 0.4f, 0, 1);


                var objIdLabelRT = idPanelRT.CreateRectTransform("Object Id Label")
                    .SetAnchors(0, 1, LABEL_DIVIDING_POINT, 1)
                    .SetOffsets(TEXT_PAD_HORIZ, -TEXT_PAD_HORIZ, 0, -TEXT_PAD);

                objIdLabelRT.AddText("OBJECT ID", TextAnchor.MiddleCenter, fontColor, labelFont)
                    .resizeTextMaxSize = MAX_TAG_FONT_SIZE;

                var objIdValueRT = idPanelRT.CreateRectTransform("Object Id Value")
                    .SetAnchors(0, 1, 0, LABEL_DIVIDING_POINT)
                    .SetOffsets(TEXT_PAD_HORIZ, -TEXT_PAD_HORIZ, TEXT_PAD, 0);

                _objectIdLabel = objIdValueRT.AddText("00", TextAnchor.MiddleCenter, fontColor, labelFont);
            }

            // Authority ID panel on right
            {
                AddAuthorityPanel(layoutRT, "Input", ref _inputValueText, ref _inputAuthBackImage)
                    .SetAnchors(0.4f, 0.7f, 0, 1);

                AddAuthorityPanel(layoutRT, "State", ref _stateValueText, ref _stateAuthBackImage)
                    .SetAnchors(0.7f, 1.0f, 0, 1);
            }
        }

        private RectTransform AddAuthorityPanel(RectTransform parent, string label, ref Text valueText,
            ref Image backImage)
        {
            var labelFont = _fusionStats.LabelFont;
            var fontColor = _fusionStats.FontColor;

            var authIdPanelRT = parent.CreateRectTransform($"{label} Id Panel", true)
                .ExpandTopAnchor()
                .SetAnchors(0.5f, 1, 0, 1)
                .AddCircleSprite(_noneAuthColor, out backImage);

            var authLabelRT = authIdPanelRT.CreateRectTransform($"{label} Label")
                .SetAnchors(0, 1, LABEL_DIVIDING_POINT, 1)
                .SetOffsets(TEXT_PAD_HORIZ, -TEXT_PAD_HORIZ, 0, -TEXT_PAD);

            var authorityText = authLabelRT.AddText(label, TextAnchor.MiddleCenter, fontColor, labelFont);
            authorityText.resizeTextMaxSize = MAX_TAG_FONT_SIZE;

            var authValueRT = authIdPanelRT.CreateRectTransform($"{label} Value")
                .SetAnchors(0, 1, 0, LABEL_DIVIDING_POINT)
                .SetOffsets(TEXT_PAD_HORIZ, -TEXT_PAD_HORIZ, TEXT_PAD, 0);

            valueText = authValueRT.AddText("P0", TextAnchor.MiddleCenter, fontColor, labelFont);

            return authIdPanelRT;
        }
    }
}