using Beat115Icon.Services;
using HMUI;
using TMPro;
using UnityEngine;

namespace Beat115Icon.Patches;

internal class Beat115VanillaIconComponent : MonoBehaviour
{
	public ImageView IconImage;

	public GameObject CanvasObject;

	public TextMeshPro NumberText;

	public SpriteRenderer UnderlineRenderer;

	public AnimationCurve FadeCurve;

	public FlyingScoreEffect EffectInstance;

	public bool IsTargetScore;

	public bool HasPickedSpriteForThisCut;

	public IconAsset CurrentAsset;

	public int CurrentFrameIndex;

	public float FrameTimer;

	public int LastAnimatedFrame = -1;

	public bool HasAppliedCustomDuration;

	public void ResetState()
	{
		IsTargetScore = false;
		HasPickedSpriteForThisCut = false;
		CurrentAsset = null;
		CurrentFrameIndex = 0;
		FrameTimer = 0f;
		LastAnimatedFrame = -1;
		HasAppliedCustomDuration = false;
		if ((Object)(object)IconImage != null)
		{
			((Component)(object)IconImage).gameObject.SetActive(value: false);
		}
		if (NumberText != null)
		{
			NumberText.enabled = true;
			if (NumberText.renderer != null)
			{
				NumberText.renderer.enabled = true;
			}
		}
	}
}
