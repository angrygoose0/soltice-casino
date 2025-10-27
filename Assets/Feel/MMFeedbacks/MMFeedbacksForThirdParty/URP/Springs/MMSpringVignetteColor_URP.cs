#if MM_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Vignette Color URP")]
	public class MMSpringVignetteColor_URP : MMSpringColorComponent<Volume>
	{
		protected Vignette _vignette;
		
		protected override void Initialization()
		{
			if (Target == null)
			{
				Target = this.gameObject.GetComponent<Volume>();
			}
			Target.profile.TryGet(out _vignette);
			base.Initialization();
		}
		
	public override Color TargetColor
	{
		get => _vignette != null ? _vignette.color.value : Color.white;
		set { if (_vignette != null) _vignette.color.Override(value); }
	}
	}
}
#endif