using UnityEngine;
using UnityEngine.InputSystem;

namespace StarterAssets
{
	/// <summary>
	/// First-person flashlight that is mounted to the active MainCamera.
	/// It creates its light rig at runtime, so no separate light GameObject is needed.
	/// </summary>
	[DisallowMultipleComponent]
	public class FirstPersonFlashlight : MonoBehaviour
	{
		[Header("Input")]
		[Tooltip("Enable the keyboard shortcut for toggling the flashlight.")]
		public bool enableKeyboardToggle = true;
		[Tooltip("Keyboard key used to toggle the flashlight.")]
		public Key toggleKey = Key.F;
		[Tooltip("Whether the flashlight begins enabled when the scene starts.")]
		public bool startOn = false;

		[Header("Mount Position")]
		[Tooltip("Horizontal offset from the centre of the camera. Negative = left, positive = right.")]
		public float horizontalOffset = 0.18f;
		[Tooltip("Height of the flashlight when it is held up. Negative = lower on the screen.")]
		public float raisedHeight = -0.12f;
		[Tooltip("Height of the flashlight when lowered. Negative = further below the screen.")]
		public float loweredHeight = -0.62f;
		[Tooltip("Distance in front of the camera. Increase this if the beam clips into nearby walls.")]
		public float forwardOffset = 0.35f;
		[Range(-89f, 89f)]
		[Tooltip("Downward angle while the flashlight is lowered. This makes the opening beam start on the floor.")]
		public float loweredPitch = 35f;
		[Range(-89f, 89f)]
		[Tooltip("Vertical angle while the flashlight is held up.")]
		public float raisedPitch = 0f;
		[Tooltip("Local rotation offset for the flashlight beam.")]
		public Vector3 localRotationOffset = Vector3.zero;

		[Header("Raise / Lower Animation")]
		[Tooltip("When enabled, the beam raises from below the screen when turned on and lowers when turned off.")]
		public bool enableRaiseAnimation = true;
		[Min(0.01f)]
		public float raiseDuration = 0.22f;
		[Tooltip("Makes the raise/lower movement feel less mechanical.")]
		public AnimationCurve raiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

		[Header("Main Beam")]
		[Min(0.1f)]
		public float range = 18f;
		[Min(0f)]
		public float intensity = 9f;
		[Range(1f, 179f)]
		public float spotAngle = 36f;
		[Range(0f, 1f)]
		[Tooltip("0 = sharp, even beam. 1 = a softer falloff from the centre to the edge.")]
		public float beamSoftness = 0.55f;
		public Color beamColor = Color.white;
		[Tooltip("Use Kelvin colour temperature in addition to Beam Color.")]
		public bool useColorTemperature = true;
		[Range(1000f, 20000f)]
		public float colorTemperature = 5200f;
		[Tooltip("Optional cookie texture for a more realistic beam shape. Import it as a regular Texture.")]
		public Texture mainBeamCookie;
		[Tooltip("Uses soft real-time shadows for the central beam. This costs more performance.")]
		public bool useSoftShadows = true;

		[Header("Dual Beam / Two Rings")]
		[Tooltip("Adds a wide, dim outer spotlight around the bright central beam.")]
		public bool useDualBeam = true;
		[Range(1f, 3f)]
		public float outerWidthMultiplier = 1.65f;
		[Range(0f, 1f)]
		public float outerIntensityMultiplier = 0.24f;
		[Range(0.1f, 2f)]
		public float outerRangeMultiplier = 1.1f;
		public Color outerBeamColor = new Color(1f, 0.94f, 0.78f);
		[Tooltip("Optional cookie texture for the wide outer beam.")]
		public Texture outerBeamCookie;

		[Header("Sound Effects")]
		[Tooltip("Master switch for the on/off sound effects.")]
		public bool enableSoundEffects = true;
		[Tooltip("Optional AudioSource. Leave empty to create one automatically on MainCamera.")]
		public AudioSource audioSource;
		public AudioClip switchOnClip;
		public AudioClip switchOffClip;
		[Range(0f, 1f)]
		public float soundVolume = 0.7f;

		private Transform _rig;
		private Light _mainBeam;
		private Light _outerBeam;
		private bool _isOn;
		private float _raiseProgress;

		private void Awake()
		{
			EnsureRig();
			_isOn = startOn;
			_raiseProgress = startOn ? 1f : 0f;
			ApplyLightSettings();
			UpdateRigPose();
			SetLightEnabled(startOn);
		}

		private void Update()
		{
			if (enableKeyboardToggle && Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
				Toggle();

			float target = _isOn ? 1f : 0f;
			if (enableRaiseAnimation)
			{
				float speed = 1f / Mathf.Max(0.01f, raiseDuration);
				_raiseProgress = Mathf.MoveTowards(_raiseProgress, target, speed * Time.deltaTime);
			}
			else
			{
				_raiseProgress = target;
			}
			UpdateRigPose();

			if (!_isOn && _raiseProgress <= 0f)
				SetLightEnabled(false);
		}

		private void OnValidate()
		{
			range = Mathf.Max(0.1f, range);
			intensity = Mathf.Max(0f, intensity);
			raiseDuration = Mathf.Max(0.01f, raiseDuration);
			if (Application.isPlaying)
			{
				EnsureRig();
				ApplyLightSettings();
				UpdateRigPose();
			}
		}

		/// <summary>Can also be called from a UI Button or UnityEvent.</summary>
		public void Toggle()
		{
			SetFlashlight(!_isOn);
		}

		/// <summary>Can also be called from a UI Button or UnityEvent.</summary>
		public void SetFlashlight(bool enabled)
		{
			if (_isOn == enabled) return;
			_isOn = enabled;
			if (enabled) SetLightEnabled(true);
			PlayToggleSound(enabled);

			if (!enableRaiseAnimation)
			{
				_raiseProgress = enabled ? 1f : 0f;
				UpdateRigPose();
				SetLightEnabled(enabled);
			}
		}

		public void ApplyLightSettings()
		{
			EnsureRig();
			ConfigureBeam(_mainBeam, range, intensity, spotAngle, beamColor, mainBeamCookie, useSoftShadows);

			float outerAngle = Mathf.Min(179f, spotAngle * outerWidthMultiplier);
			ConfigureBeam(
				_outerBeam,
				range * outerRangeMultiplier,
				intensity * outerIntensityMultiplier,
				outerAngle,
				outerBeamColor,
				outerBeamCookie,
				false);
			_outerBeam.enabled = _isOn && useDualBeam;
		}

		private void EnsureRig()
		{
			if (_rig == null)
			{
				Transform existing = transform.Find("FirstPersonFlashlightRig");
				if (existing != null) _rig = existing;
				else
				{
					GameObject rigObject = new GameObject("FirstPersonFlashlightRig");
					_rig = rigObject.transform;
					_rig.SetParent(transform, false);
				}
			}

			if (_mainBeam == null) _mainBeam = CreateBeam("Core Beam");
			if (_outerBeam == null) _outerBeam = CreateBeam("Outer Beam");

			if (audioSource == null)
			{
				audioSource = GetComponent<AudioSource>();
				if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
				audioSource.spatialBlend = 0f;
				audioSource.playOnAwake = false;
			}
		}

		private Light CreateBeam(string beamName)
		{
			GameObject beamObject = new GameObject(beamName);
			beamObject.transform.SetParent(_rig, false);
			Light beam = beamObject.AddComponent<Light>();
			beam.type = LightType.Spot;
			beam.renderMode = LightRenderMode.ForcePixel;
			return beam;
		}

		private void ConfigureBeam(Light beam, float beamRange, float beamIntensity, float beamAngle,
			Color beamColour, Texture cookie, bool castSoftShadows)
		{
			if (beam == null) return;
			beam.type = LightType.Spot;
			beam.range = beamRange;
			beam.intensity = beamIntensity;
			beam.spotAngle = beamAngle;
			beam.innerSpotAngle = Mathf.Lerp(beamAngle, beamAngle * 0.12f, beamSoftness);
			beam.color = beamColour;
			beam.useColorTemperature = useColorTemperature;
			beam.colorTemperature = colorTemperature;
			beam.cookie = cookie;
			beam.shadows = castSoftShadows ? LightShadows.Soft : LightShadows.None;
		}

		private void UpdateRigPose()
		{
			if (_rig == null) return;
			float curvedProgress = enableRaiseAnimation && raiseCurve != null
				? raiseCurve.Evaluate(_raiseProgress)
				: _raiseProgress;
			float height = Mathf.Lerp(loweredHeight, raisedHeight, curvedProgress);
			_rig.localPosition = new Vector3(horizontalOffset, height, forwardOffset);
			float pitch = Mathf.Lerp(loweredPitch, raisedPitch, curvedProgress);
			_rig.localRotation = Quaternion.Euler(localRotationOffset + Vector3.right * pitch);
		}

		private void SetLightEnabled(bool enabled)
		{
			if (_mainBeam != null) _mainBeam.enabled = enabled;
			if (_outerBeam != null) _outerBeam.enabled = enabled && useDualBeam;
		}

		private void PlayToggleSound(bool enabled)
		{
			if (!enableSoundEffects || audioSource == null) return;
			AudioClip clip = enabled ? switchOnClip : switchOffClip;
			if (clip != null) audioSource.PlayOneShot(clip, soundVolume);
		}
	}
}
