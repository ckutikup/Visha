using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Visha.UI
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyTargetAnchor : MonoBehaviour
    {
        [SerializeField] private BattleHUD hud;
        [SerializeField] private int targetId;
        [SerializeField] private Camera worldCamera;
        private SpriteRenderer actor;
        private LineRenderer ring;
        private Material ringMaterial;
        public void Configure(BattleHUD view, int id, Camera camera) { hud = view; targetId = id; worldCamera = camera; }
        private void Awake()
        {
            actor = GetComponent<SpriteRenderer>();
            var go = new GameObject("TargetRing",typeof(LineRenderer));
            go.transform.SetParent(transform,false); ring = go.GetComponent<LineRenderer>();
            ring.useWorldSpace = true; ring.loop = true; ring.positionCount = 48;
            ring.startWidth = ring.endWidth = .035f;
            ringMaterial = new Material(Shader.Find("Sprites/Default")); ring.sharedMaterial = ringMaterial;
            ring.sortingLayerID = actor.sortingLayerID; ring.sortingOrder = actor.sortingOrder + 1;
        }
        private void Update()
        {
            if (!hud || !worldCamera) return;
            bool alive = hud.IsEnemyAlive(targetId);
            bool active = alive && hud.DisplayedEnemyId == targetId;
            ring.enabled = active;
            if (active)
            {
                var center = new Vector3(actor.bounds.center.x,actor.bounds.min.y + .03f,transform.position.z);
                for (int i=0;i<48;i++)
                {
                    float angle = i * Mathf.PI * 2 / 48;
                    ring.SetPosition(i,center + new Vector3(Mathf.Cos(angle)*actor.bounds.size.x*.45f,Mathf.Sin(angle)*.09f,0));
                }
                ring.startColor = ring.endColor = hud.CanSelectEnemy ? new Color(.5f,.93f,.72f) : new Color(1f,.75f,.4f);
            }
            var mouse = Mouse.current;
            if (!alive || !hud.CanSelectEnemy || mouse == null || !mouse.leftButton.wasPressedThisFrame ||
                (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
            Vector3 point = worldCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            var bounds = actor.bounds;
            if (point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.y && point.y <= bounds.max.y)
                hud.SelectEnemy(targetId);
        }
        private void OnDestroy() { if (ringMaterial) Destroy(ringMaterial); }
    }
}
