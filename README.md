# <img src=Icon.png width=32> UnityEventListenerTools

Unity Plugin that adds the following features:

* The ability to listen for messages and events on game objects and invoke multi-parameter method calls without writing custom scripts

## Support for the following callbacks
* [Dialogue System for Unity (PixelCrushers)](https://www.pixelcrushers.com/dialogue-system/) Messages
  * OnUse,
  * OnBarkStart,
  * OnBarkEnd,
  * OnConversationStart,
  * OnConversationEnd,
  * OnSequenceStart,
  * OnSequenceEnd,
* [Unity Collider](https://docs.unity3d.com/ScriptReference/Collider.html) Messages
  * OnTriggerStay,
  * OnTriggerEnter,
  * OnTriggerExit,
  * OnCollisionStay,
  * OnCollisionEnter,
  * OnCollisionExit,
* [Unity Collider2D](https://docs.unity3d.com/ScriptReference/Collider2D.html) Messages
  * OnTriggerStay2D,
  * OnTriggerEnter2D,
  * OnTriggerExit2D,
  * OnCollisionStay2D,
  * OnCollisionEnter2D,
  * OnCollisionExit2D
* [Unity PlayableDirector](https://docs.unity3d.com/ScriptReference/Playables.PlayableDirector.html) Events
  * OnPlayed (played Event)
  * OnPaused (paused Event)
  * OnStopped (stopped Event)
