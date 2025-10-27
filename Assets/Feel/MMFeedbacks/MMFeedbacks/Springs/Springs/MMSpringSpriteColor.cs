using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	[AddComponentMenu("More Mountains/Springs/MM Spring Sprite Color")]
	public class MMSpringSpriteColor : MMSpringColorComponent<SpriteRenderer>
	{
		public override Color TargetColor
		{
			get => Target != null ? Target.color : Color.white;
			set { if (Target != null) Target.color = value; }
		}
	}
}
