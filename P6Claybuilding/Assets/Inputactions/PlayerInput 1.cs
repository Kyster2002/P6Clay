//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by com.unity.inputsystem:InputActionCodeGenerator
//     version 1.12.0
//     from Assets/Inputactions/PlayerInput 1.inputactions
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

/// <summary>
/// Provides programmatic access to <see cref="InputActionAsset" />, <see cref="InputActionMap" />, <see cref="InputAction" /> and <see cref="InputControlScheme" /> instances defined in asset "Assets/Inputactions/PlayerInput 1.inputactions".
/// </summary>
/// <remarks>
/// This class is source generated and any manual edits will be discarded if the associated asset is reimported or modified.
/// </remarks>
/// <example>
/// <code>
/// using namespace UnityEngine;
/// using UnityEngine.InputSystem;
///
/// // Example of using an InputActionMap named "Player" from a UnityEngine.MonoBehaviour implementing callback interface.
/// public class Example : MonoBehaviour, MyActions.IPlayerActions
/// {
///     private MyActions_Actions m_Actions;                  // Source code representation of asset.
///     private MyActions_Actions.PlayerActions m_Player;     // Source code representation of action map.
///
///     void Awake()
///     {
///         m_Actions = new MyActions_Actions();              // Create asset object.
///         m_Player = m_Actions.Player;                      // Extract action map object.
///         m_Player.AddCallbacks(this);                      // Register callback interface IPlayerActions.
///     }
///
///     void OnDestroy()
///     {
///         m_Actions.Dispose();                              // Destroy asset object.
///     }
///
///     void OnEnable()
///     {
///         m_Player.Enable();                                // Enable all actions within map.
///     }
///
///     void OnDisable()
///     {
///         m_Player.Disable();                               // Disable all actions within map.
///     }
///
///     #region Interface implementation of MyActions.IPlayerActions
///
///     // Invoked when "Move" action is either started, performed or canceled.
///     public void OnMove(InputAction.CallbackContext context)
///     {
///         Debug.Log($"OnMove: {context.ReadValue&lt;Vector2&gt;()}");
///     }
///
///     // Invoked when "Attack" action is either started, performed or canceled.
///     public void OnAttack(InputAction.CallbackContext context)
///     {
///         Debug.Log($"OnAttack: {context.ReadValue&lt;float&gt;()}");
///     }
///
///     #endregion
/// }
/// </code>
/// </example>
public partial class @XRbutton: IInputActionCollection2, IDisposable
{
    /// <summary>
    /// Provides access to the underlying asset instance.
    /// </summary>
    public InputActionAsset asset { get; }

    /// <summary>
    /// Constructs a new instance.
    /// </summary>
    public @XRbutton()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerInput 1"",
    ""maps"": [
        {
            ""name"": ""Main"",
            ""id"": ""2f3eb0eb-9ec1-43f7-80f3-10747e17fc82"",
            ""actions"": [
                {
                    ""name"": ""LeftToggleMenu"",
                    ""type"": ""Button"",
                    ""id"": ""222cbb9f-40e6-42bc-a710-38933a9a09a9"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""leftDelete"",
                    ""type"": ""Button"",
                    ""id"": ""6fc8cacb-08ce-4b97-bb07-f79ebd6cf660"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Rotate"",
                    ""type"": ""Button"",
                    ""id"": ""69071f48-1940-4c1b-8031-8a8b14d7b85f"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""SelectAndSet"",
                    ""type"": ""Button"",
                    ""id"": ""5cb896b8-2a85-462d-9ffd-43a9571b9d89"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""FlatRotate"",
                    ""type"": ""Button"",
                    ""id"": ""30dbd18f-1871-455b-98d9-525027068458"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Deselect"",
                    ""type"": ""Button"",
                    ""id"": ""ac873255-605a-448e-98a9-f7f2c25162ca"",
                    ""expectedControlType"": """",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""7181944f-3ad7-48a0-936b-f87fa8908c1a"",
                    ""path"": ""<XRController>{LeftHand}/{PrimaryButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""LeftToggleMenu"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""300e72da-c721-4949-99f1-c2a891e56f0a"",
                    ""path"": ""<XRController>{LeftHand}/{SecondaryButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""leftDelete"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4db35b5f-c40c-4acc-87db-91c652240b0e"",
                    ""path"": ""<XRController>{RightHand}/{PrimaryButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Rotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c57eaa93-30dd-4677-baec-ff01dec3b5a0"",
                    ""path"": ""<XRController>{RightHand}/{TriggerButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""SelectAndSet"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""c8a4defc-cffe-44f6-9157-5fa10c8e262c"",
                    ""path"": ""<XRController>{RightHand}/{SecondaryButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""FlatRotate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""7e6c624a-ba95-4ed0-a25f-c69b9c01058b"",
                    ""path"": ""<XRController>{RightHand}/{GripButton}"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Deselect"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": []
}");
        // Main
        m_Main = asset.FindActionMap("Main", throwIfNotFound: true);
        m_Main_LeftToggleMenu = m_Main.FindAction("LeftToggleMenu", throwIfNotFound: true);
        m_Main_leftDelete = m_Main.FindAction("leftDelete", throwIfNotFound: true);
        m_Main_Rotate = m_Main.FindAction("Rotate", throwIfNotFound: true);
        m_Main_SelectAndSet = m_Main.FindAction("SelectAndSet", throwIfNotFound: true);
        m_Main_FlatRotate = m_Main.FindAction("FlatRotate", throwIfNotFound: true);
        m_Main_Deselect = m_Main.FindAction("Deselect", throwIfNotFound: true);
    }

    ~@XRbutton()
    {
        UnityEngine.Debug.Assert(!m_Main.enabled, "This will cause a leak and performance issues, XRbutton.Main.Disable() has not been called.");
    }

    /// <summary>
    /// Destroys this asset and all associated <see cref="InputAction"/> instances.
    /// </summary>
    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.bindingMask" />
    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.devices" />
    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.controlSchemes" />
    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.Contains(InputAction)" />
    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.GetEnumerator()" />
    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    /// <inheritdoc cref="IEnumerable.GetEnumerator()" />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.Enable()" />
    public void Enable()
    {
        asset.Enable();
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.Disable()" />
    public void Disable()
    {
        asset.Disable();
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.bindings" />
    public IEnumerable<InputBinding> bindings => asset.bindings;

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.FindAction(string, bool)" />
    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    /// <inheritdoc cref="UnityEngine.InputSystem.InputActionAsset.FindBinding(InputBinding, out InputAction)" />
    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // Main
    private readonly InputActionMap m_Main;
    private List<IMainActions> m_MainActionsCallbackInterfaces = new List<IMainActions>();
    private readonly InputAction m_Main_LeftToggleMenu;
    private readonly InputAction m_Main_leftDelete;
    private readonly InputAction m_Main_Rotate;
    private readonly InputAction m_Main_SelectAndSet;
    private readonly InputAction m_Main_FlatRotate;
    private readonly InputAction m_Main_Deselect;
    /// <summary>
    /// Provides access to input actions defined in input action map "Main".
    /// </summary>
    public struct MainActions
    {
        private @XRbutton m_Wrapper;

        /// <summary>
        /// Construct a new instance of the input action map wrapper class.
        /// </summary>
        public MainActions(@XRbutton wrapper) { m_Wrapper = wrapper; }
        /// <summary>
        /// Provides access to the underlying input action "Main/LeftToggleMenu".
        /// </summary>
        public InputAction @LeftToggleMenu => m_Wrapper.m_Main_LeftToggleMenu;
        /// <summary>
        /// Provides access to the underlying input action "Main/leftDelete".
        /// </summary>
        public InputAction @leftDelete => m_Wrapper.m_Main_leftDelete;
        /// <summary>
        /// Provides access to the underlying input action "Main/Rotate".
        /// </summary>
        public InputAction @Rotate => m_Wrapper.m_Main_Rotate;
        /// <summary>
        /// Provides access to the underlying input action "Main/SelectAndSet".
        /// </summary>
        public InputAction @SelectAndSet => m_Wrapper.m_Main_SelectAndSet;
        /// <summary>
        /// Provides access to the underlying input action "Main/FlatRotate".
        /// </summary>
        public InputAction @FlatRotate => m_Wrapper.m_Main_FlatRotate;
        /// <summary>
        /// Provides access to the underlying input action "Main/Deselect".
        /// </summary>
        public InputAction @Deselect => m_Wrapper.m_Main_Deselect;
        /// <summary>
        /// Provides access to the underlying input action map instance.
        /// </summary>
        public InputActionMap Get() { return m_Wrapper.m_Main; }
        /// <inheritdoc cref="UnityEngine.InputSystem.InputActionMap.Enable()" />
        public void Enable() { Get().Enable(); }
        /// <inheritdoc cref="UnityEngine.InputSystem.InputActionMap.Disable()" />
        public void Disable() { Get().Disable(); }
        /// <inheritdoc cref="UnityEngine.InputSystem.InputActionMap.enabled" />
        public bool enabled => Get().enabled;
        /// <summary>
        /// Implicitly converts an <see ref="MainActions" /> to an <see ref="InputActionMap" /> instance.
        /// </summary>
        public static implicit operator InputActionMap(MainActions set) { return set.Get(); }
        /// <summary>
        /// Adds <see cref="InputAction.started"/>, <see cref="InputAction.performed"/> and <see cref="InputAction.canceled"/> callbacks provided via <param cref="instance" /> on all input actions contained in this map.
        /// </summary>
        /// <param name="instance">Callback instance.</param>
        /// <remarks>
        /// If <paramref name="instance" /> is <c>null</c> or <paramref name="instance"/> have already been added this method does nothing.
        /// </remarks>
        /// <seealso cref="MainActions" />
        public void AddCallbacks(IMainActions instance)
        {
            if (instance == null || m_Wrapper.m_MainActionsCallbackInterfaces.Contains(instance)) return;
            m_Wrapper.m_MainActionsCallbackInterfaces.Add(instance);
            @LeftToggleMenu.started += instance.OnLeftToggleMenu;
            @LeftToggleMenu.performed += instance.OnLeftToggleMenu;
            @LeftToggleMenu.canceled += instance.OnLeftToggleMenu;
            @leftDelete.started += instance.OnLeftDelete;
            @leftDelete.performed += instance.OnLeftDelete;
            @leftDelete.canceled += instance.OnLeftDelete;
            @Rotate.started += instance.OnRotate;
            @Rotate.performed += instance.OnRotate;
            @Rotate.canceled += instance.OnRotate;
            @SelectAndSet.started += instance.OnSelectAndSet;
            @SelectAndSet.performed += instance.OnSelectAndSet;
            @SelectAndSet.canceled += instance.OnSelectAndSet;
            @FlatRotate.started += instance.OnFlatRotate;
            @FlatRotate.performed += instance.OnFlatRotate;
            @FlatRotate.canceled += instance.OnFlatRotate;
            @Deselect.started += instance.OnDeselect;
            @Deselect.performed += instance.OnDeselect;
            @Deselect.canceled += instance.OnDeselect;
        }

        /// <summary>
        /// Removes <see cref="InputAction.started"/>, <see cref="InputAction.performed"/> and <see cref="InputAction.canceled"/> callbacks provided via <param cref="instance" /> on all input actions contained in this map.
        /// </summary>
        /// <remarks>
        /// Calling this method when <paramref name="instance" /> have not previously been registered has no side-effects.
        /// </remarks>
        /// <seealso cref="MainActions" />
        private void UnregisterCallbacks(IMainActions instance)
        {
            @LeftToggleMenu.started -= instance.OnLeftToggleMenu;
            @LeftToggleMenu.performed -= instance.OnLeftToggleMenu;
            @LeftToggleMenu.canceled -= instance.OnLeftToggleMenu;
            @leftDelete.started -= instance.OnLeftDelete;
            @leftDelete.performed -= instance.OnLeftDelete;
            @leftDelete.canceled -= instance.OnLeftDelete;
            @Rotate.started -= instance.OnRotate;
            @Rotate.performed -= instance.OnRotate;
            @Rotate.canceled -= instance.OnRotate;
            @SelectAndSet.started -= instance.OnSelectAndSet;
            @SelectAndSet.performed -= instance.OnSelectAndSet;
            @SelectAndSet.canceled -= instance.OnSelectAndSet;
            @FlatRotate.started -= instance.OnFlatRotate;
            @FlatRotate.performed -= instance.OnFlatRotate;
            @FlatRotate.canceled -= instance.OnFlatRotate;
            @Deselect.started -= instance.OnDeselect;
            @Deselect.performed -= instance.OnDeselect;
            @Deselect.canceled -= instance.OnDeselect;
        }

        /// <summary>
        /// Unregisters <param cref="instance" /> and unregisters all input action callbacks via <see cref="MainActions.UnregisterCallbacks(IMainActions)" />.
        /// </summary>
        /// <seealso cref="MainActions.UnregisterCallbacks(IMainActions)" />
        public void RemoveCallbacks(IMainActions instance)
        {
            if (m_Wrapper.m_MainActionsCallbackInterfaces.Remove(instance))
                UnregisterCallbacks(instance);
        }

        /// <summary>
        /// Replaces all existing callback instances and previously registered input action callbacks associated with them with callbacks provided via <param cref="instance" />.
        /// </summary>
        /// <remarks>
        /// If <paramref name="instance" /> is <c>null</c>, calling this method will only unregister all existing callbacks but not register any new callbacks.
        /// </remarks>
        /// <seealso cref="MainActions.AddCallbacks(IMainActions)" />
        /// <seealso cref="MainActions.RemoveCallbacks(IMainActions)" />
        /// <seealso cref="MainActions.UnregisterCallbacks(IMainActions)" />
        public void SetCallbacks(IMainActions instance)
        {
            foreach (var item in m_Wrapper.m_MainActionsCallbackInterfaces)
                UnregisterCallbacks(item);
            m_Wrapper.m_MainActionsCallbackInterfaces.Clear();
            AddCallbacks(instance);
        }
    }
    /// <summary>
    /// Provides a new <see cref="MainActions" /> instance referencing this action map.
    /// </summary>
    public MainActions @Main => new MainActions(this);
    /// <summary>
    /// Interface to implement callback methods for all input action callbacks associated with input actions defined by "Main" which allows adding and removing callbacks.
    /// </summary>
    /// <seealso cref="MainActions.AddCallbacks(IMainActions)" />
    /// <seealso cref="MainActions.RemoveCallbacks(IMainActions)" />
    public interface IMainActions
    {
        /// <summary>
        /// Method invoked when associated input action "LeftToggleMenu" is either <see cref="UnityEngine.InputSystem.InputAction.started" />, <see cref="UnityEngine.InputSystem.InputAction.performed" /> or <see cref="UnityEngine.InputSystem.InputAction.canceled" />.
        /// </summary>
        /// <seealso cref="UnityEngine.InputSystem.InputAction.started" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.performed" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.canceled" />
        void OnLeftToggleMenu(InputAction.CallbackContext context);
        /// <summary>
        /// Method invoked when associated input action "leftDelete" is either <see cref="UnityEngine.InputSystem.InputAction.started" />, <see cref="UnityEngine.InputSystem.InputAction.performed" /> or <see cref="UnityEngine.InputSystem.InputAction.canceled" />.
        /// </summary>
        /// <seealso cref="UnityEngine.InputSystem.InputAction.started" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.performed" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.canceled" />
        void OnLeftDelete(InputAction.CallbackContext context);
        /// <summary>
        /// Method invoked when associated input action "Rotate" is either <see cref="UnityEngine.InputSystem.InputAction.started" />, <see cref="UnityEngine.InputSystem.InputAction.performed" /> or <see cref="UnityEngine.InputSystem.InputAction.canceled" />.
        /// </summary>
        /// <seealso cref="UnityEngine.InputSystem.InputAction.started" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.performed" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.canceled" />
        void OnRotate(InputAction.CallbackContext context);
        /// <summary>
        /// Method invoked when associated input action "SelectAndSet" is either <see cref="UnityEngine.InputSystem.InputAction.started" />, <see cref="UnityEngine.InputSystem.InputAction.performed" /> or <see cref="UnityEngine.InputSystem.InputAction.canceled" />.
        /// </summary>
        /// <seealso cref="UnityEngine.InputSystem.InputAction.started" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.performed" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.canceled" />
        void OnSelectAndSet(InputAction.CallbackContext context);
        /// <summary>
        /// Method invoked when associated input action "FlatRotate" is either <see cref="UnityEngine.InputSystem.InputAction.started" />, <see cref="UnityEngine.InputSystem.InputAction.performed" /> or <see cref="UnityEngine.InputSystem.InputAction.canceled" />.
        /// </summary>
        /// <seealso cref="UnityEngine.InputSystem.InputAction.started" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.performed" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.canceled" />
        void OnFlatRotate(InputAction.CallbackContext context);
        /// <summary>
        /// Method invoked when associated input action "Deselect" is either <see cref="UnityEngine.InputSystem.InputAction.started" />, <see cref="UnityEngine.InputSystem.InputAction.performed" /> or <see cref="UnityEngine.InputSystem.InputAction.canceled" />.
        /// </summary>
        /// <seealso cref="UnityEngine.InputSystem.InputAction.started" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.performed" />
        /// <seealso cref="UnityEngine.InputSystem.InputAction.canceled" />
        void OnDeselect(InputAction.CallbackContext context);
    }
}
