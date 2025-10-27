using MoreMountains.Tools;
using UnityEngine;
#if MM_UI
using UnityEngine.UI;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Image Color")]
	public class MMSpringImageColor : MMSpringColorComponent<Image>
	{
		public override Color TargetColor
		{
			get => Target != null ? Target.color : Color.white;
			set { if (Target != null) Target.color = value; }
		}
	}
}
#endif