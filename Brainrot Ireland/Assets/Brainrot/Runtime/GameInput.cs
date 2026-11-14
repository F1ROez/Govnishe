using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace Brainrot {
 /// <summary>Supports the owner's Input System configuration and legacy-only projects.</summary>
 public static class GameInput {
  public static Vector2 Move { get {
#if ENABLE_INPUT_SYSTEM
   var k=Keyboard.current;if(k==null)return Vector2.zero;
   return new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
#else
   return new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"));
#endif
  }}
  public static Vector2 Look { get {
#if ENABLE_INPUT_SYSTEM
   return Mouse.current!=null?Mouse.current.delta.ReadValue()*.1f:Vector2.zero;
#else
   return new Vector2(Input.GetAxisRaw("Mouse X"),Input.GetAxisRaw("Mouse Y"));
#endif
  }}
  public static bool Attack {get {
#if ENABLE_INPUT_SYSTEM
   return Mouse.current!=null&&Mouse.current.leftButton.isPressed;
#else
   return Input.GetMouseButton(0);
#endif
  }}
  public static bool Sprint {get {
#if ENABLE_INPUT_SYSTEM
   return Keyboard.current!=null&&Keyboard.current.leftShiftKey.isPressed;
#else
   return Input.GetKey(KeyCode.LeftShift);
#endif
  }}
  public static bool Dash {get {
#if ENABLE_INPUT_SYSTEM
   return Keyboard.current!=null&&Keyboard.current.spaceKey.wasPressedThisFrame;
#else
   return Input.GetKeyDown(KeyCode.Space);
#endif
  }}
  public static bool MapToggle {get {
#if ENABLE_INPUT_SYSTEM
   return Keyboard.current!=null&&Keyboard.current.mKey.wasPressedThisFrame;
#else
   return Input.GetKeyDown(KeyCode.M);
#endif
  }}
  public static bool CameraToggle {get {
#if ENABLE_INPUT_SYSTEM
   return Keyboard.current!=null&&Keyboard.current.vKey.wasPressedThisFrame;
#else
   return Input.GetKeyDown(KeyCode.V);
#endif
  }}
  public static bool Pause {get {
#if ENABLE_INPUT_SYSTEM
   return Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame;
#else
   return Input.GetKeyDown(KeyCode.Escape);
#endif
  }}
 }
}
