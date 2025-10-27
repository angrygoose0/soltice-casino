using UnityEngine;
#if MM_POSTPROCESSING
using UnityEngine.Rendering.PostProcessing;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Vignette Color")]
	public class MMSpringVignetteColor : MMSpringColorComponent<PostProcessVolume>
	{
		protected Vignette _vignette;
		
		protected override void Initialization()
		{
			if (Target == null)
			{
				Target = this.gameObject.GetComponent<PostProcessVolume>();
			}
			Target.profile.TryGetSettings(out _vignette);
			base.Initialization();
		}
		
	public override Color TargetColor
	{
		get => _vignette != null ? _vignette.color : Color.white;
		set { if (_vignette != null) _vignette.color.Override(value); }
	}
	}
}
#endif