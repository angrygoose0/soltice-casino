#if MM_UI
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring CanvasGroup Alpha")]
	public class MMSpringCanvasGroupAlpha : MMSpringFloatComponent<CanvasGroup>
	{
		public override float TargetFloat
		{
			get => Target.alpha;
			set => Target.alpha = value;
		}
	}
}
#endif

