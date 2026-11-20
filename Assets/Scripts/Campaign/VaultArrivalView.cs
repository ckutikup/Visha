using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visha.Campaign
{
    public sealed class VaultArrivalView : MonoBehaviour
    {
        private GameObject panel;
        private Action continueAction;
        private Action cancelAction;
        public bool IsOpen => panel != null && panel.activeSelf;

        public void Show(Transform parent, Font font, Action proceed, Action cancel)
        {
            if (IsOpen) return;
            if (panel == null) Build(parent, font);
            continueAction = proceed;
            cancelAction = cancel;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            var first = panel.transform.Find("Continue").gameObject;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(first);
        }

        public void ShowChapter(Transform parent, Font font, int destination, Action proceed, Action cancel)
        {
            Show(parent, font, proceed, cancel);
            string chapter, title, story, hint, button;
            if (destination == 4)
            {
                chapter = "III / THE SANCTUM"; title = "The source of the curse";
                story = "The final seal breaks. Beyond it, the Asura waits beside the shattered altar.\nThe guardians were bound to his will. End the curse and release them.";
                hint = "Temple's Blessing protects you from one lethal hit.\nWatch for Ruin. At half health, the Asura changes his attack pattern.";
                button = "Face the Asura";
            }
            else if (destination == 3)
            {
                chapter = "II / THE INNER VAULT"; title = "Two guardians. One final seal.";
                story = "Two chains hold the Sanctum shut. Two cursed guardians defend them.\nVishakanya must defeat both to reach the heart of the temple.";
                hint = "Both enemies act each round. Focus your poison and read both intentions.\nFade protects against the entire enemy phase.";
                button = "Break the seal";
            }
            else
            {
                chapter = "II / THE VAULT"; title = "Guardian Awakening";
                story = "The temple restores your guardian armor and awakens your dagger.\nYour first Awakening is ready.";
                hint = "Press 4 to awaken for two actions. Bite becomes Twin Fang once, leaving 2 poison.\nLater, gain charge with maximum-poison Bite or Fade against attacks of 8+ damage.";
                button = "Enter the Vault";
            }
            panel.transform.Find("Chapter").GetComponent<Text>().text = chapter;
            panel.transform.Find("Title").GetComponent<Text>().text = title;
            panel.transform.Find("Story").GetComponent<Text>().text = story;
            panel.transform.Find("Hint").GetComponent<Text>().text = hint;
            panel.transform.Find("Continue").GetComponentInChildren<Text>().text = button;
        }

        public void Continue() => Close(true);
        public void Cancel() => Close(false);
        private void Close(bool proceed)
        {
            if (!IsOpen) return;
            var callback = proceed ? continueAction : cancelAction;
            panel.SetActive(false);
            continueAction = cancelAction = null;
            callback?.Invoke();
        }
        private void OnDestroy() { if (panel != null) Destroy(panel); }

        private void Build(Transform parent, Font font)
        {
            panel = new GameObject("VaultArrival", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.025f,.045f,.055f,1f);
            Label("Chapter", "II  /  THE VAULT", .72f,.80f,30,font,new Color(.82f,.7f,.44f));
            Label("Title", "Guardian Awakening", .56f,.70f,48,font,new Color(.91f,.9f,.8f));
            Label("Story", "The seal breaks. The temple restores your guardian armor and awakens your dagger.\nYour first Awakening is ready.", .38f,.55f,28,font,new Color(.73f,.8f,.76f));
            Label("Hint", "Press 4 to awaken for two actions. Bite becomes Twin Fang once, leaving 2 poison.\nLater, gain charge with maximum-poison Bite or Fade against attacks of 8+ damage.", .26f,.36f,24,font,new Color(.73f,.8f,.76f));
            AddButton("Continue", "Enter the Vault", .52f,.77f, Continue,font);
            AddButton("Back", "Back", .23f,.48f, Cancel,font);
            panel.SetActive(false);
        }
        private Text Label(string name,string value,float bottom,float top,int size,Font font,Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Text));
            go.transform.SetParent(panel.transform,false);
            var text = go.GetComponent<Text>(); text.font=font; text.text=value; text.fontSize=size;
            text.resizeTextForBestFit=true; text.resizeTextMinSize=18; text.resizeTextMaxSize=size;
            text.alignment=TextAnchor.MiddleCenter; text.color=color; text.raycastTarget=false;
            var rect=text.rectTransform; rect.anchorMin=new Vector2(.12f,bottom); rect.anchorMax=new Vector2(.88f,top);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            return text;
        }
        private void AddButton(string name,string label,float left,float right,UnityEngine.Events.UnityAction action,Font font)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));
            go.transform.SetParent(panel.transform,false);
            var rect=(RectTransform)go.transform; rect.anchorMin=new Vector2(left,.13f); rect.anchorMax=new Vector2(right,.21f);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>(); image.color=new Color(.12f,.27f,.23f);
            var button=go.GetComponent<Button>(); button.targetGraphic=image; button.onClick.AddListener(action);
            var text=Label(name+"Label",label,0,1,28,font,new Color(.91f,.9f,.8f));
            text.transform.SetParent(go.transform,false); text.rectTransform.anchorMin=Vector2.zero; text.rectTransform.anchorMax=Vector2.one;
            text.rectTransform.offsetMin=new Vector2(12,4); text.rectTransform.offsetMax=new Vector2(-12,-4);
        }
    }
}
