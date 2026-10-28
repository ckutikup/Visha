using UnityEngine;
using UnityEngine.UI;
using Visha.UI;

namespace Visha.Campaign
{
    public sealed class TempleMapView : MonoBehaviour
    {
        [SerializeField] private Sprite courtyard, vault;
        private CampaignController campaign;
        private GameObject panel;
        private Text description;
        private Button enter, replay;
        private readonly Button[] nodes = new Button[5];
        private int selected;
        private bool vaultArrivalSeen;
        private VaultArrivalView arrival;
        private Font font;
        public bool IsOpen => panel != null && panel.activeSelf;
        private static readonly string[] Names = { "Temple Guardian", "Guardian Warden", "Vault Sentinel", "Inner Vault", "Sanctum" };
        private static readonly string[] Challenges = {
            "At the temple steps, the first guardian repeats an old oath.\nLearn to build poison, spend it with Bite, and read the next attack.",
            "Beyond the courtyard, a second guardian raises its weapon.\nIt opens with Heavy Blow. Guard with Fade or plan a poison finisher.",
            "A broken chain lies before the sealed vault.\nSingle-enemy preview: Recover, Crush, then Shield Bash. Watch the heavy attack.",
            "Two guardians will defend the inner vault with overlapping intentions.\nUnavailable in this build.",
            "The curse still binds the final guardian.\nA telegraphed boss encounter is planned. Unavailable in this build."
        };
        private void Awake() => campaign = GetComponent<CampaignController>();
        private void OnEnable() { if (campaign != null) campaign.StateChanged += OnState; }
        private void OnDisable() { if (campaign != null) campaign.StateChanged -= OnState; }
        private void OnState(CampaignState state)
        {
            if (arrival != null && arrival.IsOpen) arrival.Cancel();
            if (state == CampaignState.Title) vaultArrivalSeen = false;
            if (panel != null) panel.SetActive(false);
            SetUnderlyingInput(true);
        }
        private void SetUnderlyingInput(bool value)
        {
            var screens = FindAnyObjectByType<BattleScreenView>();
            if (screens == null) return;
            foreach (string name in new[] { "TitlePanel", "VictoryPanel", "DefeatPanel", "CompletePanel" })
            {
                var child = screens.transform.Find(name);
                var group = child.GetComponent<CanvasGroup>();
                if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
                group.interactable = value;
            }
        }
        private void OnDestroy() { if (panel != null) Destroy(panel); }

        public void Open()
        {
            if (campaign.State == CampaignState.InBattle) return;
            if (panel == null) Build();
            FindAnyObjectByType<BattleScreenView>().RejectNavigation(string.Empty);
            selected = Mathf.Clamp(campaign.HighestCompletedEncounter + 1, 0, 4);
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            Refresh();
            SetUnderlyingInput(false);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(enter.interactable ? enter.gameObject : nodes[selected].gameObject);
        }
        public void SelectNode(int index)
        {
            if (!IsOpen || index < 0 || index >= 5) return;
            selected = index;
            Refresh();
        }
        public void EnterSelected()
        {
            if (!IsOpen || !campaign.CanEnterEncounter(selected)) return;
            if (arrival != null && arrival.IsOpen) return;
            if (selected >= 2 && !vaultArrivalSeen)
            {
                int destination = selected;
                if (arrival == null) arrival = gameObject.AddComponent<VaultArrivalView>();
                arrival.Show(panel.transform.parent, font, () =>
                {
                    vaultArrivalSeen = campaign.EnterEncounter(destination);
                }, () =>
                {
                    if (IsOpen && UnityEngine.EventSystems.EventSystem.current != null)
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(enter.gameObject);
                });
                return;
            }
            campaign.EnterEncounter(selected);
        }
        private void Refresh()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                string state = i >= campaign.EncounterCount ? "SEALED · UNAVAILABLE"
                    : i <= campaign.HighestCompletedEncounter ? "CLEARED · REPLAY"
                    : campaign.CanEnterEncounter(i) ? "CURRENT PATH" : "LOCKED";
                nodes[i].GetComponentInChildren<Text>().text = $"{i + 1}  {Names[i]}\n{state}";
                nodes[i].image.color = i == selected ? new Color(.18f, .38f, .32f, 1)
                    : new Color(.04f, .09f, .1f, .95f);
            }
            description.text = Challenges[selected];
            replay.interactable = campaign.HighestCompletedEncounter >= 0;
            enter.interactable = campaign.CanEnterEncounter(selected);
            enter.GetComponentInChildren<Text>().text = enter.interactable
                ? selected <= campaign.HighestCompletedEncounter ? "Replay Encounter" : "Enter Encounter"
                : "Unavailable";
        }
        private void Build()
        {
            var screens = FindAnyObjectByType<BattleScreenView>();
            font = screens.transform.Find("TitlePanel/Instructions").GetComponent<Text>().font;
            panel = new GameObject("TempleMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(screens.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.025f, .05f, .065f, 1f);
            Label("TEMPLE OF THE BROKEN OATH", 0, 420, 1500, 65, 38);
            Label("COURTYARD · Learn the venom", -580, 350, 540, 50, 24);
            Label("VAULT · Divide your attention", 0, 350, 540, 50, 24);
            Label("SANCTUM · Break the curse", 580, 350, 540, 50, 24);
            for (int area = 0; area < 3; area++)
            {
                float x = -580 + area * 580;
                var image = Element("Area", x, 130, 535, 290).AddComponent<Image>();
                image.sprite = area == 0 ? courtyard : vault;
                image.color = area == 2 ? new Color(.32f, .26f, .4f) : new Color(.7f, .75f, .72f);
                image.raycastTarget = false;
                // A stepped roof and pillars mark the authored temple route.
                for (int tier = 0; tier < 3; tier++)
                {
                    var roof = Element("TempleRoof", x, 210 + tier * 22, 120 - tier * 28, 5).AddComponent<Image>();
                    roof.color = new Color(.83f, .7f, .43f, .5f); roof.raycastTarget = false;
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    var pillar = Element("TemplePillar", x + side * 44, 166, 5, 80).AddComponent<Image>();
                    pillar.color = new Color(.83f, .7f, .43f, .5f); pillar.raycastTarget = false;
                }
            }
            float[] positions = { -720, -440, -140, 140, 580 };
            for (int i = 0; i < 4; i++)
            {
                var path = Element("Path", (positions[i] + positions[i + 1]) / 2, -65, positions[i + 1] - positions[i], 3).AddComponent<Image>();
                path.color = new Color(.65f, .55f, .36f); path.raycastTarget = false;
            }
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                nodes[i] = Button("Encounter" + (i + 1), "", positions[i], -65, 260, 100, () => SelectNode(index));
            }
            description = Label("", 0, -220, 1520, 120, 28);
            enter = Button("EnterEncounter", "Enter Encounter", 0, -365, 410, 65, EnterSelected);
            Button("ReturnToTitle", "Return to Title", -500, -365, 370, 65, () => { panel.SetActive(false); campaign.ReturnToTitle(); });
            replay = Button("Replay", "Replay Available Encounters", 500, -365, 460, 65, () => { selected = 0; Refresh(); });
            Label("Five encounters planned · three available here · Vault multi-enemy combat and Sanctum still in development", 0, -465, 1650, 45, 22);
        }
        private GameObject Element(string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(panel.transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
            return go;
        }
        private Text Label(string value, float x, float y, float w, float h, int size)
        {
            var text = Element("Label", x, y, w, h).AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter; text.color = new Color(.9f, .87f, .75f);
            text.raycastTarget = false; return text;
        }
        private Button Button(string name, string value, float x, float y, float w, float h, UnityEngine.Events.UnityAction action)
        {
            var go = Element(name, x, y, w, h);
            var image = go.AddComponent<Image>(); image.color = new Color(.12f, .24f, .22f);
            var button = go.AddComponent<Button>(); button.targetGraphic = image;
            var label = Label(value, x, y, w - 16, h - 10, 24);
            label.transform.SetParent(go.transform, false);
            ((RectTransform)label.transform).anchoredPosition = Vector2.zero;
            button.onClick.AddListener(action); return button;
        }
    }
}
