using HarmonyLib;
using TMPro;
using UnityEngine.Events;

namespace OctoberJemini.PortalAtlas
{
    // Scope the explicit Enter binding to our rename requests; leave other game dialogs alone.
    internal static class AtlasRenameDialog
    {
        private static TMP_InputField input;
        private static UnityAction<string> submit;
        internal static void Open(TextReceiver receiver,string topic)
        {
            Detach();
            var dialog=TextInput.instance;
            dialog.RequestText(receiver,topic,48);
            input=AccessTools.Field(typeof(TextInput),"m_inputField").GetValue(dialog) as TMP_InputField;
            if(!input)return;
            submit=text=>{if(dialog && dialog.m_panel.activeInHierarchy)dialog.OnEnter();};
            input.onSubmit.AddListener(submit);
        }
        internal static void Detach()
        {
            if(input && submit!=null)input.onSubmit.RemoveListener(submit);
            input=null;submit=null;
        }
        [HarmonyPatch(typeof(TextInput),nameof(TextInput.Hide))]
        private static class Closed
        {
            private static void Postfix(){Detach();}
        }
        [HarmonyPatch(typeof(TextInput),nameof(TextInput.RequestText))]
        private static class Requested
        {
            private static void Prefix(){Detach();}
        }
    }
}
