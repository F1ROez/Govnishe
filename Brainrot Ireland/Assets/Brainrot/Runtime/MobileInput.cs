using UnityEngine;
using UnityEngine.EventSystems;
namespace Brainrot
{
    public static class MobileInput
    {
        public static Vector2 Move,Look;
        public static bool Attack,Sprint,Dash,Throw,ThrowPressed;
        public static void Clear() {Move=Look=Vector2.zero;Attack=Sprint=Dash=Throw=ThrowPressed=false;}
    }
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        int finger=int.MinValue;
        public void OnPointerDown(PointerEventData e) {if(finger!=int.MinValue)return;finger=e.pointerId;OnDrag(e);}
        public void OnDrag(PointerEventData e)
        {
            if(finger!=e.pointerId)return;
            var rect=(RectTransform)transform;Vector2 point;RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out point);
            MobileInput.Move=Vector2.ClampMagnitude(new Vector2(point.x/(rect.rect.width*.4f),point.y/(rect.rect.height*.4f)),1);
        }
        public void OnPointerUp(PointerEventData e) {if(finger==e.pointerId){finger=int.MinValue;MobileInput.Move=Vector2.zero;}}
        void OnDisable(){finger=int.MinValue;MobileInput.Move=Vector2.zero;}
    }
    public sealed class MobileLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        int finger=int.MinValue;
        public void OnPointerDown(PointerEventData e) {if(finger==int.MinValue)finger=e.pointerId;}
        public void OnDrag(PointerEventData e) {if(e.pointerId==finger)MobileInput.Look+=e.delta*.1f;}
        public void OnPointerUp(PointerEventData e) {if(finger==e.pointerId)finger=int.MinValue;}
        void OnDisable(){finger=int.MinValue;MobileInput.Look=Vector2.zero;}
    }
    public sealed class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public int Action;int finger=int.MinValue;
        public void OnPointerDown(PointerEventData e) {if(finger!=int.MinValue||e==null)return;finger=e.pointerId;Set(true);}
        public void OnPointerUp(PointerEventData e) {if(e==null||e.pointerId!=finger)return;finger=int.MinValue;Set(false);}
        void OnDisable(){if(finger!=int.MinValue)Set(false);finger=int.MinValue;}
        void Set(bool value){if(Action==0)MobileInput.Attack=value;else if(Action==1)MobileInput.Sprint=value;else if(Action==2){if(value&&!MobileInput.Throw)MobileInput.ThrowPressed=true;MobileInput.Throw=value;}}
    }
}
