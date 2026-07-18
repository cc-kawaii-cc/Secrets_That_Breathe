using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HoldQTE : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Panel/Root ของ QTE — โชว์ตอนเริ่ม ซ่อนตอนจบ")]
    public GameObject root;
    [Tooltip("Image แบบ Filled สำหรับหลอดความคืบหน้า")]
    public Image progressBar;

    [Header("Input (New Input System)")]
    public InputActionReference holdAction;

    [Header("Settings")]
    public float holdDuration = 2f;
    [Tooltip("ปล่อยปุ่มแล้วรีเซ็ตหลอด (true = ต้องกดค้างต่อเนื่อง)")]
    public bool resetOnRelease = true;

    private bool _active;
    private float _timer;
    private Action _onComplete;

    private void Awake()
    {
        if (root) root.SetActive(false);
    }

    private void OnEnable()  => holdAction?.action.Enable();
    private void OnDisable() => holdAction?.action.Disable();

    public void Begin(Action onComplete = null)
    {
        _onComplete = onComplete;
        _timer = 0f;
        _active = true;
        if (root) root.SetActive(true);
        if (progressBar) progressBar.fillAmount = 0f;
    }

    private void Update()
    {
        if (!_active || holdAction == null) return;

        if (holdAction.action.IsPressed())
            _timer += Time.deltaTime;
        else if (resetOnRelease)
            _timer = 0f;

        if (progressBar) progressBar.fillAmount = Mathf.Clamp01(_timer / holdDuration);

        if (_timer >= holdDuration) Complete();
    }

    private void Complete()
    {
        _active = false;
        if (root) root.SetActive(false);
        Action cb = _onComplete;
        _onComplete = null;
        cb?.Invoke();
    }
}