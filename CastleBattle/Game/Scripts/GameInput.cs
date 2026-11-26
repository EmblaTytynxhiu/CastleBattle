using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Mouse/keyboard helper that works with the old Input Manager AND the new Input System.</summary>
public static class GameInput
{
    public static Vector2 MouseScreen
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null) return m.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return Vector2.zero;
#endif
        }
    }

    public static bool LeftDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null && m.leftButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0)) return true;
#endif
            return false;
        }
    }

    public static bool LeftUp
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null && m.leftButton.wasReleasedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonUp(0)) return true;
#endif
            return false;
        }
    }

    public static bool RightDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m != null && m.rightButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(1)) return true;
#endif
            return false;
        }
    }

    /// <summary>Number keys 1..9 on the top row.</summary>
    public static bool NumberDown(int n)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null)
        {
            UnityEngine.InputSystem.Controls.KeyControl k = null;
            switch (n)
            {
                case 1: k = kb.digit1Key; break;
                case 2: k = kb.digit2Key; break;
                case 3: k = kb.digit3Key; break;
                case 4: k = kb.digit4Key; break;
                case 5: k = kb.digit5Key; break;
                case 6: k = kb.digit6Key; break;
                case 7: k = kb.digit7Key; break;
                case 8: k = kb.digit8Key; break;
                case 9: k = kb.digit9Key; break;
            }
            if (k != null && k.wasPressedThisFrame) return true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (n >= 1 && n <= 9 && Input.GetKeyDown(KeyCode.Alpha0 + n)) return true;
#endif
        return false;
    }

    /// <summary>+1 while W / Up arrow is held, -1 while S / Down arrow is held (cannon angle).</summary>
    public static float AngleAxis
    {
        get
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                return v;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
#endif
            return v;
        }
    }

    /// <summary>+1 while D / Right arrow is held, -1 while A / Left arrow is held (cannon power).</summary>
    public static float PowerAxis
    {
        get
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v -= 1f;
                return v;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v -= 1f;
#endif
            return v;
        }
    }

    /// <summary>Space bar pressed this frame (fire).</summary>
    public static bool FireDown
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space)) return true;
#endif
            return false;
        }
    }

    public static bool OverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    public static Vector2 MouseWorld(Camera cam)
    {
        if (cam == null) return Vector2.zero;
        Vector3 p = MouseScreen;
        p.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(p);
    }
}

/// <summary>Rigidbody2D velocity that compiles on Unity 2021-2022 (velocity) and Unity 6 (linearVelocity).</summary>
public static class Rb2DCompat
{
    public static Vector2 Vel(this Rigidbody2D rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVel(this Rigidbody2D rb, Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}

public static class FindCompat
{
    public static T[] All<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<T>(FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<T>();
#endif
    }

    public static T First<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<T>();
#else
        return Object.FindObjectOfType<T>();
#endif
    }
}
