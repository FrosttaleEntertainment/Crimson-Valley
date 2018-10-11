using UnityEngine;
using UnityEngine.Networking;

namespace Invector
{
    public  class vMonoBehaviour : NetworkBehaviour
    {
        [SerializeField]
        private bool openCloseEvents ;
        [SerializeField]
        private bool openCloseWindow;
        [SerializeField]       
        private int selectedToolbar;
    }  
}
